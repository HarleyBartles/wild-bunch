from __future__ import annotations

import hashlib
import json
from pathlib import Path

import pytest

from tools.release_artifacts import create_manifest, verify_manifest


def _expected_tree_digest(root: Path) -> tuple[int, str]:
    records = []
    for path in sorted(item for item in root.rglob("*") if item.is_file()):
        relative = path.relative_to(root).as_posix()
        records.append(f"{relative}\0{hashlib.sha256(path.read_bytes()).hexdigest()}\n")
    digest = hashlib.sha256("".join(records).encode("utf-8")).hexdigest()
    return len(records), digest


def _bundle(root: Path) -> None:
    for name in ("api", "web-public", "web-playtest"):
        target = root / name
        target.mkdir(parents=True)
        (target / "index.txt").write_text(f"{name}\n", encoding="utf-8")


def test_manifest_binds_independent_file_paths_and_bytes(tmp_path):
    _bundle(tmp_path)

    manifest = create_manifest(tmp_path, "a" * 40, "0.1.0-dev.197")

    for name in ("api", "web-public", "web-playtest"):
        expected_count, expected_digest = _expected_tree_digest(tmp_path / name)
        assert manifest["artifacts"][name] == {
            "fileCount": expected_count,
            "path": name,
            "sha256": expected_digest,
        }
    original = manifest["artifacts"]["api"]["sha256"]
    (tmp_path / "api" / "index.txt").rename(tmp_path / "api" / "renamed.txt")
    assert (
        create_manifest(tmp_path, "a" * 40, "0.1.0-dev.197")["artifacts"]["api"]["sha256"]
        != original
    )


@pytest.mark.parametrize("mutation", ["changed", "missing", "added"])
def test_verifier_rejects_artifact_tree_changes(tmp_path, mutation):
    _bundle(tmp_path)
    manifest = create_manifest(tmp_path, "a" * 40, "0.1.0-dev.197")
    (tmp_path / "release-manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
    artifact = tmp_path / "api" / "index.txt"
    if mutation == "changed":
        artifact.write_text("changed\n", encoding="utf-8")
    elif mutation == "missing":
        artifact.unlink()
    else:
        (tmp_path / "api" / "new.txt").write_text("new\n", encoding="utf-8")

    with pytest.raises(ValueError, match="api"):
        verify_manifest(tmp_path, "a" * 40, "0.1.0-dev.197")
