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


def _supported_file_hashes(root: Path) -> dict[str, str]:
    style = _style_module(root)
    hashes: dict[str, str] = {}
    for path in style._tracked_and_untracked_files(root):
        try:
            resolved = path.resolve(strict=True)
            resolved.relative_to(root)
        except (OSError, ValueError):
            continue
        if not resolved.is_file() or not style._is_supported(root, resolved):
            continue
        try:
            hashes[resolved.relative_to(root).as_posix()] = hashlib.sha256(
                resolved.read_bytes()
            ).hexdigest()
        except OSError:
            continue
    return hashes


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
    temporary.write_text(json.dumps(_supported_file_hashes(root), sort_keys=True), encoding="utf-8")
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
    quoted_paths = " ".join(f'"{path}"' for path in paths)
    return f"py -3 tools/run.py format --apply {quoted_paths}"


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
        before = json.loads(state_file.read_text(encoding="utf-8"))
        state_file.unlink(missing_ok=True)
        if not isinstance(before, dict) or not all(
            isinstance(path, str) and isinstance(digest, str) for path, digest in before.items()
        ):
            message = (
                "Codex formatting hook's before-snapshot is invalid; inspect the completed write "
                "and run `py -3 tools/run.py format --apply <changed-paths>` for its supported "
                "paths."
            )
            _hook_output(_post_tool_block(message))
            return 0
        after = _supported_file_hashes(root)
        changed_paths = sorted(path for path, digest in after.items() if before.get(path) != digest)
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
