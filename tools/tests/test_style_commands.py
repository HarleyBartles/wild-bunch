from __future__ import annotations

import pytest
from test_run import _git, _git_repo, run


def test_format_check_reports_difference_without_changing_file(
    tmp_path, monkeypatch, capfd
) -> None:
    monkeypatch.setattr(run, "ROOT", tmp_path)
    (tmp_path / "pyproject.toml").write_text("[tool.ruff]\nline-length = 88\n", encoding="utf-8")
    source = tmp_path / "source with spaces.py"
    source.write_text("value=1\n", encoding="utf-8")

    status = run.main(["format", "--check", str(source.relative_to(tmp_path))])

    assert status != 0
    assert source.read_text(encoding="utf-8") == "value=1\n"
    assert "would be reformatted" in capfd.readouterr().out.lower()


def test_format_apply_changes_only_explicit_path_with_spaces(tmp_path, monkeypatch) -> None:
    _git_repo(tmp_path)
    _git(tmp_path, "checkout", "-b", "codex/style-test")
    monkeypatch.setattr(run, "ROOT", tmp_path)
    (tmp_path / "pyproject.toml").write_text("[tool.ruff]\nline-length = 88\n", encoding="utf-8")
    selected = tmp_path / "source with spaces.py"
    untouched = tmp_path / "untouched.py"
    selected.write_text("value=1\n", encoding="utf-8")
    untouched.write_text("other=2\n", encoding="utf-8")

    status = run.main(["format", "--apply", str(selected.relative_to(tmp_path))])

    assert status == 0
    assert selected.read_text(encoding="utf-8") == "value = 1\n"
    assert untouched.read_text(encoding="utf-8") == "other=2\n"


def test_format_apply_all_selects_the_supported_repository_scope(tmp_path, monkeypatch) -> None:
    _git_repo(tmp_path)
    _git(tmp_path, "checkout", "-b", "codex/style-test")
    monkeypatch.setattr(run, "ROOT", tmp_path)
    (tmp_path / "pyproject.toml").write_text("[tool.ruff]\nline-length = 88\n", encoding="utf-8")
    first = tmp_path / "first.py"
    second = tmp_path / "second.py"
    first.write_text("first=1\n", encoding="utf-8")
    second.write_text("second=2\n", encoding="utf-8")

    status = run.main(["format", "--apply", "--all"])

    assert status == 0
    assert first.read_text(encoding="utf-8") == "first = 1\n"
    assert second.read_text(encoding="utf-8") == "second = 2\n"


def test_dotnet_format_splits_large_file_scopes_into_bounded_commands(
    tmp_path, monkeypatch
) -> None:
    _git_repo(tmp_path)
    _git(tmp_path, "checkout", "-b", "codex/style-test")
    monkeypatch.setattr(run, "ROOT", tmp_path)
    invoked: list[list[str]] = []
    monkeypatch.setattr(run, "_run", lambda command, _ctx: invoked.append(command))
    paths = []
    for index in range(600):
        source = tmp_path / f"src/WildBunch.Domain/Feature{index:03d}.cs"
        source.parent.mkdir(parents=True, exist_ok=True)
        source.write_text("namespace Example;\n", encoding="utf-8")
        paths.append(source.relative_to(tmp_path).as_posix())

    status = run.main(["format", "--check", *paths])

    assert status == 0
    assert len(invoked) > 1
    formatted_paths = [
        argument for command in invoked for argument in command if argument.endswith(".cs")
    ]
    assert sorted(formatted_paths) == sorted(paths)
    assert all(sum(len(argument) + 1 for argument in command) < 20000 for command in invoked)


@pytest.mark.parametrize(
    "path",
    [
        "../outside.py",
        "src/bin/generated.py",
        "src/node_modules/package/index.py",
        "docs/guide.md",
    ],
    ids=["repository-escape", "build-output", "vendor-dependency", "markdown"],
)
def test_format_apply_rejects_paths_outside_supported_source(
    tmp_path, monkeypatch, capfd, path
) -> None:
    _git_repo(tmp_path)
    _git(tmp_path, "checkout", "-b", "codex/style-test")
    monkeypatch.setattr(run, "ROOT", tmp_path)
    invoked: list[list[str]] = []
    monkeypatch.setattr(run, "_run", lambda command, _ctx: invoked.append(command))

    status = run.main(["format", "--apply", path])

    assert status != 0
    assert invoked == []
    assert "path" in capfd.readouterr().err.lower()


def test_format_apply_rejects_absolute_path_outside_repository(
    tmp_path, monkeypatch, capfd
) -> None:
    _git_repo(tmp_path)
    _git(tmp_path, "checkout", "-b", "codex/style-test")
    monkeypatch.setattr(run, "ROOT", tmp_path)
    outside = tmp_path.parent / "outside.py"
    outside.write_text("value=1\n", encoding="utf-8")
    invoked: list[list[str]] = []
    monkeypatch.setattr(run, "_run", lambda command, _ctx: invoked.append(command))

    status = run.main(["format", "--apply", str(outside)])

    assert status != 0
    assert invoked == []
    assert "outside the repository" in capfd.readouterr().err.lower()


def test_format_apply_requires_an_explicit_scope(tmp_path, monkeypatch, capfd) -> None:
    _git_repo(tmp_path)
    _git(tmp_path, "checkout", "-b", "codex/style-test")
    monkeypatch.setattr(run, "ROOT", tmp_path)
    invoked: list[list[str]] = []
    monkeypatch.setattr(run, "_run", lambda command, _ctx: invoked.append(command))

    status = run.main(["format", "--apply"])

    assert status != 0
    assert invoked == []
    assert "requires explicit" in capfd.readouterr().err.lower()


def test_lint_check_reports_errors_without_fixing_source(tmp_path, monkeypatch, capfd) -> None:
    monkeypatch.setattr(run, "ROOT", tmp_path)
    (tmp_path / "pyproject.toml").write_text('[tool.ruff.lint]\nselect = ["F"]\n', encoding="utf-8")
    source = tmp_path / "broken.py"
    source.write_text("import os\n\nvalue = 1\n", encoding="utf-8")

    status = run.main(["lint", "--check", str(source.relative_to(tmp_path))])

    assert status != 0
    assert source.read_text(encoding="utf-8") == "import os\n\nvalue = 1\n"
    captured = capfd.readouterr()
    output = captured.out + captured.err
    assert "F401" in output
