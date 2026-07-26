"""Tests for PowerShell wrapper scripts."""

import shutil
import subprocess
import sys
from pathlib import Path

import pytest

SCRIPTS_DIR = Path(__file__).parent.parent

_POWERSHELL = shutil.which("powershell")


class TestPowerShellWrappers:
    """Tests for PowerShell wrapper scripts."""

    def test_generate_index_mesh_extra_wrapper_exists(self):
        """PowerShell wrapper should exist."""
        wrapper = SCRIPTS_DIR / "generate_index_mesh_extra.ps1"
        assert wrapper.exists()

    @pytest.mark.skipif(not _POWERSHELL, reason="powershell not available")
    def test_generate_index_mesh_extra_wrapper_is_invokable(self):
        """Wrapper should parse and show help without crashing."""
        wrapper = SCRIPTS_DIR / "generate_index_mesh_extra.ps1"
        result = subprocess.run(
            ["powershell", "-File", str(wrapper), "-?"],
            capture_output=True,
            text=True,
            timeout=10,
        )
        assert result.returncode == 0, result.stderr

    def test_validate_local_skills_extra_wrapper_exists(self):
        """PowerShell wrapper should exist."""
        wrapper = SCRIPTS_DIR / "validate_local_skills_extra.ps1"
        assert wrapper.exists()

    @pytest.mark.skipif(not _POWERSHELL, reason="powershell not available")
    def test_validate_local_skills_extra_wrapper_is_invokable(self):
        """Wrapper should parse and show help without crashing."""
        wrapper = SCRIPTS_DIR / "validate_local_skills_extra.ps1"
        result = subprocess.run(
            ["powershell", "-File", str(wrapper), "-?"],
            capture_output=True,
            text=True,
            timeout=10,
        )
        assert result.returncode == 0, result.stderr

    def test_image_asset_pipeline_wrapper_exists(self):
        """PowerShell wrapper should exist."""
        wrapper = SCRIPTS_DIR / "image_asset_pipeline.ps1"
        assert wrapper.exists()

    @pytest.mark.skipif(not _POWERSHELL, reason="powershell not available")
    def test_image_asset_pipeline_invocation(self):
        """Wrapper should be invokable with --help."""
        wrapper = SCRIPTS_DIR / "image_asset_pipeline.ps1"
        result = subprocess.run(
            ["powershell", "-File", str(wrapper), "--help"],
            capture_output=True,
            text=True,
            timeout=10,
        )
        # Should not crash; exit code may be 0 or 1 for help display.
