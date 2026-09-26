#!/usr/bin/env python3
"""Apply or check the portable Markdown formatting contract."""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import tomllib
from dataclasses import dataclass
from importlib import metadata as importlib_metadata
from pathlib import Path, PurePosixPath
from typing import Literal, Sequence


CONTRACT_PATH = Path(".agents/contracts/markdown-formatting.json")
CONFIG_PATH = Path(".mdformat.toml")
MAX_COMMAND_CHARS = 28_000
REQUIREMENTS_PATH = Path(__file__).resolve().parents[1] / "requirements.txt"
REQUIRED_DISTRIBUTION_NAMES = frozenset(
    {"mdformat", "mdformat-frontmatter", "mdformat-gfm", "mdformat-safe-link-labels"}
)
RENDERER_VERSION = "1.0.0"
RENDERER_WHEEL_REQUIREMENT = (
    "./.agents/plugins/marketplace-source/codex-marketplace/packages/"
    "mdformat-safe-link-labels/wheels/mdformat_safe_link_labels-1.0.0-py3-none-any.whl"
)


def _required_distributions() -> dict[str, str]:
    pins: dict[str, str] = {}
    try:
        lines = REQUIREMENTS_PATH.read_text(encoding="utf-8").splitlines()
    except OSError as exc:
        raise ToolchainError(f"cannot read {REQUIREMENTS_PATH}: {exc}") from exc
    for line in lines:
        match = re.fullmatch(r"([A-Za-z0-9_.-]+)==([^\s]+)", line.strip())
        if match:
            pins[match.group(1)] = match.group(2)
    if RENDERER_WHEEL_REQUIREMENT in {line.strip() for line in lines}:
        pins["mdformat-safe-link-labels"] = RENDERER_VERSION
    missing = REQUIRED_DISTRIBUTION_NAMES - pins.keys()
    if missing:
        raise ToolchainError(f"{REQUIREMENTS_PATH}: missing exact pins for {', '.join(sorted(missing))}")
    return pins


class MarkdownFormattingError(RuntimeError):
    """Base error for concise CLI handling."""


class ContractError(MarkdownFormattingError):
    """The consumer contract or requested paths are invalid."""


class ToolchainError(MarkdownFormattingError):
    """The active interpreter does not provide the pinned formatter."""


class FormatterError(MarkdownFormattingError):
    """The formatter rejected an input or found drift."""


@dataclass(frozen=True)
class Exclusion:
    kind: Literal["file", "tree"]
    path: PurePosixPath
    reason: str


@dataclass(frozen=True)
class MarkdownContract:
    state: Literal["adopted", "enforced"]
    exclusions: tuple[Exclusion, ...]


def _git(repo_root: Path, *args: str) -> str:
    result = subprocess.run(["git", *args], cwd=repo_root, text=True, capture_output=True, check=False)
    if result.returncode:
        raise ContractError(result.stderr.strip() or "Git command failed")
    return result.stdout


def _tracked_markdown(repo_root: Path) -> tuple[PurePosixPath, ...]:
    output = _git(repo_root, "ls-files", "--", "*.md")
    return tuple(sorted(PurePosixPath(line) for line in output.splitlines() if line))


def _staged_markdown(repo_root: Path) -> tuple[PurePosixPath, ...]:
    output = _git(repo_root, "diff", "--cached", "--name-only", "--diff-filter=ACMR", "--", "*.md")
    return tuple(
        sorted(
            PurePosixPath(line) for line in output.splitlines() if line and (repo_root / PurePosixPath(line)).is_file()
        )
    )


def _safe_relative_path(value: object, *, label: str) -> PurePosixPath:
    if not isinstance(value, str) or not value.strip():
        raise ContractError(f"{label} must be a non-empty repository-relative path")
    if any(character in value for character in "*?["):
        raise ContractError(f"{label}: globs are not supported")
    path = PurePosixPath(value.replace("\\", "/"))
    if path.is_absolute() or ".." in path.parts or path == PurePosixPath("."):
        raise ContractError(f"{label} must be repository-relative and remain inside the repository")
    return path


def load_contract(repo_root: Path) -> MarkdownContract:
    path = repo_root / CONTRACT_PATH
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ContractError(f"{CONTRACT_PATH.as_posix()}: {exc}") from exc
    if not isinstance(data, dict) or data.get("version") != 1:
        raise ContractError(f"{CONTRACT_PATH.as_posix()}: version must be 1")
    state = data.get("state")
    if state not in {"adopted", "enforced"}:
        raise ContractError(f"{CONTRACT_PATH.as_posix()}: state must be adopted or enforced")
    raw_exclusions = data.get("exclusions")
    if not isinstance(raw_exclusions, list):
        raise ContractError(f"{CONTRACT_PATH.as_posix()}: exclusions must be a list")
    tracked = _tracked_markdown(repo_root)
    exclusions: list[Exclusion] = []
    for index, raw in enumerate(raw_exclusions):
        label = f"{CONTRACT_PATH.as_posix()} exclusions[{index}]"
        if not isinstance(raw, dict):
            raise ContractError(f"{label} must be an object")
        kind = raw.get("kind")
        if kind not in {"file", "tree"}:
            raise ContractError(f"{label} kind must be file or tree")
        relative = _safe_relative_path(raw.get("path"), label=label)
        reason = raw.get("reason")
        if not isinstance(reason, str) or not reason.strip():
            raise ContractError(f"{label} must have a non-empty reason")
        matches = (
            [candidate for candidate in tracked if candidate == relative]
            if kind == "file"
            else [candidate for candidate in tracked if relative in candidate.parents]
        )
        if not matches:
            raise ContractError(f"{label} {relative.as_posix()!r} matches no tracked Markdown")
        if kind == "file" and not (repo_root / relative).is_file():
            raise ContractError(f"{label} declares a file but is not a file")
        if kind == "tree" and not (repo_root / relative).is_dir():
            raise ContractError(f"{label} declares a tree but is not a directory")
        exclusions.append(Exclusion(kind=kind, path=relative, reason=reason.strip()))
    return MarkdownContract(state=state, exclusions=tuple(exclusions))


def eligible_markdown(repo_root: Path, contract: MarkdownContract) -> tuple[Path, ...]:
    def excluded(candidate: PurePosixPath) -> bool:
        return any(
            candidate == item.path if item.kind == "file" else item.path in candidate.parents
            for item in contract.exclusions
        )

    candidates = (
        _staged_markdown(repo_root)
        if os.environ.get("REPO_STANDARDS_STAGED_SNAPSHOT") == "1"
        else _tracked_markdown(repo_root)
    )
    return tuple(repo_root / path for path in candidates if not excluded(path))


def validate_requested_files(repo_root: Path, values: Sequence[str]) -> tuple[Path, ...]:
    selected: list[Path] = []
    for index, value in enumerate(values):
        relative = _safe_relative_path(value, label=f"--check-files[{index}]")
        if relative.suffix.lower() != ".md":
            raise ContractError(f"{relative.as_posix()}: requested output is not Markdown")
        target = repo_root / relative
        if not target.is_file():
            raise ContractError(f"{relative.as_posix()}: requested Markdown does not exist")
        selected.append(target)
    return tuple(sorted(set(selected), key=lambda path: path.relative_to(repo_root).as_posix()))


def verify_configuration(repo_root: Path) -> None:
    path = repo_root / CONFIG_PATH
    try:
        config = tomllib.loads(path.read_text(encoding="utf-8"))
    except (OSError, tomllib.TOMLDecodeError) as exc:
        raise ContractError(f"{CONFIG_PATH.as_posix()}: {exc}") from exc
    expected = {"wrap": "no", "end_of_line": "lf", "validate": True, "number": True}
    for key, value in expected.items():
        if config.get(key) != value:
            raise ContractError(f"{CONFIG_PATH.as_posix()}: {key} must be {value!r}")
    extensions = config.get("extensions")
    required_extensions = {"gfm", "frontmatter", "safe-link-labels"}
    if not isinstance(extensions, list) or not required_extensions.issubset(extensions):
        raise ContractError(f"{CONFIG_PATH.as_posix()}: extensions must include gfm, frontmatter, and safe-link-labels")


def verify_toolchain() -> None:
    for distribution, expected in _required_distributions().items():
        try:
            actual = importlib_metadata.version(distribution)
        except importlib_metadata.PackageNotFoundError as exc:
            raise ToolchainError(f"missing {distribution}=={expected} in {sys.executable}") from exc
        if actual != expected:
            raise ToolchainError(
                f"{distribution} version {actual} does not match required {expected} in {sys.executable}"
            )


def _batches(repo_root: Path, files: Sequence[Path], prefix: Sequence[str]):
    current: list[str] = []
    current_size = sum(len(value) + 1 for value in prefix)
    for path in files:
        relative = path.relative_to(repo_root).as_posix()
        size = len(relative) + 1
        if current and current_size + size > MAX_COMMAND_CHARS:
            yield current
            current = []
            current_size = sum(len(value) + 1 for value in prefix)
        current.append(relative)
        current_size += size
    if current:
        yield current


def run_formatter(repo_root: Path, files: Sequence[Path], *, mode: Literal["check", "apply"]) -> None:
    prefix = [sys.executable, "-m", "mdformat"]
    if mode == "check":
        prefix.append("--check")
    originals = {path: path.read_bytes() for path in files} if mode == "apply" else {}
    try:
        for batch in _batches(repo_root, files, prefix):
            result = subprocess.run([*prefix, *batch], cwd=repo_root, check=False)
            if result.returncode:
                raise FormatterError(f"mdformat {mode} failed for {batch[0]}")
    except BaseException:
        for path, content in originals.items():
            path.write_bytes(content)
        raise


def _repo_root() -> Path:
    result = subprocess.run(["git", "rev-parse", "--show-toplevel"], text=True, capture_output=True, check=False)
    if result.returncode:
        raise ContractError(result.stderr.strip() or "not inside a Git repository")
    return Path(result.stdout.strip()).resolve()


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=f"{__doc__} (mixed: --check is read-only; --apply mutates)")
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument("--check", action="store_true", help="check all eligible tracked Markdown")
    modes.add_argument("--apply", action="store_true", help="format all eligible tracked Markdown")
    modes.add_argument("--check-files", nargs="+", metavar="PATH", help="check explicit producer outputs")
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    try:
        repo_root = _repo_root()
        verify_configuration(repo_root)
        verify_toolchain()
        contract = load_contract(repo_root)
        files = (
            validate_requested_files(repo_root, args.check_files)
            if args.check_files
            else eligible_markdown(repo_root, contract)
        )
        run_formatter(repo_root, files, mode="apply" if args.apply else "check")
    except MarkdownFormattingError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
