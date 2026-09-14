#!/usr/bin/env python3
"""Locked state storage and content-addressed evidence for the v2 kernel.

One exclusive lock covers load, evidence ingestion, validation, history
append, generation increment, and atomic replace. State bytes are canonical
JSON written to a sibling temporary file, fsynced, then os.replace()d; a
failed commit never disturbs the last valid persisted state.
"""

from __future__ import annotations

import hashlib
import json
import os
import sys
from contextlib import AbstractContextManager
from dataclasses import dataclass
from pathlib import Path
from typing import Mapping

from . import model

_STATE_FILE = "review-state.json"
_LOCK_FILE = "review-state.lock"
_STORE_SUBDIR = ("evidence-store", "sha256")

_SNAPSHOT_ACTIONS = frozenset({"freeze-review-input", "enter-fixing", "refresh-review-input"})


class StoreError(RuntimeError):
    def __init__(self, code: str, message: str):
        super().__init__(f"{code}: {message}")
        self.code = code


class ConcurrentStateError(StoreError):
    pass


class UnsafeEvidenceSourceError(StoreError):
    pass


@dataclass(frozen=True)
class EvidenceRegistration:
    content_id: str
    evidence_id: str
    bytes: int
    sha256: str


@dataclass(frozen=True)
class EvidenceRequest:
    alias: str
    kind: str
    source: Path
    snapshot_epoch: int
    snapshot_fingerprint: str


@dataclass(frozen=True)
class EvidenceIngestionPolicy:
    source_id: str
    source_version: str
    sha256: str
    per_kind_max_bytes: Mapping[str, int]
    transaction_max_bytes: int
    review_max_bytes: int
    windows_allowed_trustee_sids: tuple
    posix_directory_mode: int
    posix_file_mode: int


@dataclass(frozen=True)
class EvidenceIngestionContext:
    action: str
    candidate_snapshot: dict | None
    eligible_sources: tuple


# ---------------------------------------------------------------------------
# Locking


def _lock_exclusive(fd: int) -> None:
    if sys.platform == "win32":
        import msvcrt

        os.lseek(fd, 0, os.SEEK_SET)
        msvcrt.locking(fd, msvcrt.LK_LOCK, 1)
    else:
        import fcntl

        fcntl.flock(fd, fcntl.LOCK_EX)


def _unlock(fd: int) -> None:
    try:
        if sys.platform == "win32":
            import msvcrt

            os.lseek(fd, 0, os.SEEK_SET)
            msvcrt.locking(fd, msvcrt.LK_UNLCK, 1)
        else:
            import fcntl

            fcntl.flock(fd, fcntl.LOCK_UN)
    except OSError:
        pass


# ---------------------------------------------------------------------------
# Serialization


def _serialize(state: dict) -> bytes:
    return model.canonical_json(state) + b"\n"


def load_state(path: Path) -> dict:
    path = Path(path)
    if not path.is_file():
        raise StoreError("state-missing", f"no state file at {path}")
    raw = path.read_bytes()
    state = model.strict_json_loads(raw, source=str(path))
    if not isinstance(state, dict):
        raise StoreError("state-invalid", f"{path} is not a state object")
    if state.get("schema_version") != model.SCHEMA_VERSION:
        raise StoreError(
            "state-version",
            f"{path} is not a version-{model.SCHEMA_VERSION} state",
        )
    model.validate_state(state)
    return state


def create_state(path: Path, state: dict) -> None:
    path = Path(path)
    model.validate_state(state)
    if state["generation"] != 0:
        raise StoreError("state-invalid", "initial state must have generation 0")
    if path.exists():
        raise StoreError("state-exists", f"state already exists at {path}")
    path.parent.mkdir(parents=True, exist_ok=True)
    blob = _serialize(state)
    tmp = path.with_name(path.name + ".tmp")
    try:
        with open(tmp, "wb") as fh:
            fh.write(blob)
            fh.flush()
            os.fsync(fh.fileno())
        os.replace(tmp, path)
    finally:
        if tmp.exists():
            tmp.unlink()


# ---------------------------------------------------------------------------
# Transaction


class StateTransaction(AbstractContextManager):
    """Exclusive locked compare-and-swap over one state file."""

    def __init__(self, path: Path):
        self._path = Path(path)
        self._lock_path = self._path.with_name(_LOCK_FILE)
        self._lock_fd: int | None = None
        self.prior_generation: int = 0
        self.prior_bytes_sha256: str = ""
        self.candidate: dict | None = None
        self._committed = False

    def __enter__(self) -> "StateTransaction":
        self._path.parent.mkdir(parents=True, exist_ok=True)
        self._lock_path.touch(exist_ok=True)
        self._lock_fd = os.open(str(self._lock_path), os.O_RDWR | os.O_CREAT)
        _lock_exclusive(self._lock_fd)
        state = load_state(self._path)
        prior_raw = self._path.read_bytes()
        self.prior_generation = state["generation"]
        self.prior_bytes_sha256 = hashlib.sha256(prior_raw).hexdigest()
        self.candidate = json.loads(prior_raw.decode("utf-8"))
        self.candidate["generation"] = self.prior_generation + 1
        return self

    def commit(self) -> dict:
        if self._committed:
            raise StoreError("state-invalid", "transaction already committed")
        if self.candidate is None:
            raise StoreError("state-invalid", "no candidate state")
        # Re-read through the held lock; the persisted bytes and generation
        # must be exactly what we captured at entry.
        current_raw = self._path.read_bytes()
        if hashlib.sha256(current_raw).hexdigest() != self.prior_bytes_sha256:
            raise ConcurrentStateError("state-concurrent", "state bytes changed under the transaction")
        current = model.strict_json_loads(current_raw, source=str(self._path))
        if current["generation"] != self.prior_generation:
            raise ConcurrentStateError("state-concurrent", "state generation changed under the transaction")
        if self.candidate["generation"] != self.prior_generation + 1:
            raise StoreError(
                "state-invalid",
                "candidate generation must advance by exactly one",
            )
        model.validate_state(self.candidate)
        blob = _serialize(self.candidate)
        tmp = self._path.with_name(self._path.name + ".tmp")
        try:
            with open(tmp, "wb") as fh:
                fh.write(blob)
                fh.flush()
                os.fsync(fh.fileno())
            os.replace(tmp, self._path)
            if sys.platform != "win32":
                try:
                    dir_fd = os.open(str(self._path.parent), os.O_RDONLY | os.O_DIRECTORY)
                except OSError:
                    dir_fd = None
                if dir_fd is not None:
                    try:
                        os.fsync(dir_fd)
                    finally:
                        os.close(dir_fd)
        except BaseException:
            if tmp.exists():
                tmp.unlink()
            raise
        self._committed = True
        return self.candidate

    def __exit__(self, exc_type, exc, tb) -> None:
        try:
            if self._lock_fd is not None:
                _unlock(self._lock_fd)
                os.close(self._lock_fd)
        finally:
            self._lock_fd = None


def locked_state_transaction(path: Path) -> StateTransaction:
    return StateTransaction(path)


# ---------------------------------------------------------------------------
# Evidence source safety


def _is_unc_or_device(path: Path) -> bool:
    text = str(path)
    return (
        text.startswith("\\\\")
        or text.startswith("//")
        or text.startswith("\\\\?")
        or text.startswith("\\\\.")
        or ":\\?\\" in text
    )


def _has_ads(path: Path) -> bool:
    # Alternate data stream syntax: a ':' beyond the drive prefix.
    name = path.name
    return ":" in name


def _is_reparse(path: Path) -> bool:
    if os.path.islink(path):
        return True
    if sys.platform == "win32":
        try:
            st = os.stat(path, follow_symlinks=False)
            attrs = getattr(st, "st_file_attributes", 0)
            FILE_ATTRIBUTE_REPARSE_POINT = 0x400
            return bool(attrs & FILE_ATTRIBUTE_REPARSE_POINT)
        except OSError:
            return False
    return False


def _reject_unsafe_components(source: Path) -> None:
    if _is_unc_or_device(source):
        raise UnsafeEvidenceSourceError("unsafe-source", f"UNC/device path: {source}")
    if _has_ads(source):
        raise UnsafeEvidenceSourceError("unsafe-source", f"alternate data stream: {source}")
    # Walk components; any symlink/reparse point is rejected.
    probe = source
    parts = []
    while True:
        parts.append(probe)
        parent = probe.parent
        if parent == probe:
            break
        probe = parent
    for component in reversed(parts):
        if component.exists() or os.path.islink(component):
            if _is_reparse(component):
                raise UnsafeEvidenceSourceError("unsafe-source", f"reparse point or symlink in {component}")


def _verify_handle_identity(fd: int, expected: Path) -> None:
    """Final-handle identity check: the opened file must be the allowlisted path."""
    if sys.platform == "win32":
        import ctypes
        import msvcrt
        from ctypes import wintypes

        kernel32 = ctypes.windll.kernel32
        handle = msvcrt.get_osfhandle(fd)
        buf = ctypes.create_unicode_buffer(4096)
        if kernel32.GetFinalPathNameByHandleW(wintypes.HANDLE(handle), buf, 4096, 0) == 0:
            raise UnsafeEvidenceSourceError("unsafe-source", f"GetFinalPathNameByHandleW failed for {expected}")
        final = buf.value
        if final.startswith("\\\\?\\"):
            final = final[4:]
        if os.path.normcase(os.path.normpath(final)) != os.path.normcase(os.path.normpath(str(expected))):
            raise UnsafeEvidenceSourceError("unsafe-source", f"final handle path {final!r} != {expected}")
    else:
        proc_fd = Path("/proc/self/fd")
        if not proc_fd.exists():
            raise UnsafeEvidenceSourceError(
                "unsafe-source",
                "handle identity verification unavailable on this platform",
            )
        resolved = os.readlink(proc_fd / str(fd))
        if os.path.normcase(os.path.normpath(resolved)) != os.path.normcase(os.path.normpath(str(expected))):
            raise UnsafeEvidenceSourceError("unsafe-source", f"final handle path {resolved!r} != {expected}")


def _register_source_bytes(source: Path, cap: int, path: str) -> bytes:
    """Open once, verify identity, bound the read, never reopen."""
    st = os.stat(source, follow_symlinks=False)
    if not os.path.isfile(source):
        raise UnsafeEvidenceSourceError("unsafe-source", f"not a regular file: {source}")
    if st.st_size == 0:
        raise UnsafeEvidenceSourceError("unsafe-source", f"empty source: {source}")
    if st.st_size > cap:
        raise StoreError("size-cap", f"{path} exceeds cap ({st.st_size} > {cap})")
    fd = os.open(str(source), os.O_RDONLY | getattr(os, "O_BINARY", 0))
    try:
        _verify_handle_identity(fd, source)
        st2 = os.fstat(fd)
        if st2.st_size != st.st_size:
            raise UnsafeEvidenceSourceError("content-drift", f"{path} resized between stat and open")
        chunks = []
        remaining = cap
        while remaining > 0:
            chunk = os.read(fd, min(65536, remaining))
            if not chunk:
                break
            chunks.append(chunk)
            remaining -= len(chunk)
        data = b"".join(chunks)
        if len(data) != st.st_size:
            raise UnsafeEvidenceSourceError("content-drift", f"{path} length changed while reading")
        return data
    finally:
        os.close(fd)


def _store_root(state: dict) -> Path:
    return Path(state["scratch_dir"]) / _STORE_SUBDIR[0] / _STORE_SUBDIR[1]


def _store_content(state: dict, data: bytes) -> tuple[str, Path]:
    digest = hashlib.sha256(data).hexdigest()
    cid = "sha256:" + digest
    root = _store_root(state)
    root.mkdir(parents=True, exist_ok=True)
    target = root / digest
    if not target.exists():
        tmp = target.with_name(target.name + ".tmp")
        try:
            with open(tmp, "wb") as fh:
                fh.write(data)
                fh.flush()
                os.fsync(fh.fileno())
            os.replace(tmp, target)
        except BaseException:
            if tmp.exists():
                tmp.unlink()
            raise
    return cid, target


# ---------------------------------------------------------------------------
# Evidence registration


def register_evidence(
    tx: StateTransaction,
    source: Path,
    *,
    alias: str,
    kind: str,
    snapshot_epoch: int,
    snapshot_fingerprint: str,
    policy: EvidenceIngestionPolicy,
    context: EvidenceIngestionContext,
) -> EvidenceRegistration:
    if tx.candidate is None:
        raise StoreError("state-invalid", "transaction has no candidate")
    path = f"evidence[{alias}]"
    source = Path(source)

    if kind not in model.EVIDENCE_KINDS:
        raise StoreError("bad-kind", f"{path}: unknown evidence kind {kind!r}")
    cap = policy.per_kind_max_bytes.get(kind)
    if cap is None or cap <= 0:
        raise StoreError("policy-mismatch", f"no positive cap for kind {kind!r}")
    if policy.transaction_max_bytes <= 0 or policy.review_max_bytes <= 0:
        raise StoreError("policy-mismatch", "transaction/review caps must be positive")
    if sys.platform != "win32":
        if policy.posix_directory_mode & 0o077 or policy.posix_file_mode & 0o077:
            raise UnsafeEvidenceSourceError("acl-untrusted", "POSIX modes must not grant group/other access")
    if sys.platform == "win32" and policy.windows_allowed_trustee_sids:
        # Trustee verification requires win32 security APIs; fail closed when
        # the policy demands them and the runtime cannot check.
        try:
            import win32security  # noqa: F401
        except ImportError:
            raise UnsafeEvidenceSourceError(
                "acl-untrusted",
                "cannot verify Windows trustees without win32security",
            )

    eligible = {os.path.normcase(os.path.normpath(str(Path(p)))) for p in context.eligible_sources}
    if os.path.normcase(os.path.normpath(str(source))) not in eligible:
        raise UnsafeEvidenceSourceError("unsafe-source", f"{source} is outside the action's eligible source set")

    _reject_unsafe_components(source)
    if not source.is_file():
        raise UnsafeEvidenceSourceError("unsafe-source", f"missing source: {source}")

    state = tx.candidate
    existing_bytes = sum(c["bytes"] for c in state["content_objects"].values())
    data = _register_source_bytes(source, min(cap, policy.transaction_max_bytes), path)
    if existing_bytes + len(data) > policy.review_max_bytes:
        raise StoreError("size-cap", f"{path} exceeds the per-review cap")

    epoch, fp = (None, None)
    if state["snapshot"] is not None:
        epoch = state["snapshot"]["epoch"]
        fp = state["snapshot"]["fingerprint"]
    # During a snapshot action the persisted snapshot is still the previous
    # epoch (or absent at freeze): non-snapshot evidence must bind the
    # candidate snapshot this transaction installs.
    if context.action in _SNAPSHOT_ACTIONS and context.candidate_snapshot is not None:
        epoch = context.candidate_snapshot["epoch"]
        fp = context.candidate_snapshot["fingerprint"]
    if kind == "snapshot":
        if context.action not in _SNAPSHOT_ACTIONS:
            raise StoreError(
                "policy-mismatch",
                f"snapshot evidence is not ingestible during action {context.action!r}",
            )
        if context.candidate_snapshot is None:
            raise StoreError("policy-mismatch", "no candidate snapshot bound to this transaction")
        snapshot_epoch = context.candidate_snapshot["epoch"]
        snapshot_fingerprint = context.candidate_snapshot["fingerprint"]
    else:
        if epoch is None or snapshot_epoch != epoch:
            raise StoreError("stale-epoch", f"{path}: epoch {snapshot_epoch} is not current {epoch}")
        if fp is not None and snapshot_fingerprint != fp:
            raise StoreError("stale-fingerprint", f"{path}: fingerprint does not match the snapshot")

    cid, target = _store_content(state, data)
    if cid not in state["content_objects"]:
        state["content_objects"][cid] = {
            "content_id": cid,
            "path": str(target),
            "sha256": hashlib.sha256(data).hexdigest(),
            "bytes": len(data),
        }
    binding = {
        "evidence_id": "",
        "content_id": cid,
        "kind": kind,
        "snapshot_epoch": snapshot_epoch,
        "snapshot_fingerprint": snapshot_fingerprint,
    }
    binding["evidence_id"] = "evidence:" + model.sha256_json(model.evidence_binding_subject(binding))
    state["evidence"][binding["evidence_id"]] = binding
    return EvidenceRegistration(
        content_id=cid,
        evidence_id=binding["evidence_id"],
        bytes=len(data),
        sha256=binding["content_id"].split(":", 1)[1],
    )


def register_evidence_batch(
    tx: StateTransaction,
    requests: tuple,
    *,
    policy: EvidenceIngestionPolicy,
    context: EvidenceIngestionContext,
) -> dict:
    aliases = [r.alias for r in requests]
    if len(set(aliases)) != len(aliases):
        raise StoreError("duplicate-alias", "batch contains duplicate aliases")
    registrations = {}
    for request in requests:
        if request.kind not in model.EVIDENCE_KINDS:
            raise StoreError("bad-kind", f"unknown evidence kind {request.kind!r}")
        registrations[request.alias] = register_evidence(
            tx,
            request.source,
            alias=request.alias,
            kind=request.kind,
            snapshot_epoch=request.snapshot_epoch,
            snapshot_fingerprint=request.snapshot_fingerprint,
            policy=policy,
            context=context,
        )
    return registrations


def resolve_evidence_aliases(payload: dict, registrations: dict) -> dict:
    """Replace @alias references in evidence fields with evidence IDs.

    Only keys whose value is a string beginning with '@' or a list of such
    strings are rewritten; other fields are copied through and will be caught
    by strict record validation if unexpected.
    """

    used = set()

    def resolve(value):
        if isinstance(value, str) and value.startswith("@"):
            alias = value[1:]
            if alias not in registrations:
                raise StoreError("unresolved-alias", f"no evidence for alias {alias!r}")
            used.add(alias)
            return registrations[alias].evidence_id
        if isinstance(value, list):
            return [resolve(v) for v in value]
        if isinstance(value, dict):
            return {k: resolve(v) for k, v in value.items()}
        return value

    resolved = {k: resolve(v) for k, v in payload.items()}
    unused = set(registrations) - used
    if unused:
        raise StoreError("unused-alias", f"unused evidence alias(es): {sorted(unused)}")
    return resolved


def verify_evidence_files(state: dict, evidence_ids: tuple) -> None:
    for eid in evidence_ids:
        record = state["evidence"].get(eid)
        if record is None:
            raise StoreError("state-invalid", f"unknown evidence id {eid!r}")
        content = state["content_objects"].get(record["content_id"])
        if content is None:
            raise StoreError("state-invalid", f"evidence {eid!r} references missing content")
        path = Path(content["path"])
        if not path.is_file():
            raise StoreError("content-drift", f"evidence file vanished: {path}")
        raw = path.read_bytes()
        if len(raw) != content["bytes"] or hashlib.sha256(raw).hexdigest() != content["sha256"]:
            raise StoreError("content-drift", f"evidence file drifted: {path}")


# ---------------------------------------------------------------------------
# History


def append_history(candidate: dict, *, event: str, data_sha256: str) -> dict:
    history = candidate["history"]
    previous = history[-1]["record_sha256"] if history else "0" * 64
    snap = candidate["snapshot"]
    record = {
        "sequence": len(history) + 1,
        "generation": candidate["generation"],
        "event": event,
        "snapshot_epoch": snap["epoch"] if snap else None,
        "snapshot_fingerprint": snap["fingerprint"] if snap else None,
        "data_sha256": data_sha256,
        "previous_record_sha256": previous,
        "record_sha256": "",
    }
    record["record_sha256"] = model.sha256_json(model.history_record_subject(record))
    history.append(record)
    return candidate
