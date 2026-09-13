#!/usr/bin/env python3
"""PreToolUse transcript recorder for the iterative-review hooks pack.

Reads the hook payload as UTF-8 JSON on stdin (or argv[1] fallback) and
appends the parsed record as one compact JSONL line to
``<transcript_root>/<session_id>.jsonl``. On POSIX the transcript root is
locked to 0700 and each transcript file to 0600; a pre-existing transcript
that grants group/other access is refused (``acl-untrusted``) rather than
appended to. Never exits nonzero: a recorder failure must not break the
session; malformed input is logged to ``hook-errors.jsonl`` instead.
"""

import json
import os
import re
import sys
from datetime import datetime, timezone
from pathlib import Path


def _env() -> dict:
    env_path = os.environ.get("IR_HOOK_ENV") or str(Path(__file__).parent / "hook-env.json")
    try:
        env = json.loads(Path(env_path).read_text(encoding="utf-8"))
        return env if isinstance(env, dict) else {}
    except Exception:
        return {}


def _transcript_root(env: dict) -> Path:
    configured = env.get("transcript_root")
    if not isinstance(configured, (str, bytes, os.PathLike)):
        configured = None
    try:
        return Path(configured) if configured else Path(__file__).parent / "transcripts"
    except Exception:
        return Path(__file__).parent / "transcripts"


def _lockdown(path: Path, *, directory: bool) -> None:
    if sys.platform == "win32":
        return
    import os as _os

    _os.chmod(path, 0o700 if directory else 0o600)


def _untrusted(path: Path) -> str | None:
    if sys.platform == "win32" or not path.exists():
        return None
    import os as _os
    import stat

    mode = stat.S_IMODE(_os.stat(path).st_mode)
    if mode & 0o077:
        return f"acl-untrusted: {path} grants group/other access (mode {mode:o})"
    return None


def _log_error(root: Path, message: str) -> None:
    try:
        root.mkdir(parents=True, exist_ok=True)
        _lockdown(root, directory=True)
        target = root / "hook-errors.jsonl"
        if _untrusted(target) is not None:
            return
        with target.open("a", encoding="utf-8") as fh:
            fh.write(
                json.dumps(
                    {
                        "hook-error": message,
                        "recorded_at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
                    },
                    separators=(",", ":"),
                )
                + "\n"
            )
        _lockdown(target, directory=False)
    except Exception:
        pass


def _payload() -> dict:
    stream = getattr(sys.stdin, "buffer", None)
    raw = stream.read().decode("utf-8", errors="surrogateescape") if stream is not None else sys.stdin.read()
    if not raw.strip() and len(sys.argv) > 1:
        raw = sys.argv[1]
    parsed = json.loads(raw)
    if not isinstance(parsed, dict):
        raise ValueError("hook payload is not an object")
    return parsed


def main() -> int:
    env = _env()
    root = _transcript_root(env)
    try:
        rec = _payload()
    except Exception as exc:
        _log_error(root, f"malformed payload: {exc}")
        return 0
    session = rec.get("session_id")
    safe = re.sub(r"[^A-Za-z0-9_.-]", "_", str(session)) if session else "unknown-session"
    try:
        root.mkdir(parents=True, exist_ok=True)
        _lockdown(root, directory=True)
        target = root / f"{safe}.jsonl"
        untrusted = _untrusted(target)
        if untrusted is not None:
            raise OSError(untrusted)
        with target.open("a", encoding="utf-8") as fh:
            fh.write(json.dumps(rec, separators=(",", ":")) + "\n")
        _lockdown(target, directory=False)
    except Exception as exc:
        _log_error(root, f"transcript write failed: {exc}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
