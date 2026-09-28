#!/usr/bin/env python3
"""Tests for canonical changed-surface derivation (Plan 3 Task 1)."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR.parent / "scripts"))

from review_core import model  # noqa: E402
from review_core import surfaces  # noqa: E402


MODIFIED = """\
diff --git a/src/foo.py b/src/foo.py
index 1111111..2222222 100644
--- a/src/foo.py
+++ b/src/foo.py
@@ -10,2 +10,2 @@ def f():
 context line
-old line
+new line
@@ -30,1 +30,2 @@
 ctx
+added
"""

ADDED = """\
diff --git a/src/new.py b/src/new.py
new file mode 100644
index 0000000..3333333
--- /dev/null
+++ b/src/new.py
@@ -0,0 +1,2 @@
+one
+two
"""

DELETED = """\
diff --git a/src/old.py b/src/old.py
deleted file mode 100644
index 3333333..0000000
--- a/src/old.py
+++ /dev/null
@@ -1,2 +0,0 @@
-one
-two
"""

RENAMED = """\
diff --git a/src/old_name.py b/src/new_name.py
similarity index 90%
rename from src/old_name.py
rename to src/new_name.py
index 1111111..2222222 100644
--- a/src/old_name.py
+++ b/src/new_name.py
@@ -1,1 +1,1 @@
-x
+y
"""

BINARY = """\
diff --git a/img.bin b/img.bin
index 1111111..2222222 100644
Binary files a/img.bin and b/img.bin differ
"""


class TestParseDiffSurfaces:
    def test_modified_file_with_hunks(self):
        result = surfaces.parse_diff_surfaces(MODIFIED)
        assert len(result) == 1
        entry = result[0]
        assert entry["path"] == "src/foo.py"
        assert entry["change_kind"] == "modified"
        assert entry["renamed_from"] is None
        assert entry["hunks"] == [
            {"old_start": 10, "old_lines": 2, "new_start": 10, "new_lines": 2},
            {"old_start": 30, "old_lines": 1, "new_start": 30, "new_lines": 2},
        ]

    def test_added_and_deleted_files(self):
        result = surfaces.parse_diff_surfaces(ADDED + DELETED)
        assert [e["path"] for e in result] == ["src/new.py", "src/old.py"]
        added, deleted = result[0], result[1]
        assert added["change_kind"] == "added"
        assert added["renamed_from"] is None
        assert added["hunks"] == [{"old_start": 0, "old_lines": 0, "new_start": 1, "new_lines": 2}]
        assert deleted["change_kind"] == "deleted"
        assert deleted["renamed_from"] is None
        assert deleted["hunks"] == [{"old_start": 1, "old_lines": 2, "new_start": 0, "new_lines": 0}]

    def test_rename_records_renamed_from(self):
        result = surfaces.parse_diff_surfaces(RENAMED)
        assert len(result) == 1
        entry = result[0]
        assert entry["path"] == "src/new_name.py"
        assert entry["change_kind"] == "renamed"
        assert entry["renamed_from"] == "src/old_name.py"
        assert entry["hunks"] == [{"old_start": 1, "old_lines": 1, "new_start": 1, "new_lines": 1}]

    def test_pure_rename_no_hunks(self):
        diff = "diff --git a/a.py b/b.py\nsimilarity index 100%\nrename from a/a.py\nrename to b/b.py\n"
        result = surfaces.parse_diff_surfaces(diff)
        assert result == ({"path": "b.py", "change_kind": "renamed", "renamed_from": "a/a.py", "hunks": []},)

    def test_binary_file_empty_hunks(self):
        result = surfaces.parse_diff_surfaces(BINARY)
        assert len(result) == 1
        assert result[0]["path"] == "img.bin"
        assert result[0]["change_kind"] == "modified"
        assert result[0]["hunks"] == []

    def test_empty_diff_returns_empty(self):
        assert surfaces.parse_diff_surfaces("") == ()
        assert surfaces.parse_diff_surfaces("diff-bytes") == ()

    def test_malformed_git_header_fails_closed(self):
        with pytest.raises(surfaces.SurfaceParseError):
            surfaces.parse_diff_surfaces("diff --git a/x\n")
        with pytest.raises(surfaces.SurfaceParseError):
            surfaces.parse_diff_surfaces("diff --git x b/y\nindex 1..2\n")
        # '---' without '+++' is a malformed header triple
        with pytest.raises(surfaces.SurfaceParseError):
            surfaces.parse_diff_surfaces("diff --git a/x b/x\nindex 1..2 100644\n--- a/x\n")

    def test_malformed_hunk_header_fails_closed(self):
        with pytest.raises(surfaces.SurfaceParseError):
            surfaces.parse_diff_surfaces("diff --git a/x b/x\nindex 1..2 100644\n--- a/x\n+++ b/x\n@@ -a +b @@\n")
        # truncated hunk body also fails closed
        with pytest.raises(surfaces.SurfaceParseError):
            surfaces.parse_diff_surfaces(
                "diff --git a/x b/x\nindex 1..2 100644\n--- a/x\n+++ b/x\n@@ -1,3 +1,3 @@\n ctx\n"
            )

    def test_paths_sorted_and_unique(self):
        combined = (
            "diff --git a/z.py b/z.py\n"
            "index 1..2 100644\n--- a/z.py\n+++ b/z.py\n@@ -1 +1 @@\n-a\n+b\n"
            "diff --git a/a.py b/a.py\n"
            "index 1..2 100644\n--- a/a.py\n+++ b/a.py\n@@ -1 +1 @@\n-c\n+d\n"
        )
        result = surfaces.parse_diff_surfaces(combined)
        assert [e["path"] for e in result] == ["a.py", "z.py"]

    def test_duplicate_surface_path_fails_closed(self):
        dup = (
            "diff --git a/x.py b/x.py\n"
            "index 1..2 100644\n--- a/x.py\n+++ b/x.py\n@@ -1 +1 @@\n-a\n+b\n"
            "diff --git a/x.py b/x.py\n"
            "index 3..4 100644\n--- a/x.py\n+++ b/x.py\n@@ -1 +1 @@\n-c\n+d\n"
        )
        with pytest.raises(surfaces.SurfaceParseError):
            surfaces.parse_diff_surfaces(dup)

    def test_quoted_paths_unescaped(self):
        diff = (
            'diff --git "a/dir x/f.py" "b/dir x/f.py"\n'
            "index 1..2 100644\n"
            '--- "a/dir x/f.py"\n'
            '+++ "b/dir x/f.py"\n'
            "@@ -1 +1 @@\n-a\n+b\n"
        )
        result = surfaces.parse_diff_surfaces(diff)
        assert result[0]["path"] == "dir x/f.py"


class TestSurfacesDocument:
    def test_canonical_bytes_round_trip(self):
        entries = surfaces.parse_diff_surfaces(MODIFIED + ADDED)
        raw = surfaces.surfaces_document(entries)
        assert model.strict_json_loads(raw, source="surfaces.json") == list(entries)

    def test_empty_document(self):
        assert model.strict_json_loads(surfaces.surfaces_document(()), source="s") == []
