#!/usr/bin/env python3
"""Live acquisition adapter for freeze/refresh review input (Plan 2 Task 4).

``enumerate_acquisition`` runs the witnessed enumeration under an injected
``run_git``/``run_gh`` pair and materializes an acquisition directory:
``data.json`` (candidate snapshot sans fingerprint, manifest payload,
authority records, findings, drift_reasons), ``evidence/`` byte blobs plus a
``manifest.json`` alias map, and ``enumeration.json`` carrying the
enumeration-id marker printed to stdout for transcript binding.

``LiveAuthorityDiscovery`` is the ``AuthorityDiscoverySource`` that loads a
produced acquisition directory at complete-time, binds the transcript
segment witnessing the enumerate exec, and returns the
``TrustedActionPayload`` envelope the engine installs.
"""

from __future__ import annotations

import base64
import json
import os
import shutil
import stat
import types
import urllib.parse
from pathlib import Path

from . import discovery_policy, engine, feedback_policy, model, witness_log

REQUIRED_AUTHORITY_KINDS = ("repo-law", "pr-description")
_PR_JSON_FIELDS = (
    "number,url,title,body,isDraft,state,baseRefOid,headRefOid,baseRefName,"
    "closingIssuesReferences,labels,assignees,milestone,author"
)

_DRIFT_NAME_MAP = {
    "authority_manifest_sha256": "authority_manifest",
    "feedback_history_sha256": "feedback_history",
    "unresolved_feedback_sha256": "unresolved_feedback",
}


class AcquisitionError(Exception):
    def __init__(self, blocker_class: str, detail: str):
        super().__init__(f"{blocker_class}: {detail}")
        self.blocker_class = blocker_class
        self.detail = detail


def _tool_guard(name: str, runner):
    def wrapped(argv):
        try:
            return runner(argv)
        except OSError as exc:
            raise AcquisitionError("tool-blocked", f"{name} invocation failed: {exc}") from exc

    return wrapped


def _need(args, rc_out_err, what):
    rc, out, err = rc_out_err
    if rc != 0:
        raise AcquisitionError("tool-blocked", f"{what} failed: {err.strip() or out.strip()}")
    return out


def _parse_repo_id(pr_url: str) -> str:
    owner, repo, _n = feedback_policy.parse_pr_url(pr_url)
    return f"{owner}/{repo}"


def _required_check_digest(run_gh, repo_id: str, base_ref: str) -> str:
    rc, out, _err = run_gh(["api", f"repos/{repo_id}/branches/{base_ref}/protection"])
    if rc != 0:
        return model.sha256_json({"contexts": [], "enforced": False})
    try:
        doc = json.loads(out)
        contexts = sorted((doc.get("required_status_checks") or {}).get("contexts") or [])
    except Exception:
        raise AcquisitionError("tool-blocked", "branch protection response malformed")
    return model.sha256_json({"contexts": contexts, "enforced": True})


def _pr_metadata_projection(pr: dict) -> dict:
    issues = pr.get("closingIssuesReferences") or []
    if isinstance(issues, dict):
        issues = issues.get("nodes") or []
    numbers = sorted(i["number"] for i in issues if isinstance(i, dict) and "number" in i)
    labels = sorted(lbl.get("name", "") for lbl in (pr.get("labels") or []) if isinstance(lbl, dict))
    assignees = sorted(a.get("login", "") for a in (pr.get("assignees") or []) if isinstance(a, dict))
    milestone = pr.get("milestone")
    return {
        "title": pr.get("title") or "",
        "body": pr.get("body") or "",
        "labels": labels,
        "assignees": assignees,
        "milestone": milestone.get("title") if isinstance(milestone, dict) else None,
        "linked_issues": numbers,
    }


def _load_seed_bytes(seed, *, run_git, run_gh, base_sha: str, repo_id: str, pr_meta: dict) -> bytes:
    loc = seed.locator
    if loc.startswith("repo:"):
        rc, out, err = run_git(["show", f"{base_sha}:{loc[5:]}"])
        if rc != 0:
            raise AcquisitionError("authority-missing", f"{loc}: {err.strip() or 'unreadable'}")
        return out.encode("utf-8", errors="surrogateescape")
    if loc.startswith("gh:pr/") and loc.endswith("#body"):
        if loc != f"gh:pr/{pr_meta.get('number')}#body":
            raise AcquisitionError(
                "authority-missing",
                f"{loc}: locator does not match the enumerated PR",
            )
        return (pr_meta.get("body") or "").encode("utf-8", errors="surrogateescape")
    if loc.startswith("gh:issue/"):
        n = loc.rsplit("/", 1)[-1]
        rc, out, err = run_gh(["api", f"repos/{repo_id}/issues/{n}"])
        if rc != 0:
            raise AcquisitionError("authority-missing", f"{loc}: {err.strip() or 'unreadable'}")
        try:
            doc = json.loads(out)
        except ValueError as exc:
            raise AcquisitionError("authority-missing", f"{loc}: unexpected API response shape") from exc
        if not isinstance(doc, dict):
            raise AcquisitionError("authority-missing", f"{loc}: unexpected API response shape")
        return model.canonical_json({"title": doc.get("title"), "body": doc.get("body"), "number": doc.get("number")})
    if loc.startswith("gh:doc/"):
        path = urllib.parse.quote(loc[7:], safe="/")
        rc, out, err = run_gh(["api", f"repos/{repo_id}/contents/{path}?ref={base_sha}"])
        if rc != 0:
            raise AcquisitionError("authority-missing", f"{loc}: {err.strip() or 'unreadable'}")
        try:
            doc = json.loads(out)
        except ValueError as exc:
            raise AcquisitionError("authority-missing", f"{loc}: unexpected API response shape") from exc
        if not isinstance(doc, dict) or not isinstance(doc.get("content"), str):
            raise AcquisitionError("authority-missing", f"{loc}: unexpected API response shape")
        try:
            return base64.b64decode(doc["content"])
        except ValueError as exc:
            raise AcquisitionError("authority-missing", f"{loc}: undecodable content") from exc
    raise AcquisitionError("authority-missing", f"{loc}: unsupported locator")


def enumerate_acquisition(
    *,
    run_git,
    run_gh,
    repo_root: Path,
    pr_number: int,
    out_dir: Path,
    scratch_dir: Path,
    epoch: int = 1,
) -> dict:
    out_dir = Path(out_dir)
    scratch_dir = Path(scratch_dir)
    run_git = _tool_guard("git", run_git)
    run_gh = _tool_guard("gh", run_gh)

    # 1. gh connector
    try:
        rc, _o, err = run_gh(["auth", "status"])
    except OSError as exc:
        raise AcquisitionError("tool-blocked", f"gh not runnable: {exc}") from exc
    if rc != 0:
        raise AcquisitionError("tool-blocked", f"gh auth status: {err.strip() or 'unauthenticated'}")

    # 2. PR identity
    rc, out, err = run_gh(["repo", "view", "--json", "nameWithOwner"])
    if rc != 0:
        raise AcquisitionError("tool-blocked", f"gh repo view: {err.strip()}")
    try:
        repo_id = json.loads(out)["nameWithOwner"]
    except Exception as exc:
        raise AcquisitionError("tool-blocked", f"gh repo view malformed: {exc}") from exc
    rc, out, err = run_gh(["pr", "view", str(pr_number), "--json", _PR_JSON_FIELDS, "-R", repo_id])
    if rc != 0:
        raise AcquisitionError("tool-blocked", f"gh pr view: {err.strip()}")
    try:
        pr_meta = json.loads(out)
    except Exception as exc:
        raise AcquisitionError("tool-blocked", f"gh pr view malformed: {exc}") from exc
    if not isinstance(pr_meta, dict):
        raise AcquisitionError("tool-blocked", "gh pr view response is not an object")
    base_sha = pr_meta.get("baseRefOid")
    head_sha = pr_meta.get("headRefOid")
    if not base_sha or not head_sha or not pr_meta.get("url"):
        raise AcquisitionError("tool-blocked", "gh pr view response lacks required fields")
    rc, _o, err = run_gh(["api", f"repos/{repo_id}/commits/{head_sha}"])
    if rc != 0:
        raise AcquisitionError("snapshot-drift", f"head {head_sha} is not remote-reachable: {err.strip()}")

    # 3. base resolution + shallow check
    try:
        rc, out, err = run_git(["rev-parse", "--is-shallow-repository"])
    except OSError as exc:
        raise AcquisitionError("tool-blocked", f"git not runnable: {exc}") from exc
    if rc != 0 or out.strip() != "false":
        raise AcquisitionError("snapshot-drift", "repository is shallow or unreadable")
    rc, out, err = run_git(["merge-base", "--all", base_sha, head_sha])
    bases = [line.strip() for line in out.splitlines() if line.strip()]
    if rc != 0 or len(bases) != 1:
        raise AcquisitionError("snapshot-drift", f"expected exactly one merge base, got {bases}")
    merge_base = bases[0]

    # 4. worktree cleanliness + HEAD identity
    rc, out, err = run_git(["status", "--porcelain"])
    if rc != 0 or out.strip():
        raise AcquisitionError("snapshot-drift", f"worktree not clean: {out.strip()[:120]}")
    rc, out, err = run_git(["rev-parse", "HEAD"])
    if rc != 0 or out.strip() != head_sha:
        raise AcquisitionError("snapshot-drift", f"checked-out HEAD {out.strip() or '<error>'} != pr head {head_sha}")

    # 5. hashes and formats
    rc, out, _ = run_git(["rev-parse", "--show-object-format"])
    obj_format = out.strip()
    if rc != 0 or obj_format not in model.GIT_OBJECT_FORMATS:
        raise AcquisitionError("snapshot-drift", f"unsupported object format {obj_format!r}")
    rc, out, err = run_git(["rev-parse", f"{head_sha}^{{tree}}"])
    if rc != 0:
        raise AcquisitionError("snapshot-drift", f"tree resolution failed: {err.strip()}")
    tree_sha = out.strip()
    rc, out, err = run_git(["diff", merge_base, head_sha])
    if rc != 0:
        raise AcquisitionError("snapshot-drift", f"diff failed: {err.strip()}")
    diff_sha256 = model.sha256_hex(out.encode("utf-8", errors="surrogateescape"))
    pr_metadata_sha256 = model.sha256_json(_pr_metadata_projection(pr_meta))

    witness_pol = witness_log.TranscriptWitnessPolicy(
        transcript_root=scratch_dir / "transcripts",
        witness_root=scratch_dir / "witness",
    )

    # 6. authority discovery at base_sha
    gh_text_cache: dict[str, str | None] = {}

    def _gh_text(locator: str):
        if locator in gh_text_cache:
            return gh_text_cache[locator]
        try:
            raw = _load_seed_bytes(
                types.SimpleNamespace(locator=locator),
                run_git=run_git,
                run_gh=run_gh,
                base_sha=base_sha,
                repo_id=repo_id,
                pr_meta=pr_meta,
            )
            gh_text_cache[locator] = raw.decode("utf-8", errors="surrogateescape")
        except AcquisitionError as exc:
            if exc.blocker_class != "authority-missing":
                raise
            gh_text_cache[locator] = None
        except (ValueError, KeyError, TypeError, AttributeError):
            gh_text_cache[locator] = None
        return gh_text_cache[locator]

    try:
        disc = discovery_policy.resolve_policy(run_git=run_git, base_sha=base_sha)
        seeds, disc_failures = discovery_policy.enumerate_authorities(
            policy=disc,
            run_git=run_git,
            base_sha=base_sha,
            pr_metadata={
                "number": pr_number,
                "body": pr_meta.get("body") or "",
                "linked_issues": _pr_metadata_projection(pr_meta)["linked_issues"],
            },
            load_text=_gh_text,
        )
    except discovery_policy.DiscoveryPolicyError as exc:
        raise AcquisitionError("authority-missing", str(exc)) from exc
    if disc_failures:
        raise AcquisitionError("authority-missing", json.dumps(disc_failures, sort_keys=True))

    # 7. materialize every seed's bytes
    if out_dir.is_symlink():
        raise AcquisitionError("tool-blocked", f"refusing symlinked acquisition dir {out_dir}")
    if out_dir.exists():
        try:
            resolved = out_dir.resolve()
            scratch_resolved = scratch_dir.resolve()
        except (OSError, ValueError) as exc:
            raise AcquisitionError("tool-blocked", f"could not resolve acquisition dir {out_dir}: {exc}") from exc
        if not resolved.is_relative_to(scratch_resolved):
            raise AcquisitionError("tool-blocked", f"refusing to clear path outside scratch: {resolved}")
        if not resolved.is_dir():
            raise AcquisitionError("tampered-source", f"acquisition dir is not a directory: {resolved}")
        if resolved.name != "latest" or resolved.parent.name != "acquire":
            raise AcquisitionError("tool-blocked", f"refusing to clear unexpected path {resolved}")
        try:
            shutil.rmtree(resolved, onerror=_remove_readonly)
        except OSError as exc:
            raise AcquisitionError("tampered-source", f"could not clear acquisition dir {resolved}: {exc}") from exc
    fb_policy = feedback_policy.default_policy()
    builtins = engine.load_witness_sources()
    ev_dir = out_dir / "evidence"
    ev_dir.mkdir(parents=True, exist_ok=True)
    evidence_map: dict[str, dict] = {}
    authorities_records: list[dict] = []
    manifest_entries: list[dict] = []

    def write_evidence(alias: str, kind: str, raw: bytes) -> str:
        digest = model.sha256_hex(raw)
        fname = f"{alias}.bin"
        (ev_dir / fname).write_bytes(raw)
        evidence_map[alias] = {"file": fname, "kind": kind, "sha256": digest}
        return digest

    for i, seed in enumerate(seeds):
        aid = "authority:" + model.sha256_json({"kind": seed.kind, "locator": seed.locator})
        try:
            raw = _load_seed_bytes(
                seed, run_git=run_git, run_gh=run_gh, base_sha=base_sha, repo_id=repo_id, pr_meta=pr_meta
            )
        except AcquisitionError as exc:
            if seed.kind in REQUIRED_AUTHORITY_KINDS:
                raise
            err_bytes = str(exc).encode("utf-8", errors="surrogateescape")
            fail_sha = write_evidence(f"failure-{i}", "authority", err_bytes)
            entry = {
                "authority_id": aid,
                "kind": seed.kind,
                "locator": seed.locator,
                "availability": "unavailable",
                "sha256": None,
                "failure_class": exc.blocker_class,
                "failure_sha256": fail_sha,
            }
            manifest_entries.append(entry)
            authorities_records.append(
                {
                    "authority_id": aid,
                    "kind": seed.kind,
                    "locator": seed.locator,
                    "availability": "unavailable",
                    "failure_class": exc.blocker_class,
                    "failure_sha256": fail_sha,
                    "failure_evidence_id": f"@failure-{i}",
                }
            )
            continue
        sha = write_evidence(f"authority-{i}", "authority", raw)
        entry = {
            "authority_id": aid,
            "kind": seed.kind,
            "locator": seed.locator,
            "availability": "loaded",
            "sha256": sha,
            "failure_class": None,
            "failure_sha256": None,
        }
        manifest_entries.append(entry)
        authorities_records.append(
            {
                "authority_id": aid,
                "kind": seed.kind,
                "locator": seed.locator,
                "availability": "loaded",
                "sha256": sha,
                "evidence_id": f"@authority-{i}",
            }
        )

    # 8. feedback history
    try:
        items = feedback_policy.enumerate_feedback(run_gh=run_gh, pr_url=pr_meta["url"])
    except feedback_policy.FeedbackPolicyError as exc:
        raise AcquisitionError("authority-missing", str(exc)) from exc
    for j, item in enumerate(items):
        alias = f"feedback-{j}"
        write_evidence(alias, "authority", item.raw)
        aid = "authority:" + model.sha256_json({"kind": "review-feedback", "locator": item.canonical_id})
        entry = {
            "authority_id": aid,
            "kind": "review-feedback",
            "locator": item.canonical_id,
            "availability": "loaded",
            "sha256": item.bytes_sha256,
            "failure_class": None,
            "failure_sha256": None,
        }
        manifest_entries.append(entry)
        authorities_records.append(
            {
                "authority_id": aid,
                "kind": "review-feedback",
                "locator": item.canonical_id,
                "availability": "loaded",
                "sha256": item.bytes_sha256,
                "evidence_id": f"@{alias}",
            }
        )

    manifest_entries.sort(key=lambda e: e["locator"])
    fb_history_sha = feedback_policy.feedback_history_sha256(items)
    unresolved_sha = feedback_policy.unresolved_feedback_sha256(items)
    required_checks = _required_check_digest(run_gh, repo_id, pr_meta.get("baseRefName") or "main")
    pol = builtins.policies
    ingestion = builtins.evidence_ingestion_policy
    manifest_payload = model.manifest_payload(
        repository_id=repo_id,
        pr_number=pr_number,
        pr_url=pr_meta["url"],
        authority_discovery_policy_id=disc.policy_id,
        authority_discovery_policy_version=disc.version,
        authority_discovery_policy_sha256=disc.document_sha256,
        authorities=tuple(manifest_entries),
        feedback_history_policy_id=fb_policy.policy_id,
        feedback_history_policy_version=fb_policy.version,
        feedback_history_policy_sha256=fb_policy.document_sha256,
        feedback_history_sha256=fb_history_sha,
        local_check_policy_id=pol.local_checks.source_id,
        local_check_policy_version=pol.local_checks.source_version,
        local_check_policy_sha256=pol.local_checks.sha256,
        required_check_policy_sha256=required_checks,
        review_assignment_policy_id=pol.review_assignments.source_id,
        review_assignment_policy_version=pol.review_assignments.source_version,
        review_assignment_policy_sha256=pol.review_assignments.sha256,
        command_execution_policy_id=pol.command_execution.source_id,
        command_execution_policy_version=pol.command_execution.source_version,
        command_execution_policy_sha256=pol.command_execution.sha256,
        evidence_ingestion_policy_id=ingestion.source_id,
        evidence_ingestion_policy_version=ingestion.source_version,
        evidence_ingestion_policy_sha256=ingestion.sha256,
        hypothesis_derivation_policy_id=pol.hypotheses.source_id,
        hypothesis_derivation_policy_version=pol.hypotheses.source_version,
        hypothesis_derivation_policy_sha256=pol.hypotheses.sha256,
        unresolved_feedback_sha256=unresolved_sha,
    )
    manifest_id = model.authority_manifest_id(manifest_payload)

    snapshot = {
        "epoch": epoch,
        "repository_id": repo_id,
        "pr_number": pr_number,
        "pr_url": pr_meta["url"],
        "git_object_format": obj_format,
        "base_sha": base_sha,
        "head_sha": head_sha,
        "tree_sha": tree_sha,
        "diff_sha256": diff_sha256,
        "pr_metadata_sha256": pr_metadata_sha256,
        "authority_manifest_sha256": manifest_id,
        "authority_discovery_policy_id": disc.policy_id,
        "authority_discovery_policy_version": disc.version,
        "authority_discovery_policy_sha256": disc.document_sha256,
        "witness_policy_sha256": witness_pol.sha256,
        "feedback_history_policy_id": fb_policy.policy_id,
        "feedback_history_policy_version": fb_policy.version,
        "feedback_history_policy_sha256": fb_policy.document_sha256,
        "feedback_history_sha256": fb_history_sha,
        "local_check_policy_id": pol.local_checks.source_id,
        "local_check_policy_version": pol.local_checks.source_version,
        "local_check_policy_sha256": pol.local_checks.sha256,
        "required_check_policy_sha256": required_checks,
        "review_assignment_policy_id": pol.review_assignments.source_id,
        "review_assignment_policy_version": pol.review_assignments.source_version,
        "review_assignment_policy_sha256": pol.review_assignments.sha256,
        "command_execution_policy_id": pol.command_execution.source_id,
        "command_execution_policy_version": pol.command_execution.source_version,
        "command_execution_policy_sha256": pol.command_execution.sha256,
        "evidence_ingestion_policy_id": ingestion.source_id,
        "evidence_ingestion_policy_version": ingestion.source_version,
        "evidence_ingestion_policy_sha256": ingestion.sha256,
        "hypothesis_derivation_policy_id": pol.hypotheses.source_id,
        "hypothesis_derivation_policy_version": pol.hypotheses.source_version,
        "hypothesis_derivation_policy_sha256": pol.hypotheses.sha256,
        "unresolved_feedback_sha256": unresolved_sha,
    }

    # 9. persist
    snap_with_fp = dict(snapshot)
    snap_with_fp["fingerprint"] = model.snapshot_fingerprint(snapshot)
    enumeration_id = model.sha256_json(model.authority_discovery_subject(snap_with_fp, manifest_payload))
    manifest_bytes = model.canonical_json(manifest_payload)
    write_evidence("manifest-payload", "authority-manifest-payload", manifest_bytes)
    findings = feedback_policy.feedback_findings(items, policy=fb_policy)
    data = {
        "snapshot": snapshot,
        "manifest_payload": manifest_payload,
        "authorities": authorities_records,
        "findings": findings,
        "drift_reasons": None,
    }
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "data.json").write_bytes(model.canonical_json(data))
    (ev_dir / "manifest.json").write_text(json.dumps(evidence_map, indent=2, sort_keys=True), encoding="utf-8")
    (out_dir / "enumeration.json").write_text(
        json.dumps(
            {
                "enumeration_id": enumeration_id,
                "inputs": {
                    "repo_root": str(repo_root),
                    "pr_number": pr_number,
                    "base_sha": base_sha,
                    "head_sha": head_sha,
                    "epoch": epoch,
                },
            }
        ),
        encoding="utf-8",
    )
    return {
        "enumeration_id": enumeration_id,
        "out_dir": str(out_dir),
        "snapshot": snap_with_fp,
        "authority_manifest_sha256": manifest_id,
        "authority_count": len(manifest_entries),
        "feedback_items": len(items),
    }


def _remove_readonly(func, path, _exc):
    os.chmod(path, stat.S_IWRITE)
    func(path)


def _walk_strings(value):
    if isinstance(value, str):
        yield value
    elif isinstance(value, dict):
        for v in value.values():
            yield from _walk_strings(v)
    elif isinstance(value, list):
        for v in value:
            yield from _walk_strings(v)


def _response_text(rec: dict) -> str:
    response = rec.get("tool_response")
    if isinstance(response, str):
        return response
    if isinstance(response, dict):
        return response.get("output", "") or ""
    return model.canonical_json(response or {}).decode("utf-8", errors="surrogateescape")


class LiveAuthorityDiscovery:
    """AuthorityDiscoverySource backed by a produced acquisition dir."""

    def __init__(self, *, acquisition_dir: Path, witness_log_path: Path, transcript_root: Path, review_id: str) -> None:
        self._dir = Path(acquisition_dir)
        self._witness_log = Path(witness_log_path)
        self._transcript_root = Path(transcript_root)
        self._review_id = review_id

    def _load_dir(self):
        try:
            data = json.loads((self._dir / "data.json").read_bytes().decode("utf-8", errors="surrogateescape"))
        except ValueError as exc:
            raise AcquisitionError("tampered-source", f"data.json is not valid JSON: {exc}") from exc
        except OSError as exc:
            raise AcquisitionError("tampered-source", f"data.json missing or unreadable: {exc}") from exc
        try:
            ev_manifest = json.loads((self._dir / "evidence" / "manifest.json").read_bytes())
        except ValueError as exc:
            raise AcquisitionError("tampered-source", f"evidence manifest is not valid JSON: {exc}") from exc
        except OSError as exc:
            raise AcquisitionError("tampered-source", f"evidence manifest missing or unreadable: {exc}") from exc
        if not isinstance(data, dict):
            raise AcquisitionError("tampered-source", "data.json is not an object")
        if not isinstance(ev_manifest, dict):
            raise AcquisitionError("tampered-source", "evidence manifest is not an object")
        if not isinstance(data.get("snapshot"), dict) or not isinstance(data.get("manifest_payload"), dict):
            raise AcquisitionError("tampered-source", "data.json missing snapshot or manifest_payload")
        manifest_auths = data["manifest_payload"].get("authorities")
        records = data.get("authorities")
        if not isinstance(manifest_auths, list) or not all(isinstance(e, dict) for e in manifest_auths):
            raise AcquisitionError("tampered-source", "witnessed manifest authorities malformed")
        if not isinstance(records, list) or not all(isinstance(r, dict) for r in records):
            raise AcquisitionError("tampered-source", "authority records malformed")
        sources = []
        for alias, rec in sorted(ev_manifest.items()):
            if (
                not isinstance(rec, dict)
                or not isinstance(rec.get("file"), str)
                or not isinstance(rec.get("kind"), str)
            ):
                raise AcquisitionError("tampered-source", f"evidence {alias} manifest entry malformed")
            fname = rec["file"]
            if not fname or fname in (".", "..") or "/" in fname or "\\" in fname:
                raise AcquisitionError("tampered-source", f"evidence {alias} file field escapes evidence dir")
            path = self._dir / "evidence" / fname
            try:
                digest = model.sha256_hex(path.read_bytes()) if path.is_file() else None
            except (OSError, ValueError) as exc:
                raise AcquisitionError("tampered-source", f"evidence {alias} unreadable: {exc}") from exc
            if digest != rec.get("sha256"):
                raise AcquisitionError("tampered-source", f"evidence {alias} digest mismatch")
            sources.append(engine.EvidenceSource(alias=alias, kind=rec["kind"], path=path))
        # Bind evidence bytes to the witnessed manifest: authority records and
        # the evidence manifest are both unbound, so reconcile each record
        # against the subject-bound manifest entry (keyed by authority_id -
        # locators can collide across kinds) and its evidence file digest,
        # including the failure fields on unavailable records. Anything
        # inconsistent is tamper evidence.
        if len({e.get("authority_id") for e in manifest_auths}) != len(manifest_auths):
            raise AcquisitionError("tampered-source", "witnessed manifest has duplicate authority_id")
        bound = {e.get("authority_id"): e for e in manifest_auths}
        seen = set()
        for rec in records:
            aid = rec.get("authority_id")
            entry = bound.get(aid)
            if entry is None or aid in seen:
                raise AcquisitionError(
                    "tampered-source",
                    f"authority {aid}: record missing from or duplicated vs witnessed manifest",
                )
            seen.add(aid)
            for field in ("availability", "sha256", "failure_class", "failure_sha256"):
                if entry.get(field) != rec.get(field):
                    raise AcquisitionError(
                        "tampered-source",
                        f"authority {aid}: record {field} diverges from witnessed manifest",
                    )
            want_field = "evidence_id" if rec.get("availability") == "loaded" else "failure_evidence_id"
            ev = rec.get(want_field)
            if not isinstance(ev, str) or not ev.startswith("@"):
                raise AcquisitionError(
                    "tampered-source",
                    f"authority {aid}: {want_field} missing or malformed",
                )
            expected = rec.get("sha256") if want_field == "evidence_id" else rec.get("failure_sha256")
            manifest_ev = ev_manifest.get(ev[1:])
            if expected is None or not isinstance(manifest_ev, dict) or manifest_ev.get("sha256") != expected:
                raise AcquisitionError(
                    "tampered-source",
                    f"authority {aid}: {want_field} digest diverges from witnessed manifest",
                )
        if len(seen) != len(bound):
            raise AcquisitionError("tampered-source", "authority records diverge from witnessed manifest")
        return data, sources

    def _find_segment(self, subject_sha: str):
        candidates = []
        unreadable = []
        root = self._transcript_root
        try:
            globbed = list(root.glob("*.jsonl"))
        except OSError as exc:
            raise AcquisitionError("tampered-source", f"transcript root unreadable: {exc}") from exc
        paths = []
        for cand in globbed:
            try:
                paths.append((cand.stat().st_mtime, cand))
            except OSError:
                unreadable.append(cand)
        for mtime, path in sorted(paths):
            try:
                lines = path.read_bytes().decode("utf-8", errors="surrogateescape").splitlines()
            except OSError:
                unreadable.append(path)
                continue
            for idx, line in enumerate(lines):
                if not line.strip():
                    continue
                try:
                    rec = json.loads(line)
                except Exception:
                    continue
                if not isinstance(rec, dict):
                    continue
                if rec.get("hook_event_name") != "PostToolUse":
                    continue
                if subject_sha not in _response_text(rec):
                    continue
                tool_input = rec.get("tool_input") or {}
                haystack = " ".join(str(v) for v in _walk_strings(tool_input))
                if "enumerate" not in haystack:
                    continue
                candidates.append((mtime, idx, path, rec))
        if not candidates:
            if unreadable:
                raise AcquisitionError(
                    "tampered-source",
                    f"witnessed transcript unreadable: {unreadable[0]}",
                )
            raise AcquisitionError("missing-source", "no witnessed enumerate transcript segment for this acquisition")
        _m, _i, path, post = max(candidates, key=lambda c: (c[0], c[1]))
        session_id, tool_use_id = post.get("session_id"), post.get("tool_use_id")
        if session_id is None or tool_use_id is None:
            raise AcquisitionError("missing-source", "witnessed Post record lacks session_id/tool_use_id")
        return path, session_id, tool_use_id

    def acquire(self, *, action: str, current_snapshot: dict | None):
        data, sources = self._load_dir()
        snapshot = dict(data["snapshot"])
        drift_reasons = None
        if action == "refresh-review-input":
            if current_snapshot is None:
                raise AcquisitionError("missing-snapshot", "refresh requires a current snapshot")
            expected_epoch = current_snapshot["epoch"] + 1
            if snapshot.get("epoch") != expected_epoch:
                raise AcquisitionError(
                    "epoch-mismatch", f"acquisition dir epoch {snapshot.get('epoch')} != required {expected_epoch}"
                )
            drift_reasons = [
                _DRIFT_NAME_MAP.get(f, f)
                for f in model.SNAPSHOT_SUBJECT_FIELDS
                if f != "epoch" and snapshot.get(f) != current_snapshot.get(f)
            ]
        elif action == "freeze-review-input":
            if snapshot.get("epoch") != 1:
                raise AcquisitionError("epoch-mismatch", "freeze requires an epoch-1 acquisition")
        else:
            raise AcquisitionError("unsupported-action", action)
        snapshot["fingerprint"] = model.snapshot_fingerprint(snapshot)

        # Re-derive feedback findings from digest-verified evidence rather than
        # trusting data.json: the snapshot's feedback shas are bound by the
        # witnessed subject, so divergence here means the acquisition dir was
        # tampered with after enumerate.
        fb_policy = feedback_policy.default_policy()
        if fb_policy.document_sha256 != snapshot.get("feedback_history_policy_sha256"):
            raise AcquisitionError("tampered-source", "feedback policy drifted from witnessed snapshot")
        fb_items = []
        for src in sources:
            if src.alias.startswith("feedback-"):
                try:
                    fb_items.append(feedback_policy.item_from_raw(src.path.read_bytes()))
                except (feedback_policy.FeedbackPolicyError, OSError) as exc:
                    raise AcquisitionError("tampered-source", str(exc)) from exc
        if feedback_policy.feedback_history_sha256(fb_items) != snapshot.get(
            "feedback_history_sha256"
        ) or feedback_policy.unresolved_feedback_sha256(fb_items) != snapshot.get("unresolved_feedback_sha256"):
            raise AcquisitionError("tampered-source", "feedback evidence diverges from witnessed snapshot")
        findings = feedback_policy.feedback_findings(fb_items, policy=fb_policy)

        manifest_payload = data["manifest_payload"]
        subject = model.authority_discovery_subject(snapshot, manifest_payload)
        subject_sha = model.sha256_json(subject)
        transcript_path, session_id, tool_use_id = self._find_segment(subject_sha)
        log = witness_log.WitnessLog(self._witness_log)
        positions, head, trange = witness_log.ingest_transcript_segment(
            log,
            transcript_path=transcript_path,
            session_id=session_id,
            tool_use_id=tool_use_id,
            marker=subject_sha,
        )
        witness = {
            "witness_id": "",
            "kind": "authority-discovery",
            "subject_sha256": subject_sha,
            "tool_use_id": tool_use_id,
            "agent_id": None,
            "transcript_range": trange,
            "record_positions": positions,
            "chain_head_at_record": head,
            "source_locator": str(self._witness_log),
            "snapshot_epoch": snapshot["epoch"],
            "snapshot_fingerprint": snapshot["fingerprint"],
        }
        witness["witness_id"] = model.derived_id("witness", snapshot["epoch"], model.witness_record_subject(witness))
        wrapper = {
            "authority_manifest_id": model.authority_manifest_id(manifest_payload),
            "payload_evidence_id": "@manifest-payload",
            "discovery_witness_id": witness["witness_id"],
            "snapshot_epoch": snapshot["epoch"],
            "snapshot_fingerprint": snapshot["fingerprint"],
        }
        payload_data = {
            "snapshot": snapshot,
            "authority_manifest": wrapper,
            "authorities": data["authorities"],
            "findings": findings,
        }
        if drift_reasons is not None:
            payload_data["drift_reasons"] = drift_reasons
        envelope = {"data": payload_data, "witnesses": [witness]}
        return engine.TrustedActionPayload(model.canonical_json(envelope), tuple(sources))
