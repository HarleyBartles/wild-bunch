"""Tests for paired bash and PowerShell script entrypoints."""

from pathlib import Path

import pytest

SCRIPTS_DIR = Path(__file__).parent.parent

SCRIPT_BASES = [
    "ci-preflight",
    "dev-servers",
    "generate_index_mesh",
    "image_asset_pipeline",
    "install_agent_skills",
    "postgres-dev",
]


@pytest.mark.parametrize("script_base", SCRIPT_BASES)
def test_bash_entrypoint_exists(script_base):
    """Every operational script should have a bash entrypoint."""
    assert (SCRIPTS_DIR / f"{script_base}.sh").exists()


@pytest.mark.parametrize("script_base", SCRIPT_BASES)
def test_powershell_entrypoint_exists(script_base):
    """Every operational script should have a PowerShell entrypoint."""
    assert (SCRIPTS_DIR / f"{script_base}.ps1").exists()
