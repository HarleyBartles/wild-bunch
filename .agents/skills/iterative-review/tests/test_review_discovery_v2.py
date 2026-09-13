#!/usr/bin/env python3
"""Tests for the sealed authority-discovery policy (Plan 2 Task 3)."""

from __future__ import annotations

import json
import sys
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
SCRIPTS = TESTS_DIR.parent / "scripts"
sys.path.insert(0, str(TESTS_DIR))
sys.path.insert(0, str(SCRIPTS))

from review_core import discovery_policy as dp  # noqa: E402

BASE = "b" * 40
OVERRIDE_PATH = ".agents/iterative-review/authority-policy.json"


class FakeGit:
    """Dispatch table for `git ls-tree`/`git show` against a base SHA."""

    def __init__(self, files: dict[str, str], *, override: str | None = None):
        self.files = dict(files)
        self.override = override  # content of the override path at base
        self.calls: list[list[str]] = []

    def __call__(self, args: list[str]) -> tuple[int, str, str]:
        self.calls.append(list(args))
        if args[:2] == ["ls-tree", "-r"]:
            return 0, "\n".join(sorted(self.files)) + "\n", ""
        if args[0] == "show" and ":" in args[1]:
            _sha, path = args[1].split(":", 1)
            if path == OVERRIDE_PATH:
                if self.override is None:
                    return 1, "", "not found"
                return 0, self.override, ""
            if path in self.files:
                return 0, self.files[path], ""
            return 1, "", f"path '{path}' does not exist"
        return 1, "", "unsupported fake git call"


def _pr(body="Fix the thing", issues=(), number=7):
    return {"number": number, "body": body, "linked_issues": list(issues)}


def _load_text(mapping):
    def load(locator: str):
        v = mapping.get(locator)
        return v.encode("utf-8") if isinstance(v, str) else v

    return load


class TestPolicyResolution:
    def test_default_policy_when_no_base_override(self):
        git = FakeGit({"AGENTS.md": "# repo"})
        pol = dp.resolve_policy(run_git=git, base_sha=BASE)
        assert pol.policy_id == "authority-discovery" and pol.version == "1"
        default = dp.default_policy()
        assert pol.document_sha256 == default.document_sha256

    def test_base_revision_override_applies(self):
        override = json.dumps(
            {
                "schema_version": 1,
                "policy_id": "authority-discovery",
                "version": "1",
                "repo_law_roots": ["AGENTS.md"],
                "pr_roots": ["description"],
                "edge_kinds": list(dp.AUTHORITY_EDGE_KINDS),
                "edge_grammars": [],
                "structural_edges": [],
                "override_path": OVERRIDE_PATH,
            }
        )
        git = FakeGit({"AGENTS.md": "# repo"}, override=override)
        pol = dp.resolve_policy(run_git=git, base_sha=BASE)
        assert pol.document_sha256 != dp.default_policy().document_sha256
        seeds, failures = dp.enumerate_authorities(
            policy=pol,
            run_git=git,
            base_sha=BASE,
            pr_metadata=_pr(issues=[12]),
            load_text=_load_text({}),
        )
        assert not failures
        kinds = {(s.kind, s.locator) for s in seeds}
        # only AGENTS.md is a repo-law root under the override
        assert ("repo-law", "repo:AGENTS.md") in kinds
        # pr_roots keeps description but drops linked_issues
        assert ("pr-description", "gh:pr/7#body") in kinds
        assert ("issue", "gh:issue/12") not in kinds

    def test_head_copy_cannot_override(self):
        git = FakeGit({"AGENTS.md": "# repo"}, override=None)
        pol = dp.resolve_policy(run_git=git, base_sha=BASE)
        assert pol.document_sha256 == dp.default_policy().document_sha256
        # the only read was the base-scoped show, never a working-tree path
        assert len(git.calls) == 1
        assert git.calls[0][0] == "show" and git.calls[0][1].startswith(BASE + ":")

    def test_malformed_base_override_fails_closed(self):
        git = FakeGit({"AGENTS.md": "x"}, override="not json {")
        with pytest.raises(dp.DiscoveryPolicyError):
            dp.resolve_policy(run_git=git, base_sha=BASE)

    def test_unknown_pr_root_fails_closed(self):
        # A typo'd pr_root ("linked-issue") would silently narrow the
        # enumerated authority set - refuse it at resolution instead.
        override = json.dumps(
            {
                "schema_version": 1,
                "policy_id": "authority-discovery",
                "version": "1",
                "repo_law_roots": ["AGENTS.md"],
                "pr_roots": ["description", "linked-issue"],
                "edge_kinds": list(dp.AUTHORITY_EDGE_KINDS),
                "edge_grammars": [],
                "structural_edges": [],
                "override_path": OVERRIDE_PATH,
            }
        )
        git = FakeGit({"AGENTS.md": "# repo"}, override=override)
        with pytest.raises(dp.DiscoveryPolicyError, match="pr_roots"):
            dp.resolve_policy(run_git=git, base_sha=BASE)

    def test_malformed_structural_edge_rule_fails_closed(self):
        # A rule missing "to"/"edge" must fail as a policy error, not crash
        # with a KeyError during traversal.
        override = json.dumps(
            {
                "schema_version": 1,
                "policy_id": "authority-discovery",
                "version": "1",
                "repo_law_roots": ["AGENTS.md"],
                "pr_roots": ["description"],
                "edge_kinds": list(dp.AUTHORITY_EDGE_KINDS),
                "edge_grammars": [],
                "structural_edges": [{"from": "AGENTS.md"}],
                "override_path": OVERRIDE_PATH,
            }
        )
        git = FakeGit({"AGENTS.md": "# repo"}, override=override)
        with pytest.raises(dp.DiscoveryPolicyError, match="structural_edges"):
            dp.resolve_policy(run_git=git, base_sha=BASE)


class TestEnumeration:
    def test_repo_law_roots_enumerated_at_base(self):
        git = FakeGit(
            {
                "AGENTS.md": "# law",
                ".devin/rules/pr.md": "rule",
                ".agents/doctrine/mesh.md": "doc",
                "src/code.py": "print()",
            }
        )
        seeds, failures = dp.enumerate_authorities(
            policy=dp.default_policy(),
            run_git=git,
            base_sha=BASE,
            pr_metadata=_pr(),
            load_text=_load_text({}),
        )
        assert not failures
        repo_law = {s.locator for s in seeds if s.kind == "repo-law"}
        assert repo_law == {
            "repo:AGENTS.md",
            "repo:.devin/rules/pr.md",
            "repo:.agents/doctrine/mesh.md",
        }
        assert "repo:src/code.py" not in {s.locator for s in seeds}

    def test_pr_description_and_linked_issues_are_roots(self):
        git = FakeGit({"AGENTS.md": "# law"})
        seeds, failures = dp.enumerate_authorities(
            policy=dp.default_policy(),
            run_git=git,
            base_sha=BASE,
            pr_metadata=_pr(issues=[12, 34]),
            load_text=_load_text({"gh:issue/12": "body", "gh:issue/34": "body"}),
        )
        kinds = {(s.kind, s.locator) for s in seeds}
        assert ("issue", "gh:issue/12") in kinds
        assert ("issue", "gh:issue/34") in kinds
        assert any(k == "pr-description" for k, _ in kinds)
        assert not failures

    def test_marker_edge_traversed_to_fixed_point(self):
        git = FakeGit(
            {
                "AGENTS.md": "<!-- authority:edge governs repo:doc/b.md -->",
                "doc/b.md": "<!-- authority:edge references-as-authority repo:doc/c.md -->",
                "doc/c.md": "leaf",
            }
        )
        seeds, failures = dp.enumerate_authorities(
            policy=dp.default_policy(),
            run_git=git,
            base_sha=BASE,
            pr_metadata=_pr(),
            load_text=_load_text({}),
        )
        assert not failures
        locators = {s.locator for s in seeds}
        assert {"repo:doc/b.md", "repo:doc/c.md"} <= locators

    def test_cycle_terminates_without_dup(self):
        git = FakeGit(
            {
                "AGENTS.md": "<!-- authority:edge governs repo:a.md -->",
                "a.md": "<!-- authority:edge depends-on repo:b.md -->",
                "b.md": "<!-- authority:edge depends-on repo:a.md -->",
            }
        )
        seeds, failures = dp.enumerate_authorities(
            policy=dp.default_policy(),
            run_git=git,
            base_sha=BASE,
            pr_metadata=_pr(),
            load_text=_load_text({}),
        )
        assert not failures
        locs = [s.locator for s in seeds]
        assert locs.count("repo:a.md") == 1 and locs.count("repo:b.md") == 1

    def test_structural_agents_edge(self):
        git = FakeGit(
            {
                "AGENTS.md": "# law, no markers",
                ".devin/rules/x.md": "r",
                ".agents/doctrine/y.md": "d",
            }
        )
        seeds, _ = dp.enumerate_authorities(
            policy=dp.default_policy(),
            run_git=git,
            base_sha=BASE,
            pr_metadata=_pr(),
            load_text=_load_text({}),
        )
        locs = {s.locator for s in seeds}
        assert {"repo:.devin/rules/x.md", "repo:.agents/doctrine/y.md"} <= locs

    def test_plan_spec_header_implements_edge(self):
        git = FakeGit(
            {
                "AGENTS.md": "<!-- authority:edge implements repo:.agents/plans/p1.md -->",
                ".agents/plans/p1.md": "**Spec:** .agents/specs/s1.md\nbody",
                ".agents/specs/s1.md": "spec text",
            }
        )
        seeds, failures = dp.enumerate_authorities(
            policy=dp.default_policy(),
            run_git=git,
            base_sha=BASE,
            pr_metadata=_pr(),
            load_text=_load_text({}),
        )
        assert not failures
        assert "repo:.agents/specs/s1.md" in {s.locator for s in seeds}

    def test_ambiguous_target_is_failure_not_omission(self):
        git = FakeGit(
            {
                "AGENTS.md": "<!-- authority:edge governs repo:doc/*.md -->",
                "doc/one.md": "1",
                "doc/two.md": "2",
            }
        )
        seeds, failures = dp.enumerate_authorities(
            policy=dp.default_policy(),
            run_git=git,
            base_sha=BASE,
            pr_metadata=_pr(),
            load_text=_load_text({}),
        )
        assert failures
        assert not any(s.locator.startswith("repo:doc/") for s in seeds)

    def test_inaccessible_target_is_failure(self):
        git = FakeGit(
            {
                "AGENTS.md": "<!-- authority:edge governs repo:missing.md -->",
            }
        )
        _seeds, failures = dp.enumerate_authorities(
            policy=dp.default_policy(),
            run_git=git,
            base_sha=BASE,
            pr_metadata=_pr(),
            load_text=_load_text({}),
        )
        assert any("missing.md" in f["locator"] for f in failures)

    def test_canonicalization_dedups_equivalent_locators(self):
        git = FakeGit(
            {
                "AGENTS.md": (
                    "<!-- authority:edge governs repo:a/../x.md -->\n"
                    "<!-- authority:edge governs repo:./x.md -->\n"
                    "<!-- authority:edge governs GH #12 -->"
                ),
                "x.md": "x",
            }
        )
        seeds, failures = dp.enumerate_authorities(
            policy=dp.default_policy(),
            run_git=git,
            base_sha=BASE,
            pr_metadata=_pr(issues=[12]),
            load_text=_load_text({"gh:issue/12": "b"}),
        )
        assert not failures
        locs = [s.locator for s in seeds]
        assert locs.count("repo:x.md") == 1
        assert locs.count("gh:issue/12") == 1

    @pytest.mark.parametrize(
        "drop",
        [
            "agents",
            "rules",
            "doctrine",
            "issues",
            "marker",
            "spec_header",
        ],
    )
    def test_omission_fixture_every_root_and_edge_kind(self, drop):
        # each edge carrier is an independent root so losses are explicit
        files = {
            "AGENTS.md": "",
            ".devin/rules/r.md": "<!-- authority:edge implements repo:.agents/plans/p.md -->",
            ".agents/doctrine/d.md": "<!-- authority:edge governs repo:doc/m.md -->",
            ".agents/plans/p.md": "**Spec:** .agents/specs/s.md",
            ".agents/specs/s.md": "s",
            "doc/m.md": "m",
        }
        issues = [12]
        if drop == "agents":
            files.pop("AGENTS.md")
        elif drop == "rules":
            files.pop(".devin/rules/r.md")
        elif drop == "doctrine":
            files.pop(".agents/doctrine/d.md")
        elif drop == "issues":
            issues = []
        elif drop == "marker":
            files[".agents/doctrine/d.md"] = "no marker"
        elif drop == "spec_header":
            files[".agents/plans/p.md"] = "no header"
        git = FakeGit(files)
        seeds, failures = dp.enumerate_authorities(
            policy=dp.default_policy(),
            run_git=git,
            base_sha=BASE,
            pr_metadata=_pr(issues=issues),
            load_text=_load_text({"gh:issue/12": "b"}),
        )
        assert not failures
        locs = {s.locator for s in seeds}
        expected = {
            "repo:AGENTS.md",
            "repo:.devin/rules/r.md",
            "repo:.agents/doctrine/d.md",
            "repo:.agents/plans/p.md",
            "gh:issue/12",
            "repo:doc/m.md",
            "repo:.agents/specs/s.md",
        }
        lost = {
            "agents": {"repo:AGENTS.md"},
            "rules": {"repo:.devin/rules/r.md", "repo:.agents/plans/p.md", "repo:.agents/specs/s.md"},
            "doctrine": {"repo:.agents/doctrine/d.md", "repo:doc/m.md"},
            "issues": {"gh:issue/12"},
            "marker": {"repo:doc/m.md"},
            "spec_header": {"repo:.agents/specs/s.md"},
        }[drop]
        assert expected - lost <= locs
        assert not (lost & locs)


class TestLocatorCanonicalization:
    def test_repo_path_normalization(self):
        assert dp.canonicalize_locator("repo:a\\b\\c.md") == "repo:a/b/c.md"
        assert dp.canonicalize_locator("repo:./x.md") == "repo:x.md"
        assert dp.canonicalize_locator("repo:a/../x.md") == "repo:x.md"

    def test_gh_issue_spellings(self):
        assert dp.canonicalize_locator("GH #12") == "gh:issue/12"
        assert dp.canonicalize_locator("gh:issue/12") == "gh:issue/12"

    def test_rejects_traversal_escape_and_control_chars(self):
        for bad in (
            "repo:../evil.md",
            "repo:a/../../evil.md",
            "repo:/abs/x.md",
            "repo:C:/win/x.md",
            "repo:a" + chr(0) + "b.md",
            "gh:doc/../escape",
            "gh:doc/evil" + chr(10) + "name.md",
            "gh:issue/notanum",
            "gh:pr/x#body",
        ):
            with pytest.raises(dp.DiscoveryPolicyError, match="invalid-locator"):
                dp.canonicalize_locator(bad)
        assert dp.canonicalize_locator("gh:issue/12") == "gh:issue/12"
        assert dp.canonicalize_locator("gh:pr/7#body") == "gh:pr/7#body"
        assert dp.canonicalize_locator("gh:doc/a/b.md") == "gh:doc/a/b.md"
        assert dp.canonicalize_locator("repo:a/b.md") == "repo:a/b.md"

    def test_invalid_edge_locator_is_failure_not_omission(self):
        git = FakeGit(
            {
                "AGENTS.md": (
                    "<!-- authority:edge governs repo:../outside.md -->" + chr(92) + "n"
                    "<!-- authority:edge governs gh:issue/abc -->"
                ),
            }
        )
        _seeds, failures = dp.enumerate_authorities(
            policy=dp.default_policy(),
            run_git=git,
            base_sha=BASE,
            pr_metadata=_pr(),
            load_text=_load_text({}),
        )
        reasons = [f["reason"] for f in failures]
        assert sum("invalid-locator" in r for r in reasons) == 2
