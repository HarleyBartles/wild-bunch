from __future__ import annotations

from pathlib import Path
import sys
from types import SimpleNamespace

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
import refresh_marketplace_skills


def test_refresh_bootstraps_pinned_vendor_deployer_only_for_child_lifetime(tmp_path: Path) -> None:
    source_root = tmp_path / "source"
    deployer = source_root / "skills" / "repo-shape" / "scripts" / "deploy_vendor_profiles.py"
    deployer.parent.mkdir(parents=True)
    deployer.write_text("pinned deployer", encoding="utf-8")
    refresher = (
        source_root
        / "dist"
        / "plugins"
        / "repo-worker-pack"
        / "skills"
        / "refreshing-installed-skills"
        / "scripts"
        / "refresh_installed_skills.py"
    )
    refresher.parent.mkdir(parents=True)
    refresher.write_text("pinned refresher", encoding="utf-8")
    root = tmp_path / "consumer"
    root.mkdir()
    observed: list[Path] = []

    def run(command, *, cwd, check):
        compatibility_path = root / "skills/repo-shape/scripts/deploy_vendor_profiles.py"
        observed.append(compatibility_path)
        assert compatibility_path.read_text(encoding="utf-8") == "pinned deployer"
        assert command[-2:] == ["--check", "--no-roll-marketplace-source"]
        assert cwd == root
        assert check is False
        return SimpleNamespace(returncode=0)

    assert refresh_marketplace_skills.run_refresh(
        root,
        source_root,
        ["--check", "--no-roll-marketplace-source"],
        runner=run,
    ) == 0
    assert len(observed) == 1
    assert not observed[0].exists()
    assert not (root / "skills").exists()


def test_refresh_refuses_to_replace_an_unowned_compatibility_file(tmp_path: Path) -> None:
    source_root = tmp_path / "source"
    deployer = source_root / "skills" / "repo-shape" / "scripts" / "deploy_vendor_profiles.py"
    deployer.parent.mkdir(parents=True)
    deployer.write_text("pinned deployer", encoding="utf-8")
    refresher = (
        source_root
        / "dist"
        / "plugins"
        / "repo-worker-pack"
        / "skills"
        / "refreshing-installed-skills"
        / "scripts"
        / "refresh_installed_skills.py"
    )
    refresher.parent.mkdir(parents=True)
    refresher.write_text("pinned refresher", encoding="utf-8")
    root = tmp_path / "consumer"
    target = root / "skills/repo-shape/scripts/deploy_vendor_profiles.py"
    target.parent.mkdir(parents=True)
    target.write_text("unowned content", encoding="utf-8")

    with pytest.raises(RuntimeError, match="already exists with different content"):
        refresh_marketplace_skills.run_refresh(root, source_root, ["--check"], runner=lambda *_args, **_kwargs: None)
    assert target.read_text(encoding="utf-8") == "unowned content"
