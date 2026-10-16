"""Create and verify immutable local release-bundle manifests."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

ARTIFACT_PATHS = {"api": "api", "web-public": "web-public", "web-playtest": "web-playtest"}
MANIFEST_NAME = "release-manifest.json"


def _tree_identity(root: Path) -> dict[str, object]:
    if not root.is_dir():
        raise ValueError(f"artifact tree is missing: {root.name}")
    records: list[str] = []
    for path in sorted(root.rglob("*")):
        if path.is_symlink():
            raise ValueError(f"artifact tree contains a symbolic link: {path.relative_to(root)}")
        if path.is_file():
            relative = path.relative_to(root).as_posix()
            content_hash = hashlib.sha256(path.read_bytes()).hexdigest()
            records.append(f"{relative}\0{content_hash}\n")
    if not records:
        raise ValueError(f"artifact tree is empty: {root.name}")
    digest = hashlib.sha256("".join(records).encode("utf-8")).hexdigest()
    return {"fileCount": len(records), "path": root.name, "sha256": digest}


def create_manifest(bundle: Path, source_commit: str, version: str) -> dict[str, object]:
    """Describe the fixed release artifact trees currently under bundle."""
    artifacts = {
        name: _tree_identity(bundle / relative) for name, relative in ARTIFACT_PATHS.items()
    }
    return {
        "formatVersion": 1,
        "sourceCommit": source_commit,
        "version": version,
        "artifacts": artifacts,
    }


def verify_manifest(bundle: Path, source_commit: str, version: str) -> dict[str, object]:
    """Verify bundle identity, fixed artifact paths and all current tree digests."""
    manifest_path = bundle / MANIFEST_NAME
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ValueError(f"release bundle manifest is missing or invalid: {exc}") from exc
    if not isinstance(manifest, dict):
        raise ValueError("release bundle manifest must be a JSON object")
    if manifest.get("formatVersion") != 1:
        raise ValueError("release bundle manifest has unsupported formatVersion")
    if manifest.get("sourceCommit") != source_commit:
        raise ValueError("release bundle source commit does not match current HEAD")
    if manifest.get("version") != version:
        raise ValueError("release bundle product version does not match Directory.Build.props")
    artifacts = manifest.get("artifacts")
    if not isinstance(artifacts, dict) or set(artifacts) != set(ARTIFACT_PATHS):
        raise ValueError("release bundle manifest has an incomplete artifact list")
    for name, relative in ARTIFACT_PATHS.items():
        actual = _tree_identity(bundle / relative)
        if artifacts[name] != actual:
            raise ValueError(f"release bundle artifact verification failed: {name}")
    return manifest
