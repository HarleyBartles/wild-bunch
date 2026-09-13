#!/usr/bin/env python3
"""PreToolUse policy gate for the iterative-review hooks pack.

Denies file and exec tool calls that touch a configured deny root (the
review's witness, transcript, and evidence-store directories). Emits a
``{"decision": "block", "reason": ...}`` object on stdout and exits 2 when a
call crosses a deny root or when the payload cannot be assessed; silent
exit 0 otherwise. Unlike the recorders, this gate fails closed: an
unparseable payload is a deny, because the sealed roots stay protected even
when input is malformed.

The acquire directory and the state file are intentionally not deny roots:
``reviewctl enumerate``/``complete --acquired`` must write and read them, and
every CLI invocation names the state path. Their integrity is enforced by the
witnessed subject digests and evidence-manifest checks instead.

Best-effort defense-in-depth only: path keys are resolved against the call's
cwd with environment variables expanded; shell indirection inside ``command``
text is matched literally only - the state kernel remains the enforcer.
"""

import json
import os
import sys
from pathlib import Path

_PATH_KEYS = ("file_path", "path", "notebook_path", "target_file", "workdir", "cwd")


def _env() -> dict | None:
    env_path = os.environ.get("IR_HOOK_ENV") or str(Path(__file__).parent / "hook-env.json")
    try:
        env = json.loads(Path(env_path).read_text(encoding="utf-8"))
        return env if isinstance(env, dict) else None
    except Exception:
        return None


def _norm(text: str) -> str:
    return text.replace("\\", "/").rstrip("/").lower()


def _deny_roots(env: dict) -> list[str]:
    roots = []
    for raw in env.get("deny_roots") or []:
        text = str(raw).strip()
        if not text:
            continue
        try:
            roots.append(_norm(str(Path(text).resolve())))
        except (OSError, ValueError):
            roots.append(_norm(text))
    return roots


def _payload() -> dict | None:
    stream = getattr(sys.stdin, "buffer", None)
    raw = stream.read().decode("utf-8", errors="surrogateescape") if stream is not None else sys.stdin.read()
    if not raw.strip() and len(sys.argv) > 1:
        raw = sys.argv[1]
    if not raw.strip():
        return None
    try:
        parsed = json.loads(raw)
    except Exception:
        return None
    return parsed if isinstance(parsed, dict) else None


def _extract_paths(tool_input: dict) -> list[str]:
    out = []
    for key in _PATH_KEYS:
        value = tool_input.get(key)
        if isinstance(value, str) and value.strip():
            out.append(value)
    return out


def _resolve(candidate: str, base: Path) -> str:
    try:
        path = Path(os.path.expandvars(candidate))
        if not path.is_absolute():
            path = base / path
        return _norm(str(path.resolve()))
    except (OSError, ValueError):
        return _norm(candidate)


def _path_under_deny(resolved: str, deny_roots: list[str]) -> str | None:
    """Resolved paths: the deny root must be the path itself or a prefix."""
    normed = _norm(resolved)
    for root in deny_roots:
        if normed == root or normed.startswith(root + "/"):
            return root
    return None


def _touches_deny(text: str, deny_roots: list[str]) -> str | None:
    """Command text: the deny root may appear anywhere in the string."""
    normed = _norm(text)
    for root in deny_roots:
        if normed == root or normed.startswith(root + "/"):
            return root
        if root + "/" in normed or normed.endswith(root):
            return root
    return None


def _block(reason: str) -> int:
    print(json.dumps({"decision": "block", "reason": reason}, separators=(",", ":")))
    return 2


def _main() -> int:
    env = _env()
    if env is None:
        return _block("iterative-review: hook env missing or unparseable")
    if env.get("deny_roots") is not None and not isinstance(env["deny_roots"], list):
        return _block("iterative-review: hook env deny_roots is malformed")
    deny_roots = _deny_roots(env)
    if not deny_roots:
        return 0
    payload = _payload()
    if payload is None:
        return _block("iterative-review: unparseable hook payload")
    tool_input = payload.get("tool_input")
    if not isinstance(tool_input, dict):
        return _block("iterative-review: hook payload lacks tool_input")
    base_arg = tool_input.get("cwd") or tool_input.get("workdir")
    if not base_arg:
        try:
            base_arg = os.getcwd()
        except OSError:
            return _block("iterative-review: gate internal error: cwd-unavailable")
    base = Path(str(base_arg))
    hit = None
    for candidate in _extract_paths(tool_input):
        hit = _path_under_deny(_resolve(candidate, base), deny_roots)
        if hit:
            break
    if hit is None:
        command = tool_input.get("command")
        if isinstance(command, list):
            command = " ".join(str(c) for c in command)
        if isinstance(command, str):
            hit = _touches_deny(command, deny_roots)
    if hit is not None:
        return _block(f"iterative-review: path under sealed review root {hit}")
    return 0


def main() -> int:
    try:
        return _main()
    except Exception as exc:
        return _block(f"iterative-review: gate internal error: {exc}")


if __name__ == "__main__":
    raise SystemExit(main())
