#!/usr/bin/env python3
"""Tests for locked state storage and content-addressed evidence."""

from __future__ import annotations

import hashlib
import json
import os
import sys
import threading
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR))
sys.path.insert(0, str(TESTS_DIR.parent / "scripts"))

from review_v2_helpers import make_empty_v2_state  # noqa: E402

from review_core import model, store  # noqa: E402


def _policy(**overrides):
    base = dict(
        source_id="test-ingestion",
        source_version="1",
        sha256="0" * 64,
        per_kind_max_bytes={kind: 1 << 20 for kind in model.EVIDENCE_KINDS},
        transaction_max_bytes=8 << 20,
        review_max_bytes=64 << 20,
        windows_allowed_trustee_sids=(),
        posix_directory_mode=0o700,
        posix_file_mode=0o600,
    )
    base.update(overrides)
    return store.EvidenceIngestionPolicy(**base)


def _ctx(source: Path, action: str = "plan-coverage", snapshot=None):
    return store.EvidenceIngestionContext(
        action=action,
        candidate_snapshot=snapshot,
        eligible_sources=(source,),
    )


def _init_state(tmp_path: Path) -> Path:
    state = model.new_state("review-test", tmp_path)
    path = tmp_path / "review-state.json"
    store.create_state(path, state)
    return path


def _snapshot(**overrides):
    snap = {
        "epoch": 1,
        "repository_id": "o/r",
        "pr_number": 7,
        "pr_url": "https://github.com/o/r/pull/7",
        "git_object_format": "sha1",
        "base_sha": "a" * 40,
        "head_sha": "b" * 40,
        "tree_sha": "c" * 40,
        "diff_sha256": "d" * 64,
        "pr_metadata_sha256": "e" * 64,
        "authority_manifest_sha256": "f" * 64,
        "authority_discovery_policy_id": "adp",
        "authority_discovery_policy_version": "1",
        "authority_discovery_policy_sha256": "0" * 64,
        "witness_policy_sha256": "1" * 64,
        "feedback_history_policy_id": "fhp",
        "feedback_history_policy_version": "1",
        "feedback_history_policy_sha256": "2" * 64,
        "feedback_history_sha256": "3" * 64,
        "local_check_policy_id": "lcp",
        "local_check_policy_version": "1",
        "local_check_policy_sha256": "4" * 64,
        "required_check_policy_sha256": "5" * 64,
        "review_assignment_policy_id": "rap",
        "review_assignment_policy_version": "1",
        "review_assignment_policy_sha256": "6" * 64,
        "command_execution_policy_id": "cep",
        "command_execution_policy_version": "1",
        "command_execution_policy_sha256": "7" * 64,
        "evidence_ingestion_policy_id": "eip",
        "evidence_ingestion_policy_version": "1",
        "evidence_ingestion_policy_sha256": "8" * 64,
        "hypothesis_derivation_policy_id": "hdp",
        "hypothesis_derivation_policy_version": "1",
        "hypothesis_derivation_policy_sha256": "9" * 64,
        "unresolved_feedback_sha256": "a" * 64,
    }
    snap.update(overrides)
    snap["fingerprint"] = model.snapshot_fingerprint(snap)
    return snap


def _src(tmp_path: Path, data: bytes = b"hello") -> Path:
    p = tmp_path / "src.txt"
    p.write_bytes(data)
    return p


# --- load / create ------------------------------------------------------------


def test_load_rejects_missing(tmp_path):
    with pytest.raises(store.StoreError) as e:
        store.load_state(tmp_path / "review-state.json")
    assert e.value.code == "state-missing"


def test_load_rejects_malformed_json(tmp_path):
    p = tmp_path / "review-state.json"
    p.write_bytes(b"{not json")
    with pytest.raises(model.StateValidationError):
        store.load_state(p)


def test_load_rejects_bom(tmp_path):
    p = tmp_path / "review-state.json"
    p.write_bytes(b'\xef\xbb\xbf{"schema_version":2}')
    with pytest.raises(model.StateValidationError):
        store.load_state(p)


def test_load_rejects_v1_state(tmp_path):
    p = tmp_path / "review-state.json"
    p.write_text(json.dumps({"current_node": "setup"}), encoding="utf-8")
    with pytest.raises(store.StoreError) as e:
        store.load_state(p)
    assert e.value.code == "state-version"


def test_create_state_validates_and_writes(tmp_path):
    path = _init_state(tmp_path)
    raw = path.read_bytes()
    assert not raw.startswith(b"\xef\xbb\xbf")
    assert raw.endswith(b"\n")
    model.strict_json_loads(raw)


def test_create_state_rejects_nonzero_generation(tmp_path):
    state = model.new_state("review-test", tmp_path)
    state["generation"] = 3
    with pytest.raises(store.StoreError):
        store.create_state(tmp_path / "review-state.json", state)


def test_create_state_refuses_overwrite(tmp_path):
    path = _init_state(tmp_path)
    with pytest.raises(store.StoreError) as e:
        store.create_state(path, model.new_state("r2", tmp_path))
    assert e.value.code == "state-exists"


# --- transaction CAS ------------------------------------------------------------


def test_commit_advances_generation_and_appends_history(tmp_path):
    path = _init_state(tmp_path)
    with store.locked_state_transaction(path) as tx:
        tx.candidate["status"] = "active"
        store.append_history(tx.candidate, event="bump", data_sha256="d" * 64)
        tx.commit()
    state = store.load_state(path)
    assert state["generation"] == 1
    assert state["history"][-1]["generation"] == 1


def test_commit_rejects_stale_prior_bytes(tmp_path):
    path = _init_state(tmp_path)
    tx = store.StateTransaction.__new__(store.StateTransaction)
    tx._path = path
    tx._lock_path = path.with_name("review-state.lock")
    tx._lock_fd = None
    tx._committed = False
    tx.prior_generation = 0
    tx.prior_bytes_sha256 = "0" * 64  # stale
    tx.candidate = store.load_state(path)
    tx.candidate["generation"] = 1
    with pytest.raises(store.ConcurrentStateError):
        tx.commit()


def test_two_processes_cannot_both_commit_generation_n(tmp_path):
    path = _init_state(tmp_path)
    acquired = threading.Event()
    results = {}

    def first():
        with store.locked_state_transaction(path) as tx:
            acquired.set()
            tx.candidate["status"] = "blocked"
            tx.candidate["stage"] = "blocked"
            tx.candidate["blockers"]["b-1"] = _blocker(tx.candidate)
            store.append_history(tx.candidate, event="block", data_sha256="d" * 64)
            tx.commit()
            results["first"] = "committed"

    def second():
        acquired.wait(timeout=10)
        try:
            with store.locked_state_transaction(path) as tx:
                # sees generation 1 because the first committed first
                tx.candidate["green_seal"] = None
                store.append_history(tx.candidate, event="noop", data_sha256="e" * 64)
                tx.commit()
                results["second"] = store.load_state(path)["generation"]
        except store.ConcurrentStateError:
            results["second"] = "rejected"

    t1 = threading.Thread(target=first)
    t2 = threading.Thread(target=second)
    t1.start()
    t2.start()
    t1.join()
    t2.join()
    state = store.load_state(path)
    assert results["first"] == "committed"
    assert results["second"] == 2
    assert "b-1" in state["blockers"]
    assert state["generation"] == 2


def _blocker(candidate):
    snap = candidate["snapshot"] or {"epoch": 1, "fingerprint": "f" * 64}
    return {
        "blocker_id": "b-1",
        "class": "tool-blocked",
        "reason": "r",
        "evidence_ids": [],
        "active": True,
        "opened_sequence": 1,
        "closed_sequence": None,
        "resolution_evidence_ids": [],
        "resolution_snapshot_epoch": None,
        "resolution_snapshot_fingerprint": None,
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }


def test_failed_commit_leaves_prior_file_intact(tmp_path):
    path = _init_state(tmp_path)
    before = path.read_bytes()
    with store.locked_state_transaction(path) as tx:
        tx.candidate["status"] = "green"  # invalid enum -> validation fails
        with pytest.raises(model.StateValidationError):
            tx.commit()
    assert path.read_bytes() == before


def test_lock_excludes_concurrent_transactions(tmp_path):
    path = _init_state(tmp_path)
    with store.locked_state_transaction(path) as tx:
        tx.candidate["status"] = "blocked"
        tx.candidate["stage"] = "blocked"
        tx.candidate["blockers"]["b-1"] = _blocker(tx.candidate)
        store.append_history(tx.candidate, event="block", data_sha256="d" * 64)
        tx.commit()
    # A hand-built stale transaction must be rejected at commit.
    tx2 = store.StateTransaction.__new__(store.StateTransaction)
    tx2._path = path
    tx2._lock_path = path.with_name("review-state.lock")
    tx2._lock_fd = None
    tx2._committed = False
    tx2.prior_generation = 0
    tx2.prior_bytes_sha256 = hashlib.sha256(b"stale").hexdigest()
    tx2.candidate = store.load_state(path)
    tx2.candidate["generation"] = 1
    with pytest.raises(store.ConcurrentStateError):
        tx2.commit()


# --- evidence registration ------------------------------------------------------


def test_register_evidence_stores_content_and_binding(tmp_path):
    path = _init_state(tmp_path)
    src = _src(tmp_path, b"payload-bytes")
    with store.locked_state_transaction(path) as tx:
        tx.candidate["snapshot"] = _snapshot()
        tx.candidate["stage"] = "authority"
        reg = store.register_evidence(
            tx,
            src,
            alias="doc",
            kind="authority",
            snapshot_epoch=1,
            snapshot_fingerprint=_snapshot()["fingerprint"],
            policy=_policy(),
            context=_ctx(src),
        )
        store.append_history(tx.candidate, event="evidence", data_sha256="d" * 64)
        tx.commit()
    state = store.load_state(path)
    assert reg.evidence_id in state["evidence"]
    content = state["content_objects"][reg.content_id]
    assert Path(content["path"]).read_bytes() == b"payload-bytes"


def test_register_evidence_rejects_missing_source(tmp_path):
    path = _init_state(tmp_path)
    missing = tmp_path / "nope.txt"
    with store.locked_state_transaction(path) as tx:
        with pytest.raises(store.UnsafeEvidenceSourceError):
            store.register_evidence(
                tx,
                missing,
                alias="doc",
                kind="authority",
                snapshot_epoch=1,
                snapshot_fingerprint="f" * 64,
                policy=_policy(),
                context=_ctx(missing),
            )


def test_register_evidence_rejects_directory(tmp_path):
    path = _init_state(tmp_path)
    with store.locked_state_transaction(path) as tx:
        with pytest.raises(store.UnsafeEvidenceSourceError):
            store.register_evidence(
                tx,
                tmp_path,
                alias="doc",
                kind="authority",
                snapshot_epoch=1,
                snapshot_fingerprint="f" * 64,
                policy=_policy(),
                context=_ctx(tmp_path),
            )


def test_register_evidence_rejects_empty_file(tmp_path):
    path = _init_state(tmp_path)
    src = _src(tmp_path, b"")
    with store.locked_state_transaction(path) as tx:
        with pytest.raises(store.UnsafeEvidenceSourceError):
            store.register_evidence(
                tx,
                src,
                alias="doc",
                kind="authority",
                snapshot_epoch=1,
                snapshot_fingerprint="f" * 64,
                policy=_policy(),
                context=_ctx(src),
            )


def test_register_evidence_rejects_unlisted_source(tmp_path):
    path = _init_state(tmp_path)
    src = _src(tmp_path)
    other = tmp_path / "other.txt"
    other.write_bytes(b"x")
    with store.locked_state_transaction(path) as tx:
        with pytest.raises(store.UnsafeEvidenceSourceError):
            store.register_evidence(
                tx,
                other,
                alias="doc",
                kind="authority",
                snapshot_epoch=1,
                snapshot_fingerprint="f" * 64,
                policy=_policy(),
                context=_ctx(src),  # only src is eligible
            )


def test_register_evidence_rejects_symlink(tmp_path):
    path = _init_state(tmp_path)
    real = _src(tmp_path)
    link = tmp_path / "link.txt"
    try:
        link.symlink_to(real)
    except OSError:
        pytest.skip("symlink creation unavailable")
    with store.locked_state_transaction(path) as tx:
        with pytest.raises(store.UnsafeEvidenceSourceError):
            store.register_evidence(
                tx,
                link,
                alias="doc",
                kind="authority",
                snapshot_epoch=1,
                snapshot_fingerprint="f" * 64,
                policy=_policy(),
                context=_ctx(link),
            )


def test_register_evidence_rejects_ads_syntax(tmp_path):
    path = _init_state(tmp_path)
    src = _src(tmp_path)
    ads = Path(str(src) + ":stream")
    with store.locked_state_transaction(path) as tx:
        with pytest.raises(store.UnsafeEvidenceSourceError):
            store.register_evidence(
                tx,
                ads,
                alias="doc",
                kind="authority",
                snapshot_epoch=1,
                snapshot_fingerprint="f" * 64,
                policy=_policy(),
                context=_ctx(ads),
            )


def test_register_evidence_size_cap(tmp_path):
    path = _init_state(tmp_path)
    src = _src(tmp_path, b"x" * 2048)
    policy = _policy(per_kind_max_bytes={k: 1024 for k in model.EVIDENCE_KINDS})
    with store.locked_state_transaction(path) as tx:
        with pytest.raises(store.StoreError) as e:
            store.register_evidence(
                tx,
                src,
                alias="doc",
                kind="authority",
                snapshot_epoch=1,
                snapshot_fingerprint="f" * 64,
                policy=policy,
                context=_ctx(src),
            )
        assert e.value.code in ("size-cap", "unsafe-source")


def test_register_evidence_stale_epoch_rejected(tmp_path):
    path = _init_state(tmp_path)
    src = _src(tmp_path)
    with store.locked_state_transaction(path) as tx:
        tx.candidate["snapshot"] = _snapshot(epoch=2)
        with pytest.raises(store.StoreError) as e:
            store.register_evidence(
                tx,
                src,
                alias="doc",
                kind="authority",
                snapshot_epoch=1,
                snapshot_fingerprint="f" * 64,
                policy=_policy(),
                context=_ctx(src),
            )
        assert e.value.code == "stale-epoch"


def test_register_evidence_dedupes_content_distinct_bindings(tmp_path):
    path = _init_state(tmp_path)
    src = _src(tmp_path, b"same-bytes")
    with store.locked_state_transaction(path) as tx:
        tx.candidate["snapshot"] = _snapshot()
        tx.candidate["stage"] = "authority"
        ctx = _ctx(src)
        r1 = store.register_evidence(
            tx,
            src,
            alias="a",
            kind="authority",
            snapshot_epoch=1,
            snapshot_fingerprint=_snapshot()["fingerprint"],
            policy=_policy(),
            context=ctx,
        )
        r2 = store.register_evidence(
            tx,
            src,
            alias="b",
            kind="finding-proof",
            snapshot_epoch=1,
            snapshot_fingerprint=_snapshot()["fingerprint"],
            policy=_policy(),
            context=ctx,
        )
        store.append_history(tx.candidate, event="ev", data_sha256="d" * 64)
        tx.commit()
    state = store.load_state(path)
    assert r1.content_id == r2.content_id
    assert r1.evidence_id != r2.evidence_id
    assert len(state["content_objects"]) == 1


def test_snapshot_registration_only_in_allowed_actions(tmp_path):
    path = _init_state(tmp_path)
    src = _src(tmp_path)
    with store.locked_state_transaction(path) as tx:
        with pytest.raises(store.StoreError) as e:
            store.register_evidence(
                tx,
                src,
                alias="snap",
                kind="snapshot",
                snapshot_epoch=1,
                snapshot_fingerprint="f" * 64,
                policy=_policy(),
                context=_ctx(src, action="plan-coverage"),
            )
        assert e.value.code == "policy-mismatch"


def test_snapshot_registration_uses_candidate_snapshot(tmp_path):
    path = _init_state(tmp_path)
    src = _src(tmp_path)
    cand = _snapshot(epoch=2)
    with store.locked_state_transaction(path) as tx:
        tx.candidate["snapshot"] = cand
        tx.candidate["stage"] = "authority"
        reg = store.register_evidence(
            tx,
            src,
            alias="snap",
            kind="snapshot",
            snapshot_epoch=99,  # overridden by candidate snapshot
            snapshot_fingerprint="0" * 64,
            policy=_policy(),
            context=_ctx(src, action="enter-fixing", snapshot=cand),
        )
        store.append_history(tx.candidate, event="ev", data_sha256="d" * 64)
        tx.commit()
    state = store.load_state(path)
    assert state["evidence"][reg.evidence_id]["snapshot_epoch"] == 2


def test_register_evidence_batch_rejects_duplicate_aliases(tmp_path):
    path = _init_state(tmp_path)
    src = _src(tmp_path)
    with store.locked_state_transaction(path) as tx:
        tx.candidate["snapshot"] = _snapshot()
        reqs = (
            store.EvidenceRequest("a", "authority", src, 1, "f" * 64),
            store.EvidenceRequest("a", "check-output", src, 1, "f" * 64),
        )
        with pytest.raises(store.StoreError) as e:
            store.register_evidence_batch(tx, reqs, policy=_policy(), context=_ctx(src))
        assert e.value.code == "duplicate-alias"


def test_resolve_evidence_aliases_rejects_unresolved(tmp_path):
    with pytest.raises(store.StoreError) as e:
        store.resolve_evidence_aliases({"evidence_id": "@nope"}, {})
    assert e.value.code == "unresolved-alias"


def test_resolve_evidence_aliases_rejects_unused(tmp_path):
    reg = store.EvidenceRegistration("sha256:" + "a" * 64, "evidence:x", 3, "a" * 64)
    with pytest.raises(store.StoreError) as e:
        store.resolve_evidence_aliases({"note": "plain"}, {"a": reg})
    assert e.value.code == "unused-alias"


def test_resolve_evidence_aliases_rewrites(tmp_path):
    reg = store.EvidenceRegistration("sha256:" + "a" * 64, "evidence:x", 3, "a" * 64)
    out = store.resolve_evidence_aliases({"evidence_id": "@a", "list": ["@a"]}, {"a": reg})
    assert out["evidence_id"] == "evidence:x"
    assert out["list"] == ["evidence:x"]


def test_verify_evidence_files_detects_drift(tmp_path):
    path = _init_state(tmp_path)
    src = _src(tmp_path)
    with store.locked_state_transaction(path) as tx:
        tx.candidate["snapshot"] = _snapshot()
        tx.candidate["stage"] = "authority"
        reg = store.register_evidence(
            tx,
            src,
            alias="a",
            kind="authority",
            snapshot_epoch=1,
            snapshot_fingerprint=_snapshot()["fingerprint"],
            policy=_policy(),
            context=_ctx(src),
        )
        store.append_history(tx.candidate, event="ev", data_sha256="d" * 64)
        tx.commit()
    state = store.load_state(path)
    store.verify_evidence_files(state, (reg.evidence_id,))
    target = Path(state["content_objects"][reg.content_id]["path"])
    target.write_bytes(b"tampered")
    with pytest.raises(store.StoreError) as e:
        store.verify_evidence_files(state, (reg.evidence_id,))
    assert e.value.code == "content-drift"


def test_append_history_chains(tmp_path):
    state = make_empty_v2_state(tmp_path, generation=1)
    store.append_history(state, event="a", data_sha256="1" * 64)
    store.append_history(state, event="b", data_sha256="2" * 64)
    h = state["history"]
    assert h[0]["sequence"] == 1 and h[1]["sequence"] == 2
    assert h[1]["previous_record_sha256"] == h[0]["record_sha256"]
    assert h[1]["record_sha256"] == model.sha256_json(model.history_record_subject(h[1]))


@pytest.mark.skipif(sys.platform != "win32", reason="Windows handle API")
def test_verify_handle_identity_api_failure_is_unsafe_source(tmp_path, monkeypatch):
    import ctypes

    source = tmp_path / "ev.bin"
    source.write_bytes(b"x")
    fd = os.open(str(source), os.O_RDONLY | getattr(os, "O_BINARY", 0))
    try:
        monkeypatch.setattr(ctypes.windll.kernel32, "GetFinalPathNameByHandleW", lambda *a: 0)
        with pytest.raises(store.UnsafeEvidenceSourceError, match="GetFinalPathNameByHandleW"):
            store._verify_handle_identity(fd, source)
    finally:
        os.close(fd)


@pytest.mark.skipif(sys.platform != "win32", reason="needs a host without /proc/self/fd")
def test_verify_handle_identity_unavailable_platform_fails_closed(tmp_path, monkeypatch):
    source = tmp_path / "ev.bin"
    source.write_bytes(b"x")
    fd = os.open(str(source), os.O_RDONLY | getattr(os, "O_BINARY", 0))
    try:
        monkeypatch.setattr(sys, "platform", "freebsd")
        with pytest.raises(store.UnsafeEvidenceSourceError, match="unavailable on this platform"):
            store._verify_handle_identity(fd, source)
    finally:
        os.close(fd)
