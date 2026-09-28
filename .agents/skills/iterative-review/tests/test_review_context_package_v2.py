#!/usr/bin/env python3
"""Tests for the exact-snapshot reviewer context package (Plan 3 Task 6)."""

from __future__ import annotations

import json
import sys
import types
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR.parent / "scripts"))

from review_core import assignment_policy, context_package, model, surfaces  # noqa: E402

HEAD = "h" * 40
FP = "f" * 64

_DIFF = (
    "diff --git a/src/foo.py b/src/foo.py\n"
    "index 1111111..2222222 100644\n"
    "--- a/src/foo.py\n"
    "+++ b/src/foo.py\n"
    "@@ -10,2 +10,2 @@ def f():\n"
    " context line\n"
    "-old line\n"
    "+new line\n"
    "diff --git a/src/bar.py b/src/bar.py\n"
    "index 3333333..4444444 100644\n"
    "--- a/src/bar.py\n"
    "+++ b/src/bar.py\n"
    "@@ -1,1 +1,1 @@\n"
    "-a\n"
    "+b\n"
)
PATCH = _DIFF.encode("utf-8")
FILES = {"src/foo.py": "def f():\n    return 1\n", "src/bar.py": "b\n"}


def _policies():
    return types.SimpleNamespace(review_assignments=assignment_policy.SealedReviewAssignmentPolicy())


def _record(rid, **over):
    rec = {"snapshot_epoch": 1, "snapshot_fingerprint": FP}
    rec.update(over)
    return rec


def _state(
    *,
    patch: bytes = PATCH,
    maps=(),
    inventory=None,
    obligations=None,
    hyps=None,
    authorities=None,
):
    return {
        "snapshot": {
            "epoch": 1,
            "fingerprint": FP,
            "head_sha": HEAD,
            "diff_sha256": model.sha256_hex(patch),
        },
        "impact_maps": {m["impact_map_id"]: m for m in maps},
        "coverage_inventory": inventory,
        "obligations": obligations or {},
        "hypothesis_assignments": hyps or {},
        "findings": {},
        "review_repairs": {},
        "reviews": {},
        "dispatches": {},
        "route_selections": {},
        "checks": {},
        "authorities": authorities or {},
        "evidence": {},
        "content_objects": {},
    }


def _acquire(tmp_path: Path, patch: bytes = PATCH) -> Path:
    acquire = tmp_path / "acquire" / "latest"
    acquire.mkdir(parents=True)
    (acquire / "diff.patch").write_bytes(patch)
    (acquire / "surfaces.json").write_bytes(
        surfaces.surfaces_document(surfaces.parse_diff_surfaces(patch.decode("utf-8")))
    )
    return acquire


def _git(files):
    def run(argv, cwd=None):
        if argv[0] == "show" and ":" in argv[1]:
            _sha, path = argv[1].split(":", 1)
            if path in files:
                return 0, files[path], ""
            return 1, "", f"missing {path}"
        return 1, "", f"unsupported {argv}"

    return run


def _resolver(blobs):
    def resolve(eid):
        return blobs[eid]

    return resolve


def _obligation(oid, *, risk="low", consequences=("none",), scope="surface"):
    return _record(
        oid,
        obligation_id=oid,
        category="file",
        surfaces=["src/foo.py"],
        risk=risk,
        consequences=list(consequences),
        scope_level=scope,
        status="pending",
    )


def _build(
    state,
    package_dir,
    acquire,
    *,
    action="run-strong-review",
    role="obligation-reviewer",
    assignment_ids=(),
    files=None,
    blobs=None,
):
    return context_package.build_context_package(
        state=state,
        action=action,
        role=role,
        assignment_ids=tuple(assignment_ids),
        package_dir=package_dir,
        acquire_dir=acquire,
        run_git=_git(FILES if files is None else files),
        evidence_resolver=_resolver({} if blobs is None else blobs),
        policies=_policies(),
    )


class TestBuildContextPackage:
    def test_package_contains_complete_patch_digest_bound(self, tmp_path):
        acquire = _acquire(tmp_path)
        frag = _build(_state(), tmp_path / "pkg", acquire)
        assert (tmp_path / "pkg" / "patch.diff").read_bytes() == PATCH
        assert frag["instruction_manifest_sha256"] == model.sha256_hex(
            (tmp_path / "pkg" / "manifest.json").read_bytes()
        )
        assert frag["hazard_framing_sha256"] == model.sha256_hex((tmp_path / "pkg" / "hazards.json").read_bytes())

    def test_patch_digest_mismatch_refused(self, tmp_path):
        acquire = _acquire(tmp_path)
        state = _state()
        state["snapshot"]["diff_sha256"] = "0" * 64
        with pytest.raises(context_package.ContextPackageError):
            _build(state, tmp_path / "pkg", acquire)

    def test_surface_files_at_head_sha(self, tmp_path):
        acquire = _acquire(tmp_path)
        inv = _record(
            "inventory:1",
            coverage_inventory_id="inventory:1",
            entries=[
                {
                    "surface": "src/foo.py",
                    "categories": ["file"],
                    "hazards": [],
                    "consequences": ["none"],
                    "obligation_ids": [],
                }
            ],
        )
        state = _state(inventory=inv)
        frag = _build(state, tmp_path / "pkg", acquire)
        written = tmp_path / "pkg" / "data" / "files" / "src" / "foo.py"
        assert written.read_bytes() == FILES["src/foo.py"].encode("utf-8")
        manifest = json.loads((tmp_path / "pkg" / "data" / "manifest.json").read_bytes())
        assert manifest["files"] == [{"path": "src/foo.py", "sha256": model.sha256_hex(FILES["src/foo.py"].encode())}]
        assert frag["data_manifest_sha256"] == model.sha256_hex(
            (tmp_path / "pkg" / "data" / "manifest.json").read_bytes()
        )

    def test_surface_files_fall_back_to_map_union(self, tmp_path):
        acquire = _acquire(tmp_path)
        m = _record(
            "map:1",
            impact_map_id="map:1",
            role="impact-mapper-semantic",
            entries=[{"surface": "src/bar.py", "category": "file", "hazards": [], "consequences": ["none"]}],
        )
        state = _state(maps=[m])
        _build(state, tmp_path / "pkg", acquire)
        written = tmp_path / "pkg" / "data" / "files" / "src" / "bar.py"
        assert written.read_bytes() == FILES["src/bar.py"].encode("utf-8")

    def test_authority_bytes_resolved_and_recorded(self, tmp_path):
        acquire = _acquire(tmp_path)
        blob = b"authority-bytes"
        auth = _record("auth:1", authority_id="auth:1", evidence_id="evidence:1")
        state = _state(authorities={"auth:1": auth})
        frag = _build(state, tmp_path / "pkg", acquire, blobs={"evidence:1": blob})
        refs = json.loads((tmp_path / "pkg" / "data" / "authorities.json").read_bytes())
        assert refs == {"evidence:1": model.sha256_hex(blob)}
        resolved = tmp_path / "pkg" / "data" / "authorities" / "evidence_1.bin"
        assert resolved.read_bytes() == blob
        assert frag["context_evidence_ids"] == ["evidence:1"]

    def test_hazard_framing_covers_assignments(self, tmp_path):
        acquire = _acquire(tmp_path)
        obl = _obligation("obligation:1", risk="high", consequences=("security",))
        hyp = _record(
            "hypothesis:1",
            hypothesis_assignment_id="hypothesis:1",
            obligation_id="obligation:1",
            family="attacker-reachability",
            statement="untrusted input reaches this surface",
        )
        state = _state(obligations={"obligation:1": obl}, hyps={"hypothesis:1": hyp})
        _build(state, tmp_path / "pkg", acquire, assignment_ids=("obligation:1",))
        hazards = json.loads((tmp_path / "pkg" / "hazards.json").read_bytes())
        assert hazards["assignments"] == [
            {
                "assignment_id": "obligation:1",
                "obligation_id": "obligation:1",
                "consequences": ["security"],
                "risk": "high",
                "hypotheses": ["hypothesis:1"],
            }
        ]

    def test_manifest_records_floors_and_independence(self, tmp_path):
        acquire = _acquire(tmp_path)
        obl = _obligation("obligation:1", risk="high", consequences=("security",), scope="cross-surface")
        state = _state(obligations={"obligation:1": obl})
        req = _policies().review_assignments.requirement(
            state=state, role="obligation-reviewer", assignment_ids=("obligation:1",)
        )
        _build(state, tmp_path / "pkg", acquire, assignment_ids=("obligation:1",))
        manifest = json.loads((tmp_path / "pkg" / "manifest.json").read_bytes())
        assert manifest == {
            "action": "run-strong-review",
            "role": "obligation-reviewer",
            "capability_tier": req.capability_tier,
            "reasoning_floor": req.reasoning_floor,
            "context_mode": req.context_mode,
            "assignment_ids": ["obligation:1"],
            "report_schema_version": 1,
            "distinct_execution_from": sorted(req.distinct_execution_from),
            "distinct_role_contract_from": sorted(req.distinct_role_contract_from),
        }
        assert manifest["capability_tier"] == "strong"

    def test_returned_fragment_binds_all_dispatch_fields(self, tmp_path):
        acquire = _acquire(tmp_path)
        pkg = tmp_path / "pkg"
        frag = _build(_state(), pkg, acquire, assignment_ids=())
        assert set(frag) == {
            "assignment_ids",
            "context_evidence_ids",
            "instruction_manifest_sha256",
            "data_manifest_sha256",
            "context_package_sha256",
            "hazard_framing_sha256",
            "required_tool_classes",
        }
        assert frag["assignment_ids"] == []
        assert set(frag["required_tool_classes"]) <= set(model.REQUIRED_TOOL_CLASSES)
        recomputed = model.sha256_json(
            {
                str(p.relative_to(pkg).as_posix()): model.sha256_hex(p.read_bytes())
                for p in sorted(pkg.rglob("*"))
                if p.is_file()
            }
        )
        assert frag["context_package_sha256"] == recomputed

    def test_deterministic_package_digest(self, tmp_path):
        acquire = _acquire(tmp_path)
        state = _state()
        frag_a = _build(state, tmp_path / "pkg-a", acquire)
        frag_b = _build(state, tmp_path / "pkg-b", acquire)
        assert frag_a == frag_b

    def test_missing_acquire_artifacts_refused(self, tmp_path):
        acquire = tmp_path / "acquire" / "latest"
        acquire.mkdir(parents=True)
        with pytest.raises(context_package.ContextPackageError):
            _build(_state(), tmp_path / "pkg", acquire)
