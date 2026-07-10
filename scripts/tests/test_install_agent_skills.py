"""Tests for install_agent_skills.py"""

import json
import shutil
from pathlib import Path
from tempfile import TemporaryDirectory

import pytest

# Import the module under test
import sys
sys.path.insert(0, str(Path(__file__).parent.parent))
from install_agent_skills import (
    _load_json,
    _files_identical,
    _load_provenance,
    _has_skill_dirs,
)


class TestLoadJson:
    """Tests for _load_json function."""

    def test_load_valid_json(self):
        """Should load and parse a valid JSON file."""
        with TemporaryDirectory() as tmpdir:
            test_file = Path(tmpdir) / "test.json"
            test_data = {"key": "value", "number": 42}
            with open(test_file, "w") as f:
                json.dump(test_data, f)
            
            result = _load_json(test_file)
            assert result == test_data

    def test_load_missing_file(self):
        """Should raise FileNotFoundError for missing file."""
        with TemporaryDirectory() as tmpdir:
            test_file = Path(tmpdir) / "nonexistent.json"
            
            with pytest.raises(FileNotFoundError):
                _load_json(test_file)

    def test_load_invalid_json(self):
        """Should raise JSONDecodeError for invalid JSON."""
        with TemporaryDirectory() as tmpdir:
            test_file = Path(tmpdir) / "invalid.json"
            with open(test_file, "w") as f:
                f.write("{ invalid json }")
            
            with pytest.raises(json.JSONDecodeError):
                _load_json(test_file)


class TestFilesIdentical:
    """Tests for _files_identical function."""

    def test_identical_directories(self):
        """Should return True for directories with identical content."""
        with TemporaryDirectory() as tmpdir:
            dir1 = Path(tmpdir) / "dir1"
            dir2 = Path(tmpdir) / "dir2"
            dir1.mkdir()
            dir2.mkdir()
            
            # Create identical files
            (dir1 / "file1.txt").write_text("content")
            (dir2 / "file1.txt").write_text("content")
            (dir1 / "subdir").mkdir()
            (dir2 / "subdir").mkdir()
            (dir1 / "subdir" / "file2.txt").write_text("nested")
            (dir2 / "subdir" / "file2.txt").write_text("nested")
            
            assert _files_identical(dir1, dir2) is True

    def test_different_file_content(self):
        """Should return False for directories with different file content."""
        with TemporaryDirectory() as tmpdir:
            dir1 = Path(tmpdir) / "dir1"
            dir2 = Path(tmpdir) / "dir2"
            dir1.mkdir()
            dir2.mkdir()
            
            (dir1 / "file1.txt").write_text("content1")
            (dir2 / "file1.txt").write_text("content2")
            
            assert _files_identical(dir1, dir2) is False

    def test_different_file_structure(self):
        """Should return False for directories with different file structure."""
        with TemporaryDirectory() as tmpdir:
            dir1 = Path(tmpdir) / "dir1"
            dir2 = Path(tmpdir) / "dir2"
            dir1.mkdir()
            dir2.mkdir()
            
            (dir1 / "file1.txt").write_text("content")
            (dir2 / "file2.txt").write_text("content")
            
            assert _files_identical(dir1, dir2) is False

    def test_one_directory_missing(self):
        """Should return False if one directory doesn't exist."""
        with TemporaryDirectory() as tmpdir:
            dir1 = Path(tmpdir) / "dir1"
            dir2 = Path(tmpdir) / "dir2"
            dir1.mkdir()
            
            assert _files_identical(dir1, dir2) is False
            assert _files_identical(dir2, dir1) is False

    def test_empty_directories(self):
        """Should return True for two empty directories."""
        with TemporaryDirectory() as tmpdir:
            dir1 = Path(tmpdir) / "dir1"
            dir2 = Path(tmpdir) / "dir2"
            dir1.mkdir()
            dir2.mkdir()
            
            assert _files_identical(dir1, dir2) is True

    def test_different_file_sizes(self):
        """Should return False for files with different sizes (optimization check)."""
        with TemporaryDirectory() as tmpdir:
            dir1 = Path(tmpdir) / "dir1"
            dir2 = Path(tmpdir) / "dir2"
            dir1.mkdir()
            dir2.mkdir()
            
            (dir1 / "file1.txt").write_text("short")
            (dir2 / "file1.txt").write_text("much longer content")
            
            assert _files_identical(dir1, dir2) is False


class TestLoadProvenance:
    """Tests for _load_provenance function."""

    def test_load_existing_provenance(self):
        """Should load existing provenance file."""
        with TemporaryDirectory() as tmpdir:
            provenance_path = Path(tmpdir) / ".provenance.json"
            test_provenance = {
                "sha": "abc123",
                "syncedAt": "2026-07-09T10:00:00Z",
                "syncedPlugins": ["plugin1"],
                "syncedSkills": 5
            }
            with open(provenance_path, "w") as f:
                json.dump(test_provenance, f)
            
            # Mock the PROVENANCE_PATH
            import install_agent_skills
            original_path = install_agent_skills.PROVENANCE_PATH
            install_agent_skills.PROVENANCE_PATH = provenance_path
            
            try:
                result = _load_provenance()
                assert result == test_provenance
            finally:
                install_agent_skills.PROVENANCE_PATH = original_path

    def test_load_missing_provenance(self):
        """Should return None when provenance file doesn't exist."""
        with TemporaryDirectory() as tmpdir:
            provenance_path = Path(tmpdir) / ".provenance.json"
            
            import install_agent_skills
            original_path = install_agent_skills.PROVENANCE_PATH
            install_agent_skills.PROVENANCE_PATH = provenance_path
            
            try:
                result = _load_provenance()
                assert result is None
            finally:
                install_agent_skills.PROVENANCE_PATH = original_path

    def test_load_invalid_provenance(self):
        """Should return None for malformed provenance file."""
        with TemporaryDirectory() as tmpdir:
            provenance_path = Path(tmpdir) / ".provenance.json"
            with open(provenance_path, "w") as f:
                f.write("{ invalid json }")
            
            import install_agent_skills
            original_path = install_agent_skills.PROVENANCE_PATH
            install_agent_skills.PROVENANCE_PATH = provenance_path
            
            try:
                result = _load_provenance()
                assert result is None
            finally:
                install_agent_skills.PROVENANCE_PATH = original_path


class TestHasSkillDirs:
    """Tests for _has_skill_dirs function."""

    def test_has_skill_directories(self):
        """Should return True when skill directories exist."""
        with TemporaryDirectory() as tmpdir:
            skills_root = Path(tmpdir) / "skills"
            skills_root.mkdir()
            (skills_root / "skill1").mkdir()
            (skills_root / "skill2").mkdir()
            
            import install_agent_skills
            original_path = install_agent_skills.SKILLS_ROOT
            install_agent_skills.SKILLS_ROOT = skills_root
            
            try:
                result = _has_skill_dirs()
                assert result is True
            finally:
                install_agent_skills.SKILLS_ROOT = original_path

    def test_no_skill_directories(self):
        """Should return False when no skill directories exist."""
        with TemporaryDirectory() as tmpdir:
            skills_root = Path(tmpdir) / "skills"
            skills_root.mkdir()
            
            import install_agent_skills
            original_path = install_agent_skills.SKILLS_ROOT
            install_agent_skills.SKILLS_ROOT = skills_root
            
            try:
                result = _has_skill_dirs()
                assert result is False
            finally:
                install_agent_skills.SKILLS_ROOT = original_path

    def test_skills_root_missing(self):
        """Should return False when skills root doesn't exist."""
        with TemporaryDirectory() as tmpdir:
            skills_root = Path(tmpdir) / "skills"
            
            import install_agent_skills
            original_path = install_agent_skills.SKILLS_ROOT
            install_agent_skills.SKILLS_ROOT = skills_root
            
            try:
                result = _has_skill_dirs()
                assert result is False
            finally:
                install_agent_skills.SKILLS_ROOT = original_path
