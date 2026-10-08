from __future__ import annotations

import importlib.util
import io
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
HOOK_PATH = ROOT / ".codex" / "hooks" / "format_after_edit.py"
SPEC = importlib.util.spec_from_file_location("format_after_edit_hook", HOOK_PATH)
assert SPEC is not None and SPEC.loader is not None
hook = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = hook
SPEC.loader.exec_module(hook)


def _git(root: Path, *arguments: str) -> str:
    result = subprocess.run(
        ["git", *arguments], cwd=root, capture_output=True, text=True, check=True
    )
    return result.stdout.strip()


def _repository(tmp_path: Path) -> tuple[Path, Path]:
    root = tmp_path / "repo with spaces"
    root.mkdir()
    _git(root, "init", "--quiet")
    _git(root, "config", "user.name", "Test")
    _git(root, "config", "user.email", "test@example.invalid")
    tools = root / "tools"
    tools.mkdir()
    shutil.copy2(ROOT / "tools" / "style.py", tools / "style.py")
    bus = tools / "run.py"
    bus.write_text(
        "import json\n"
        "import os\n"
        "from pathlib import Path\n"
        "import sys\n"
        "Path(os.environ['FAKE_BUS_LOG']).write_text(json.dumps(sys.argv[1:]), encoding='utf-8')\n"
        "if os.environ.get('FAKE_BUS_FAIL'):\n"
        "    print('formatter actual diagnostic', file=sys.stderr)\n"
        "    raise SystemExit(2)\n"
        "for name in sys.argv[3:]:\n"
        "    path = Path(name)\n"
        "    source = path.read_text(encoding='utf-8').replace('value=1', 'value = 1')\n"
        "    path.write_text(source, encoding='utf-8')\n",
        encoding="utf-8",
    )
    return root, bus


def _event(root: Path, event_name: str, tool_name: str, tool_use_id: str) -> dict[str, object]:
    return {
        "cwd": str(root),
        "session_id": "session-for-tests",
        "hook_event_name": event_name,
        "tool_name": tool_name,
        "tool_use_id": tool_use_id,
        "tool_input": {},
    }


def _hook_output(capsys) -> dict[str, object] | None:
    output = capsys.readouterr().out
    return json.loads(output) if output else None


def test_post_tool_formats_only_changed_dirty_file_before_return_and_preserves_index(
    tmp_path, monkeypatch, capsys
) -> None:
    root, _ = _repository(tmp_path)
    bus_log = tmp_path / "bus args.json"
    monkeypatch.setenv("FAKE_BUS_LOG", str(bus_log))
    edited = root / "src" / "changed with spaces.py"
    unrelated = root / "src" / "unrelated.py"
    edited.parent.mkdir()
    edited.write_text("value=1\n", encoding="utf-8")
    unrelated.write_text("other=2\n", encoding="utf-8")
    _git(root, "add", "src")
    _git(root, "commit", "-m", "baseline")
    edited.write_text("value=1\n# already dirty\n", encoding="utf-8")
    unrelated.write_text("other=2\n# unrelated dirty\n", encoding="utf-8")
    index_before = _git(root, "write-tree")
    monkeypatch.setattr(hook, "TEMP_ROOT", tmp_path / "hook-state")

    assert hook.handle_event(_event(root, "PreToolUse", "Bash", "tool-1")) == 0
    edited.write_text("value=1\n# already dirty\n# tool edit\n", encoding="utf-8")
    assert hook.handle_event(_event(root, "PostToolUse", "Bash", "tool-1")) == 0

    assert edited.read_text(encoding="utf-8") == "value = 1\n# already dirty\n# tool edit\n"
    assert unrelated.read_text(encoding="utf-8") == "other=2\n# unrelated dirty\n"
    assert _git(root, "write-tree") == index_before
    output = _hook_output(capsys)
    assert output is not None
    assert "changed with spaces.py" in json.dumps(output)
    assert json.loads(bus_log.read_text(encoding="utf-8")) == [
        "format",
        "--apply",
        "src/changed with spaces.py",
    ]


def test_post_tool_formats_clean_tracked_file_changed_by_the_tool(
    tmp_path, monkeypatch, capsys
) -> None:
    root, _ = _repository(tmp_path)
    bus_log = tmp_path / "bus args.json"
    monkeypatch.setenv("FAKE_BUS_LOG", str(bus_log))
    monkeypatch.setattr(hook, "TEMP_ROOT", tmp_path / "hook-state")
    source = root / "src" / "tracked.py"
    source.parent.mkdir()
    source.write_text("value = 0\n", encoding="utf-8")
    _git(root, "add", "src")
    _git(root, "commit", "-m", "baseline")

    assert hook.handle_event(_event(root, "PreToolUse", "Bash", "clean-tracked")) == 0
    source.write_text("value=1\n", encoding="utf-8")
    assert hook.handle_event(_event(root, "PostToolUse", "Bash", "clean-tracked")) == 0

    assert source.read_text(encoding="utf-8") == "value = 1\n"
    assert json.loads(bus_log.read_text(encoding="utf-8")) == [
        "format",
        "--apply",
        "src/tracked.py",
    ]
    assert _hook_output(capsys) is not None


def test_post_tool_does_not_format_index_only_transition_with_unchanged_worktree(
    tmp_path, monkeypatch, capsys
) -> None:
    root, _ = _repository(tmp_path)
    bus_log = tmp_path / "bus args.json"
    monkeypatch.setenv("FAKE_BUS_LOG", str(bus_log))
    monkeypatch.setattr(hook, "TEMP_ROOT", tmp_path / "hook-state")
    source = root / "src" / "staged.py"
    source.parent.mkdir()
    source.write_text("value=1\n", encoding="utf-8")
    _git(root, "add", "src")
    _git(root, "commit", "-m", "baseline")

    assert hook.handle_event(_event(root, "PreToolUse", "Bash", "index-only")) == 0
    index_blob = (
        subprocess.run(
            ["git", "hash-object", "-w", "--stdin"],
            cwd=root,
            input=b"value = 1\n",
            capture_output=True,
            check=True,
        )
        .stdout.decode("ascii")
        .strip()
    )
    _git(root, "update-index", "--cacheinfo", f"100644,{index_blob},src/staged.py")
    assert hook.handle_event(_event(root, "PostToolUse", "Bash", "index-only")) == 0

    assert source.read_text(encoding="utf-8") == "value=1\n"
    assert not bus_log.exists()
    assert _hook_output(capsys) is None


def test_post_tool_formats_tracked_file_edited_and_staged_in_one_tool_call(
    tmp_path, monkeypatch, capsys
) -> None:
    root, _ = _repository(tmp_path)
    bus_log = tmp_path / "bus args.json"
    monkeypatch.setenv("FAKE_BUS_LOG", str(bus_log))
    monkeypatch.setattr(hook, "TEMP_ROOT", tmp_path / "hook-state")
    source = root / "src" / "staged during call.py"
    source.parent.mkdir()
    source.write_text("value = 0\n", encoding="utf-8")
    _git(root, "add", "src")
    _git(root, "commit", "-m", "baseline")

    assert hook.handle_event(_event(root, "PreToolUse", "Bash", "staged-edit")) == 0
    source.write_text("value=1\n", encoding="utf-8")
    _git(root, "add", "src/staged during call.py")
    index_before = _git(root, "write-tree")
    assert hook.handle_event(_event(root, "PostToolUse", "Bash", "staged-edit")) == 0

    assert source.read_text(encoding="utf-8") == "value = 1\n"
    assert _git(root, "write-tree") == index_before
    assert json.loads(bus_log.read_text(encoding="utf-8")) == [
        "format",
        "--apply",
        "src/staged during call.py",
    ]
    assert _hook_output(capsys) is not None


@pytest.mark.parametrize(
    ("tool_name", "tool_use_id"),
    [("Bash", "shell"), ("apply_patch", "patch"), ("mcp__filesystem__write_file", "mcp")],
)
def test_supported_tool_routes_format_new_source_files(
    tmp_path, monkeypatch, capsys, tool_name, tool_use_id
) -> None:
    root, _ = _repository(tmp_path)
    monkeypatch.setattr(hook, "TEMP_ROOT", tmp_path / "hook-state")
    monkeypatch.setenv("FAKE_BUS_LOG", str(tmp_path / "bus args.json"))
    source = root / "src" / "new source.py"
    source.parent.mkdir()

    assert hook.handle_event(_event(root, "PreToolUse", tool_name, tool_use_id)) == 0
    source.write_text("value=1\n", encoding="utf-8")
    assert hook.handle_event(_event(root, "PostToolUse", tool_name, tool_use_id)) == 0

    assert source.read_text(encoding="utf-8") == "value = 1\n"
    assert "new source.py" in json.dumps(_hook_output(capsys))


def test_post_tool_ignores_no_change_markdown_generated_and_deleted_files(
    tmp_path, monkeypatch, capsys
) -> None:
    root, _ = _repository(tmp_path)
    monkeypatch.setattr(hook, "TEMP_ROOT", tmp_path / "hook-state")
    monkeypatch.setenv("FAKE_BUS_LOG", str(tmp_path / "bus args.json"))
    docs = root / "docs" / "readme.md"
    generated = root / "src" / "model.generated.py"
    deleted = root / "src" / "deleted.py"
    docs.parent.mkdir()
    (root / "src").mkdir()
    docs.write_text("# docs\n", encoding="utf-8")
    generated.write_text("value=1\n", encoding="utf-8")
    deleted.write_text("value=1\n", encoding="utf-8")
    _git(root, "add", "docs", "src")
    _git(root, "commit", "-m", "baseline")

    assert hook.handle_event(_event(root, "PreToolUse", "Bash", "tool-no-change")) == 0
    docs.write_text("# changed documentation\n", encoding="utf-8")
    generated.write_text("value=2\n", encoding="utf-8")
    deleted.unlink()
    assert hook.handle_event(_event(root, "PostToolUse", "Bash", "tool-no-change")) == 0

    assert _hook_output(capsys) is None
    assert not (tmp_path / "bus args.json").exists()


def test_missing_snapshot_reports_explicit_recovery_without_formatting_guess(
    tmp_path, monkeypatch, capsys
) -> None:
    root, _ = _repository(tmp_path)
    monkeypatch.setattr(hook, "TEMP_ROOT", tmp_path / "hook-state")

    assert hook.handle_event(_event(root, "PostToolUse", "Bash", "missing")) == 0

    output = _hook_output(capsys)
    assert output is not None
    assert output["decision"] == "block"
    assert "cannot determine" in output["reason"]
    assert "py -3 tools/run.py format --apply <changed-paths>" in output["reason"]


def test_expired_snapshot_reports_recovery_without_formatting_guess(
    tmp_path, monkeypatch, capsys
) -> None:
    root, _ = _repository(tmp_path)
    state_root = tmp_path / "hook-state"
    monkeypatch.setattr(hook, "TEMP_ROOT", state_root)
    event = _event(root, "PreToolUse", "Bash", "expired")

    assert hook.handle_event(event) == 0
    state_file = hook._snapshot_path(root.resolve(), "session-for-tests", "expired")
    os.utime(state_file, (1, 1))
    event["hook_event_name"] = "PostToolUse"
    assert hook.handle_event(event) == 0

    output = _hook_output(capsys)
    assert output is not None
    assert output["decision"] == "block"
    assert "expired" in output["reason"]
    assert "completed" in output["reason"]


def test_mismatched_invocation_cannot_consume_another_tool_snapshot(
    tmp_path, monkeypatch, capsys
) -> None:
    root, _ = _repository(tmp_path)
    monkeypatch.setattr(hook, "TEMP_ROOT", tmp_path / "hook-state")
    event = _event(root, "PreToolUse", "Bash", "original")

    assert hook.handle_event(event) == 0
    event["hook_event_name"] = "PostToolUse"
    event["tool_use_id"] = "different"
    assert hook.handle_event(event) == 0

    output = _hook_output(capsys)
    assert output is not None
    assert output["decision"] == "block"
    assert "no matching before-snapshot" in output["reason"]
    assert hook._snapshot_path(root.resolve(), "session-for-tests", "original").is_file()


def test_main_decodes_codex_event_from_stdin_and_runs_synchronously(
    tmp_path, monkeypatch, capsys
) -> None:
    root, _ = _repository(tmp_path)
    monkeypatch.setattr(hook, "TEMP_ROOT", tmp_path / "hook-state")
    monkeypatch.setenv("FAKE_BUS_LOG", str(tmp_path / "bus args.json"))
    source = root / "src" / "stdin.py"
    source.parent.mkdir()
    source.write_text("value=1\n", encoding="utf-8")
    pre_event = _event(root, "PreToolUse", "Bash", "stdin-flow")
    monkeypatch.setattr(hook.sys, "stdin", io.StringIO(json.dumps(pre_event)))

    assert hook.main() == 0
    assert capsys.readouterr().out == ""
    source.write_text("value=1\n# tool write\n", encoding="utf-8")
    post_event = _event(root, "PostToolUse", "Bash", "stdin-flow")
    monkeypatch.setattr(hook.sys, "stdin", io.StringIO(json.dumps(post_event)))

    assert hook.main() == 0

    assert source.read_text(encoding="utf-8") == "value = 1\n# tool write\n"
    assert "stdin.py" in json.dumps(_hook_output(capsys))


def test_formatter_failure_returns_actual_diagnostic_and_keeps_completed_write(
    tmp_path, monkeypatch, capsys
) -> None:
    root, _ = _repository(tmp_path)
    monkeypatch.setattr(hook, "TEMP_ROOT", tmp_path / "hook-state")
    monkeypatch.setenv("FAKE_BUS_LOG", str(tmp_path / "bus args.json"))
    monkeypatch.setenv("FAKE_BUS_FAIL", "1")
    source = root / "src" / "$HOME;`touch marker`.py"
    source.parent.mkdir()
    source.write_text("value=1\n", encoding="utf-8")

    assert hook.handle_event(_event(root, "PreToolUse", "Bash", "tool-fails")) == 0
    source.write_text("value=2\n", encoding="utf-8")
    assert hook.handle_event(_event(root, "PostToolUse", "Bash", "tool-fails")) == 0

    output = _hook_output(capsys)
    assert output is not None
    assert output["decision"] == "block"
    assert "formatter actual diagnostic" in output["reason"]
    assert "write already happened" in output["reason"]
    assert "py -3 tools/run.py format --apply <changed-paths>" in output["reason"]
    assert 'Changed paths (JSON array): ["src/$HOME;`touch marker`.py"]' in output["reason"]
    assert '`py -3 tools/run.py format --apply "src/$HOME;`' not in output["reason"]
    assert source.read_text(encoding="utf-8") == "value=2\n"
