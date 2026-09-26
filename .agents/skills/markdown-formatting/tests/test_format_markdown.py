from __future__ import annotations

import importlib.util
import json
import subprocess
import sys
from pathlib import Path

import pytest
import mdformat
from markdown_it import MarkdownIt


SCRIPT = Path(__file__).parents[1] / "scripts" / "format_markdown.py"


def load_module():
    spec = importlib.util.spec_from_file_location("format_markdown_under_test", SCRIPT)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def git(repo: Path, *args: str) -> str:
    result = subprocess.run(["git", *args], cwd=repo, text=True, capture_output=True, check=True)
    return result.stdout.strip()


def make_repo(tmp_path: Path, *, state: str = "adopted") -> Path:
    repo = tmp_path / "consumer"
    repo.mkdir()
    git(repo, "init", "-b", "main")
    git(repo, "config", "user.name", "Formatter Test")
    git(repo, "config", "user.email", "formatter@example.invalid")
    (repo / ".agents/contracts").mkdir(parents=True)
    (repo / ".mdformat.toml").write_text(
        'wrap = "no"\nend_of_line = "lf"\nvalidate = true\nnumber = true\n'
        'extensions = ["gfm", "frontmatter", "safe-link-labels"]\n',
        encoding="utf-8",
    )
    (repo / ".agents/contracts/markdown-formatting.json").write_text(
        json.dumps({"version": 1, "state": state, "exclusions": []}) + "\n",
        encoding="utf-8",
    )
    return repo


def commit_all(repo: Path) -> None:
    git(repo, "add", "--all")
    git(repo, "commit", "-m", "fixture")


def test_eligible_markdown_is_tracked_sorted_and_honors_file_and_tree(tmp_path: Path):
    module = load_module()
    repo = make_repo(tmp_path)
    (repo / "docs/vendor").mkdir(parents=True)
    for relative in ("z.md", "a.md", "docs/vendor/evidence.md"):
        (repo / relative).write_text("# title\n", encoding="utf-8")
    commit_all(repo)
    (repo / "untracked.md").write_text("# ignore\n", encoding="utf-8")
    contract_path = repo / ".agents/contracts/markdown-formatting.json"
    contract_path.write_text(
        json.dumps(
            {
                "version": 1,
                "state": "enforced",
                "exclusions": [
                    {"kind": "file", "path": "z.md", "reason": "external bytes"},
                    {
                        "kind": "tree",
                        "path": "docs/vendor",
                        "reason": "vendored evidence",
                    },
                ],
            }
        ),
        encoding="utf-8",
    )
    contract = module.load_contract(repo)
    assert contract.state == "enforced"
    assert [path.relative_to(repo).as_posix() for path in module.eligible_markdown(repo, contract)] == ["a.md"]


def test_hook_snapshot_checks_only_staged_markdown(tmp_path: Path, monkeypatch):
    module = load_module()
    repo = make_repo(tmp_path)
    for relative in ("changed.md", "unchanged.md"):
        (repo / relative).write_text("# title\n", encoding="utf-8")
    commit_all(repo)
    (repo / "changed.md").write_text("# changed\n", encoding="utf-8")
    git(repo, "add", "changed.md")
    monkeypatch.setenv("REPO_STANDARDS_STAGED_SNAPSHOT", "1")

    contract = module.load_contract(repo)
    assert module.eligible_markdown(repo, contract) == (repo / "changed.md",)


@pytest.mark.parametrize(
    ("entry", "message"),
    [
        ({"kind": "file", "path": "missing.md", "reason": "custody"}, "matches no tracked Markdown"),
        ({"kind": "tree", "path": "empty", "reason": "custody"}, "matches no tracked Markdown"),
        ({"kind": "file", "path": "../escape.md", "reason": "custody"}, "repository-relative"),
        ({"kind": "file", "path": "*.md", "reason": "custody"}, "globs are not supported"),
        ({"kind": "file", "path": "a.md", "reason": ""}, "non-empty reason"),
        ({"kind": "other", "path": "a.md", "reason": "custody"}, "kind must be"),
    ],
)
def test_invalid_exclusions_fail_closed(tmp_path: Path, entry: dict[str, str], message: str):
    module = load_module()
    repo = make_repo(tmp_path)
    (repo / "a.md").write_text("# title\n", encoding="utf-8")
    (repo / "empty").mkdir()
    commit_all(repo)
    (repo / ".agents/contracts/markdown-formatting.json").write_text(
        json.dumps({"version": 1, "state": "adopted", "exclusions": [entry]}),
        encoding="utf-8",
    )
    with pytest.raises(module.ContractError, match=message):
        module.load_contract(repo)


@pytest.mark.parametrize("state", ["disabled", "ready", ""])
def test_only_adopted_and_enforced_states_are_valid(tmp_path: Path, state: str):
    module = load_module()
    repo = make_repo(tmp_path, state=state)
    commit_all(repo)
    with pytest.raises(module.ContractError, match="state"):
        module.load_contract(repo)


def test_configuration_requires_visible_consecutive_list_numbering(tmp_path: Path):
    module = load_module()
    repo = make_repo(tmp_path)
    config = repo / ".mdformat.toml"
    config.write_text(config.read_text(encoding="utf-8").replace("number = true\n", ""), encoding="utf-8")

    with pytest.raises(module.ContractError, match="number must be True"):
        module.verify_configuration(repo)


def test_verify_toolchain_reports_missing_and_mismatched_packages(monkeypatch):
    module = load_module()
    versions = module._required_distributions()
    missing = next(iter(versions))

    def missing_version(name: str) -> str:
        if name == missing:
            raise module.importlib_metadata.PackageNotFoundError(name)
        return versions[name]

    monkeypatch.setattr(module.importlib_metadata, "version", missing_version)
    with pytest.raises(module.ToolchainError, match=missing):
        module.verify_toolchain()

    monkeypatch.setattr(
        module.importlib_metadata,
        "version",
        lambda name: "0.0.0" if name == missing else versions[name],
    )
    with pytest.raises(module.ToolchainError, match="0.0.0"):
        module.verify_toolchain()


def test_requirements_must_pin_every_required_distribution(tmp_path: Path, monkeypatch):
    module = load_module()
    requirements = tmp_path / "requirements.txt"
    requirements.write_text("mdformat==1.0.0\n", encoding="utf-8")
    monkeypatch.setattr(module, "REQUIREMENTS_PATH", requirements)

    with pytest.raises(module.ToolchainError, match="mdformat-frontmatter"):
        module._required_distributions()


def test_local_renderer_requirement_must_install_a_distribution(tmp_path: Path, monkeypatch):
    module = load_module()
    requirements = tmp_path / "requirements.txt"
    requirements.write_text(
        "mdformat==1.0.0\nmdformat-frontmatter==2.1.2\nmdformat-gfm==1.0.0\n"
        "-e .agents/skills/markdown-formatting/renderer-plugin\n",
        encoding="utf-8",
    )
    monkeypatch.setattr(module, "REQUIREMENTS_PATH", requirements)

    with pytest.raises(module.ToolchainError, match="mdformat-safe-link-labels"):
        module._required_distributions()


def test_marketplace_renderer_wheel_is_a_versioned_toolchain_requirement(tmp_path: Path, monkeypatch):
    module = load_module()
    requirements = tmp_path / "requirements.txt"
    wheel_requirement = (
        "./.agents/plugins/marketplace-source/codex-marketplace/packages/"
        "mdformat-safe-link-labels/wheels/mdformat_safe_link_labels-1.0.0-py3-none-any.whl"
    )
    requirements.write_text(
        f"mdformat==1.0.0\nmdformat-frontmatter==2.1.2\nmdformat-gfm==1.0.0\n{wheel_requirement}\n",
        encoding="utf-8",
    )
    monkeypatch.setattr(module, "REQUIREMENTS_PATH", requirements)

    assert module._required_distributions()["mdformat-safe-link-labels"] == "1.0.0"


def test_apply_restores_every_original_byte_when_later_batch_fails(tmp_path: Path, monkeypatch):
    module = load_module()
    repo = make_repo(tmp_path)
    first = repo / "first file.md"
    second = repo / "second.md"
    first.write_bytes(b"# first   \r\n")
    second.write_bytes(b"# second   \r\n")
    commit_all(repo)
    originals = {path: path.read_bytes() for path in (first, second)}
    calls = 0

    def fake_run(command, **kwargs):
        nonlocal calls
        calls += 1
        target = repo / command[-1]
        target.write_text("# changed\n", encoding="utf-8")
        return subprocess.CompletedProcess(command, 0 if calls == 1 else 1)

    monkeypatch.setattr(module.subprocess, "run", fake_run)
    monkeypatch.setattr(module, "MAX_COMMAND_CHARS", 1)
    with pytest.raises(module.FormatterError):
        module.run_formatter(repo, [first, second], mode="apply")
    assert {path: path.read_bytes() for path in originals} == originals


def test_check_files_accepts_untracked_producer_output_and_rejects_invalid_paths(tmp_path: Path):
    module = load_module()
    repo = make_repo(tmp_path)
    (repo / "tracked.md").write_text("# tracked\n", encoding="utf-8")
    (repo / "tracked.txt").write_text("text\n", encoding="utf-8")
    commit_all(repo)
    (repo / "untracked.md").write_text("# untracked\n", encoding="utf-8")
    assert module.validate_requested_files(repo, ["untracked.md"]) == (repo / "untracked.md",)
    for candidate, message in (
        ("missing.md", "does not exist"),
        ("tracked.txt", "Markdown"),
        ("../escape.md", "repository-relative"),
    ):
        with pytest.raises(module.ContractError, match=message):
            module.validate_requested_files(repo, [candidate])


def test_safe_underscores_in_link_labels_are_formatter_stable(tmp_path: Path):
    repo = make_repo(tmp_path)
    target_name = "absynth_lover__seegreenfairies.md"
    source = f"- [{target_name.removesuffix('.md')}]({target_name})\n"
    index = repo / "INDEX.md"
    index.write_text(source, encoding="utf-8", newline="\n")
    (repo / target_name).write_text("# target\n", encoding="utf-8", newline="\n")
    commit_all(repo)

    checked = subprocess.run(
        [sys.executable, str(SCRIPT), "--check-files", "INDEX.md"],
        cwd=repo,
        text=True,
        capture_output=True,
        check=False,
    )
    assert checked.returncode == 0, checked.stderr

    applied = subprocess.run(
        [sys.executable, str(SCRIPT), "--apply"],
        cwd=repo,
        text=True,
        capture_output=True,
        check=False,
    )
    assert applied.returncode == 0, applied.stderr
    assert index.read_text(encoding="utf-8") == source

    checked_again = subprocess.run(
        [sys.executable, str(SCRIPT), "--check-files", "INDEX.md"],
        cwd=repo,
        text=True,
        capture_output=True,
        check=False,
    )
    assert checked_again.returncode == 0, checked_again.stderr


def test_emphasis_in_link_labels_keeps_its_parsed_meaning():
    source = "- [_emphasis_](target.md)\n"
    formatted = mdformat.text(source, extensions={"safe-link-labels"})

    parser = MarkdownIt("commonmark")
    assert parser.render(formatted) == parser.render(source)


def test_escaped_underscores_that_prevent_emphasis_remain_escaped():
    source = r"- [\_foo\_](target.md)" + "\n"
    formatted = mdformat.text(source, extensions={"safe-link-labels"})

    parser = MarkdownIt("commonmark")
    assert parser.render(formatted) == parser.render(source)
    assert r"\_foo\_" in formatted


def test_safe_escaped_underscore_is_normalized_without_changing_rendered_label():
    source = r"- [foo\_bar](target.md)" + "\n"
    formatted = mdformat.text(source, extensions={"safe-link-labels"})

    parser = MarkdownIt("commonmark")
    assert parser.render(formatted) == parser.render(source)
    assert "[foo_bar](target.md)" in formatted


def test_help_classifies_the_cli_as_mixed():
    result = subprocess.run([sys.executable, str(SCRIPT), "--help"], text=True, capture_output=True, check=True)
    assert "mixed" in result.stdout.lower()
