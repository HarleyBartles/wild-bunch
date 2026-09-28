#!/usr/bin/env python3
"""Exact-snapshot reviewer context packages (Plan 3 Task 6).

``build_context_package`` materializes the dispatch-bound reviewer package:
the Task-1 bound patch bytes, head-revision content for every coverage
surface, resolved authority bytes, hazard framing, and the instruction
manifest carrying the sealed review-assignment floors. The returned fragment
carries the digests a dispatch record binds; Plan 4 wires it into live
``ReviewerDispatchSource`` resolution.

The builder is a deterministic pure helper: identical state + acquisition
artifacts produce byte-identical packages and digests, and it never mutates
review state. ``run_git`` must behave like ``reviewctl._run_cmd``: text
decoded with ``errors="surrogateescape"`` so
``out.encode("utf-8", "surrogateescape")`` reconstructs raw blob bytes.
"""

from __future__ import annotations

import re
import shutil
from pathlib import Path

from . import model, surfaces


class ContextPackageError(Exception):
    """A required package input is missing, malformed, or digest-divergent."""

    def __init__(self, code: str, message: str):
        super().__init__(f"{code}: {message}")
        self.code = code


DEFAULT_REQUIRED_TOOL_CLASSES = ("git-read", "github-read", "repo-read")
REPORT_SCHEMA_VERSION = 1
_SAFE_NAME = re.compile(r"[^A-Za-z0-9._-]")


def _fail(code: str, message: str) -> None:
    raise ContextPackageError(code, message)


def _current(state: dict, record: dict, rid: str | None = None) -> bool:
    snap = state["snapshot"]
    if snap is None:
        return False
    if record.get("snapshot_epoch") != snap["epoch"] or record.get("snapshot_fingerprint") != snap["fingerprint"]:
        return False
    if rid is not None:
        for repair in state.get("review_repairs", {}).values():
            if (
                repair.get("snapshot_epoch") == snap["epoch"]
                and repair.get("snapshot_fingerprint") == snap["fingerprint"]
                and rid in repair.get("invalidated_record_ids", ())
            ):
                return False
    return True


def _safe_filename(raw: str) -> str:
    name = _SAFE_NAME.sub("_", raw)
    if name in ("", ".", ".."):
        _fail("unsafe-path", f"unusable artifact name derived from {raw!r}")
    return name


def _rel_target(root: Path, rel: str) -> Path:
    parts = [p for p in rel.split("/") if p not in ("", ".")]
    if not parts or any(p == ".." for p in parts) or rel.startswith("/"):
        _fail("unsafe-path", f"unsafe package-relative path {rel!r}")
    return root.joinpath(*parts)


def _obligation_for(state: dict, aid: str) -> dict | None:
    obl = state["obligations"].get(aid)
    if obl is not None:
        return obl
    hyp = state.get("hypothesis_assignments", {}).get(aid)
    if hyp is not None:
        return state["obligations"].get(hyp["obligation_id"])
    finding = state.get("findings", {}).get(aid)
    if finding is not None and finding.get("obligation_id"):
        return state["obligations"].get(finding["obligation_id"])
    repair = state.get("review_repairs", {}).get(aid)
    if repair is not None:
        src = state.get("findings", {}).get(repair.get("finding_id"))
        if src is not None and src.get("obligation_id"):
            return state["obligations"].get(src["obligation_id"])
    return None


def _surface_paths(state: dict) -> tuple[str, ...]:
    inv = state.get("coverage_inventory")
    if inv is not None and _current(state, inv, inv.get("coverage_inventory_id")):
        return tuple(sorted({e["surface"] for e in inv["entries"]}))
    out = set()
    for mid, m in state.get("impact_maps", {}).items():
        if _current(state, m, mid):
            out.update(e["surface"] for e in m["entries"])
    if out:
        return tuple(sorted(out))
    # Pre-inventory dispatches (mappers, challenger) still get the changed
    # surface seed so the package is never context-free.
    return None


def _read_head_blob(run_git, head_sha: str, path: str) -> bytes:
    try:
        rc, out, err = run_git(["show", f"{head_sha}:{path}"])
    except OSError as exc:
        _fail("tool-blocked", f"git unavailable: {exc}")
    if rc != 0:
        _fail("surface-read", f"git show {head_sha}:{path} failed: {err.strip()[:120]}")
    return out.encode("utf-8", errors="surrogateescape")


def build_context_package(
    *,
    state: dict,
    action: str,
    role: str,
    assignment_ids: tuple[str, ...],
    package_dir: Path,
    acquire_dir: Path,
    run_git,
    evidence_resolver,
    policies,
) -> dict:
    """Materialize the dispatch-bound reviewer package under package_dir."""
    snap = state.get("snapshot")
    if snap is None:
        _fail("no-snapshot", "context package requires an installed snapshot")

    try:
        patch_bytes = (acquire_dir / "diff.patch").read_bytes()
    except OSError as exc:
        _fail("missing-source", f"diff.patch unreadable: {exc}")
    if model.sha256_hex(patch_bytes) != snap["diff_sha256"]:
        _fail("digest-mismatch", "diff.patch digest diverges from snapshot.diff_sha256")
    try:
        surface_list = model.strict_json_loads((acquire_dir / "surfaces.json").read_bytes(), source="surfaces.json")
    except (OSError, ValueError) as exc:
        _fail("missing-source", f"surfaces.json missing or invalid: {exc}")
    if not isinstance(surface_list, list):
        _fail("missing-source", "surfaces.json is not a surface list")
    try:
        parsed = surfaces.parse_diff_surfaces(patch_bytes.decode("utf-8", errors="surrogateescape"))
    except surfaces.SurfaceParseError as exc:
        _fail("unparsable-diff", f"diff.patch unparsable: {exc}")
    if list(parsed) != surface_list:
        _fail("surface-divergence", "surfaces.json diverges from diff.patch")

    try:
        requirement = policies.review_assignments.requirement(
            state=state, role=role, assignment_ids=tuple(sorted(assignment_ids))
        )
    except Exception as exc:
        _fail("requirement-error", f"review-assignment requirement failed: {exc}")

    selected = _surface_paths(state)
    if selected is None:
        selected = tuple(e["path"] for e in parsed)
    kind_by_path = {e["path"]: e.get("change_kind") for e in parsed}

    manifest = {
        "action": action,
        "role": role,
        "capability_tier": requirement.capability_tier,
        "reasoning_floor": requirement.reasoning_floor,
        "context_mode": requirement.context_mode,
        "assignment_ids": sorted(assignment_ids),
        "report_schema_version": REPORT_SCHEMA_VERSION,
        "distinct_execution_from": sorted(requirement.distinct_execution_from),
        "distinct_role_contract_from": sorted(requirement.distinct_role_contract_from),
    }

    hazards = {"assignments": []}
    hyps = state.get("hypothesis_assignments", {})
    for aid in sorted(assignment_ids):
        obl = _obligation_for(state, aid)
        bound = (
            sorted(
                hid for hid, h in hyps.items() if h["obligation_id"] == obl["obligation_id"] and _current(state, h, hid)
            )
            if obl is not None
            else []
        )
        hazards["assignments"].append(
            {
                "assignment_id": aid,
                "obligation_id": obl["obligation_id"] if obl is not None else None,
                "consequences": sorted(obl["consequences"]) if obl is not None else [],
                "risk": obl["risk"] if obl is not None else None,
                "hypotheses": bound,
            }
        )

    authority_blobs: dict[str, bytes] = {}
    for aid_rec in sorted(state.get("authorities", {}).values(), key=lambda a: a["authority_id"]):
        if not _current(state, aid_rec, aid_rec["authority_id"]):
            continue
        eid = aid_rec.get("evidence_id")
        if eid is None:
            continue
        try:
            authority_blobs[eid] = evidence_resolver(eid)
        except Exception as exc:
            _fail("authority-unresolved", f"evidence {eid!r} unresolvable: {exc}")

    if package_dir.exists() and any(package_dir.iterdir()):
        _fail("not-empty", f"package dir {package_dir} is not empty")
    package_dir.mkdir(parents=True, exist_ok=True)
    data_dir = package_dir / "data"
    files_root = data_dir / "files"
    auth_dir = data_dir / "authorities"
    files_root.mkdir(parents=True)
    auth_dir.mkdir(parents=True)

    (package_dir / "patch.diff").write_bytes(patch_bytes)

    file_entries = []
    for path in selected:
        if kind_by_path.get(path) == "deleted":
            file_entries.append({"path": path, "sha256": None, "change_kind": "deleted"})
            continue
        blob = _read_head_blob(run_git, snap["head_sha"], path)
        target = _rel_target(files_root, path)
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(blob)
        file_entries.append({"path": path, "sha256": model.sha256_hex(blob)})

    surfaces_bytes = model.canonical_json(surface_list)
    (data_dir / "surfaces.json").write_bytes(surfaces_bytes)
    authority_refs = {eid: model.sha256_hex(raw) for eid, raw in authority_blobs.items()}
    names: dict[str, str] = {}
    for eid in authority_blobs:
        name = _safe_filename(eid)
        if name in names:
            _fail("unsafe-path", f"evidence ids {names[name]!r} and {eid!r} collide as {name!r}")
        names[name] = eid
        (auth_dir / f"{name}.bin").write_bytes(authority_blobs[eid])
    authorities_bytes = model.canonical_json(authority_refs)
    (data_dir / "authorities.json").write_bytes(authorities_bytes)

    data_manifest = {
        "files": file_entries,
        "surfaces_sha256": model.sha256_hex(surfaces_bytes),
        "authorities_sha256": model.sha256_hex(authorities_bytes),
    }
    data_manifest_bytes = model.canonical_json(data_manifest)
    (data_dir / "manifest.json").write_bytes(data_manifest_bytes)

    manifest_bytes = model.canonical_json(manifest)
    (package_dir / "manifest.json").write_bytes(manifest_bytes)
    hazards_bytes = model.canonical_json(hazards)
    (package_dir / "hazards.json").write_bytes(hazards_bytes)

    package_digests = {
        str(p.relative_to(package_dir).as_posix()): model.sha256_hex(p.read_bytes())
        for p in sorted(package_dir.rglob("*"))
        if p.is_file()
    }

    return {
        "assignment_ids": sorted(assignment_ids),
        "context_evidence_ids": sorted(authority_blobs),
        "instruction_manifest_sha256": model.sha256_hex(manifest_bytes),
        "data_manifest_sha256": model.sha256_hex(data_manifest_bytes),
        "context_package_sha256": model.sha256_json(package_digests),
        "hazard_framing_sha256": model.sha256_hex(hazards_bytes),
        "required_tool_classes": list(DEFAULT_REQUIRED_TOOL_CLASSES),
    }


def evidence_bytes_for_state(state: dict, evidence_id: str) -> bytes:
    """Default ``evidence_resolver``: read evidence bytes via the state store."""
    rec = state.get("evidence", {}).get(evidence_id)
    if rec is None:
        _fail("authority-unresolved", f"unknown evidence id {evidence_id!r}")
    obj = state.get("content_objects", {}).get(rec["content_id"])
    if obj is None:
        _fail("authority-unresolved", f"evidence {evidence_id!r} references missing content")
    return Path(obj["path"]).read_bytes()


def package_dir_for(scratch_dir: Path, action: str, fragment: dict) -> Path:
    """Canonical default location ``<scratch>/packages/<action>-<digest>``."""
    prefix = fragment["context_package_sha256"][:12]
    return Path(scratch_dir) / "packages" / f"{action}-{prefix}"


def _package_digest(package_dir: Path) -> str:
    return model.sha256_json(
        {
            str(p.relative_to(package_dir).as_posix()): model.sha256_hex(p.read_bytes())
            for p in sorted(package_dir.rglob("*"))
            if p.is_file()
        }
    )


def materialize_under(*, scratch_dir: Path, action: str, build) -> tuple[dict, Path]:
    """Build via a staging dir, then move to the canonical package path."""
    packages = Path(scratch_dir) / "packages"
    staging = packages / f".staging-{action}"
    if staging.exists():
        shutil.rmtree(staging)
    staging.mkdir(parents=True)
    try:
        fragment = build(staging)
    except Exception:
        shutil.rmtree(staging, ignore_errors=True)
        raise
    target = package_dir_for(scratch_dir, action, fragment)
    if target.exists():
        if _package_digest(target) != fragment["context_package_sha256"]:
            _fail(
                "package-diverged",
                f"existing package {target} diverges from the rebuilt digest",
            )
        shutil.rmtree(staging)
    else:
        shutil.move(str(staging), str(target))
    return fragment, target
