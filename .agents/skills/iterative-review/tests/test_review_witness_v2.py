#!/usr/bin/env python3
"""Tests for the chained witness log and transcript witness verifier."""

from __future__ import annotations

import json
import os
import stat
import sys
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR))
sys.path.insert(0, str(TESTS_DIR.parent / "scripts"))

from review_core import model, policy, witness_log  # noqa: E402


def _transcript_records(session_id: str, tool_use_id: str, response_text: str) -> list[dict]:
    """Real hook-record shape: hook_event_name + object tool_response."""
    return [
        {
            "hook_event_name": "PreToolUse",
            "session_id": session_id,
            "prompt_id": "p-1",
            "tool_name": "exec",
            "tool_use_id": tool_use_id,
            "tool_input": {"command": "py -3 reviewctl.py enumerate --state s"},
        },
        {
            "hook_event_name": "PostToolUse",
            "session_id": session_id,
            "prompt_id": "p-1",
            "tool_name": "exec",
            "tool_use_id": tool_use_id,
            "tool_input": {"command": "py -3 reviewctl.py enumerate --state s"},
            "tool_response": {"success": True, "output": response_text, "error": None},
        },
    ]


def _write_transcript(path: Path, records: list[dict]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        "".join(json.dumps(r, separators=(",", ":")) + "\n" for r in records),
        encoding="utf-8",
    )


def _entry(seq: int, prev: str, **kw) -> dict:
    e = {
        "schema_version": witness_log.WITNESS_LOG_SCHEMA_VERSION,
        "seq": seq,
        "recorded_at": "2026-09-12T00:00:00Z",
        "session_id": kw.get("session_id", "sess-1"),
        "tool_use_id": kw.get("tool_use_id"),
        "record_kind": kw.get("record_kind", "marker"),
        "payload": kw.get("payload", {"note": f"entry-{seq}"}),
    }
    e["payload_sha256"] = model.sha256_hex(model.canonical_json(e["payload"]))
    e["prev_sha256"] = prev
    e["entry_sha256"] = model.sha256_hex(model.canonical_json(e))
    return e


def _write_log(path: Path, entries: list[dict]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        "".join(json.dumps(e, separators=(",", ":")) + "\n" for e in entries),
        encoding="utf-8",
    )


class TestWitnessLog:
    def test_append_and_verify_chain_roundtrip(self, tmp_path):
        log = witness_log.WitnessLog(tmp_path / "w" / "log.jsonl")
        p0 = log.append(session_id="s1", tool_use_id="tu-1", record_kind="PreToolUse", payload={"a": 1})
        p1 = log.append(session_id="s1", tool_use_id="tu-1", record_kind="PostToolUse", payload={"b": 2})
        assert (p0, p1) == (0, 1)
        ok, err = log.verify_chain()
        assert ok and err is None
        assert len(log.entries()) == 2
        assert log.chain_head() == log.entries()[-1]["entry_sha256"]

    def test_append_detects_external_append_between_calls(self, tmp_path):
        # A second writer appending between two appends on the same instance
        # must not silently produce a broken chain: the cached tail is
        # invalidated by a file-stamp check and the append re-verifies.
        path = tmp_path / "w" / "log.jsonl"
        log1 = witness_log.WitnessLog(path)
        log1.append(session_id="s", tool_use_id="t1", record_kind="marker", payload={"a": 1})
        log2 = witness_log.WitnessLog(path)
        log2.append(session_id="s", tool_use_id="t2", record_kind="marker", payload={"b": 2})
        seq = log1.append(session_id="s", tool_use_id="t3", record_kind="marker", payload={"c": 3})
        assert seq == 2
        ok, err = log1.verify_chain()
        assert ok, err
        entries = log1.entries()
        assert [e["seq"] for e in entries] == [0, 1, 2]

    def test_append_surrogate_payload_roundtrips(self, tmp_path):
        # Hook recorders preserve non-UTF-8 bytes via surrogateescape; the log
        # must store and re-verify them without crashing.
        log = witness_log.WitnessLog(tmp_path / "w" / "log.jsonl")
        payload = {"tool_response": {"output": "caf" + chr(0xDCFF) + " raw"}}
        log.append(session_id="s1", tool_use_id="tu-1", record_kind="PostToolUse", payload=payload)
        ok, err = log.verify_chain()
        assert ok and err is None
        assert log.entries()[0]["payload"]["tool_response"]["output"] == "caf" + chr(0xDCFF) + " raw"

    @pytest.mark.skipif(sys.platform == "win32", reason="POSIX mode bits do not apply on Windows")
    def test_creates_with_private_permissions(self, tmp_path):
        p = tmp_path / "w" / "log.jsonl"
        witness_log.WitnessLog(p)
        assert stat.S_IMODE(os.stat(p.parent).st_mode) & 0o777 == 0o700
        assert stat.S_IMODE(os.stat(p).st_mode) & 0o777 == 0o600

    def test_verify_chain_detects_tampered_middle_entry(self, tmp_path):
        p = tmp_path / "log.jsonl"
        e0 = _entry(0, "0" * 64)
        e1 = _entry(1, e0["entry_sha256"], payload={"note": "real"})
        e2 = _entry(2, e1["entry_sha256"])
        e1["payload"]["note"] = "forged"  # tamper without rehashing
        _write_log(p, [e0, e1, e2])
        ok, err = witness_log.WitnessLog(p).verify_chain()
        assert not ok and "tamper" in err

    def test_verify_chain_detects_dropped_tail_entry(self, tmp_path):
        # A log ending mid-chain is only detectable against a known head;
        # here dropping the middle entry breaks the prev link instead.
        p = tmp_path / "log.jsonl"
        e0 = _entry(0, "0" * 64)
        e1 = _entry(1, e0["entry_sha256"])
        e2 = _entry(2, e1["entry_sha256"])
        _write_log(p, [e0, e2])  # e1 dropped
        ok, err = witness_log.WitnessLog(p).verify_chain()
        assert not ok and err is not None

    def test_append_rejects_bad_record_kind(self, tmp_path):
        log = witness_log.WitnessLog(tmp_path / "log.jsonl")
        with pytest.raises(model.StateValidationError):
            log.append(session_id="s", tool_use_id=None, record_kind="bogus", payload={})

    @pytest.mark.skipif(sys.platform == "win32", reason="POSIX mode bits do not apply on Windows")
    def test_open_refuses_world_writable_existing_file(self, tmp_path):
        p = tmp_path / "log.jsonl"
        p.write_text("", encoding="utf-8")
        os.chmod(p, 0o666)
        with pytest.raises(witness_log.WitnessLogError):
            witness_log.WitnessLog(p)


class TestTranscriptIngest:
    def test_ingest_binds_session_tool_use_and_marker(self, tmp_path):
        tdir = tmp_path / "transcripts"
        tp = tdir / "sess-1.jsonl"
        other = _transcript_records("sess-1", "tu-other", "irrelevant output")
        target = _transcript_records("sess-1", "tu-enum", "enumeration-id: abc123\n")
        _write_transcript(tp, other + target)
        log = witness_log.WitnessLog(tmp_path / "w" / "log.jsonl")
        positions, head, trange = witness_log.ingest_transcript_segment(
            log,
            transcript_path=tp,
            session_id="sess-1",
            tool_use_id="tu-enum",
            marker="enumeration-id: abc123",
        )
        assert positions == [0, 1]
        assert head == log.entries()[-1]["entry_sha256"]
        assert trange["session_id"] == "sess-1"
        # first/last record are transcript line indices; target sits at 2-3
        assert trange["first_record"] == 2 and trange["last_record"] == 3
        seg = model.canonical_json(target[0]) + model.canonical_json(target[1])
        assert trange["transcript_sha256"] == model.sha256_hex(seg)
        assert len(log.entries()) == 2

    def test_ingest_transcript_unreadable_is_tampered(self, tmp_path, monkeypatch):
        tp = tmp_path / "t" / "s.jsonl"
        _write_transcript(tp, _transcript_records("sess-1", "tu-1", "enumeration-id: abc"))
        log = witness_log.WitnessLog(tmp_path / "w" / "log.jsonl")
        real_read = Path.read_bytes

        def blocked(self, *a, **k):
            if self == tp:
                raise PermissionError("locked")
            return real_read(self, *a, **k)

        monkeypatch.setattr(Path, "read_bytes", blocked)
        with pytest.raises(policy.WitnessVerificationError, match="tampered-source"):
            witness_log.ingest_transcript_segment(
                log,
                transcript_path=tp,
                session_id="sess-1",
                tool_use_id="tu-1",
                marker="enumeration-id: abc",
            )

    def test_ingest_transcript_invalid_path_type_is_tampered(self, tmp_path):
        log = witness_log.WitnessLog(tmp_path / "w" / "log.jsonl")
        with pytest.raises(policy.WitnessVerificationError, match="tampered-source"):
            witness_log.ingest_transcript_segment(
                log,
                transcript_path=12345,
                session_id="sess-1",
                tool_use_id="tu-1",
                marker="enumeration-id: abc",
            )

    def test_chain_head_missing_log_is_witness_error(self, tmp_path):
        # A witness log deleted after construction is tamper evidence, not
        # a raw io-error.
        log = witness_log.WitnessLog(tmp_path / "w" / "log.jsonl")
        (tmp_path / "w" / "log.jsonl").unlink()
        with pytest.raises(witness_log.WitnessLogError, match="tampered-source"):
            log.chain_head()

    def test_ingest_missing_post_record_fails(self, tmp_path):
        tp = tmp_path / "t" / "s.jsonl"
        _write_transcript(tp, _transcript_records("sess-1", "tu-1", "x")[:1])
        log = witness_log.WitnessLog(tmp_path / "w" / "log.jsonl")
        with pytest.raises(policy.WitnessVerificationError):
            witness_log.ingest_transcript_segment(
                log,
                transcript_path=tp,
                session_id="sess-1",
                tool_use_id="tu-1",
                marker="x",
            )

    def test_ingest_marker_absent_fails(self, tmp_path):
        tp = tmp_path / "t" / "s.jsonl"
        _write_transcript(tp, _transcript_records("sess-1", "tu-1", "no marker here"))
        log = witness_log.WitnessLog(tmp_path / "w" / "log.jsonl")
        with pytest.raises(policy.WitnessVerificationError):
            witness_log.ingest_transcript_segment(
                log,
                transcript_path=tp,
                session_id="sess-1",
                tool_use_id="tu-1",
                marker="enumeration-id: zzz",
            )

    def test_ingest_wrong_session_fails(self, tmp_path):
        tp = tmp_path / "t" / "s.jsonl"
        _write_transcript(tp, _transcript_records("sess-1", "tu-1", "enumeration-id: abc"))
        log = witness_log.WitnessLog(tmp_path / "w" / "log.jsonl")
        with pytest.raises(policy.WitnessVerificationError):
            witness_log.ingest_transcript_segment(
                log,
                transcript_path=tp,
                session_id="sess-2",
                tool_use_id="tu-1",
                marker="enumeration-id: abc",
            )


class TestTranscriptWitnessPolicy:
    def test_permits_local_kind_under_witness_root(self, tmp_path):
        wroot = tmp_path / "w"
        pol = witness_log.TranscriptWitnessPolicy(transcript_root=tmp_path / "t", witness_root=wroot)
        assert pol.permits(
            kind="authority-discovery",
            source="hook-transcript",
            locator=str(wroot / "log.jsonl"),
        )

    def test_denies_locator_traversal_outside_root(self, tmp_path):
        pol = witness_log.TranscriptWitnessPolicy(transcript_root=tmp_path / "t", witness_root=tmp_path / "w")
        assert not pol.permits(
            kind="authority-discovery",
            source="hook-transcript",
            locator=str(tmp_path / "elsewhere" / "log.jsonl"),
        )

    def test_denies_remote_source_for_local_kind(self, tmp_path):
        pol = witness_log.TranscriptWitnessPolicy(transcript_root=tmp_path / "t", witness_root=tmp_path / "w")
        assert not pol.permits(
            kind="authority-discovery",
            source="github-remote",
            locator="github://o/r/x",
        )

    def test_denies_unknown_kind(self, tmp_path):
        pol = witness_log.TranscriptWitnessPolicy(transcript_root=tmp_path / "t", witness_root=tmp_path / "w")
        assert not pol.permits(
            kind="nonsense",
            source="hook-transcript",
            locator=str(tmp_path / "w" / "l.jsonl"),
        )

    def test_sha256_stable_for_same_roots(self, tmp_path):
        kw = dict(transcript_root=tmp_path / "t", witness_root=tmp_path / "w")
        a = witness_log.TranscriptWitnessPolicy(**kw)
        b = witness_log.TranscriptWitnessPolicy(**kw)
        assert a.sha256 == b.sha256
        c = witness_log.TranscriptWitnessPolicy(transcript_root=tmp_path / "t2", witness_root=tmp_path / "w")
        assert a.sha256 != c.sha256


class TestTranscriptWitnessVerifier:
    def _setup(self, tmp_path):
        """Build a real log, ingest a fake enumerate session, mint a record."""
        wroot = tmp_path / "w"
        troot = tmp_path / "t"
        pol = witness_log.TranscriptWitnessPolicy(transcript_root=troot, witness_root=wroot)
        verifier = witness_log.TranscriptWitnessVerifier(pol, witness_root=wroot, review_id="review-1")
        subject = {
            "snapshot_subject": {"epoch": 1},
            "snapshot_fingerprint": "f" * 64,
            "authority_manifest_sha256": "a" * 64,
        }
        subject_bytes = model.canonical_json(subject)
        subj_sha = model.sha256_hex(subject_bytes)
        tp = troot / "sess-1.jsonl"
        _write_transcript(tp, _transcript_records("sess-1", "tu-enum", f"enumeration-id: {subj_sha}\n"))
        log = witness_log.WitnessLog(wroot / "log.jsonl")
        positions, head, trange = witness_log.ingest_transcript_segment(
            log,
            transcript_path=tp,
            session_id="sess-1",
            tool_use_id="tu-enum",
            marker=f"enumeration-id: {subj_sha}",
        )
        record = {
            "kind": "authority-discovery",
            "subject_sha256": subj_sha,
            "tool_use_id": "tu-enum",
            "agent_id": None,
            "transcript_range": trange,
            "record_positions": positions,
            "chain_head_at_record": head,
            "source_locator": str(wroot / "log.jsonl"),
            "snapshot_epoch": 1,
            "snapshot_fingerprint": "f" * 64,
        }
        return verifier, record, subject_bytes

    def _verify(self, verifier, record, subject_bytes, **over):
        kw = dict(
            stored_record_bytes=model.canonical_json(record),
            expected_kind="authority-discovery",
            expected_review_id="review-1",
            expected_dispatch_id=None,
            expected_snapshot_epoch=1,
            expected_snapshot_fingerprint="f" * 64,
            expected_subject=subject_bytes,
            expected_tool_use_id=None,
            expected_agent_id=None,
        )
        kw.update(over)
        return verifier.verify(**kw)

    def test_verify_accepts_well_formed_record(self, tmp_path):
        verifier, record, subject = self._setup(tmp_path)
        vw = self._verify(verifier, record, subject)
        assert vw.subject_sha256 == record["subject_sha256"]
        assert vw.kind == "authority-discovery"

    def test_verify_rejects_subject_digest_mismatch(self, tmp_path):
        verifier, record, subject = self._setup(tmp_path)
        record["subject_sha256"] = "0" * 64
        with pytest.raises(policy.WitnessVerificationError):
            self._verify(verifier, record, subject)

    def test_verify_rejects_epoch_mismatch(self, tmp_path):
        verifier, record, subject = self._setup(tmp_path)
        record["snapshot_epoch"] = 2
        with pytest.raises(policy.WitnessVerificationError):
            self._verify(verifier, record, subject)

    def test_verify_rejects_positions_not_in_log(self, tmp_path):
        verifier, record, subject = self._setup(tmp_path)
        record["record_positions"] = [9, 10]
        with pytest.raises(policy.WitnessVerificationError):
            self._verify(verifier, record, subject)

    def test_verify_rejects_chain_head_divergence(self, tmp_path):
        verifier, record, subject = self._setup(tmp_path)
        record["chain_head_at_record"] = "0" * 64
        with pytest.raises(policy.WitnessVerificationError):
            self._verify(verifier, record, subject)

    def test_verify_rejects_tool_use_mismatch(self, tmp_path):
        verifier, record, subject = self._setup(tmp_path)
        with pytest.raises(policy.WitnessVerificationError):
            self._verify(verifier, record, subject, expected_tool_use_id="tu-other")

    def test_verify_rejects_unpermitted_locator(self, tmp_path):
        verifier, record, subject = self._setup(tmp_path)
        record["source_locator"] = str(tmp_path / "elsewhere" / "log.jsonl")
        with pytest.raises(policy.WitnessVerificationError):
            self._verify(verifier, record, subject)

    def test_verify_rejects_segment_not_containing_subject(self, tmp_path):
        # The post record must contain the enumeration-id marker equal to the
        # claimed subject digest - a segment witnessing a different
        # enumeration fails even with intact chain bookkeeping.
        verifier, record, subject = self._setup(tmp_path)
        wroot = Path(record["source_locator"]).parent
        tp = wroot.parent / "t" / "sess-2.jsonl"
        _write_transcript(tp, _transcript_records("sess-2", "tu-e2", "enumeration-id: " + "0" * 64))
        log = witness_log.WitnessLog(wroot / "log2.jsonl")
        positions, head, trange = witness_log.ingest_transcript_segment(
            log,
            transcript_path=tp,
            session_id="sess-2",
            tool_use_id="tu-e2",
            marker="enumeration-id:",
        )
        record.update(
            record_positions=positions,
            chain_head_at_record=head,
            transcript_range=trange,
            source_locator=str(wroot / "log2.jsonl"),
        )
        with pytest.raises(policy.WitnessVerificationError):
            self._verify(verifier, record, subject)

    def test_verify_remote_kind_admitted_without_log(self, tmp_path):
        wroot = tmp_path / "w"
        pol = witness_log.TranscriptWitnessPolicy(transcript_root=tmp_path / "t", witness_root=wroot)
        verifier = witness_log.TranscriptWitnessVerifier(pol, witness_root=wroot, review_id="review-1")
        subject = model.canonical_json({"obs": "remote"})
        record = {
            "kind": "remote-observation",
            "subject_sha256": model.sha256_hex(subject),
            "tool_use_id": None,
            "agent_id": None,
            "transcript_range": None,
            "record_positions": [],
            "chain_head_at_record": "0" * 64,
            "source_locator": "github://o/r/pull/7",
            "snapshot_epoch": 1,
            "snapshot_fingerprint": "f" * 64,
        }
        vw = verifier.verify(
            stored_record_bytes=model.canonical_json(record),
            expected_kind="remote-observation",
            expected_review_id="review-1",
            expected_dispatch_id=None,
            expected_snapshot_epoch=1,
            expected_snapshot_fingerprint="f" * 64,
            expected_subject=subject,
            expected_tool_use_id=None,
            expected_agent_id=None,
        )
        assert vw.kind == "remote-observation"

    def test_verify_rejects_bound_entry_missing_session_id(self, tmp_path):
        # A chain-valid entry crafted without session_id must fail as a
        # witness-mismatch, not escape as KeyError.
        verifier, record, subject = self._setup(tmp_path)
        log_path = Path(record["source_locator"])
        log = witness_log.WitnessLog(log_path)
        entries = log.entries()
        prev = witness_log.ZERO_SHA
        rewritten = []
        for e in entries:
            e2 = {k: v for k, v in e.items() if k not in ("prev_sha256", "entry_sha256", "session_id")}
            e2["prev_sha256"] = prev
            e2["entry_sha256"] = witness_log._entry_digest(e2)
            rewritten.append(e2)
            prev = e2["entry_sha256"]
        log_path.write_text(
            "".join(
                json.dumps(e, sort_keys=True, separators=(",", ":"), ensure_ascii=True) + chr(10) for e in rewritten
            ),
            encoding="utf-8",
        )
        record["chain_head_at_record"] = rewritten[record["record_positions"][-1]]["entry_sha256"]
        with pytest.raises(policy.WitnessVerificationError, match="session binding"):
            self._verify(verifier, record, subject)
