#!/usr/bin/env python3
"""Sealed authority-discovery policy (Plan 2 Task 3).

Resolves the authority-discovery policy - default document shipped with the
skill, or a base-revision override read via ``git show <base>:<override>`` so
the reviewed head can never qualify itself - then enumerates authority seeds
by traversing marker/front-matter/structural edges to a cycle-safe fixed
point. Ambiguous, inaccessible, or unsupported targets are returned as
failure records; callers must treat a non-empty failures list as a block.
"""

from __future__ import annotations

import fnmatch
import posixpath
import re
from dataclasses import dataclass
from pathlib import Path

from . import model

AUTHORITY_EDGE_KINDS = (
    "governs",
    "implements",
    "depends-on",
    "supersedes",
    "references-as-authority",
)

_POLICY_DOC = Path(__file__).resolve().parent.parent.parent / "references" / "authority-discovery-policy.v1.json"

_MARKER_RE = re.compile(r"<!--\s*authority:edge\s+(\S+)\s+(\S+)\s*-->", re.IGNORECASE)
_SPEC_HEADER_RE = re.compile(r"^\*\*Spec:\*\*\s+(\S+)", re.MULTILINE)
_GH_ISSUE_RE = re.compile(r"^[Gg][Hh]\s*#(\d+)$")
_GLOB_CHARS = re.compile(r"[*?\[\]]")
_CONTROL_CHARS = re.compile(r"[\x00-\x1f\x7f]")


class DiscoveryPolicyError(Exception):
    """Raised when policy resolution or validation fails closed."""


@dataclass(frozen=True)
class AuthoritySeed:
    kind: str
    locator: str
    declared_by: str


@dataclass(frozen=True)
class AuthorityDiscoveryPolicy:
    policy_id: str
    version: str
    document_sha256: str
    document: dict


def _validate_document(doc: object, *, source: str) -> dict:
    if not isinstance(doc, dict):
        raise DiscoveryPolicyError(f"{source}: policy document is not an object")
    if doc.get("schema_version") != 1:
        raise DiscoveryPolicyError(f"{source}: unsupported schema_version")
    if doc.get("policy_id") != "authority-discovery":
        raise DiscoveryPolicyError(f"{source}: wrong policy_id")
    for key in ("repo_law_roots", "pr_roots", "edge_kinds", "edge_grammars", "structural_edges"):
        if not isinstance(doc.get(key), list):
            raise DiscoveryPolicyError(f"{source}: {key} must be a list")
    if not isinstance(doc.get("override_path"), str):
        raise DiscoveryPolicyError(f"{source}: override_path must be a string")
    for key in ("repo_law_roots", "pr_roots", "edge_kinds"):
        if not all(isinstance(v, str) for v in doc[key]):
            raise DiscoveryPolicyError(f"{source}: {key} entries must be strings")
    unknown = set(doc.get("edge_kinds", [])) - set(AUTHORITY_EDGE_KINDS)
    if unknown:
        raise DiscoveryPolicyError(f"{source}: unknown edge kinds {sorted(unknown)}")
    # An unrecognized pr_root would silently narrow the enumerated authority
    # set - a recall failure - so refuse it at resolution instead.
    unknown_roots = set(doc["pr_roots"]) - {"description", "linked_issues"}
    if unknown_roots:
        raise DiscoveryPolicyError(f"{source}: unknown pr_roots {sorted(unknown_roots)}")
    for i, rule in enumerate(doc["structural_edges"]):
        if (
            not isinstance(rule, dict)
            or not isinstance(rule.get("from"), str)
            or not isinstance(rule.get("to"), list)
            or not all(isinstance(g, str) for g in rule["to"])
            or not isinstance(rule.get("edge"), str)
            or rule["edge"] not in doc["edge_kinds"]
        ):
            raise DiscoveryPolicyError(f"{source}: structural_edges[{i}] malformed")
    return doc


def default_policy() -> AuthorityDiscoveryPolicy:
    doc = _validate_document(
        model.strict_json_loads(_POLICY_DOC.read_bytes(), source=_POLICY_DOC.name),
        source=_POLICY_DOC.name,
    )
    return AuthorityDiscoveryPolicy(
        policy_id=doc["policy_id"],
        version=str(doc["version"]),
        document_sha256=model.sha256_hex(model.canonical_json(doc)),
        document=doc,
    )


def resolve_policy(*, run_git, base_sha: str) -> AuthorityDiscoveryPolicy:
    """Read the override at ``<base_sha>:<override_path>`` only; the working
    tree and reviewed head are never consulted."""
    default = default_policy()
    override_path = default.document["override_path"]
    rc, out, _err = run_git(["show", f"{base_sha}:{override_path}"])
    if rc != 0:
        return default
    try:
        doc = model.strict_json_loads(out.encode("utf-8"), source=override_path)
        doc = _validate_document(doc, source=override_path)
    except DiscoveryPolicyError:
        raise
    except Exception as exc:
        raise DiscoveryPolicyError(f"{override_path}: malformed JSON: {exc}") from exc
    return AuthorityDiscoveryPolicy(
        policy_id=doc["policy_id"],
        version=str(doc["version"]),
        document_sha256=model.sha256_hex(model.canonical_json(doc)),
        document=doc,
    )


def _norm_repo_path(path: str) -> str:
    p = path.replace("\\", "/")
    p = posixpath.normpath(p)
    while p.startswith("./"):
        p = p[2:]
    return p


def _checked_repo_path(path: str) -> str:
    """Normalize a reviewed-repo-sourced path, refusing control characters and
    any form that escapes the repository root after normalization."""
    if _CONTROL_CHARS.search(path):
        raise DiscoveryPolicyError(f"invalid-locator: path contains control characters: {path!r}")
    p = _norm_repo_path(path)
    if p == ".." or p.startswith("../") or posixpath.isabs(p) or re.match(r"^[A-Za-z]:", p):
        raise DiscoveryPolicyError(f"invalid-locator: path escapes the repository root: {path!r}")
    return p


def canonicalize_locator(raw: str) -> str:
    raw = raw.strip()
    if _CONTROL_CHARS.search(raw):
        raise DiscoveryPolicyError(f"invalid-locator: control characters in {raw!r}")
    m = _GH_ISSUE_RE.match(raw)
    if m:
        return f"gh:issue/{m.group(1)}"
    for prefix in ("repo:", "gh:"):
        if raw.startswith(prefix):
            if prefix == "repo:":
                return "repo:" + _checked_repo_path(raw[5:])
            head, _, rest = raw[3:].partition("/")
            if head == "doc":
                return "gh:doc/" + _checked_repo_path(rest)
            if head == "issue":
                if not rest.isdigit():
                    raise DiscoveryPolicyError(f"invalid-locator: gh:issue requires digits: {raw!r}")
                return f"gh:issue/{rest}"
            if head == "pr":
                if not re.fullmatch(r"\d+#body", rest):
                    raise DiscoveryPolicyError(f"invalid-locator: gh:pr requires <n>#body: {raw!r}")
                return raw
            return raw
    if raw.startswith("/") or re.match(r"^[A-Za-z]:", raw):
        return raw
    return "repo:" + _checked_repo_path(raw)


def _classify(locator: str, *, doc: dict) -> str:
    if locator.startswith("gh:issue/"):
        return "issue"
    if locator.startswith("gh:pr/") and locator.endswith("#body"):
        return "pr-description"
    if locator.startswith("gh:"):
        return "document"
    path = locator[5:] if locator.startswith("repo:") else locator
    if any(fnmatch.fnmatch(path, g) for g in doc["repo_law_roots"]):
        return "repo-law"
    low = path.lower()
    name = low.rsplit("/", 1)[-1]
    if "non-goal" in name or "/non-goals/" in low:
        return "non-goal"
    if "/plans/" in low or low.startswith("plans/"):
        return "plan"
    if "/specs/" in low or low.startswith("specs/"):
        return "spec"
    return "document"


def _front_matter_edges(text: str) -> list[tuple[str, str]]:
    if not text.startswith("---"):
        return []
    end = text.find("\n---", 3)
    if end == -1:
        return []
    body = text[3:end].splitlines()
    edges: list[tuple[str, str]] = []
    in_list = False
    pending_kind = None
    for line in body:
        stripped = line.strip()
        if not in_list:
            if stripped.startswith("authority_edges:"):
                in_list = True
            continue
        if stripped.startswith("- "):
            item = stripped[2:].strip()
            if item.startswith("kind:"):
                pending_kind = item[5:].strip().strip("\"'")
            elif ":" in item:
                kv = dict(p.split(":", 1) for p in item.strip("{} ").split(",") if ":" in p)
                if "kind" in kv and "target" in kv:
                    edges.append((kv["kind"].strip().strip("\"'"), kv["target"].strip().strip("\"'")))
                pending_kind = None
            else:
                parts = item.split(None, 1)
                if len(parts) == 2:
                    edges.append((parts[0], parts[1]))
                pending_kind = None
        elif stripped.startswith("target:") and pending_kind:
            edges.append((pending_kind, stripped[7:].strip().strip("\"'")))
            pending_kind = None
        elif stripped and not stripped.startswith("#"):
            in_list = False
            pending_kind = None
    return edges


def _extract_edges(text: str, *, doc: dict) -> list[tuple[str, str]]:
    edges = [(m.group(1), m.group(2)) for m in _MARKER_RE.finditer(text)]
    edges.extend(_front_matter_edges(text))
    for m in _SPEC_HEADER_RE.finditer(text):
        edges.append(("implements", m.group(1)))
    return edges


def _seed_text(seed: AuthoritySeed, *, run_git, base_sha: str, load_text, show_cache: dict) -> str | None:
    if seed.locator.startswith("repo:"):
        path = seed.locator[5:]
        if path not in show_cache:
            rc, out, _ = run_git(["show", f"{base_sha}:{path}"])
            show_cache[path] = out if rc == 0 else None
        return show_cache[path]
    raw = load_text(seed.locator)
    if raw is None:
        return None
    return raw.decode("utf-8") if isinstance(raw, bytes) else str(raw)


def enumerate_authorities(
    *,
    policy: AuthorityDiscoveryPolicy,
    run_git,
    base_sha: str,
    pr_metadata: dict,
    load_text,
) -> tuple[list[AuthoritySeed], list[dict]]:
    doc = policy.document
    rc, out, err = run_git(["ls-tree", "-r", "--name-only", base_sha])
    if rc != 0:
        raise DiscoveryPolicyError(f"git ls-tree failed: {err.strip()}")
    files = {line.strip() for line in out.splitlines() if line.strip()}

    seeds: list[AuthoritySeed] = []
    seen: set[tuple[str, str]] = set()
    failures: list[dict] = []
    show_cache: dict[str, str | None] = {}

    def add(kind, locator, declared_by):
        key = (kind, locator)
        if key in seen:
            return None
        seen.add(key)
        seed = AuthoritySeed(kind=kind, locator=locator, declared_by=declared_by)
        seeds.append(seed)
        return seed

    for pattern in doc["repo_law_roots"]:
        for path in sorted(files):
            if fnmatch.fnmatch(path, pattern):
                add("repo-law", "repo:" + _norm_repo_path(path), f"policy:{pattern}")
    if "description" in doc["pr_roots"]:
        number = pr_metadata.get("number", 0)
        add("pr-description", f"gh:pr/{number}#body", "policy:pr_roots")
    if "linked_issues" in doc["pr_roots"]:
        for n in pr_metadata.get("linked_issues", []):
            add("issue", f"gh:issue/{n}", "policy:pr_roots")

    idx = 0
    while idx < len(seeds):
        seed = seeds[idx]
        idx += 1

        edges: list[tuple[str, str]] = []
        if seed.locator.startswith("repo:"):
            path = seed.locator[5:]
            for rule in doc["structural_edges"]:
                if fnmatch.fnmatch(path, rule["from"]):
                    for glob in rule["to"]:
                        for match in sorted(files):
                            if fnmatch.fnmatch(match, glob):
                                edges.append((rule["edge"], "repo:" + match))
        text = _seed_text(seed, run_git=run_git, base_sha=base_sha, load_text=load_text, show_cache=show_cache)
        if text is None:
            if not seed.declared_by.startswith("policy:"):
                failures.append({"locator": seed.locator, "reason": "inaccessible"})
            continue
        edges.extend(_extract_edges(text, doc=doc))

        for kind, target in edges:
            if kind not in doc["edge_kinds"]:
                failures.append(
                    {
                        "locator": f"{seed.locator} -> {target}",
                        "reason": f"unsupported-edge-kind {kind}",
                    }
                )
                continue
            try:
                locator = canonicalize_locator(target)
            except DiscoveryPolicyError as exc:
                failures.append(
                    {
                        "locator": f"{seed.locator} -> {target}",
                        "reason": str(exc),
                    }
                )
                continue
            if locator.startswith("repo:"):
                tpath = locator[5:]
                if _GLOB_CHARS.search(tpath):
                    matches = [f for f in sorted(files) if fnmatch.fnmatch(f, tpath)]
                    if len(matches) != 1:
                        failures.append(
                            {
                                "locator": f"{seed.locator} -> {target}",
                                "reason": ("ambiguous" if matches else "inaccessible"),
                            }
                        )
                        continue
                    locator = "repo:" + matches[0]
                elif tpath not in files:
                    failures.append(
                        {
                            "locator": f"{seed.locator} -> {target}",
                            "reason": "inaccessible",
                        }
                    )
                    continue
            elif locator.startswith("gh:"):
                pass
            else:
                failures.append(
                    {
                        "locator": f"{seed.locator} -> {target}",
                        "reason": "unsupported-locator",
                    }
                )
                continue
            add(_classify(locator, doc=doc), locator, seed.locator)

    return seeds, failures
