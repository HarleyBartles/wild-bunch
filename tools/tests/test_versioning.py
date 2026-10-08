from __future__ import annotations

import json
from pathlib import Path

import pytest

from tools.versioning import VersionIdentityError, check_version_identity, read_authored_version

ROOT = Path(__file__).resolve().parents[2]


def _write_valid_repo(root: Path, version: str = "0.1.0-dev.2") -> None:
    web = root / "src" / "WildBunch.Web"
    web.mkdir(parents=True)
    (root / "Directory.Build.props").write_text(
        f"<Project><PropertyGroup><Version>{version}</Version></PropertyGroup></Project>",
        encoding="utf-8",
    )
    package = {
        "name": "wildbunch-web",
        "private": True,
        "type": "module",
        "dependencies": {"react": "^18.3.1"},
    }
    lock = {
        "name": "wildbunch-web",
        "lockfileVersion": 3,
        "requires": True,
        "packages": {
            "": {"name": "wildbunch-web", "dependencies": {"react": "^18.3.1"}},
            "node_modules/react": {"version": "18.3.1"},
        },
    }
    (web / "package.json").write_text(json.dumps(package), encoding="utf-8")
    (web / "package-lock.json").write_text(json.dumps(lock), encoding="utf-8")
    (web / "dist").mkdir()
    (web / "dist" / "version.json").write_text(json.dumps({"version": version}), encoding="utf-8")


def test_version_identity_accepts_one_authored_version_and_matching_production_artifact(tmp_path):
    _write_valid_repo(tmp_path)

    assert read_authored_version(tmp_path / "Directory.Build.props") == "0.1.0-dev.2"
    check_version_identity(tmp_path)


@pytest.mark.parametrize(
    "props",
    [
        "<Project><PropertyGroup /></Project>",
        "<Project><PropertyGroup><Version>0.1.0-dev.2</Version></PropertyGroup>"
        "<PropertyGroup><Version>0.1.0-dev.3</Version></PropertyGroup></Project>",
        "<Project><PropertyGroup><Version>0.1.0</Version></PropertyGroup></Project>",
        "<Project><PropertyGroup><Version>0.1.0-dev.0</Version></PropertyGroup></Project>",
        "<Project><PropertyGroup><Version>not-semver</Version></PropertyGroup></Project>",
    ],
    ids=["missing", "duplicate", "release-version", "zero-dev-number", "malformed"],
)
def test_version_identity_rejects_missing_duplicate_or_unsupported_authored_versions(
    tmp_path, props
):
    _write_valid_repo(tmp_path)
    (tmp_path / "Directory.Build.props").write_text(props, encoding="utf-8")

    with pytest.raises(VersionIdentityError, match="Directory.Build.props"):
        check_version_identity(tmp_path)


@pytest.mark.parametrize("target", ["package", "lock-root", "lock-package-root"])
def test_version_identity_rejects_duplicate_npm_root_version_metadata(tmp_path, target):
    _write_valid_repo(tmp_path)
    web = tmp_path / "src" / "WildBunch.Web"
    package_path = web / "package.json"
    lock_path = web / "package-lock.json"
    package = json.loads(package_path.read_text(encoding="utf-8"))
    lock = json.loads(lock_path.read_text(encoding="utf-8"))
    if target == "package":
        package["version"] = "0.1.0-dev.2"
        package_path.write_text(json.dumps(package), encoding="utf-8")
    elif target == "lock-root":
        lock["version"] = "0.1.0-dev.2"
        lock_path.write_text(json.dumps(lock), encoding="utf-8")
    else:
        lock["packages"][""]["version"] = "0.1.0-dev.2"
        lock_path.write_text(json.dumps(lock), encoding="utf-8")

    with pytest.raises(VersionIdentityError, match="version"):
        check_version_identity(tmp_path)


@pytest.mark.parametrize(
    "artifact,remove_artifact",
    [
        (None, True),
        ("{", False),
        ("{}", False),
        ('{"version":"0.1.0-dev.1"}', False),
        ('{"version":null}', False),
    ],
    ids=["missing", "malformed-json", "missing-version", "stale", "invalid-version-type"],
)
def test_version_identity_rejects_missing_malformed_or_stale_build_identity(
    tmp_path, artifact, remove_artifact
):
    _write_valid_repo(tmp_path)
    path = tmp_path / "src" / "WildBunch.Web" / "dist" / "version.json"
    if remove_artifact:
        path.unlink()
    else:
        path.write_text(artifact, encoding="utf-8")

    with pytest.raises(VersionIdentityError, match="version.json"):
        check_version_identity(tmp_path)
