"""Tests for paired bash and PowerShell script entrypoints."""

from pathlib import Path

import pytest

SCRIPTS_DIR = Path(__file__).parent.parent

SCRIPT_BASES = [
    "ci-preflight",
    "dev-servers",
    "image_asset_pipeline",
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


@pytest.mark.parametrize("script_base", ["ci-preflight", "dev-servers", "postgres-dev"])
def test_bash_entrypoint_is_not_a_powershell_bridge(script_base):
    """Linux bash entrypoints should not depend on PowerShell."""
    contents = (SCRIPTS_DIR / f"{script_base}.sh").read_text(encoding="utf-8")
    assert "pwsh" not in contents
    assert "powershell" not in contents
    assert "powershell.exe" not in contents
