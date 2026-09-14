#!/usr/bin/env python3
"""reviewctl: the single version-2 review control plane.

This CLI is the only mutation authority for version-2 (schema_version 2)
review state. It is experimental until the cutover plan; version-1 reviews
continue through next_node.py. All commands except ``doctor`` and ``hooks`` take
``--state`` pointing at a review-state.json file; ``doctor`` and ``hooks``
take ``--scratch-dir`` instead.

Mutation commands (``init --apply``, ``dispatch``, ``enumerate``,
``complete``, ``block``, ``resume``, and the ``freeze``/``refresh``
aliases) run only on the Devin Desktop runtime; on any other harness they
report ``unsupported-runtime`` and exit 1 without creating or mutating
state. ``status``, ``next``, ``validate``, and ``doctor`` are read-only and
run on any runtime; ``doctor`` exits 1 with ``verdict: inert`` off Devin
Desktop.
"""

from __future__ import annotations

import argparse
import functools
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from review_core import acquisition, engine, model, policy, store, witness_log  # noqa: E402


USAGE_ERRORS = 2
_HOOK_SCRIPT_NAMES = ("record_pretool.py", "record_posttool.py", "gate_review_paths.py")
_SEALED_SUBDIRS = ("witness", "transcripts", "evidence-store")


def _fail(message: str, code: int = 1) -> int:
    print(message, file=sys.stderr)
    return code


def _emit(obj: dict, json_mode: bool) -> int:
    if json_mode:
        print(json.dumps(obj, indent=2, sort_keys=True))
    else:
        for key, value in obj.items():
            if isinstance(value, (dict, list)):
                value = json.dumps(value, sort_keys=True)
            print(f"{key}: {value}")
    return 0


def _decision_obj(result: engine.EngineResult) -> dict:
    d = result.decision
    obj = {
        "allowed": d.allowed,
        "action": d.action,
        "reason": d.reason,
        "missing": list(d.missing),
        "generation": result.generation,
        "state": str(result.state_path),
    }
    if d.status is not None:
        obj["status"] = d.status
    if d.recipe is not None:
        obj["recipe"] = {
            "dispatch_required": d.recipe.dispatch_required,
            "required_role": d.recipe.required_role,
            "minimum_capability_tier": d.recipe.minimum_capability_tier,
            "minimum_reasoning_floor": d.recipe.minimum_reasoning_floor,
            "preferred_profile": d.recipe.preferred_profile,
            "data_keys": list(d.recipe.data_keys),
            "evidence_kinds": list(d.recipe.evidence_kinds),
            "record_command": d.recipe.record_command,
        }
    return obj


def _schema_version_of(path: Path):
    """Read a state file's schema_version without imposing v2 validity."""
    try:
        raw = path.read_bytes()
    except OSError:
        return "missing"
    try:
        obj = model.strict_json_loads(raw, source=str(path))
    except Exception:
        return "unparseable"
    if isinstance(obj, dict):
        return obj.get("schema_version")
    return "unparseable"


def _require_v2_state(path: Path) -> int:
    """Exit 1 unless ``path`` holds a version-2 state; version-1 or legacy
    files are routed back to the legacy toolchain."""
    version = _schema_version_of(path)
    if version == model.SCHEMA_VERSION:
        return 0
    if version == "missing":
        return _fail(f"state-missing: no state file at {path}")
    if version == "unparseable":
        return _fail(f"state-invalid: {path} is not a JSON object")
    return _fail(f"state-version: {path} is a version-1 review state; version-2 control lives only in reviewctl")


def _runtime_gate() -> int:
    runtime = engine.detect_runtime()
    if runtime == engine.RUNTIME_DEVIN_DESKTOP:
        return 0
    return _fail(f"unsupported-runtime: version-2 mutations require devin-desktop (detected {runtime})")


def _sources() -> engine.WitnessSources:
    return engine.load_witness_sources()


def _parse_evidence_specs(specs) -> tuple:
    out = []
    for spec in specs or ():
        parts = spec.split("=", 2)
        if len(parts) != 3:
            raise model.StateValidationError(
                "bad-usage",
                "evidence-file",
                f"expected alias=kind=absolute-path, got {spec!r}",
            )
        alias, kind, path = parts
        if not path:
            raise model.StateValidationError("bad-usage", "evidence-file", f"empty path in {spec!r}")
        out.append(engine.EvidenceSource(alias=alias, kind=kind, path=Path(path)))
    return tuple(out)


def _hook_assets_dir() -> Path:
    return Path(__file__).resolve().parent.parent / "assets" / "hooks"


def _hook_install_dir(scratch_dir: Path) -> Path:
    return Path(scratch_dir) / "hooks"


def _run_cmd(argv, cwd=None) -> tuple[int, str, str]:
    proc = subprocess.run(
        [str(a) for a in argv],
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="surrogateescape",
        cwd=cwd,
    )
    return proc.returncode, proc.stdout, proc.stderr


def _run_git(argv, cwd=None) -> tuple[int, str, str]:
    return _run_cmd(["git", *argv], cwd=cwd)


def _run_gh(argv, cwd=None) -> tuple[int, str, str]:
    return _run_cmd(["gh", *argv], cwd=cwd)


def _doctor_rows(*, runtime, scratch_dir=None, repo=None, run_cmd=None):
    """Live per-row capability rechecks. Each row is
    ``{name, status: pass|fail|skip, detail, remediation}``."""
    run_cmd = run_cmd or _run_cmd
    rows: list[dict] = []

    def add(name, status, detail="", remediation=""):
        rows.append({"name": name, "status": status, "detail": detail, "remediation": remediation})

    scratch = Path(scratch_dir) if scratch_dir else None
    if scratch is None:
        add("hooks-installed", "skip", "no --scratch-dir given", "run `reviewctl hooks install --scratch-dir <dir>`")
        add("transcript-dir-writable", "skip", "no --scratch-dir given")
        add("witness-log-roundtrip", "skip", "no --scratch-dir given")
    else:
        hook_dir = _hook_install_dir(scratch)
        present = (hook_dir / "hooks.v1.json").is_file() and all((hook_dir / n).is_file() for n in _HOOK_SCRIPT_NAMES)
        add(
            "hooks-installed",
            "pass" if present else "fail",
            str(hook_dir),
            "run `reviewctl hooks install --scratch-dir <dir>` then install the rendered hooks.v1.json",
        )
        tdir = scratch / "transcripts"
        try:
            tdir.mkdir(parents=True, exist_ok=True)
            probe = tdir / ".doctor-probe"
            probe.write_text("ok", encoding="utf-8")
            probe.unlink()
            add("transcript-dir-writable", "pass", str(tdir))
        except OSError as exc:
            add("transcript-dir-writable", "fail", str(exc), "create the transcript directory with write access")
        try:
            log = witness_log.WitnessLog(scratch / "witness" / "doctor-probe.jsonl")
            log.append(session_id="doctor", tool_use_id=None, record_kind="marker", payload={"probe": True})
            ok, err = log.verify_chain()
            add("witness-log-roundtrip", "pass" if ok else "fail", err or "chain verified")
        except Exception as exc:  # noqa: BLE001 - row reports, not crashes
            add("witness-log-roundtrip", "fail", str(exc), "check scratch-store permissions")

    try:
        rc, out, _err = run_cmd(["git", "--version"])
    except OSError as exc:
        add("git-present", "fail", str(exc), "install git on PATH")
    else:
        add(
            "git-present",
            "pass" if rc == 0 else "fail",
            out.strip()[:80] if rc == 0 else "git not found",
            "install git on PATH",
        )

    if repo is None:
        add("repo-non-shallow", "skip", "no --repo given")
    else:
        try:
            rc, out, err = run_cmd(["git", "-C", str(repo), "rev-parse", "--is-shallow-repository"])
        except OSError as exc:
            add("repo-non-shallow", "fail", str(exc), "install git on PATH")
        else:
            shallow = out.strip() == "true"
            add(
                "repo-non-shallow",
                "pass" if rc == 0 and not shallow else "fail",
                out.strip() or err.strip()[:80],
                "fetch full history (git fetch --unshallow)",
            )

    try:
        rc, out, err = run_cmd(["gh", "auth", "status"])
    except OSError as exc:
        add("gh-authenticated", "fail", str(exc), "run `gh auth login`")
    else:
        add(
            "gh-authenticated",
            "pass" if rc == 0 else "fail",
            ((out or err).strip().splitlines() or [""])[0][:80],
            "run `gh auth login`",
        )

    verdict = "capability-floor-failed" if any(r["status"] == "fail" for r in rows) else "pass"
    return rows, verdict


def _cmd_doctor(args, json_mode: bool) -> int:
    runtime = engine.detect_runtime()
    supported = runtime == engine.RUNTIME_DEVIN_DESKTOP
    if not supported:
        obj = {"runtime": runtime, "supported": False, "verdict": "inert"}
        _emit(obj, json_mode)
        return 1
    rows, verdict = _doctor_rows(
        runtime=runtime,
        scratch_dir=getattr(args, "scratch_dir", None),
        repo=getattr(args, "repo", None),
    )
    obj = {
        "runtime": runtime,
        "supported": True,
        "verdict": verdict,
        "rows": rows,
    }
    _emit(obj, json_mode)
    return 0 if verdict == "pass" else 1


def _cmd_hooks(args, json_mode: bool) -> int:
    scratch = Path(args.scratch_dir).resolve()
    if args.hooks_command == "install":
        hook_dir = _hook_install_dir(scratch)
        hook_dir.mkdir(parents=True, exist_ok=True)
        assets = _hook_assets_dir()
        for name in _HOOK_SCRIPT_NAMES:
            shutil.copyfile(assets / name, hook_dir / name)
        env = {
            "transcript_root": str(scratch / "transcripts"),
            "deny_roots": [str(scratch / sub) for sub in _SEALED_SUBDIRS],
        }
        (hook_dir / "hook-env.json").write_text(json.dumps(env, indent=2), encoding="utf-8")
        template = (assets / "hooks.v1.json").read_text(encoding="utf-8")
        # The placeholder sits inside a JSON string literal, so substitute the
        # JSON-escaped content (without the surrounding quotes) to keep the
        # rendered file valid when the path contains a quote or backslash.
        escaped_dir = json.dumps(str(hook_dir).replace("\\", "/"))[1:-1]
        rendered = template.replace("{{IR_HOOK_DIR}}", escaped_dir).replace(
            "{{IR_PY}}", "py -3" if sys.platform == "win32" else "python3"
        )
        (hook_dir / "hooks.v1.json").write_text(rendered, encoding="utf-8")
        obj = {
            "installed": str(hook_dir),
            "hooks_config": str(hook_dir / "hooks.v1.json"),
            "note": (
                "install the rendered hooks.v1.json as .devin/hooks.v1.json in the "
                "reviewed project (or user-level); hooks load at session start, "
                "so a restart is required before records are emitted"
            ),
        }
        _emit(obj, json_mode)
        return 0
    if args.hooks_command == "status":
        hook_dir = _hook_install_dir(scratch)
        installed = (hook_dir / "hooks.v1.json").is_file() and all((hook_dir / n).is_file() for n in _HOOK_SCRIPT_NAMES)
        transcripts = scratch / "transcripts"
        obj = {
            "installed": installed,
            "hook_dir": str(hook_dir),
            "transcript_root": str(transcripts),
            "transcript_root_exists": transcripts.is_dir(),
        }
        _emit(obj, json_mode)
        return 0
    return _fail(f"unknown hooks command {args.hooks_command!r}", USAGE_ERRORS)


def _cmd_init(args, json_mode: bool) -> int:
    gate = _runtime_gate()
    if gate:
        return gate
    if not args.apply:
        result = engine.init_review(
            Path(args.state),
            review_id=args.review_id,
            scratch_dir=Path(args.scratch_dir).resolve(),
            apply=False,
        )
        return _emit(_decision_obj(result), json_mode)
    result = engine.init_review(
        Path(args.state),
        review_id=args.review_id,
        scratch_dir=Path(args.scratch_dir).resolve(),
        apply=True,
    )
    return _emit(_decision_obj(result), json_mode)


def _cmd_status(args, json_mode: bool) -> int:
    bad = _require_v2_state(Path(args.state))
    if bad:
        return bad
    state = store.load_state(Path(args.state))
    findings = state["findings"]
    open_findings = sum(1 for f in findings.values() if f["disposition"] in ("open", "unassessed"))
    obj = {
        "schema_version": state["schema_version"],
        "review_id": state["review_id"],
        "generation": state["generation"],
        "status": state["status"],
        "stage": state["stage"],
        "counts": {
            "findings": len(findings),
            "open_findings": open_findings,
            "dispatches": len(state["dispatches"]),
            "active_blockers": sum(1 for b in state["blockers"].values() if b["active"]),
            "witness_records": len(state["witness_records"]),
            "checks": len(state["checks"]),
        },
        "green_seal": state["green_seal"] is not None,
        "ready_transition": (state["ready_transition"]["status"] if state["ready_transition"] else None),
        "state": str(Path(args.state)),
    }
    return _emit(obj, json_mode)


def _cmd_next(args, json_mode: bool) -> int:
    bad = _require_v2_state(Path(args.state))
    if bad:
        return bad
    result = engine.next_action_for(Path(args.state), policies=_sources().policies)
    return _emit(_decision_obj(result), json_mode)


def _cmd_dispatch(args, json_mode: bool) -> int:
    gate = _runtime_gate()
    if gate:
        return gate
    if not args.apply:
        return _fail(
            f"BLOCKED: dispatch --action {args.action} requires --apply to mutate version-2 state",
            USAGE_ERRORS,
        )
    bad = _require_v2_state(Path(args.state))
    if bad:
        return bad
    sources = _sources()
    registered = engine.register_dispatch_transaction(Path(args.state), action=args.action, sources=sources)
    if not registered.decision.allowed:
        _emit(_decision_obj(registered), json_mode)
        return 1
    state = store.load_state(Path(args.state))
    role = policy.action_recipe(args.action).required_role
    dispatch_id = engine.find_pending_dispatch_id(state, role)
    if dispatch_id is None:
        # A pending dispatch may already be launched; nothing left to do.
        return _emit(_decision_obj(registered), json_mode)
    launched = engine.launch_transaction(Path(args.state), dispatch_id=dispatch_id, sources=sources)
    _emit(_decision_obj(launched), json_mode)
    return 0 if launched.decision.allowed else 1


def _cmd_enumerate(args, json_mode: bool) -> int:
    gate = _runtime_gate()
    if gate:
        return gate
    bad = _require_v2_state(Path(args.state))
    if bad:
        return bad
    state = store.load_state(Path(args.state))
    scratch = Path(state["scratch_dir"])
    epoch = state["snapshot"]["epoch"] + 1 if state["snapshot"] else 1
    repo = Path(args.repo).resolve()
    out_dir = scratch / "acquire" / "latest"
    summary = acquisition.enumerate_acquisition(
        run_git=functools.partial(_run_git, cwd=repo),
        run_gh=functools.partial(_run_gh, cwd=repo),
        repo_root=repo,
        pr_number=int(args.pr),
        out_dir=out_dir,
        scratch_dir=scratch,
        epoch=epoch,
    )
    if json_mode:
        _emit(summary, True)
    else:
        print(f"enumeration-id: {summary['enumeration_id']}")
    return 0


def _cmd_freeze(args, json_mode: bool) -> int:
    return _acquired_alias(args, "freeze-review-input", json_mode)


def _cmd_refresh(args, json_mode: bool) -> int:
    return _acquired_alias(args, "refresh-review-input", json_mode)


def _acquired_alias(args, action: str, json_mode: bool) -> int:
    """freeze/refresh conveniences over the witnessed two-command flow.

    Acquisition is always `reviewctl enumerate` then `reviewctl complete
    --action <freeze|refresh>-review-input --acquired <dir>`; these aliases
    refuse unless a prior enumerate exists for the exact current inputs
    (repo root, PR number, checked-out HEAD). The input/HEAD pre-check is a
    friendlier early refusal only: the enumeration-id witness binding at
    complete time is the authoritative staleness check, so a stale or swapped
    acquisition directory still fails closed inside `complete`.
    """
    gate = _runtime_gate()
    if gate:
        return gate
    bad = _require_v2_state(Path(args.state))
    if bad:
        return bad
    state = store.load_state(Path(args.state))
    acquire_dir = Path(state["scratch_dir"]) / "acquire" / "latest"
    try:
        enum_rec = json.loads((acquire_dir / "enumeration.json").read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return _fail(f"stale-acquisition: no enumeration under {acquire_dir}; run `reviewctl enumerate` first")
    if not isinstance(enum_rec, dict) or not isinstance(enum_rec.get("inputs"), dict):
        return _fail(f"stale-acquisition: enumeration under {acquire_dir} is malformed; re-run `reviewctl enumerate`")
    inputs = enum_rec["inputs"]
    repo = Path(args.repo).resolve()
    stored_root = inputs.get("repo_root")
    try:
        same_repo = stored_root is not None and os.path.normcase(
            str(Path(str(stored_root)).resolve())
        ) == os.path.normcase(str(repo))
    except (OSError, ValueError):
        same_repo = False
    if inputs.get("pr_number") != int(args.pr) or not same_repo:
        return _fail("stale-acquisition: enumeration was produced for different inputs; re-run `reviewctl enumerate`")
    try:
        rc, out, _err = _run_git(["rev-parse", "HEAD"], cwd=repo)
    except OSError as exc:
        return _fail(f"tool-blocked: git unavailable: {exc}")
    if rc != 0 or out.strip() != inputs.get("head_sha"):
        return _fail("stale-acquisition: checked-out HEAD moved since enumerate; re-run `reviewctl enumerate`")
    args.action = action
    args.acquired = str(acquire_dir)
    args.data_file = None
    args.evidence_file = []
    return _cmd_complete(args, json_mode)


def _cmd_complete(args, json_mode: bool) -> int:
    gate = _runtime_gate()
    if gate:
        return gate
    if not args.apply:
        return _fail(
            f"BLOCKED: complete --action {args.action} requires --apply to mutate version-2 state",
            USAGE_ERRORS,
        )
    bad = _require_v2_state(Path(args.state))
    if bad:
        return bad
    if args.acquired is not None:
        if args.action not in ("freeze-review-input", "refresh-review-input"):
            return _fail(
                f"bad-usage: --acquired only applies to freeze/refresh, not {args.action!r}",
                USAGE_ERRORS,
            )
        if args.data_file:
            return _fail(
                "bad-usage: --acquired and --data-file are mutually exclusive",
                USAGE_ERRORS,
            )
        if args.evidence_file:
            return _fail(
                "bad-usage: --acquired and --evidence-file are mutually exclusive",
                USAGE_ERRORS,
            )
        state = store.load_state(Path(args.state))
        sources = engine.load_witness_sources(
            scratch_dir=Path(state["scratch_dir"]),
            review_id=state["review_id"],
            acquisition_dir=Path(args.acquired),
        )
    else:
        sources = _sources()
    caller_data = b"{}"
    if args.data_file:
        try:
            caller_data = Path(args.data_file).read_bytes()
        except OSError as exc:
            raise acquisition.AcquisitionError(
                "missing-source", f"--data-file {args.data_file} unreadable: {exc}"
            ) from exc
    caller_evidence = _parse_evidence_specs(args.evidence_file)
    result = engine.complete_transaction(
        Path(args.state),
        action=args.action,
        caller_data_bytes=caller_data,
        caller_evidence=caller_evidence,
        sources=sources,
    )
    _emit(_decision_obj(result), json_mode)
    return 0 if result.decision.allowed else 1


def _cmd_block(args, json_mode: bool) -> int:
    gate = _runtime_gate()
    if gate:
        return gate
    if not args.apply:
        return _fail(
            "BLOCKED: block requires --apply to mutate version-2 state",
            USAGE_ERRORS,
        )
    bad = _require_v2_state(Path(args.state))
    if bad:
        return bad
    evidence = _parse_evidence_specs(args.evidence_file)
    result = engine.block_transaction(
        Path(args.state),
        blocker_class=args.blocker_class,
        reason=args.reason,
        evidence=evidence,
        sources=_sources(),
    )
    _emit(_decision_obj(result), json_mode)
    return 0 if result.decision.allowed else 1


def _cmd_resume(args, json_mode: bool) -> int:
    gate = _runtime_gate()
    if gate:
        return gate
    if not args.apply:
        return _fail(
            "BLOCKED: resume requires --apply to mutate version-2 state",
            USAGE_ERRORS,
        )
    bad = _require_v2_state(Path(args.state))
    if bad:
        return bad
    evidence = _parse_evidence_specs(args.evidence_file)
    result = engine.resume_transaction(
        Path(args.state),
        blocker_id=args.blocker_id,
        resolution_evidence=evidence,
        sources=_sources(),
    )
    _emit(_decision_obj(result), json_mode)
    return 0 if result.decision.allowed else 1


def _cmd_validate(args, json_mode: bool) -> int:
    bad = _require_v2_state(Path(args.state))
    if bad:
        return bad
    try:
        result = engine.validate_state_file(Path(args.state))
    except (model.StateValidationError, store.StoreError) as exc:
        _emit(
            {"valid": False, "error": str(exc), "state": str(Path(args.state))},
            json_mode,
        )
        return 1
    _emit(
        {
            "valid": True,
            "generation": result.generation,
            "state": str(result.state_path),
        },
        json_mode,
    )
    return 0


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="reviewctl",
        description=(
            "version-2 review control plane (experimental until cutover). "
            "The only mutation authority for schema_version-2 review state. "
            "(mixed)"
        ),
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help=(
            "self-check: parse arguments and exit 0 without touching files; "
            "subcommand arguments are validated at the parser level only, "
            "handlers are not run"
        ),
    )
    parser.add_argument("--json", action="store_true", help="emit one JSON object per command")
    sub = parser.add_subparsers(dest="command")

    d = sub.add_parser("doctor", help="report runtime detection, capability rows, and verdict")
    d.add_argument("--scratch-dir")
    d.add_argument("--repo")

    h = sub.add_parser("hooks", help="install or inspect the hooks pack")
    hsub = h.add_subparsers(dest="hooks_command", required=True)
    hi = hsub.add_parser("install", help="render the hooks pack under the scratch store")
    hi.add_argument("--scratch-dir", required=True)
    hs = hsub.add_parser("status", help="report hooks pack install state")
    hs.add_argument("--scratch-dir", required=True)

    p = sub.add_parser("init", help="create a generation-0 intake state")
    p.add_argument("--state", required=True)
    p.add_argument("--review-id", required=True)
    p.add_argument("--scratch-dir", required=True)
    p.add_argument("--apply", action="store_true")

    p = sub.add_parser("status", help="read current state summary")
    p.add_argument("--state", required=True)

    p = sub.add_parser("next", help="report the next lawful action and recipe")
    p.add_argument("--state", required=True)

    p = sub.add_parser("dispatch", help="register and launch a reviewer dispatch")
    p.add_argument("--state", required=True)
    p.add_argument("--action", required=True)
    p.add_argument("--apply", action="store_true")

    p = sub.add_parser(
        "enumerate",
        help=(
            "run transcript-witnessed authority/feedback acquisition into the "
            "scratch store (writes <scratch>/acquire/latest; review state is "
            "unchanged)"
        ),
    )
    p.add_argument("--state", required=True)
    p.add_argument("--repo", required=True)
    p.add_argument("--pr", required=True, type=int)

    for name, action in (("freeze", "freeze-review-input"), ("refresh", "refresh-review-input")):
        p = sub.add_parser(
            name,
            help=(
                f"convenience for complete --action {action} --acquired "
                "<scratch>/acquire/latest; refuses when no current "
                "enumeration exists"
            ),
        )
        p.add_argument("--state", required=True)
        p.add_argument("--repo", required=True)
        p.add_argument("--pr", required=True, type=int)
        p.add_argument("--apply", action="store_true")

    p = sub.add_parser("complete", help="record completion of a lawful action")
    p.add_argument("--state", required=True)
    p.add_argument("--action", required=True)
    p.add_argument("--data-file")
    p.add_argument("--acquired")
    p.add_argument("--evidence-file", action="append", default=[])
    p.add_argument("--apply", action="store_true")

    p = sub.add_parser("block", help="open a blocker on the review")
    p.add_argument("--state", required=True)
    p.add_argument("--class", dest="blocker_class", required=True)
    p.add_argument("--reason", required=True)
    p.add_argument("--evidence-file", action="append", default=[])
    p.add_argument("--apply", action="store_true")

    p = sub.add_parser("resume", help="resolve the active blocker")
    p.add_argument("--state", required=True)
    p.add_argument("--blocker-id", required=True)
    p.add_argument("--evidence-file", action="append", default=[])
    p.add_argument("--apply", action="store_true")

    p = sub.add_parser("validate", help="validate a version-2 state file")
    p.add_argument("--state", required=True)

    for parser_obj in (*sub.choices.values(), hi, hs):
        parser_obj.add_argument(
            "--json",
            action="store_true",
            default=argparse.SUPPRESS,
            help="emit one JSON object",
        )

    return parser


_HANDLERS = {
    "doctor": _cmd_doctor,
    "hooks": _cmd_hooks,
    "init": _cmd_init,
    "status": _cmd_status,
    "next": _cmd_next,
    "dispatch": _cmd_dispatch,
    "enumerate": _cmd_enumerate,
    "freeze": _cmd_freeze,
    "refresh": _cmd_refresh,
    "complete": _cmd_complete,
    "block": _cmd_block,
    "resume": _cmd_resume,
    "validate": _cmd_validate,
}


def main(argv=None) -> int:
    argv = list(sys.argv[1:] if argv is None else argv)
    parser = _build_parser()
    args = parser.parse_args(argv)
    json_mode = bool(getattr(args, "json", False))
    if args.check:
        return 0
    if args.command is None:
        parser.print_help()
        return USAGE_ERRORS
    handler = _HANDLERS[args.command]
    try:
        return handler(args, json_mode)
    except model.StateValidationError as exc:
        return _fail(f"{exc.code}: {exc}")
    except acquisition.AcquisitionError as exc:
        return _fail(f"{exc.blocker_class}: {exc}")
    except witness_log.WitnessLogError as exc:
        return _fail(f"witness-error: {exc}")
    except policy.WitnessVerificationError as exc:
        return _fail(f"witness-error: {exc}")
    except store.StoreError as exc:
        return _fail(str(exc))
    except OSError as exc:
        return _fail(f"io-error: {exc}")
    except Exception as exc:  # noqa: BLE001 - last line of defense, never a traceback
        return _fail(f"unexpected: {exc}")


if __name__ == "__main__":
    sys.exit(main())
