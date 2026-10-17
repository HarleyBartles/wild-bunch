from __future__ import annotations

import os
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from tools.deployment.source import TrustedSourceError, codebuild_main_source_version, resolve_trusted_source


def git(directory: Path, *arguments: str) -> str:
    return subprocess.run(
        ["git", *arguments], cwd=directory, check=True, capture_output=True, text=True
    ).stdout.strip()


class TrustedSourceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.previous_directory = Path.cwd()
        self.temporary = tempfile.TemporaryDirectory()
        root = Path(self.temporary.name)
        self.remote = root / "origin.git"
        self.seed = root / "seed"
        self.checkout = root / "checkout"
        self.remote.mkdir()
        git(root, "init", "--bare", "--initial-branch=main", str(self.remote))
        git(root, "init", "--initial-branch=main", str(self.seed))
        git(self.seed, "config", "user.name", "Deployment test")
        git(self.seed, "config", "user.email", "deployment-test@example.invalid")
        (self.seed / "history.txt").write_text("first\n", encoding="utf-8")
        git(self.seed, "add", "history.txt")
        git(self.seed, "commit", "-m", "first main revision")
        self.historical_main = git(self.seed, "rev-parse", "HEAD")
        (self.seed / "history.txt").write_text("second\n", encoding="utf-8")
        git(self.seed, "commit", "-am", "second main revision")
        git(self.seed, "checkout", "-b", "unmerged")
        (self.seed / "unmerged.txt").write_text("untrusted\n", encoding="utf-8")
        git(self.seed, "add", "unmerged.txt")
        git(self.seed, "commit", "-m", "unmerged branch")
        self.unmerged = git(self.seed, "rev-parse", "HEAD")
        git(self.seed, "push", str(self.remote), "HEAD:refs/heads/unmerged")
        git(self.seed, "checkout", "main")
        git(self.seed, "push", str(self.remote), "main")
        git(root, "clone", str(self.remote), str(self.checkout))
        git(self.checkout, "config", "user.name", "Deployment test")
        git(self.checkout, "config", "user.email", "deployment-test@example.invalid")
        self.environment = patch.dict(
            os.environ,
            {"GITHUB_REPOSITORY": "HarleyBartles/wild-bunch", "GITHUB_REF": "refs/heads/main"},
        )
        self.environment.start()
        os.chdir(self.checkout)

    def tearDown(self) -> None:
        os.chdir(self.previous_directory)
        self.environment.stop()
        self.temporary.cleanup()

    def test_accepts_historical_commit_reachable_from_main(self) -> None:
        with patch("tools.deployment.source.subprocess.run", wraps=subprocess.run) as run:
            self.assertEqual(self.historical_main, resolve_trusted_source(self.historical_main))
        fetch = run.call_args_list[0].args[0]
        self.assertEqual("fetch", fetch[1])
        self.assertIn("+refs/heads/main:refs/remotes/origin/main", fetch)

    def test_rejects_commit_from_unmerged_branch(self) -> None:
        with self.assertRaisesRegex(TrustedSourceError, "not reachable from origin/main"):
            resolve_trusted_source(self.unmerged)

    def test_rejects_ref_options_and_non_full_sha(self) -> None:
        for ref in ("--upload-pack=evil", "refs/pull/12/head", "main", "a" * 39, "A" * 40):
            with self.subTest(ref=ref), self.assertRaises(TrustedSourceError):
                resolve_trusted_source(ref)

    def test_rejects_unexpected_repository_and_non_main_workflow(self) -> None:
        for name, value in (("GITHUB_REPOSITORY", "fork/wild-bunch"), ("GITHUB_REF", "refs/pull/12/merge")):
            with self.subTest(name=name, value=value), patch.dict(os.environ, {name: value}):
                with self.assertRaises(TrustedSourceError):
                    resolve_trusted_source(self.historical_main)

    def test_rejects_alternate_approved_branch(self) -> None:
        with self.assertRaisesRegex(TrustedSourceError, "restricted to main"):
            resolve_trusted_source(self.historical_main, "release")

    def test_codebuild_source_version_binds_a_commit_to_main(self) -> None:
        self.assertEqual(
            f"refs/heads/main^{{{self.historical_main}}}",
            codebuild_main_source_version(self.historical_main),
        )
        for value in ("main", "a" * 39, "A" * 40, "--upload-pack=bad"):
            with self.subTest(value=value), self.assertRaises(TrustedSourceError):
                codebuild_main_source_version(value)


if __name__ == "__main__":
    unittest.main()
