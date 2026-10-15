"""Validate the authored application version and generated web identity."""

from __future__ import annotations

import json
import re
import xml.etree.ElementTree as ElementTree
from pathlib import Path

_SEMVER = re.compile(
    r"(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)"
    r"(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?"
    r"(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?\Z"
)
_DEVELOPMENT_SEQUENCE = re.compile(r"0\.1\.0-dev\.(0|[1-9][0-9]*)\Z")


def _is_semver(version: str) -> bool:
    match = _SEMVER.fullmatch(version)
    if match is None:
        return False

    prerelease = match.group(4)
    if prerelease is None:
        return True
    return all(
        not (identifier.isdigit() and len(identifier) > 1 and identifier.startswith("0"))
        for identifier in prerelease.split(".")
    )


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
    if not _is_semver(version):
        raise VersionIdentityError(
            f"{path}: expected a valid SemVer 2.0.0 version, found {version!r}"
        )
    development = _DEVELOPMENT_SEQUENCE.fullmatch(version)
    if development is not None and development.group(1) == "0":
        raise VersionIdentityError(
            f"{path}: development sequence must be positive, found {version!r}"
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


def check_generated_version(path: Path, expected_version: str) -> None:
    """Require one generated frontend output to carry the authored version."""
    artifact = _object(_read_json(path), path)
    generated_version = artifact.get("version")
    if not isinstance(generated_version, str):
        raise VersionIdentityError(f'{path}: expected a string "version" property')
    if generated_version != expected_version:
        raise VersionIdentityError(
            f"{path}: contains {generated_version!r}, expected {expected_version!r}"
        )


def check_version_identity(root: Path, generated_artifact_path: Path | None = None) -> None:
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

    check_generated_version(
        generated_artifact_path or web / "dist" / "version.json", authored_version
    )
