"""Path policy and command construction for repository format/lint operations."""

from __future__ import annotations

import os
import shutil
import subprocess
import sys
from pathlib import Path

SUPPORTED_SUFFIXES = {
    ".cs": "dotnet",
    ".js": "web",
    ".py": "python",
    ".pyi": "python",
    ".scss": "web",
    ".ts": "web",
    ".tsx": "web",
}
EXCLUDED_DIRECTORY_NAMES = {
    ".git",
    ".venv",
    "artifacts",
    "bin",
    "build",
    "coverage",
    "dist",
    "node_modules",
    "obj",
    "packages",
    "testresults",
    "vendor",
    "venv",
}
NPM = shutil.which("npm") or "npm"


class StylePathError(ValueError):
    """A requested path does not belong to the maintained style scope."""


def _path_batches(paths: list[str], *, max_characters: int = 18000) -> list[list[str]]:
    batches: list[list[str]] = []
    current: list[str] = []
    current_length = 0
    for path in paths:
        path_length = len(path) + 1
        if current and current_length + path_length > max_characters:
            batches.append(current)
            current = []
            current_length = 0
        current.append(path)
        current_length += path_length
    if current:
        batches.append(current)
    return batches


def _is_supported(root: Path, path: Path) -> bool:
    try:
        relative = path.relative_to(root)
    except ValueError:
        return False
    if any(part.casefold() in EXCLUDED_DIRECTORY_NAMES for part in relative.parts[:-1]):
        return False
    name = path.name.casefold()
    if name.endswith(".d.ts") or any(marker in name for marker in (".generated.", ".g.", ".min.")):
        return False
    return path.suffix.casefold() in SUPPORTED_SUFFIXES


def _tracked_and_untracked_files(root: Path) -> list[Path]:
    result = subprocess.run(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"],
        cwd=root,
        check=True,
        capture_output=True,
    )
    return [root / Path(os.fsdecode(raw)) for raw in result.stdout.split(b"\0") if raw]


def select_files(
    root: Path, arguments: tuple[str, ...], *, allow_default_scope: bool
) -> dict[str, list[str]]:
    """Resolve an explicit scope or the repository's supported source scope."""
    root = root.resolve()
    args = list(arguments)
    all_scope = args.count("--all") == 1
    if "--all" in args and (len(args) != 1 or not all_scope):
        raise StylePathError("--all cannot be combined with explicit paths")
    if args.count("--all") > 1:
        raise StylePathError("--all may be specified only once")
    if not args and not allow_default_scope:
        raise StylePathError("format --apply requires explicit file paths or --all")

    selected: list[Path]
    if not args or all_scope:
        selected = [path.resolve() for path in _tracked_and_untracked_files(root)]
        selected = [path for path in selected if path.is_file() and _is_supported(root, path)]
    else:
        selected = []
        for argument in args:
            candidate = (root / Path(argument)).resolve()
            try:
                candidate.relative_to(root)
            except ValueError as exc:
                raise StylePathError(f"path is outside the repository: {argument}") from exc
            if not candidate.is_file():
                raise StylePathError(f"path is not an existing file: {argument}")
            if not _is_supported(root, candidate):
                raise StylePathError(f"path is generated, excluded, or unsupported: {argument}")
            selected.append(candidate)

    groups: dict[str, list[str]] = {"python": [], "web": [], "dotnet": []}
    for path in sorted(set(selected), key=lambda item: item.as_posix().casefold()):
        language = SUPPORTED_SUFFIXES[path.suffix.casefold()]
        groups[language].append(path.relative_to(root).as_posix())
    return {language: paths for language, paths in groups.items() if paths}


def formatter_commands(groups: dict[str, list[str]], *, check: bool) -> list[tuple[str, list[str]]]:
    """Build formatter argv vectors without shell interpolation."""
    commands: list[tuple[str, list[str]]] = []
    if "python" in groups:
        command = [sys.executable, "-m", "ruff", "format"]
        if check:
            command.append("--check")
        commands.append(("python", [*command, *groups["python"]]))
    if "web" in groups:
        command = [
            NPM,
            "--prefix",
            "src/WildBunch.Web",
            "exec",
            "--",
            "prettier",
        ]
        command.append("--check" if check else "--write")
        commands.append(("web", [*command, "--", *groups["web"]]))
    if "dotnet" in groups:
        for paths in _path_batches(groups["dotnet"]):
            command = ["dotnet", "format", "whitespace", "WildBunch.sln", "--include", *paths]
            if check:
                command.append("--verify-no-changes")
            commands.append(("dotnet", command))
    return commands


def linter_commands(groups: dict[str, list[str]]) -> list[tuple[str, list[str]]]:
    """Build linter argv vectors without auto-fix options or shell interpolation."""
    commands: list[tuple[str, list[str]]] = []
    if "python" in groups:
        commands.append(
            (
                "python",
                [
                    sys.executable,
                    "-m",
                    "ruff",
                    "check",
                    "--output-format",
                    "concise",
                    *groups["python"],
                ],
            )
        )
    web_lint_paths = [
        path for path in groups.get("web", []) if Path(path).suffix.casefold() != ".scss"
    ]
    if web_lint_paths:
        commands.append(
            (
                "web",
                [
                    NPM,
                    "--prefix",
                    "src/WildBunch.Web",
                    "exec",
                    "--",
                    "eslint",
                    "--config",
                    "src/WildBunch.Web/eslint.config.js",
                    "--",
                    *web_lint_paths,
                ],
            )
        )
    if "dotnet" in groups:
        for paths in _path_batches(groups["dotnet"]):
            commands.append(
                (
                    "dotnet",
                    [
                        "dotnet",
                        "format",
                        "analyzers",
                        "WildBunch.sln",
                        "--verify-no-changes",
                        "--severity",
                        "warn",
                        "--include",
                        *paths,
                    ],
                )
            )
    return commands
