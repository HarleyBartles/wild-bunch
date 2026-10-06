"""Run repository deployment commands without leaking captured input on failure."""

from __future__ import annotations

import os
import subprocess
from collections.abc import Mapping, Sequence
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]


class CommandFailed(RuntimeError):
    """A command failed; its potentially sensitive output is deliberately omitted."""

    def __init__(self, executable: str, returncode: int | None) -> None:
        self.returncode = returncode
        message = (
            f"{Path(executable).name} exited with status {returncode}"
            if returncode is not None
            else f"Could not start {Path(executable).name}"
        )
        super().__init__(message)


def run_checked(
    args: Sequence[str],
    *,
    env: Mapping[str, str] | None = None,
    stdin: str | None = None,
) -> str:
    """Run one argument-vector command from the repo root and return its stdout."""
    if not args:
        raise ValueError("A command argument vector cannot be empty")

    child_env = os.environ.copy()
    if env is not None:
        child_env.update(env)

    try:
        result = subprocess.run(
            list(args),
            cwd=REPOSITORY_ROOT,
            env=child_env,
            input=stdin,
            text=True,
            capture_output=True,
            check=False,
        )
    except OSError:
        raise CommandFailed(args[0], None) from None

    if result.returncode != 0:
        raise CommandFailed(args[0], result.returncode)
    return result.stdout
