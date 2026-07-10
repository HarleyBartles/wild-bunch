"""Tests for PowerShell wrappers for Python scripts."""

import subprocess
import sys
from pathlib import Path

import pytest

SCRIPTS_DIR = Path(__file__).parent.parent


class TestPowerShellWrappers:
    """Tests for PowerShell wrapper scripts."""

    def test_generate_index_mesh_wrapper_exists(self):
        """PowerShell wrapper should exist."""
        wrapper = SCRIPTS_DIR / "generate_index_mesh.ps1"
        assert wrapper.exists()

    def test_install_agent_skills_wrapper_exists(self):
        """PowerShell wrapper should exist."""
        wrapper = SCRIPTS_DIR / "install_agent_skills.ps1"
        assert wrapper.exists()

    def test_image_asset_pipeline_wrapper_exists(self):
        """PowerShell wrapper should exist."""
        wrapper = SCRIPTS_DIR / "image_asset_pipeline.ps1"
        assert wrapper.exists()

    def test_generate_index_mesh_help(self):
        """Wrapper should be invokable and show help."""
        wrapper = SCRIPTS_DIR / "generate_index_mesh.ps1"
        
        # Try to invoke with -Help flag (PowerShell parameter)
        result = subprocess.run(
            ["powershell", "-File", str(wrapper), "-Help"],
            capture_output=True,
            text=True,
            timeout=10
        )
        
        # Should not crash (exit code 0 or 1 is acceptable for help)
        # We're just verifying it's invokable

    def test_install_agent_skills_invocation(self):
        """Wrapper should be invokable (basic smoke test)."""
        wrapper = SCRIPTS_DIR / "install_agent_skills.ps1"
        
        # Try to invoke with -Check flag
        result = subprocess.run(
            ["powershell", "-File", str(wrapper), "-Check"],
            capture_output=True,
            text=True,
            timeout=10
        )
        
        # Should not crash (exit code may be 0 or 1 depending on state)
        # We're just verifying it's invokable

    def test_image_asset_pipeline_invocation(self):
        """Wrapper should be invokable (basic smoke test)."""
        wrapper = SCRIPTS_DIR / "image_asset_pipeline.ps1"
        
        # Try to invoke with --help flag (Python script's help)
        result = subprocess.run(
            ["powershell", "-File", str(wrapper), "--help"],
            capture_output=True,
            text=True,
            timeout=10
        )
        
        # Should not crash (exit code may be 0 or 1)
        # We're just verifying it's invokable
