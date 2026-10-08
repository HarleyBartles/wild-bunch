"""Validate the authored application version and generated web identity."""

from __future__ import annotations

import json
import re
import xml.etree.ElementTree as ElementTree
from pathlib import Path

_DEVELOPMENT_VERSION = re.compile(r"0\.1\.0-dev\.([1-9][0-9]*)\Z")


class VersionIdentityError(ValueError):
    """The repository's authored and generated application versions disagree."""


def read_authored_version(path: Path) -> str:
    """Read the single direct Project/PropertyGroup/Version value."""
    try:
        project = ElementTree.parse(path).getroot()
    except (OSError, ElementTree.ParseError) as exc:
        raise VersionIdentityError(f"{path}: could not read MSBuild project XML: {exc}") from exc

    if project.tag != "Project":
        raise VersionIdentityError(f"{path}: expected a Project root element")

    versions = [
        child for group in project.findall("PropertyGroup") for child in group.findall("Version")
    ]
    if len(versions) != 1:
        raise VersionIdentityError(
            f"{path}: expected exactly one direct Project/PropertyGroup/Version element, "
            f"found {len(versions)}"
        )

    version = (versions[0].text or "").strip()
    if _DEVELOPMENT_VERSION.fullmatch(version) is None:
        raise VersionIdentityError(
            f"{path}: expected a positive 0.1.0-dev.N version, found {version!r}"
        )
    return version


def _read_json(path: Path) -> object:
    try:
        with path.open(encoding="utf-8") as stream:
            return json.load(stream)
    except FileNotFoundError as exc:
        raise VersionIdentityError(f"{path}: file is missing") from exc
    except (OSError, json.JSONDecodeError) as exc:
        raise VersionIdentityError(f"{path}: could not read valid JSON: {exc}") from exc


def _object(value: object, path: Path) -> dict[str, object]:
    if not isinstance(value, dict):
        raise VersionIdentityError(f"{path}: expected a JSON object")
    return value


def check_version_identity(root: Path) -> None:
    """Require npm metadata to defer to MSBuild and the build artifact to match it."""
    authored_version = read_authored_version(root / "Directory.Build.props")
    web = root / "src" / "WildBunch.Web"

    package_path = web / "package.json"
    package = _object(_read_json(package_path), package_path)
    if "version" in package:
        raise VersionIdentityError(
            f'{package_path}: remove the root "version" field; Directory.Build.props is '
            "the sole application version"
        )

    lock_path = web / "package-lock.json"
    lock = _object(_read_json(lock_path), lock_path)
    if "version" in lock:
        raise VersionIdentityError(
            f'{lock_path}: remove the root "version" field; Directory.Build.props is '
            "the sole application version"
        )
    lock_packages = lock.get("packages")
    if not isinstance(lock_packages, dict):
        raise VersionIdentityError(f'{lock_path}: expected the lockfile "packages" object')
    lock_root = lock_packages.get("")
    if not isinstance(lock_root, dict):
        raise VersionIdentityError(f'{lock_path}: expected the root package entry at packages[""]')
    if "version" in lock_root:
        raise VersionIdentityError(
            f'{lock_path}: remove packages[""].version; Directory.Build.props is '
            "the sole application version"
        )

    artifact_path = web / "dist" / "version.json"
    artifact = _object(_read_json(artifact_path), artifact_path)
    generated_version = artifact.get("version")
    if not isinstance(generated_version, str):
        raise VersionIdentityError(f'{artifact_path}: expected a string "version" property')
    if generated_version != authored_version:
        raise VersionIdentityError(
            f"{artifact_path}: contains {generated_version!r}, expected {authored_version!r}; "
            "rebuild with npm --prefix src/WildBunch.Web run build"
        )
