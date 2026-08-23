#!/usr/bin/env python3
"""Focused tests for diff_slicer.py helpers."""

import importlib.util
import json
import subprocess
import tempfile
import unittest
from pathlib import Path


SKILL_DIR = Path(__file__).resolve().parent.parent
DIFF_SLICER = SKILL_DIR / "scripts" / "diff_slicer.py"


def _load_diff_slicer():
    spec = importlib.util.spec_from_file_location("diff_slicer", DIFF_SLICER)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class TestDiffSlicerCLI(unittest.TestCase):
    def test_diff_slicer_check(self):
        result = subprocess.run(
            ["py", "-3", str(DIFF_SLICER), "--check"],
            capture_output=True,
            text=True,
        )
        self.assertEqual(result.returncode, 0)


class TestDiffSlicerHelpers(unittest.TestCase):
    def test_glob_match_supports_double_star(self):
        module = _load_diff_slicer()
        source = "codex-marketplace/plugins/**/skills/**/*.md"
        in_source = "codex-marketplace/plugins/superpowers-plus/skills/iterative-review/SKILL.md"
        in_mirror = ".agents/skills/iterative-review/SKILL.md"
        self.assertTrue(module._glob_match(source, in_source))
        self.assertFalse(module._glob_match(source, in_mirror))

    def test_installed_mirror_paths_excludes_source_duplicates(self):
        module = _load_diff_slicer()
        changed = [
            "codex-marketplace/plugins/superpowers-plus/skills/iterative-review/SKILL.md",
            ".agents/skills/iterative-review/SKILL.md",
            ".agents/skills/new-skill/SKILL.md",
        ]
        mirrors = module._installed_mirror_paths(changed, Path("."))
        self.assertIn(".agents/skills/iterative-review/SKILL.md", mirrors)
        self.assertNotIn(".agents/skills/new-skill/SKILL.md", mirrors)

    def test_slice_hunks_keeps_matching_paths(self):
        module = _load_diff_slicer()
        diff = (
            "diff --git a/foo.py b/foo.py\n"
            "--- a/foo.py\n"
            "+++ b/foo.py\n"
            "@@ -1 +1 @@\n"
            "-x\n"
            "+y\n"
            "diff --git a/bar.md b/bar.md\n"
            "--- a/bar.md\n"
            "+++ b/bar.md\n"
            "@@ -1 +1 @@\n"
            "-x\n"
            "+y\n"
        )
        result = module._slice_hunks(diff, ["**/*.py"], set())
        self.assertIn("diff --git a/foo.py b/foo.py", result)
        self.assertNotIn("diff --git a/bar.md b/bar.md", result)

    def test_slice_hunks_respects_exclude_paths(self):
        module = _load_diff_slicer()
        mirror = ".agents/skills/iterative-review/SKILL.md"
        source = "codex-marketplace/plugins/superpowers-plus/skills/iterative-review/SKILL.md"
        diff = (
            f"diff --git a/{mirror} b/{mirror}\n"
            f"--- a/{mirror}\n"
            f"+++ b/{mirror}\n"
            "@@ -1 +1 @@\n"
            "-x\n"
            "+y\n"
            f"diff --git a/{source} b/{source}\n"
            f"--- a/{source}\n"
            f"+++ b/{source}\n"
            "@@ -1 +1 @@\n"
            "-x\n"
            "+y\n"
        )
        result = module._slice_hunks(diff, ["**/*.md"], {mirror})
        self.assertIn(f"diff --git a/{source}", result)
        self.assertNotIn(f"diff --git a/{mirror}", result)

    def test_main_full_diff_for_lens_with_no_globs(self):
        module = _load_diff_slicer()
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            diff_path = scratch / "review-abc..def.diff"
            diff_path.write_text(
                "diff --git a/foo.py b/foo.py\n--- a/foo.py\n+++ b/foo.py\n@@ -1 +1 @@\n-x\n+y\n",
                encoding="utf-8",
            )
            no_globs_profile = scratch / "reviewer-no-globs.md"
            no_globs_profile.write_text("# No globs\n", encoding="utf-8")
            (scratch / "lenses.jsonl").write_text(
                json.dumps(
                    {
                        "lens": "reviewer-no-globs",
                        "profile_path": str(no_globs_profile),
                        "output_path": str(scratch / "review-log-reviewer-no-globs.md"),
                    }
                )
                + "\n",
                encoding="utf-8",
            )
            (scratch / "review-state.json").write_text(
                json.dumps(
                    {
                        "scratch_dir": str(scratch),
                        "diff_path": str(diff_path),
                        "repo_root": str(td),
                    }
                ),
                encoding="utf-8",
            )
            code = module.main(["--state", str(scratch / "review-state.json"), "--apply"])
            self.assertEqual(code, 0)
            slice_path = scratch / "lens-reviewer-no-globs-review-abc..def.diff"
            self.assertFalse(slice_path.exists())
            lenses = (scratch / "lenses.jsonl").read_text(encoding="utf-8").splitlines()
            entry = json.loads(lenses[0])
            self.assertEqual(entry["diff_path"], str(diff_path))

    def test_main_full_diff_for_reviewer_fast(self):
        module = _load_diff_slicer()
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            diff_path = scratch / "review-abc..def.diff"
            diff_path.write_text(
                "diff --git a/foo.py b/foo.py\n--- a/foo.py\n+++ b/foo.py\n@@ -1 +1 @@\n-x\n+y\n",
                encoding="utf-8",
            )
            profile = SKILL_DIR / ".." / "selecting-a-subagent" / "assets" / "reviewer-fast.md"
            (scratch / "lenses.jsonl").write_text(
                json.dumps(
                    {
                        "lens": "reviewer-fast",
                        "profile_path": str(profile),
                        "output_path": str(scratch / "review-log-reviewer-fast.md"),
                    }
                )
                + "\n",
                encoding="utf-8",
            )
            (scratch / "review-state.json").write_text(
                json.dumps(
                    {
                        "scratch_dir": str(scratch),
                        "diff_path": str(diff_path),
                        "repo_root": str(td),
                    }
                ),
                encoding="utf-8",
            )
            code = module.main(["--state", str(scratch / "review-state.json"), "--apply"])
            self.assertEqual(code, 0)
            slice_path = scratch / "lens-reviewer-fast-review-abc..def.diff"
            self.assertFalse(slice_path.exists())
            lenses = (scratch / "lenses.jsonl").read_text(encoding="utf-8").splitlines()
            entry = json.loads(lenses[0])
            self.assertEqual(entry["diff_path"], str(diff_path))

    def test_main_slices_for_selected_lenses(self):
        module = _load_diff_slicer()
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            diff_path = scratch / "review-abc..def.diff"
            diff_path.write_text(
                "diff --git a/foo.py b/foo.py\n"
                "--- a/foo.py\n"
                "+++ b/foo.py\n"
                "@@ -1 +1 @@\n"
                "-x\n"
                "+y\n"
                "diff --git a/bar.md b/bar.md\n"
                "--- a/bar.md\n"
                "+++ b/bar.md\n"
                "@@ -1 +1 @@\n"
                "-x\n"
                "+y\n",
                encoding="utf-8",
            )
            profile = SKILL_DIR / ".." / "selecting-a-subagent" / "assets" / "reviewer-skills.md"
            (scratch / "lenses.jsonl").write_text(
                json.dumps(
                    {
                        "lens": "reviewer-skills",
                        "profile_path": str(profile),
                        "output_path": str(scratch / "review-log-reviewer-skills.md"),
                    }
                )
                + "\n",
                encoding="utf-8",
            )
            (scratch / "review-state.json").write_text(
                json.dumps(
                    {
                        "scratch_dir": str(scratch),
                        "diff_path": str(diff_path),
                        "repo_root": str(td),
                    }
                ),
                encoding="utf-8",
            )
            code = module.main(["--state", str(scratch / "review-state.json"), "--apply"])
            self.assertEqual(code, 0)
            slice_path = scratch / "lens-reviewer-skills-review-abc..def.diff"
            self.assertTrue(slice_path.exists())
            text = slice_path.read_text(encoding="utf-8")
            self.assertIn("bar.md", text)
            self.assertNotIn("foo.py", text)


if __name__ == "__main__":
    unittest.main()
