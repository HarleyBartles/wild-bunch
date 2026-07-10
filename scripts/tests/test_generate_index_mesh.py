"""Tests for generate_index_mesh.py"""

from pathlib import Path

import pytest

# Import the module under test
import sys
sys.path.insert(0, str(Path(__file__).parent.parent))
from generate_index_mesh import (
    is_gitignored,
    ALWAYS_EXCLUDED_DIR_NAMES,
    ALWAYS_EXCLUDED_FILE_NAMES,
)


class TestIsGitignored:
    """Tests for is_gitignored function."""

    def test_gitignore_respects_pytest_cache(self):
        """Should ignore .pytest_cache directory (in repo .gitignore)."""
        import generate_index_mesh
        original_root = generate_index_mesh.ROOT
        
        try:
            # Test with actual repo root
            pytest_cache = original_root / ".pytest_cache"
            assert is_gitignored(pytest_cache) is True
        finally:
            generate_index_mesh.ROOT = original_root

    def test_gitignore_respects_pyc_files(self):
        """Should ignore .pyc files (in repo .gitignore)."""
        import generate_index_mesh
        original_root = generate_index_mesh.ROOT
        
        try:
            # Test with actual repo root
            pyc_file = original_root / "test.pyc"
            assert is_gitignored(pyc_file) is True
        finally:
            generate_index_mesh.ROOT = original_root

    def test_gitignore_does_not_ignore_py_files(self):
        """Should not ignore .py files (not in repo .gitignore)."""
        import generate_index_mesh
        original_root = generate_index_mesh.ROOT
        
        try:
            # Test with actual repo root
            py_file = original_root / "test.py"
            assert is_gitignored(py_file) is False
        finally:
            generate_index_mesh.ROOT = original_root

    def test_always_excluded_git_directory(self):
        """Should always exclude .git directory regardless of .gitignore."""
        import generate_index_mesh
        original_root = generate_index_mesh.ROOT
        
        try:
            git_dir = original_root / ".git"
            assert is_gitignored(git_dir) is True
        finally:
            generate_index_mesh.ROOT = original_root

    def test_always_excluded_marketplace_source(self):
        """Should always exclude marketplace-source directory (submodule)."""
        import generate_index_mesh
        original_root = generate_index_mesh.ROOT
        
        try:
            marketplace_dir = original_root / ".agents" / "plugins" / "marketplace-source"
            assert is_gitignored(marketplace_dir) is True
        finally:
            generate_index_mesh.ROOT = original_root

    def test_always_excluded_output_directory(self):
        """Should always exclude output directory (binary artifacts)."""
        import generate_index_mesh
        original_root = generate_index_mesh.ROOT
        
        try:
            # Check that 'output' is in the always-excluded list
            assert "output" in generate_index_mesh.ALWAYS_EXCLUDED_DIR_NAMES
            
            # Test with a path that has 'output' as the directory name
            output_dir = original_root / "output"
            assert is_gitignored(output_dir) is True
        finally:
            generate_index_mesh.ROOT = original_root

    def test_no_gitignore_fallback(self):
        """Should return False when gitignore spec is not available."""
        import generate_index_mesh
        original_spec = generate_index_mesh.GITIGNORE_SPEC
        generate_index_mesh.GITIGNORE_SPEC = None
        
        try:
            test_file = Path("test.txt")
            assert is_gitignored(test_file) is False
        finally:
            generate_index_mesh.GITIGNORE_SPEC = original_spec

    def test_sdd_directory_ignored(self):
        """Should ignore .agents/superpowers/sdd/ directory (in repo .gitignore)."""
        import generate_index_mesh
        original_root = generate_index_mesh.ROOT
        
        try:
            sdd_dir = original_root / ".agents" / "superpowers" / "sdd"
            assert is_gitignored(sdd_dir) is True
        finally:
            generate_index_mesh.ROOT = original_root
