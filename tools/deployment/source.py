"""Resolve deployment code only from the protected repository's main history."""

from __future__ import annotations

import os
import re
import subprocess


EXPECTED_REPOSITORY = "HarleyBartles/wild-bunch"
_COMMIT = re.compile(r"^[0-9a-f]{40}$")


class TrustedSourceError(ValueError):
    """The selected source or privileged workflow context is not trusted."""


def _git(*arguments: str) -> str:
    try:
        result = subprocess.run(
            ["git", *arguments], check=True, capture_output=True, text=True, timeout=120
        )
    except (OSError, subprocess.CalledProcessError, subprocess.TimeoutExpired):
        raise TrustedSourceError("Trusted source could not be resolved from origin/main.") from None
    return result.stdout.strip()


def resolve_trusted_source(ref: str, approved_branch: str = "main") -> str:
    """Return a full commit SHA only when it is reachable from the trusted main branch.

    The privileged workflow itself must have been dispatched from main in the expected
    repository. The selected source is a full commit SHA, which permits historical main
    revisions while excluding branch names, pull request refs and option-like input.
    """
    if approved_branch != "main":
        raise TrustedSourceError("Privileged deployment workflows are restricted to main.")
    if os.environ.get("GITHUB_REPOSITORY") != EXPECTED_REPOSITORY:
        raise TrustedSourceError("Privileged deployment workflows are restricted to the Wild Bunch repository.")
    if os.environ.get("GITHUB_REF") != "refs/heads/main":
        raise TrustedSourceError("Privileged deployment workflows must run from refs/heads/main.")
    if not isinstance(ref, str) or not _COMMIT.fullmatch(ref):
        raise TrustedSourceError("Select a full 40-character lowercase commit SHA from main.")

    _git("fetch", "--no-tags", "origin", "+refs/heads/main:refs/remotes/origin/main")
    try:
        commit = _git("rev-parse", "--verify", f"{ref}^{{commit}}")
        _git("merge-base", "--is-ancestor", commit, "refs/remotes/origin/main")
    except TrustedSourceError:
        raise TrustedSourceError("Selected commit is not reachable from origin/main.") from None
    if not _COMMIT.fullmatch(commit):
        raise TrustedSourceError("Git returned an invalid commit identity.")
    return commit


def codebuild_main_source_version(commit_sha: str) -> str:
    """Pin CodeBuild to a trusted commit through main so branch-aware IAM conditions apply."""
    if not isinstance(commit_sha, str) or not _COMMIT.fullmatch(commit_sha):
        raise TrustedSourceError("CodeBuild source must be a full lowercase commit SHA.")
    return f"refs/heads/main^{{{commit_sha}}}"
