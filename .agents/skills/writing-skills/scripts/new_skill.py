"""(mixed: create custody-aware skill scaffolds; --check reports what would be written)

Create custody-aware skill scaffolds without overwriting authored files.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import re
import shutil
import subprocess
from typing import Final


CUSTODIES: Final = {"local", "marketplace"}
LOCAL_PREFIX: Final = "mark-"
NAME_PATTERN: Final = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
SCRIPT_ROOT = Path(__file__).resolve().parent
TEMPLATE_ROOT = SCRIPT_ROOT.parent / "templates"


def _marketplace_plugins(repo_root: Path) -> set[str]:
    plugins_dir = repo_root / "codex-marketplace" / "plugins"
    if not plugins_dir.is_dir():
        return set()
    return {p.name for p in plugins_dir.iterdir() if p.is_dir()}


def validate_request(name: str, custody: str, lane: str, plugins: set[str]) -> None:
    if custody not in CUSTODIES:
        raise ValueError(f"unsupported custody: {custody}")
    if len(name) > 64 or not NAME_PATTERN.fullmatch(name):
        raise ValueError("skill name must use lowercase letters, numbers, and single hyphens (64 characters maximum)")
    if custody == "local" and not name.startswith(LOCAL_PREFIX):
        raise ValueError("local custody requires the mark- prefix")
    if custody == "marketplace" and lane not in plugins:
        raise ValueError(f"--lane must be a marketplace plugin pack; got {lane!r}")
    if custody == "marketplace" and name.startswith(LOCAL_PREFIX):
        raise ValueError("marketplace custody cannot use the mark- prefix")


def destination_for(repo_root: Path, name: str, custody: str, lane: str) -> Path:
    if custody == "local":
        return repo_root / ".agents" / "skills" / name
    return repo_root / "codex-marketplace" / "plugins" / lane / "skills" / name


def _template(path: str, **values: str) -> str:
    return (TEMPLATE_ROOT / path).read_text(encoding="utf-8").format(**values).replace("\r\n", "\n").rstrip("\n") + "\n"


def _metadata_for(name: str, custody: str, lane: str) -> str:
    if custody == "local":
        return f"  custody: {custody}\n  lane: {lane}"
    description = f"Use when authoring or reviewing the {name} skill."
    title = name.replace("-", " ").title()
    return (
        f"  source-id: {json.dumps(name)}\n"
        f"  source-path: {json.dumps(f'codex-marketplace/plugins/{lane}/skills/{name}/SKILL.md')}\n"
        f"  provenance-name: {json.dumps(f'{title} first-party skill')}\n"
        "  source-category: first_party\n"
        "  status: active\n"
        '  owner: "Harley Bartles"\n'
        f"  scope: {json.dumps(description)}\n"
        "  use_when:\n"
        f"  - {json.dumps(description)}\n"
        "  do_not_use_when:\n"
        '  - "Do not use when another more specific skill owns this task."'
    )


def render_scaffold(name: str, custody: str, lane: str) -> dict[str, str]:
    files: dict[str, str] = {
        "SKILL.md": _template(
            "skill/SKILL.md",
            name=name,
            custody=custody,
            lane=lane,
            metadata=_metadata_for(name, custody, lane),
        )
    }
    if custody == "local":
        return files
    files["references/.gitkeep"] = "\n"
    return files


def _git(repo_root: Path, *args: str) -> str:
    result = subprocess.run(["git", *args], cwd=repo_root, check=True, capture_output=True, text=True)
    return result.stdout.strip()


def _guard_write_checkout(repo_root: Path, allow_shared_checkout: bool) -> Path:
    superproject = _git(repo_root, "rev-parse", "--show-superproject-working-tree")
    if superproject:
        raise ValueError("refusing to scaffold from a submodule checkout")
    checkout_root = Path(_git(repo_root, "rev-parse", "--show-toplevel")).resolve()
    git_dir = Path(_git(checkout_root, "rev-parse", "--path-format=absolute", "--git-dir")).resolve()
    common_dir = Path(_git(checkout_root, "rev-parse", "--path-format=absolute", "--git-common-dir")).resolve()
    if git_dir == common_dir:
        if not allow_shared_checkout:
            raise ValueError(
                "refusing to scaffold from a shared main checkout; "
                "use --allow-shared-checkout "
                "with current human approval"
            )
        print("WARNING: --allow-shared-checkout is active; current human approval is required.")
    return checkout_root


def _resolve_cli_repo_root(start_directory: Path) -> Path:
    """Use the Git top-level for CLI use while preserving testable non-Git calls."""
    try:
        return Path(_git(start_directory, "rev-parse", "--show-toplevel")).resolve()
    except (OSError, subprocess.CalledProcessError):
        return start_directory


def scaffold(
    repo_root: Path, name: str, custody: str, lane: str, check: bool, *, allow_shared_checkout: bool = False
) -> int:
    plugins = _marketplace_plugins(repo_root)
    validate_request(name, custody, lane, plugins)
    if not check:
        repo_root = _guard_write_checkout(repo_root, allow_shared_checkout)
    destination = destination_for(repo_root, name, custody, lane)
    if destination.exists():
        raise FileExistsError(f"destination already exists: {destination}")
    files = render_scaffold(name, custody, lane)
    if check:
        for relative_path in files:
            print(destination / relative_path)
        return 0
    created_destination = False
    created_parents: list[Path] = []
    missing_parents: list[Path] = []
    parent = destination.parent
    while not parent.exists():
        missing_parents.append(parent)
        parent = parent.parent
    try:
        for parent in reversed(missing_parents):
            parent.mkdir()
            created_parents.append(parent)
        destination.mkdir()
        created_destination = True
        for relative_path, content in files.items():
            output_path = destination / relative_path
            output_path.parent.mkdir(parents=True, exist_ok=True)
            with output_path.open("x", encoding="utf-8", newline="\n") as handle:
                handle.write(content)
    except Exception:
        if created_destination:
            shutil.rmtree(destination)
        for parent in reversed(created_parents):
            try:
                parent.rmdir()
            except OSError:
                pass
        raise
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--name")
    parser.add_argument("--custody", choices=sorted(CUSTODIES))
    parser.add_argument("--lane", help="marketplace plugin pack for marketplace custody; ignored for local")
    parser.add_argument("--check", action="store_true", help="report what would be scaffolded (read-only)")
    parser.add_argument("--allow-shared-checkout", action="store_true")
    args = parser.parse_args()
    if not (args.name or args.custody or args.lane or args.check):
        parser.print_help()
        return 0
    if args.check and not (args.name and args.custody and args.lane):
        print("No scaffold requested; --check requires --name, --custody, and --lane for a dry run.")
        return 0
    missing = [
        arg
        for arg, value in (
            ("--name", args.name),
            ("--custody", args.custody),
            ("--lane", args.lane),
        )
        if not value
    ]
    if missing:
        parser.error(f"required: {', '.join(missing)}")
    try:
        repo_root = _resolve_cli_repo_root(Path.cwd().resolve())
        return scaffold(
            repo_root, args.name, args.custody, args.lane, args.check, allow_shared_checkout=args.allow_shared_checkout
        )
    except (OSError, subprocess.CalledProcessError, ValueError, FileExistsError) as error:
        parser.error(str(error))
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
