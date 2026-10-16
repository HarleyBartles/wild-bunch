"""Format supported source files changed by one Codex tool invocation."""

from __future__ import annotations

import hashlib
import importlib.util
import json
import os
import subprocess
import sys
import tempfile
import time
from pathlib import Path
from typing import Any

TEMP_ROOT = Path(tempfile.gettempdir()) / "wild-bunch-format-hook"
SNAPSHOT_MAX_AGE_SECONDS = 24 * 60 * 60
MAX_DIAGNOSTIC_CHARACTERS = 4000


def _event_string(event: dict[str, Any], key: str) -> str | None:
    value = event.get(key)
    return value if isinstance(value, str) and value else None


def _repository_root(cwd: str) -> Path | None:
    result = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        cwd=cwd,
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0 or not result.stdout.strip():
        return None
    root = Path(result.stdout.strip()).resolve()
    if not (root / "tools" / "style.py").is_file():
        return None
    return root


def _style_module(root: Path) -> Any:
    path = root / "tools" / "style.py"
    spec = importlib.util.spec_from_file_location("wild_bunch_hook_style", path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"could not load repository style policy from {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _supported_changed_paths(root: Path, *, worktree_only: bool = False) -> set[str]:
    result = subprocess.run(
        ["git", "status", "--porcelain=v1", "--untracked-files=all", "-z"],
        cwd=root,
        check=True,
        capture_output=True,
    )
    style = _style_module(root)
    paths: set[str] = set()
    records = iter(result.stdout.split(b"\0"))
    for record in records:
        if not record:
            continue
        if len(record) < 4:
            raise RuntimeError("git status returned an invalid porcelain record")
        status = record[:2].decode("ascii", errors="strict")
        raw_path = record[3:]
        # Porcelain -z uses the destination path first, followed by the source
        # path for renames and copies. Only the destination can be formatted.
        if "R" in status or "C" in status:
            next(records, None)
        if worktree_only and status[1] == " " and status != "??":
            continue
        try:
            candidate = root / Path(os.fsdecode(raw_path))
            if candidate.is_symlink():
                continue
            resolved = candidate.resolve(strict=True)
            resolved.relative_to(root)
        except (OSError, ValueError):
            continue
        if not resolved.is_file() or not style._is_supported(root, resolved):
            continue
        paths.add(resolved.relative_to(root).as_posix())
    return paths


def _supported_file_hashes(root: Path, paths: set[str]) -> dict[str, str]:
    hashes: dict[str, str] = {}
    for relative in paths:
        path = root / Path(relative)
        try:
            hashes[relative] = hashlib.sha256(path.read_bytes()).hexdigest()
        except OSError:
            continue
    return hashes


def _tracked_blob_ids(root: Path) -> dict[str, str]:
    result = subprocess.run(
        ["git", "ls-files", "--stage", "-z"],
        cwd=root,
        check=True,
        capture_output=True,
    )
    style = _style_module(root)
    blobs: dict[str, str] = {}
    for record in result.stdout.split(b"\0"):
        if not record:
            continue
        metadata, separator, raw_path = record.partition(b"\t")
        if not separator:
            raise RuntimeError("git ls-files returned an invalid index record")
        parts = metadata.split()
        if len(parts) != 3 or parts[0] not in {b"100644", b"100755"} or parts[2] != b"0":
            continue
        relative = Path(os.fsdecode(raw_path)).as_posix()
        if style._is_supported(root, root / relative):
            blobs[relative] = parts[1].decode("ascii")
    return blobs


def _working_blob_id(root: Path, relative: str) -> str | None:
    result = subprocess.run(
        ["git", "hash-object", f"--path={relative}", relative],
        cwd=root,
        check=False,
        capture_output=True,
    )
    return result.stdout.decode("ascii").strip() if result.returncode == 0 else None


def _snapshot_path(root: Path, session_id: str, tool_use_id: str) -> Path:
    root_key = hashlib.sha256(str(root).encode("utf-8")).hexdigest()
    invocation_key = hashlib.sha256(f"{session_id}\0{tool_use_id}".encode("utf-8")).hexdigest()
    return TEMP_ROOT / root_key / f"{invocation_key}.json"


def _remove_expired_snapshots(root: Path) -> None:
    root_key = hashlib.sha256(str(root).encode("utf-8")).hexdigest()
    state_directory = TEMP_ROOT / root_key
    if not state_directory.is_dir():
        return
    cutoff = time.time() - SNAPSHOT_MAX_AGE_SECONDS
    for state_file in state_directory.glob("*.json"):
        try:
            if state_file.stat().st_mtime < cutoff:
                state_file.unlink()
        except OSError:
            continue


def _write_snapshot(root: Path, session_id: str, tool_use_id: str) -> None:
    state_file = _snapshot_path(root, session_id, tool_use_id)
    state_file.parent.mkdir(parents=True, exist_ok=True)
    temporary = state_file.with_suffix(f".{os.getpid()}.tmp")
    dirty_paths = _supported_changed_paths(root, worktree_only=True)
    temporary.write_text(
        json.dumps(
            {
                "worktree_hashes": _supported_file_hashes(root, dirty_paths),
                "tracked_blob_ids": _tracked_blob_ids(root),
            },
            sort_keys=True,
        ),
        encoding="utf-8",
    )
    temporary.replace(state_file)


def _hook_output(payload: dict[str, Any]) -> None:
    sys.stdout.write(json.dumps(payload, separators=(",", ":")) + "\n")


def _post_tool_context(message: str) -> dict[str, Any]:
    return {
        "hookSpecificOutput": {
            "hookEventName": "PostToolUse",
            "additionalContext": message,
        }
    }


def _post_tool_block(message: str) -> dict[str, Any]:
    return {
        "decision": "block",
        "reason": message,
        **_post_tool_context(message),
    }


def _pre_tool_deny(message: str) -> dict[str, Any]:
    return {
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": message,
        }
    }


def _format_recovery(paths: list[str]) -> str:
    encoded_paths = json.dumps(paths, ensure_ascii=True)
    return (
        "run `py -3 tools/run.py format --apply <changed-paths>` with each listed path "
        f"as a separate argument. Changed paths (JSON array): {encoded_paths}"
    )


def _format_changed_files(root: Path, paths: list[str]) -> tuple[int, str]:
    command = [sys.executable, str(root / "tools" / "run.py"), "format", "--apply", *paths]
    result = subprocess.run(command, cwd=root, capture_output=True, text=True, check=False)
    diagnostic = "\n".join(part for part in (result.stdout.strip(), result.stderr.strip()) if part)
    return result.returncode, diagnostic[-MAX_DIAGNOSTIC_CHARACTERS:]


def handle_event(event: dict[str, Any]) -> int:
    event_name = _event_string(event, "hook_event_name")
    if event_name not in {"PreToolUse", "PostToolUse"}:
        return 0
    cwd = _event_string(event, "cwd")
    if cwd is None:
        return 0
    root = _repository_root(cwd)
    if root is None:
        return 0

    session_id = _event_string(event, "session_id")
    tool_use_id = _event_string(event, "tool_use_id")
    if session_id is None or tool_use_id is None:
        message = (
            "Codex formatting hook could not identify this tool invocation; use an explicit "
            "format command."
        )
        _hook_output(
            _pre_tool_deny(message) if event_name == "PreToolUse" else _post_tool_block(message)
        )
        return 0

    try:
        if event_name == "PreToolUse":
            _remove_expired_snapshots(root)
            _write_snapshot(root, session_id, tool_use_id)
            return 0

        state_file = _snapshot_path(root, session_id, tool_use_id)
        if not state_file.is_file():
            message = (
                "Codex formatting hook has no matching before-snapshot, so it cannot determine "
                "which files this completed tool call changed. Inspect that call's files, then "
                "run `py -3 tools/run.py format --apply <changed-paths>` for the changed "
                "supported paths."
            )
            _hook_output(_post_tool_block(message))
            return 0
        if time.time() - state_file.stat().st_mtime > SNAPSHOT_MAX_AGE_SECONDS:
            state_file.unlink(missing_ok=True)
            message = (
                "Codex formatting hook's before-snapshot expired after the write completed. "
                "Inspect that call's files, then run `py -3 tools/run.py format --apply "
                "<changed-paths>` for the changed supported paths."
            )
            _hook_output(_post_tool_block(message))
            return 0
        snapshot = json.loads(state_file.read_text(encoding="utf-8"))
        state_file.unlink(missing_ok=True)
        if not isinstance(snapshot, dict):
            snapshot = {}
        before = snapshot.get("worktree_hashes")
        before_index = snapshot.get("tracked_blob_ids")
        if not all(
            isinstance(values, dict)
            and all(
                isinstance(path, str) and isinstance(digest, str) for path, digest in values.items()
            )
            for values in (before, before_index)
        ):
            message = (
                "Codex formatting hook's before-snapshot is invalid; inspect the completed write "
                "and run `py -3 tools/run.py format --apply <changed-paths>` for its supported "
                "paths."
            )
            _hook_output(_post_tool_block(message))
            return 0
        after_paths = _supported_changed_paths(root)
        changed_paths = {
            path
            for path, digest in _supported_file_hashes(root, after_paths & before.keys()).items()
            if before[path] != digest
        }
        for path in after_paths - before.keys():
            current_blob = _working_blob_id(root, path)
            if path not in before_index or current_blob != before_index[path]:
                changed_paths.add(path)
        changed_paths = sorted(changed_paths)
        if not changed_paths:
            return 0

        return_code, diagnostic = _format_changed_files(root, changed_paths)
        if return_code != 0:
            recovery = _format_recovery(changed_paths)
            message = (
                "The tool write already happened. Automatic formatting failed; review the "
                f"diagnostic and run `{recovery}` after resolving its cause.\n{diagnostic}"
            )
            _hook_output(_post_tool_block(message))
            return 0
        _hook_output(
            _post_tool_context(
                "Automatically formatted the supported files changed by the completed tool "
                f"call: {', '.join(changed_paths)}."
            )
        )
        return 0
    except (OSError, RuntimeError, subprocess.SubprocessError, ValueError) as exc:
        message = (
            "The tool write may already have happened, but the automatic formatting hook failed "
            f"before it could verify or format the changed paths: {exc}. Inspect the write, then "
            "run `py -3 tools/run.py format --apply <changed-paths>` for its changed supported "
            "paths."
        )
        _hook_output(
            _pre_tool_deny(message) if event_name == "PreToolUse" else _post_tool_block(message)
        )
        return 0


def main() -> int:
    try:
        event = json.load(sys.stdin)
    except (json.JSONDecodeError, OSError) as exc:
        _hook_output(_pre_tool_deny(f"Codex formatting hook received invalid event JSON: {exc}"))
        return 0
    if not isinstance(event, dict):
        _hook_output(_pre_tool_deny("Codex formatting hook event must be a JSON object."))
        return 0
    return handle_event(event)


if __name__ == "__main__":
    raise SystemExit(main())
