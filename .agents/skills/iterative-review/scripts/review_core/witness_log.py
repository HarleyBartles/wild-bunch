#!/usr/bin/env python3
"""Hash-chained witness log and transcript witness verification.

The witness log is an append-only JSONL store. Every entry chains to its
predecessor through ``prev_sha256``/``entry_sha256``, so a tampered, dropped,
or reordered middle entry breaks verification. ``ingest_transcript_segment``
copies the harness-emitted transcript records for one tool call into the log
and returns the ``transcript_range``/``record_positions``/
``chain_head_at_record`` values a witness record binds. ``TranscriptWitnessVerifier``
re-proves those bindings at verification time.
"""

from __future__ import annotations

import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path

from . import model, policy

WITNESS_LOG_SCHEMA_VERSION = 1
RECORD_KINDS = ("PreToolUse", "PostToolUse", "marker")
ZERO_SHA = "0" * 64


class WitnessLogError(Exception):
    """Witness-log integrity or permission failure."""

    def __init__(self, code: str, message: str):
        super().__init__(f"{code}: {message}")
        self.code = code


def _entry_digest(entry: dict) -> str:
    return model.sha256_hex(model.canonical_json({k: v for k, v in entry.items() if k != "entry_sha256"}))


class WitnessLog:
    """sha256-chained append-only JSONL witness store at ``path``."""

    def __init__(self, path: Path, *, posix_directory_mode: int = 0o700, posix_file_mode: int = 0o600):
        self._path = Path(path)
        self._posix_directory_mode = posix_directory_mode
        self._posix_file_mode = posix_file_mode
        parent = self._path.parent
        if not parent.exists():
            parent.mkdir(parents=True)
        if sys.platform != "win32":
            import os
            import stat as _stat

            os.chmod(parent, posix_directory_mode)
            if self._path.exists():
                mode = _stat.S_IMODE(os.stat(self._path).st_mode)
                if mode & 0o077:
                    raise WitnessLogError(
                        "acl-untrusted",
                        f"{self._path} grants group/other access (mode {mode:o})",
                    )
            dir_mode = _stat.S_IMODE(os.stat(parent).st_mode)
            if dir_mode & 0o077:
                raise WitnessLogError(
                    "acl-untrusted",
                    f"{parent} grants group/other access (mode {dir_mode:o})",
                )
        if not self._path.exists():
            self._path.touch()
        if sys.platform != "win32":
            import os

            os.chmod(self._path, posix_file_mode)
        # Open never mutates: a corrupt log stays inspectable so verify_chain
        # can report the tamper; append() refuses on a broken chain.
        # _tail caches the verified (seq, head) so multi-append ingest is O(1)
        # per record instead of re-verifying the whole chain each time. The
        # cache is per-instance; a WitnessLog constructed later re-verifies.
        self._tail: tuple[int, str] | None = None
        self._stamp: tuple[int, int] | None = None

    def _file_stamp(self) -> tuple[int, int] | None:
        try:
            st = self._path.stat()
            return (st.st_mtime_ns, st.st_size)
        except OSError:
            return None

    def _refresh_tail(self) -> None:
        # Bracket the verify with file stamps: a concurrent append during
        # verification must invalidate the result rather than chain a stale
        # tail onto entries verify never saw.
        stamp0 = self._file_stamp()
        ok, err = self.verify_chain()
        if not ok:
            raise WitnessLogError("chain-invalid", f"{self._path}: {err}")
        entries = self._read_entries()
        if stamp0 is None or self._file_stamp() != stamp0:
            raise WitnessLogError("chain-invalid", f"{self._path}: log mutated during verification")
        self._tail = (len(entries), entries[-1]["entry_sha256"] if entries else ZERO_SHA)
        self._stamp = stamp0

    def _read_entries(self) -> list[dict]:
        entries: list[dict] = []
        try:
            raw = self._path.read_bytes()
        except (OSError, ValueError) as exc:
            raise WitnessLogError("tampered-source", f"{self._path}: unreadable: {exc}") from exc
        if not raw:
            return entries
        for i, line in enumerate(raw.decode("utf-8", errors="surrogateescape").splitlines()):
            if not line.strip():
                continue
            rec = model.strict_json_loads(line.encode("utf-8"), source=f"{self._path}:{i + 1}")
            if not isinstance(rec, dict):
                raise WitnessLogError("malformed", f"line {i + 1} is not an object")
            entries.append(rec)
        return entries

    def entries(self) -> list[dict]:
        return self._read_entries()

    def chain_head(self) -> str:
        entries = self._read_entries()
        return entries[-1]["entry_sha256"] if entries else ZERO_SHA

    def append(self, *, session_id: str, tool_use_id: str | None, record_kind: str, payload: dict) -> int:
        if self._tail is None or self._file_stamp() != self._stamp:
            self._refresh_tail()
        if not isinstance(payload, dict):
            raise model.StateValidationError("bad-type", "payload", "witness-log payload must be an object")
        if not session_id or not isinstance(session_id, str):
            raise model.StateValidationError("missing-field", "session_id", "witness-log entry requires session_id")
        if record_kind not in RECORD_KINDS:
            raise model.StateValidationError("bad-value", "record_kind", f"record_kind must be one of {RECORD_KINDS}")
        seq, prev = self._tail
        entry = {
            "schema_version": WITNESS_LOG_SCHEMA_VERSION,
            "seq": seq,
            "recorded_at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
            "session_id": session_id,
            "tool_use_id": tool_use_id,
            "record_kind": record_kind,
            "payload": payload,
        }
        entry["payload_sha256"] = model.sha256_hex(model.canonical_json(payload))
        entry["prev_sha256"] = prev
        entry["entry_sha256"] = _entry_digest(entry)
        # ensure_ascii keeps stored lines valid UTF-8 even when the payload
        # carries surrogateescape'd bytes; digests cover canonical_json
        # fields, not the line serialization.
        line = json.dumps(entry, sort_keys=True, separators=(",", ":"), ensure_ascii=True)
        with self._path.open("ab") as fh:
            fh.write(line.encode("ascii") + b"\n")
            fh.flush()
            import os

            os.fsync(fh.fileno())
        self._tail = (seq + 1, entry["entry_sha256"])
        self._stamp = self._file_stamp()
        return seq

    def verify_chain(self) -> tuple[bool, str | None]:
        try:
            entries = self._read_entries()
        except Exception as exc:  # noqa: BLE001 - report as integrity failure
            return False, f"malformed log: {exc}"
        prev = ZERO_SHA
        for i, e in enumerate(entries):
            if e.get("schema_version") != WITNESS_LOG_SCHEMA_VERSION:
                return False, f"malformed line {i}: bad schema_version"
            if e.get("seq") != i:
                return False, f"gap at seq {i}: entry claims seq {e.get('seq')}"
            if e.get("prev_sha256") != prev:
                return False, f"tamper at seq {i}: prev link broken"
            payload = e.get("payload")
            if not isinstance(payload, dict):
                return False, f"malformed line {i}: payload not an object"
            if e.get("payload_sha256") != model.sha256_hex(model.canonical_json(payload)):
                return False, f"tamper at seq {i}: payload digest mismatch"
            if e.get("entry_sha256") != _entry_digest(e):
                return False, f"tamper at seq {i}: entry digest mismatch"
            prev = e["entry_sha256"]
        return True, None


def ingest_transcript_segment(
    log: WitnessLog,
    *,
    transcript_path: Path,
    session_id: str,
    tool_use_id: str,
    marker: str,
) -> tuple[list[int], str, dict]:
    """Append the transcript records witnessing one tool call.

    Selects the Pre/Post records in ``transcript_path`` (session-keyed JSONL
    hook transcript) matching ``session_id``/``tool_use_id``, requires the
    marker string inside the Post ``tool_response``, appends them to ``log``,
    and returns ``(record_positions, chain_head_at_record, transcript_range)``.
    """
    try:
        transcript_path = Path(transcript_path)
        raw_lines = transcript_path.read_bytes().decode("utf-8", errors="surrogateescape").splitlines()
    except FileNotFoundError:
        raise policy.WitnessVerificationError("missing-source", f"no transcript at {transcript_path}") from None
    except (OSError, ValueError, TypeError) as exc:
        raise policy.WitnessVerificationError("tampered-source", f"transcript unreadable: {exc}") from exc
    matched: list[tuple[int, dict]] = []
    for i, line in enumerate(raw_lines):
        if not line.strip():
            continue
        try:
            rec = model.strict_json_loads(line.encode("utf-8"), source=f"{transcript_path}:{i + 1}")
        except Exception:
            continue
        if not isinstance(rec, dict):
            continue
        if rec.get("session_id") == session_id and rec.get("tool_use_id") == tool_use_id:
            matched.append((i, rec))
    if not matched:
        raise policy.WitnessVerificationError(
            "missing-source",
            f"no transcript records for session {session_id!r} tool_use {tool_use_id!r}",
        )
    kinds = {r.get("hook_event_name") for _, r in matched}
    if "PreToolUse" not in kinds or "PostToolUse" not in kinds:
        raise policy.WitnessVerificationError("missing-source", "transcript segment lacks a complete Pre/Post pair")
    post = next(r for _, r in matched if r.get("hook_event_name") == "PostToolUse")
    response = post.get("tool_response")
    response_text = (
        response
        if isinstance(response, str)
        else (
            response.get("output", "")
            if isinstance(response, dict)
            else model.canonical_json(response or {}).decode("utf-8", errors="surrogateescape")
        )
    )
    if marker not in response_text:
        raise policy.WitnessVerificationError("missing-source", "post record does not carry the enumeration marker")
    positions: list[int] = []
    for _idx, rec in matched:
        positions.append(
            log.append(
                session_id=session_id,
                tool_use_id=tool_use_id,
                record_kind=rec["hook_event_name"] if rec.get("hook_event_name") in RECORD_KINDS else "marker",
                payload=rec,
            )
        )
    head = log.chain_head()
    segment_bytes = b"".join(model.canonical_json(r) for _, r in matched)
    transcript_range = {
        "session_id": session_id,
        "transcript_sha256": model.sha256_hex(segment_bytes),
        "first_record": matched[0][0],
        "last_record": matched[-1][0],
    }
    return positions, head, transcript_range


class TranscriptWitnessPolicy:
    """WitnessPolicy admitting hook-transcript locators for local kinds and
    github-remote locators for remote kinds."""

    POLICY_ID = "review-core-transcript-witness"
    POLICY_VERSION = "1"

    def __init__(self, *, transcript_root: Path, witness_root: Path):
        self._transcript_root = Path(transcript_root).resolve()
        self._witness_root = Path(witness_root).resolve()
        doc = {
            "policy_id": self.POLICY_ID,
            "version": self.POLICY_VERSION,
            "transcript_root_sha256": model.sha256_hex(str(self._transcript_root).encode("utf-8")),
            "witness_root_sha256": model.sha256_hex(str(self._witness_root).encode("utf-8")),
        }
        self._sha = model.sha256_hex(model.canonical_json(doc))
        srcs = [
            policy.WitnessSource(kind=k, source="hook-transcript", locator_prefix=str(self._witness_root))
            for k in model.LOCAL_WITNESS_KINDS
        ]
        srcs += [
            policy.WitnessSource(kind=k, source="github-remote", locator_prefix="github://")
            for k in model.REMOTE_WITNESS_KINDS
        ]
        self._sources = tuple(srcs)

    @property
    def sha256(self) -> str:
        return self._sha

    @property
    def sources(self) -> tuple:
        return self._sources

    def permits(self, *, kind: str, source: str, locator: str) -> bool:
        if kind in model.LOCAL_WITNESS_KINDS:
            if source != "hook-transcript":
                return False
            try:
                resolved = Path(locator).resolve()
            except (OSError, ValueError):
                return False
            try:
                resolved.relative_to(self._witness_root)
            except ValueError:
                return False
            return True
        if kind in model.REMOTE_WITNESS_KINDS:
            if source != "github-remote":
                return False
            return re.match(r"^github://[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+/", locator) is not None
        return False


class TranscriptWitnessVerifier:
    """WitnessVerifier that re-proves a record's bindings against the chained
    witness log."""

    def __init__(self, policy_obj: TranscriptWitnessPolicy, *, witness_root: Path, review_id: str):
        self._policy = policy_obj
        self._witness_root = Path(witness_root).resolve()
        self._review_id = review_id

    @property
    def policy(self) -> TranscriptWitnessPolicy:
        return self._policy

    def verify(
        self,
        *,
        stored_record_bytes: bytes,
        expected_kind: str,
        expected_review_id: str,
        expected_dispatch_id: str | None,
        expected_snapshot_epoch: int,
        expected_snapshot_fingerprint: str,
        expected_subject: bytes,
        expected_tool_use_id: str | None,
        expected_agent_id: str | None,
    ) -> policy.VerifiedWitness:
        record = model.strict_json_loads(stored_record_bytes, source="witness-record")
        if record.get("kind") != expected_kind:
            raise policy.WitnessVerificationError("witness-mismatch", "kind mismatch")
        if self._review_id != expected_review_id:
            raise policy.WitnessVerificationError("scope-mismatch", "review mismatch")
        if record.get("snapshot_epoch") != expected_snapshot_epoch:
            raise policy.WitnessVerificationError("scope-mismatch", "epoch mismatch")
        if record.get("snapshot_fingerprint") != expected_snapshot_fingerprint:
            raise policy.WitnessVerificationError("scope-mismatch", "fingerprint mismatch")
        if expected_tool_use_id is not None and record.get("tool_use_id") != expected_tool_use_id:
            raise policy.WitnessVerificationError("witness-mismatch", "tool_use_id mismatch")
        if expected_agent_id is not None and record.get("agent_id") != expected_agent_id:
            raise policy.WitnessVerificationError("witness-mismatch", "agent_id mismatch")
        subject_sha = model.sha256_hex(expected_subject)
        if record.get("subject_sha256") != subject_sha:
            raise policy.WitnessVerificationError("witness-mismatch", "subject digest mismatch")

        source = "github-remote" if expected_kind in model.REMOTE_WITNESS_KINDS else "hook-transcript"
        locator = record.get("source_locator", "")
        if not self._policy.permits(kind=expected_kind, source=source, locator=locator):
            raise policy.WitnessVerificationError("locator-untrusted", "locator not permitted")

        observed_at = datetime.now(timezone.utc)
        if expected_kind in model.REMOTE_WITNESS_KINDS:
            # Remote re-fetch verification is a later-plan deliverable; the
            # policy admits the locator namespace and records it verified at
            # that boundary only.
            return self._verified(record, expected_dispatch_id, observed_at)

        trange = record.get("transcript_range")
        if not isinstance(trange, dict):
            raise policy.WitnessVerificationError("missing-source", "local witness lacks transcript_range")
        log = WitnessLog(Path(locator))
        ok, err = log.verify_chain()
        if not ok:
            raise policy.WitnessVerificationError("tampered-source", f"witness log: {err}")
        entries = log.entries()
        positions = record.get("record_positions") or []
        if not positions or any(not isinstance(p, int) or p < 0 or p >= len(entries) for p in positions):
            raise policy.WitnessVerificationError(
                "missing-source", "record_positions do not resolve inside the witness log"
            )
        bound = [entries[p] for p in positions]
        if any(e.get("session_id") != trange.get("session_id") for e in bound):
            raise policy.WitnessVerificationError("witness-mismatch", "session binding mismatch")
        if record.get("tool_use_id") is not None and any(e.get("tool_use_id") != record["tool_use_id"] for e in bound):
            raise policy.WitnessVerificationError("witness-mismatch", "tool_use binding mismatch")
        segment = b"".join(model.canonical_json(e["payload"]) for e in bound)
        if model.sha256_hex(segment) != trange.get("transcript_sha256"):
            raise policy.WitnessVerificationError("witness-mismatch", "segment digest mismatch")
        if entries[positions[-1]]["entry_sha256"] != record.get("chain_head_at_record"):
            raise policy.WitnessVerificationError("tampered-source", "chain head divergence")
        # The enumeration marker is the claimed subject digest: the witnessed
        # Post record's response must contain it, proving the enumerated
        # subject is what the session actually emitted.
        if expected_kind == "authority-discovery":
            post = next(
                (e["payload"] for e in bound if e["payload"].get("hook_event_name") == "PostToolUse"),
                None,
            )
            response = (post or {}).get("tool_response")
            response_text = (
                response
                if isinstance(response, str)
                else (
                    response.get("output", "")
                    if isinstance(response, dict)
                    else model.canonical_json(response or {}).decode("utf-8", errors="surrogateescape")
                )
            )
            if subject_sha not in response_text:
                raise policy.WitnessVerificationError(
                    "witness-mismatch",
                    "witnessed enumeration did not emit the claimed subject digest",
                )
        return self._verified(record, expected_dispatch_id, observed_at)

    def _verified(self, record: dict, dispatch_id: str | None, observed_at) -> policy.VerifiedWitness:
        return policy.VerifiedWitness(
            witness_id=record.get("witness_id", ""),
            kind=record["kind"],
            source="github-remote" if record["kind"] in model.REMOTE_WITNESS_KINDS else "hook-transcript",
            locator=record.get("source_locator", ""),
            review_id=self._review_id,
            dispatch_id=dispatch_id,
            snapshot_epoch=record["snapshot_epoch"],
            snapshot_fingerprint=record["snapshot_fingerprint"],
            subject_sha256=record["subject_sha256"],
            tool_use_id=record.get("tool_use_id"),
            agent_id=record.get("agent_id"),
            observed_at=observed_at,
            record_sha256=model.sha256_hex(model.canonical_json(record)),
        )
