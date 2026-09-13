#!/usr/bin/env python3
"""Tests for the hooks pack and live doctor capability rechecks."""

from __future__ import annotations

import json
import os
import subprocess
import sys
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
SCRIPTS = TESTS_DIR.parent / "scripts"
sys.path.insert(0, str(TESTS_DIR))
sys.path.insert(0, str(SCRIPTS))

from review_core import engine  # noqa: E402

import reviewctl  # noqa: E402

REVIEWCTL = SCRIPTS / "reviewctl.py"
HOOKS_DIR = TESTS_DIR.parent / "assets" / "hooks"


def _ctl(*args, runtime="devin-desktop", env_extra=None):
    env = dict(os.environ)
    if runtime is None:
        env.pop(engine.RUNTIME_ENV_VAR, None)
    else:
        env[engine.RUNTIME_ENV_VAR] = runtime
    if env_extra:
        env.update(env_extra)
    return subprocess.run(
        ["py", "-3", str(REVIEWCTL), *args],
        capture_output=True,
        text=True,
        env=env,
    )


def _run_hook(script: Path, stdin_payload) -> subprocess.CompletedProcess:
    env = dict(os.environ)
    env["IR_HOOK_ENV"] = str(script.parent / "hook-env.json")
    data = stdin_payload if isinstance(stdin_payload, str) else json.dumps(stdin_payload)
    return subprocess.run(
        ["py", "-3", str(script)],
        input=data,
        capture_output=True,
        text=True,
        encoding="utf-8",
        env=env,
    )


def _hook_env(root: Path, *, deny_roots=()) -> Path:
    """Materialize a hook working dir: scripts + hook-env.json."""
    hook_dir = root / "hooks"
    hook_dir.mkdir(parents=True, exist_ok=True)
    env = {
        "transcript_root": str(root / "transcripts"),
        "deny_roots": [str(r) for r in deny_roots],
    }
    (hook_dir / "hook-env.json").write_text(json.dumps(env), encoding="utf-8")
    for name in ("record_pretool.py", "record_posttool.py", "gate_review_paths.py"):
        target = hook_dir / name
        target.write_text((HOOKS_DIR / name).read_text(encoding="utf-8"), encoding="utf-8")
    return hook_dir


class TestHookScripts:
    def test_pretool_appends_verbatim_jsonl_line(self, tmp_path):
        hooks = _hook_env(tmp_path)
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "exec",
            "tool_input": {"command": "git status"},
            "tool_use_id": "exec_1",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "record_pretool.py", payload)
        assert r.returncode == 0, r.stderr
        out = tmp_path / "transcripts" / "sess-1.jsonl"
        lines = out.read_text(encoding="utf-8").splitlines()
        assert len(lines) == 1
        assert json.loads(lines[0]) == payload

    def test_posttool_appends_and_keeps_tool_response(self, tmp_path):
        hooks = _hook_env(tmp_path)
        payload = {
            "hook_event_name": "PostToolUse",
            "tool_name": "exec",
            "tool_input": {"command": "git status"},
            "tool_use_id": "exec_1",
            "session_id": "sess-1",
            "prompt_id": "p-1",
            "tool_response": {"success": True, "output": "clean", "error": None},
        }
        r = _run_hook(hooks / "record_posttool.py", payload)
        assert r.returncode == 0, r.stderr
        line = (tmp_path / "transcripts" / "sess-1.jsonl").read_text().splitlines()[0]
        assert json.loads(line)["tool_response"]["output"] == "clean"

    def test_malformed_stdin_writes_hook_error_not_crash(self, tmp_path):
        hooks = _hook_env(tmp_path)
        r = _run_hook(hooks / "record_pretool.py", "not json {")
        assert r.returncode == 0
        err = tmp_path / "transcripts" / "hook-errors.jsonl"
        assert err.is_file() and "hook-error" in err.read_text(encoding="utf-8")

    def test_gate_denies_exec_command_containing_deny_root(self, tmp_path):
        deny = tmp_path / "review-state"
        hooks = _hook_env(tmp_path, deny_roots=(deny,))
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "exec",
            "tool_input": {"command": f'type "{deny / "state.json"}"'},
            "tool_use_id": "exec_9",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "gate_review_paths.py", payload)
        assert r.returncode == 2
        assert '"decision": "block"' in r.stdout or '"decision":"block"' in r.stdout

    def test_gate_denies_list_form_command(self, tmp_path):
        deny = tmp_path / "review-state"
        hooks = _hook_env(tmp_path, deny_roots=(deny,))
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "exec",
            "tool_input": {"command": ["type", str(deny / "state.json")]},
            "tool_use_id": "exec_9",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "gate_review_paths.py", payload)
        assert r.returncode == 2
        assert '"decision": "block"' in r.stdout or '"decision":"block"' in r.stdout

    def test_gate_deny_roots_unresolvable_falls_back_to_text(self, tmp_path, monkeypatch):
        # A deny_roots entry that fails Path.resolve (e.g. ValueError on a
        # NUL byte) falls back to literal-text matching instead of crashing
        # through the generic internal-error path.
        import importlib.util

        hooks = _hook_env(tmp_path)
        spec = importlib.util.spec_from_file_location("gate_review_paths", hooks / "gate_review_paths.py")
        gate = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(gate)

        def bad_resolve(self, *a, **k):
            raise ValueError("embedded null byte")

        monkeypatch.setattr(Path, "resolve", bad_resolve)
        roots = gate._deny_roots({"deny_roots": ["sealed-root"]})
        assert roots == [gate._norm("sealed-root")]

    def test_recorder_malformed_transcript_root_never_nonzero(self, tmp_path):
        # A NUL-byte transcript_root in hook-env.json must never produce a
        # nonzero exit; the recorder swallows it per contract.
        hooks = _hook_env(tmp_path)
        env_path = hooks / "hook-env.json"
        env = json.loads(env_path.read_text(encoding="utf-8"))
        env["transcript_root"] = "evil\x00root"
        env_path.write_text(json.dumps(env), encoding="utf-8")
        r = _run_hook(
            hooks / "record_pretool.py",
            {"tool_input": {"command": "x"}, "session_id": "s", "hook_event_name": "PreToolUse"},
        )
        assert r.returncode == 0

    def test_gate_allows_unrelated_exec(self, tmp_path):
        hooks = _hook_env(tmp_path, deny_roots=(tmp_path / "review-state",))
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "exec",
            "tool_input": {"command": "git status"},
            "tool_use_id": "exec_9",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "gate_review_paths.py", payload)
        assert r.returncode == 0 and "block" not in r.stdout

    def test_gate_denies_write_under_deny_root(self, tmp_path):
        deny = tmp_path / "review-state"
        hooks = _hook_env(tmp_path, deny_roots=(deny,))
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "write",
            "tool_input": {"file_path": str(deny / "x.json")},
            "tool_use_id": "w_1",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "gate_review_paths.py", payload)
        assert r.returncode == 2
        assert "block" in r.stdout

    def test_gate_denies_read_of_witness_log(self, tmp_path):
        deny = tmp_path / "review-state"
        hooks = _hook_env(tmp_path, deny_roots=(deny,))
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "read",
            "tool_input": {"file_path": str(deny / "witness" / "witness-log.jsonl")},
            "tool_use_id": "r_1",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "gate_review_paths.py", payload)
        assert r.returncode == 2
        assert "block" in r.stdout

    def test_gate_fails_closed_on_malformed_payload(self, tmp_path):
        hooks = _hook_env(tmp_path, deny_roots=(tmp_path / "review-state",))
        r = _run_hook(hooks / "gate_review_paths.py", "not json {")
        assert r.returncode == 2
        assert "block" in r.stdout

    def test_gate_denies_when_env_missing(self, tmp_path):
        hooks = _hook_env(tmp_path, deny_roots=(tmp_path / "sealed",))
        (hooks / "hook-env.json").unlink()
        r = _run_hook(hooks / "gate_review_paths.py", {"tool_input": {"cwd": str(tmp_path)}})
        assert r.returncode == 2
        assert "block" in r.stdout

    def test_gate_denies_when_env_corrupt(self, tmp_path):
        hooks = _hook_env(tmp_path, deny_roots=(tmp_path / "sealed",))
        (hooks / "hook-env.json").write_text("{ not json", encoding="utf-8")
        r = _run_hook(hooks / "gate_review_paths.py", {"tool_input": {"cwd": str(tmp_path)}})
        assert r.returncode == 2
        assert "block" in r.stdout

    def test_gate_unconfigured_env_stays_open(self, tmp_path):
        hooks = _hook_env(tmp_path)
        r = _run_hook(hooks / "gate_review_paths.py", "not json {")
        assert r.returncode == 0

    def test_gate_blocks_on_non_list_deny_roots(self, tmp_path):
        hooks = _hook_env(tmp_path, deny_roots=(tmp_path / "sealed",))
        (hooks / "hook-env.json").write_text(json.dumps({"deny_roots": 123}), encoding="utf-8")
        r = _run_hook(hooks / "gate_review_paths.py", {"tool_input": {"cwd": str(tmp_path)}})
        assert r.returncode == 2
        assert "block" in r.stdout

    def test_gate_blocks_on_string_deny_roots(self, tmp_path):
        hooks = _hook_env(tmp_path, deny_roots=(tmp_path / "sealed",))
        (hooks / "hook-env.json").write_text(json.dumps({"deny_roots": str(tmp_path / "sealed")}), encoding="utf-8")
        r = _run_hook(hooks / "gate_review_paths.py", {"tool_input": {"cwd": str(tmp_path)}})
        assert r.returncode == 2
        assert "block" in r.stdout

    def test_recorder_survives_malformed_transcript_root(self, tmp_path):
        hooks = _hook_env(tmp_path)
        (hooks / "hook-env.json").write_text(json.dumps({"transcript_root": 123}), encoding="utf-8")
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "exec",
            "tool_input": {"command": "git status"},
            "tool_use_id": "exec_1",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "record_pretool.py", payload)
        assert r.returncode == 0, r.stderr
        out = hooks / "transcripts" / "sess-1.jsonl"
        assert json.loads(out.read_text(encoding="utf-8").splitlines()[0]) == payload

    def test_recorder_survives_non_object_env(self, tmp_path):
        hooks = _hook_env(tmp_path)
        (hooks / "hook-env.json").write_text("123", encoding="utf-8")
        payload = {"session_id": "sess-9", "tool_input": {"command": "git status"}}
        r = _run_hook(hooks / "record_posttool.py", payload)
        assert r.returncode == 0, r.stderr

    def test_gate_denies_relative_path_via_cwd(self, tmp_path):
        deny = tmp_path / "review-state"
        deny.mkdir()
        hooks = _hook_env(tmp_path, deny_roots=(deny,))
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "write",
            "tool_input": {"file_path": "state.json", "cwd": str(deny)},
            "tool_use_id": "w_2",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "gate_review_paths.py", payload)
        assert r.returncode == 2

    @pytest.mark.skipif(sys.platform == "win32", reason="posix mode bits")
    def test_recorder_locks_down_transcript_permissions(self, tmp_path):
        import stat

        hooks = _hook_env(tmp_path)
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "exec",
            "tool_input": {"command": "git status"},
            "tool_use_id": "exec_1",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "record_pretool.py", payload)
        assert r.returncode == 0, r.stderr
        tdir = tmp_path / "transcripts"
        assert stat.S_IMODE(tdir.stat().st_mode) & 0o077 == 0
        assert stat.S_IMODE((tdir / "sess-1.jsonl").stat().st_mode) & 0o077 == 0

    @pytest.mark.skipif(sys.platform == "win32", reason="posix mode bits")
    def test_recorder_refuses_world_writable_transcript(self, tmp_path):
        hooks = _hook_env(tmp_path)
        tdir = tmp_path / "transcripts"
        tdir.mkdir(parents=True)
        target = tdir / "sess-1.jsonl"
        target.write_text('{"existing": true}' + chr(10), encoding="utf-8")
        os.chmod(target, 0o666)
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "exec",
            "tool_input": {"command": "git status"},
            "tool_use_id": "exec_1",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "record_pretool.py", payload)
        assert r.returncode == 0
        assert target.read_text(encoding="utf-8").count(chr(10)) == 1
        errs = (tdir / "hook-errors.jsonl").read_text(encoding="utf-8")
        assert "acl-untrusted" in errs

    def test_recorder_decodes_utf8_stdin(self, tmp_path):
        hooks = _hook_env(tmp_path)
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "exec",
            "tool_input": {"command": "café 中文"},
            "tool_use_id": "exec_1",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "record_pretool.py", json.dumps(payload, ensure_ascii=False))
        assert r.returncode == 0, r.stderr
        line = (tmp_path / "transcripts" / "sess-1.jsonl").read_text(encoding="utf-8").splitlines()[0]
        assert json.loads(line)["tool_input"]["command"] == "café 中文"


class TestGateBoundaryMatching:
    def test_gate_allows_path_with_root_as_inner_segment(self, tmp_path):
        # Resolved paths only deny on prefix match; a deny root appearing as
        # a mid-path segment (e.g. a backup tree) is not a denial.
        deny = tmp_path / "s" / "witness"
        deny.mkdir(parents=True)
        candidate = tmp_path / "a" / "b" / "s" / "witness"
        candidate.mkdir(parents=True)
        target = candidate / "file.txt"
        target.write_text("x")
        hooks = _hook_env(tmp_path, deny_roots=(deny,))
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "read",
            "tool_input": {"file_path": str(target), "cwd": str(tmp_path)},
            "tool_use_id": "e_9",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "gate_review_paths.py", payload)
        assert r.returncode == 0

    def test_gate_allows_sibling_of_deny_root(self, tmp_path):
        deny = tmp_path / "review-state" / "witness"
        deny.mkdir(parents=True)
        sibling = tmp_path / "review-state" / "witness-backup"
        sibling.mkdir()
        hooks = _hook_env(tmp_path, deny_roots=(deny,))
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "write",
            "tool_input": {"file_path": str(sibling / "x.txt")},
            "tool_use_id": "w_1",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "gate_review_paths.py", payload)
        assert r.returncode == 0

    def test_gate_denies_env_var_path(self, tmp_path):
        deny = tmp_path / "review-state"
        deny.mkdir()
        hooks = _hook_env(tmp_path, deny_roots=(deny,))
        os.environ["IR_TEST_DENY"] = str(deny)
        try:
            payload = {
                "hook_event_name": "PreToolUse",
                "tool_name": "write",
                "tool_input": {"file_path": "%IR_TEST_DENY%/state.json"},
                "tool_use_id": "w_2",
                "session_id": "sess-1",
                "prompt_id": "p-1",
            }
            r = _run_hook(hooks / "gate_review_paths.py", payload)
            assert r.returncode == 2
        finally:
            del os.environ["IR_TEST_DENY"]

    def test_gate_denies_command_text_mentioning_root(self, tmp_path):
        deny = tmp_path / "review-state" / "witness"
        deny.mkdir(parents=True)
        hooks = _hook_env(tmp_path, deny_roots=(deny,))
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "exec",
            "tool_input": {"command": f"cat {deny}/log.jsonl"},
            "tool_use_id": "e_1",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "gate_review_paths.py", payload)
        assert r.returncode == 2

    def test_gate_allows_command_with_sibling_name(self, tmp_path):
        deny = tmp_path / "review-state" / "witness"
        deny.mkdir(parents=True)
        hooks = _hook_env(tmp_path, deny_roots=(deny,))
        payload = {
            "hook_event_name": "PreToolUse",
            "tool_name": "exec",
            "tool_input": {"command": f"cat {deny}-backup/log.jsonl"},
            "tool_use_id": "e_2",
            "session_id": "sess-1",
            "prompt_id": "p-1",
        }
        r = _run_hook(hooks / "gate_review_paths.py", payload)
        assert r.returncode == 0


class TestHooksInstall:
    def test_install_renders_pack_and_env(self, tmp_path):
        scratch = tmp_path / "scratch"
        r = _ctl("hooks", "install", "--scratch-dir", str(scratch))
        assert r.returncode == 0, r.stderr
        hook_dir = scratch / "hooks"
        for name in (
            "record_pretool.py",
            "record_posttool.py",
            "gate_review_paths.py",
            "hooks.v1.json",
            "hook-env.json",
        ):
            assert (hook_dir / name).is_file(), name
        cfg = json.loads((hook_dir / "hooks.v1.json").read_text(encoding="utf-8"))
        rendered = json.dumps(cfg)
        assert "{{" not in rendered
        env = json.loads((hook_dir / "hook-env.json").read_text(encoding="utf-8"))
        assert env["transcript_root"] == str(scratch / "transcripts")
        assert str(scratch) in env["deny_roots"][0] or str(scratch) in env["deny_roots"]

    def test_status_reports_transcript_dir(self, tmp_path):
        scratch = tmp_path / "scratch"
        _ctl("hooks", "install", "--scratch-dir", str(scratch))
        r = _ctl("hooks", "status", "--scratch-dir", str(scratch), "--json")
        assert r.returncode == 0
        obj = json.loads(r.stdout)
        assert obj["installed"] is True

    def test_hooks_bare_subcommand_is_usage_error(self):
        r = _ctl("hooks")
        assert r.returncode == 2


class TestDoctorRows:
    def _run_cmd_ok(self, argv, **_kw):
        cmd = " ".join(str(a) for a in argv)
        if "auth status" in cmd:
            return 0, "Logged in", ""
        if "is-shallow-repository" in cmd:
            return 0, "false\n", ""
        return 0, "git version 2.45\n", ""

    def test_doctor_rows_all_pass_on_healthy(self, tmp_path):
        scratch = tmp_path / "scratch"
        _ctl("hooks", "install", "--scratch-dir", str(scratch))
        rows, verdict = reviewctl._doctor_rows(
            runtime=engine.RUNTIME_DEVIN_DESKTOP,
            scratch_dir=scratch,
            repo=tmp_path,
            run_cmd=self._run_cmd_ok,
        )
        assert verdict == "pass"
        assert all(r["status"] in ("pass", "skip") for r in rows), rows
        names = {r["name"] for r in rows}
        assert {
            "hooks-installed",
            "transcript-dir-writable",
            "witness-log-roundtrip",
            "git-present",
            "repo-non-shallow",
            "gh-authenticated",
        } <= names

    def test_doctor_fails_when_gh_unauthenticated(self, tmp_path):
        def bad(argv, **_kw):
            cmd = " ".join(str(a) for a in argv)
            if "auth status" in cmd:
                return 1, "", "not logged in"
            return self._run_cmd_ok(argv)

        _ctl("hooks", "install", "--scratch-dir", str(tmp_path / "scratch"))
        rows, verdict = reviewctl._doctor_rows(
            runtime=engine.RUNTIME_DEVIN_DESKTOP,
            scratch_dir=tmp_path / "scratch",
            repo=tmp_path,
            run_cmd=bad,
        )
        assert verdict == "capability-floor-failed"
        gh = next(r for r in rows if r["name"] == "gh-authenticated")
        assert gh["status"] == "fail" and gh["remediation"]

    def test_doctor_witness_log_roundtrip_row(self, tmp_path):
        _ctl("hooks", "install", "--scratch-dir", str(tmp_path / "scratch"))
        rows, _ = reviewctl._doctor_rows(
            runtime=engine.RUNTIME_DEVIN_DESKTOP,
            scratch_dir=tmp_path / "scratch",
            repo=tmp_path,
            run_cmd=self._run_cmd_ok,
        )
        row = next(r for r in rows if r["name"] == "witness-log-roundtrip")
        assert row["status"] == "pass"
        assert (tmp_path / "scratch" / "witness" / "doctor-probe.jsonl").is_file()

    def test_doctor_rows_skip_without_scratch(self, tmp_path):
        rows, verdict = reviewctl._doctor_rows(
            runtime=engine.RUNTIME_DEVIN_DESKTOP,
            scratch_dir=None,
            repo=None,
            run_cmd=self._run_cmd_ok,
        )
        row = next(r for r in rows if r["name"] == "witness-log-roundtrip")
        assert row["status"] == "skip"

    def test_doctor_gh_whitespace_output_reports_fail_not_crash(self, tmp_path):
        def run_cmd(argv, **_kw):
            if argv[:2] == ["gh", "auth"]:
                return 1, "", "   " + chr(10)
            return 0, "ok", ""

        rows, verdict = reviewctl._doctor_rows(
            runtime=engine.RUNTIME_DEVIN_DESKTOP,
            scratch_dir=None,
            repo=None,
            run_cmd=run_cmd,
        )
        gh = next(r for r in rows if r["name"] == "gh-authenticated")
        assert gh["status"] == "fail" and gh["detail"] == ""
        assert verdict == "capability-floor-failed"

    def test_run_cmd_decodes_utf8_output(self):
        rc, out, _err = reviewctl._run_cmd(
            [
                "py",
                "-3",
                "-c",
                "import sys; sys.stdout.buffer.write('café'.encode('utf-8'))",
            ]
        )
        assert rc == 0
        assert out == "café"

    def test_doctor_inert_runtime_still_verdict_inert(self, tmp_path):
        r = _ctl("doctor", runtime="unknown")
        assert r.returncode == 1
        assert "inert" in r.stdout

    def test_doctor_cli_rows_in_json(self, tmp_path):
        scratch = tmp_path / "scratch"
        _ctl("hooks", "install", "--scratch-dir", str(scratch))
        r = _ctl("doctor", "--scratch-dir", str(scratch), "--repo", str(tmp_path), "--json")
        obj = json.loads(r.stdout)
        assert "rows" in obj and obj["verdict"] in ("pass", "capability-floor-failed")

    def test_gate_blocks_stable_reason_when_cwd_unavailable(self, tmp_path, monkeypatch, capsys):
        import importlib.util
        import io
        from types import SimpleNamespace

        hooks = _hook_env(tmp_path, deny_roots=[tmp_path / "sealed"])
        spec = importlib.util.spec_from_file_location("gate_review_paths", hooks / "gate_review_paths.py")
        gate = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(gate)
        payload = {"tool_input": {"command": "echo hi"}}
        monkeypatch.setattr(sys, "stdin", SimpleNamespace(buffer=io.BytesIO(json.dumps(payload).encode())))

        def no_cwd():
            raise OSError("cwd deleted")

        monkeypatch.setattr(os, "getcwd", no_cwd)
        rc = gate.main()
        out = json.loads(capsys.readouterr().out)
        assert rc == 2
        assert out["decision"] == "block"
        assert "cwd-unavailable" in out["reason"]
