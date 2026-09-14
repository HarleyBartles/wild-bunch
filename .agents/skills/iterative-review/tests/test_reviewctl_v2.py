#!/usr/bin/env python3
"""Contract tests for reviewctl.py and the review_core engine.

Two surfaces are covered:

- The public CLI: init/status/next/dispatch/complete/block/resume/validate,
  plus doctor/--help/--check/--json, the Devin-Desktop runtime gate, and the
  version-1/version-2 boundary (legacy scripts must never touch v2 state).
- The engine transaction layer: one locked transaction per mutation,
  generation bookkeeping, durable intent before side effects, witness-bound
  completion, and fail-closed behavior when no source is wired.
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
SKILL_DIR = TESTS_DIR.parent
SCRIPTS = SKILL_DIR / "scripts"
sys.path.insert(0, str(SCRIPTS))
sys.path.insert(0, str(TESTS_DIR))

from review_core import engine, model, policy, store  # noqa: E402
import review_v2_helpers as helpers  # noqa: E402

REVIEWCTL = SCRIPTS / "reviewctl.py"
NEXT_NODE = SCRIPTS / "next_node.py"
COMPILE_METRICS = SCRIPTS / "compile_metrics.py"
RESOLVED_LEDGER = SCRIPTS / "resolved_ledger.py"


# ---------------------------------------------------------------------------
# CLI plumbing


def _ctl(*args, runtime="devin-desktop", cwd=None):
    env = dict(os.environ)
    if runtime is None:
        env.pop(engine.RUNTIME_ENV_VAR, None)
    else:
        env[engine.RUNTIME_ENV_VAR] = runtime
    return subprocess.run(
        ["py", "-3", str(REVIEWCTL), *args],
        capture_output=True,
        text=True,
        env=env,
        cwd=cwd,
    )


def _run(script: Path, *args, env_extra=None):
    env = dict(os.environ)
    if env_extra:
        env.update(env_extra)
    return subprocess.run(
        ["py", "-3", str(script), *args],
        capture_output=True,
        text=True,
        env=env,
    )


def _init_state(tmp_path: Path, review_id: str = "review-cli") -> Path:
    state = tmp_path / "review-state.json"
    result = _ctl(
        "init",
        "--state",
        str(state),
        "--review-id",
        review_id,
        "--scratch-dir",
        str(tmp_path),
        "--apply",
    )
    assert result.returncode == 0, result.stderr
    assert state.is_file()
    return state


def _v1_state(tmp_path: Path) -> Path:
    p = tmp_path / "v1-state.json"
    p.write_text(
        json.dumps(
            {
                "current_node": "setup",
                "round": 1,
                "max_fix_rounds": 4,
                "pr": {"head_sha": "abc123"},
                "scratch_dir": str(tmp_path),
            }
        ),
        encoding="utf-8",
    )
    return p


# ---------------------------------------------------------------------------
# Engine test doubles
#
# Each double plays the role later plans wire to real hook-transcript, gh, and
# sandbox sources. They hold no state authority: they emit bytes, evidence
# files, and witness records, and the engine re-verifies everything.


def _evidence_id_for(content: bytes, kind: str, snap: dict) -> str:
    """The evidence id register_evidence will derive for this content."""
    cid = "sha256:" + model.sha256_hex(content)
    binding = {
        "evidence_id": "",
        "content_id": cid,
        "kind": kind,
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    return "evidence:" + model.sha256_json(model.evidence_binding_subject(binding))


class _DoubleBase:
    def __init__(self, state_path: Path, registry: dict, workdir: Path, policies):
        self.state_path = Path(state_path)
        self.registry = registry
        self.workdir = Path(workdir)
        self.policies = policies
        self.serial = 0

    def _load(self) -> dict:
        return store.load_state(self.state_path)

    def _next(self) -> int:
        self.serial += 1
        return self.serial

    def _file(self, name: str, payload) -> Path:
        path = self.workdir / name
        data = payload if isinstance(payload, bytes) else model.canonical_json(payload)
        path.write_bytes(data)
        return path

    def _witness(
        self,
        st: dict,
        *,
        kind,
        subject,
        tool_use_id=None,
        agent_id=None,
        locator=None,
        snap=None,
    ) -> dict:
        snap = st["snapshot"] if snap is None else snap
        if locator is None:
            prefix = "gh:" if kind.startswith("remote-") else "hook-transcript:"
            locator = f"{prefix}{st['review_id']}/{kind}/{len(st['witness_records'])}"
        rec = {
            "witness_id": "",
            "kind": kind,
            "subject_sha256": model.sha256_json(subject),
            "tool_use_id": tool_use_id,
            "agent_id": agent_id,
            "transcript_range": None,
            "record_positions": [len(st["witness_records"])],
            "chain_head_at_record": model.sha256_hex(f"chain:{locator}".encode()),
            "source_locator": locator,
            "snapshot_epoch": snap["epoch"],
            "snapshot_fingerprint": snap["fingerprint"],
        }
        rec["witness_id"] = model.derived_id("witness", snap["epoch"], model.witness_record_subject(rec))
        self.registry[locator] = model.canonical_json(rec)
        return rec


class _AuthorityDouble(_DoubleBase):
    """Produces the freeze-review-input payload as a trusted source would:
    manifest + authority content as evidence files, the discovery witness in
    the envelope, and the candidate snapshot in data."""

    def acquire(self, *, action: str, current_snapshot: dict | None):
        st = self._load()
        serial = self._next()
        epoch = 1 if current_snapshot is None else current_snapshot["epoch"] + 1
        auth_obj = {"path": "AGENTS.md", "note": "repo law", "epoch": epoch}
        auth_path = self._file(f"auth-{serial}.json", auth_obj)
        auth_bytes = auth_path.read_bytes()
        auth_sha = model.sha256_hex(auth_bytes)
        empty_feedback_sha = model.sha256_hex(model.canonical_json([]))
        pol = self.policies
        auth_entry = {
            "authority_id": "auth-agents",
            "kind": "repo-law",
            "locator": "AGENTS.md",
            "availability": "loaded",
            "sha256": auth_sha,
            "failure_class": None,
            "failure_sha256": None,
        }
        manifest_payload = model.manifest_payload(
            repository_id="o/r",
            pr_number=7,
            pr_url="https://github.com/o/r/pull/7",
            authority_discovery_policy_id="adp",
            authority_discovery_policy_version="1",
            authority_discovery_policy_sha256=model.sha256_hex(b"adp"),
            authorities=(auth_entry,),
            feedback_history_policy_id="fhp",
            feedback_history_policy_version="1",
            feedback_history_policy_sha256=model.sha256_hex(b"fhp"),
            feedback_history_sha256=empty_feedback_sha,
            local_check_policy_id=pol.local_checks.source_id,
            local_check_policy_version=pol.local_checks.source_version,
            local_check_policy_sha256=pol.local_checks.sha256,
            required_check_policy_sha256=model.sha256_hex(b"required-checks"),
            review_assignment_policy_id=pol.review_assignments.source_id,
            review_assignment_policy_version=pol.review_assignments.source_version,
            review_assignment_policy_sha256=pol.review_assignments.sha256,
            command_execution_policy_id=pol.command_execution.source_id,
            command_execution_policy_version=pol.command_execution.source_version,
            command_execution_policy_sha256=pol.command_execution.sha256,
            evidence_ingestion_policy_id="eip",
            evidence_ingestion_policy_version="1",
            evidence_ingestion_policy_sha256=model.sha256_hex(b"eip"),
            hypothesis_derivation_policy_id=pol.hypotheses.source_id,
            hypothesis_derivation_policy_version=pol.hypotheses.source_version,
            hypothesis_derivation_policy_sha256=pol.hypotheses.sha256,
            unresolved_feedback_sha256=empty_feedback_sha,
        )
        manifest_id = model.authority_manifest_id(manifest_payload)
        snap = {
            "epoch": epoch,
            "repository_id": "o/r",
            "pr_number": 7,
            "pr_url": "https://github.com/o/r/pull/7",
            "git_object_format": "sha1",
            "base_sha": "a" * 40,
            "head_sha": "b" * 40,
            "tree_sha": "c" * 40,
            "diff_sha256": "d" * 64,
            "pr_metadata_sha256": "e" * 64,
            "authority_manifest_sha256": manifest_id,
            "authority_discovery_policy_id": "adp",
            "authority_discovery_policy_version": "1",
            "authority_discovery_policy_sha256": model.sha256_hex(b"adp"),
            "witness_policy_sha256": pol.witness_verifier.policy.sha256,
            "feedback_history_policy_id": "fhp",
            "feedback_history_policy_version": "1",
            "feedback_history_policy_sha256": model.sha256_hex(b"fhp"),
            "feedback_history_sha256": empty_feedback_sha,
            "local_check_policy_id": pol.local_checks.source_id,
            "local_check_policy_version": pol.local_checks.source_version,
            "local_check_policy_sha256": pol.local_checks.sha256,
            "required_check_policy_sha256": model.sha256_hex(b"required-checks"),
            "review_assignment_policy_id": pol.review_assignments.source_id,
            "review_assignment_policy_version": pol.review_assignments.source_version,
            "review_assignment_policy_sha256": pol.review_assignments.sha256,
            "command_execution_policy_id": pol.command_execution.source_id,
            "command_execution_policy_version": pol.command_execution.source_version,
            "command_execution_policy_sha256": pol.command_execution.sha256,
            "evidence_ingestion_policy_id": "eip",
            "evidence_ingestion_policy_version": "1",
            "evidence_ingestion_policy_sha256": model.sha256_hex(b"eip"),
            "hypothesis_derivation_policy_id": pol.hypotheses.source_id,
            "hypothesis_derivation_policy_version": pol.hypotheses.source_version,
            "hypothesis_derivation_policy_sha256": pol.hypotheses.sha256,
            "unresolved_feedback_sha256": empty_feedback_sha,
        }
        snap["fingerprint"] = model.snapshot_fingerprint(snap)
        manifest_path = self._file(f"manifest-{serial}.json", manifest_payload)
        discovery = self._witness(
            st,
            kind="authority-discovery",
            subject=model.authority_discovery_subject(snap, manifest_payload),
            snap=snap,
        )
        wrapper = {
            "authority_manifest_id": manifest_id,
            "payload_evidence_id": "@manifest",
            "discovery_witness_id": discovery["witness_id"],
            "snapshot_epoch": snap["epoch"],
            "snapshot_fingerprint": snap["fingerprint"],
        }
        auth_rec = {
            "authority_id": "auth-agents",
            "kind": "repo-law",
            "locator": "AGENTS.md",
            "availability": "loaded",
            "sha256": auth_sha,
            "evidence_id": "@auth",
            "snapshot_epoch": snap["epoch"],
            "snapshot_fingerprint": snap["fingerprint"],
        }
        data = {
            "snapshot": snap,
            "authority_manifest": wrapper,
            "authorities": [auth_rec],
            "findings": [],
        }
        envelope = {"data": data, "witnesses": [discovery]}
        return engine.TrustedActionPayload(
            model.canonical_json(envelope),
            (
                engine.EvidenceSource("manifest", "authority-manifest-payload", manifest_path),
                engine.EvidenceSource("auth", "authority", auth_path),
            ),
        )


class _DispatchDouble(_DoubleBase):
    """resolve_profile -> launch -> collect for dispatch-backed actions.

    ``data_builders`` maps a required role to ``fn(double, st, dispatch, att)``
    returning ``(data_dict, extra_evidence_sources)``; roles without a builder
    get the generic ``{"attestations": [att], "findings": []}`` payload.
    """

    def __init__(self, *args, assignments=None, data_builders=None, **kw):
        super().__init__(*args, **kw)
        self.assignments = assignments or {}
        self.data_builders = data_builders or {}
        self._att_paths = {}

    def resolve_profile(self, *, action: str, recipe) -> engine.TrustedActionPayload:
        st = self._load()
        snap = st["snapshot"]
        serial = self._next()
        role = recipe.required_role
        profile = f"{role}-profile"
        tier = recipe.minimum_capability_tier or "strong"
        reasoning = recipe.minimum_reasoning_floor or "high"
        route_path = self._file(
            f"route-{serial}.json",
            {"route": profile, "role": role, "serial": serial},
        )
        ev_id = _evidence_id_for(route_path.read_bytes(), "route-selection", snap)
        rs_resolved = {
            "route_selection_id": "",
            "observed_at": "2026-09-12T00:00:00Z",
            "inventory_evidence_sha256": model.sha256_hex(f"inv:{serial}".encode()),
            "budget_contract_sha256": model.sha256_hex(f"budget:{serial}".encode()),
            "profile_authority_sha256": model.sha256_hex(f"authority:{profile}".encode()),
            "resolved_route_token_sha256": model.sha256_hex(f"token:{profile}:{serial}".encode()),
            "required_capability_tier": tier,
            "required_role": role,
            "qualified_roles": sorted({role} | set(self.assignments.get(role + ":qualified", ()))),
            "profile": profile,
            "profile_sha256": model.sha256_hex(f"profile:{profile}".encode()),
            "selection_mode": "profile",
            "selected_model": "profile-defined",
            "selected_reasoning": reasoning,
            "selected_context_mode": "fresh",
            "parent_model": None,
            "parent_reasoning": None,
            "qualification_source": "profile-file",
            "rationale": "test route",
            "evidence_id": ev_id,
            "snapshot_epoch": snap["epoch"],
            "snapshot_fingerprint": snap["fingerprint"],
        }
        rsid = model.derived_id("route", snap["epoch"], model.route_selection_subject(rs_resolved))
        pw = self._witness(
            st,
            kind="profile-resolution",
            subject=model.profile_resolution_subject({**rs_resolved, "route_selection_id": rsid}),
            locator=f"hook-transcript:{st['review_id']}/profile-resolution/{rsid}",
        )
        dispatch = {
            "dispatch_id": "",
            "route_selection_id": rsid,
            "profile_resolution_witness_id": pw["witness_id"],
            "assignment_ids": sorted(self.assignments.get(role, ())),
            "context_evidence_ids": [],
            "instruction_manifest_sha256": model.sha256_hex(f"instr:{serial}".encode()),
            "data_manifest_sha256": model.sha256_hex(f"data:{serial}".encode()),
            "tool_confinement_policy_sha256": model.sha256_hex(f"tools:{serial}".encode()),
            "context_package_sha256": model.sha256_hex(f"ctx:{serial}".encode()),
            "hazard_framing_sha256": model.sha256_hex(f"hazard:{serial}".encode()),
            "required_tool_classes": ["git-read", "github-read", "repo-read"],
        }
        # The persisted route_selection references the evidence file by alias;
        # registration produces the id the subject was derived over.
        rs_payload = dict(rs_resolved)
        rs_payload["evidence_id"] = "@route"
        profiles = getattr(self.policies, "available_profiles", None)
        if profiles is not None:
            self.policies.available_profiles = tuple(sorted(set(profiles) | {profile}))
        envelope = {
            "data": {"route_selection": rs_payload, "dispatch": dispatch},
            "witnesses": [pw],
        }
        return engine.TrustedActionPayload(
            model.canonical_json(envelope),
            (engine.EvidenceSource("route", "route-selection", route_path),),
        )

    def launch(self, *, dispatch: dict) -> engine.TrustedActionPayload:
        st = self._load()
        d = dispatch
        rs = st["route_selections"][d["route_selection_id"]]
        serial = self._next()
        tool_use = f"toolu-launch-{serial}"
        agent = f"agent-{serial}"
        rec = self._witness(
            st,
            kind="review-launch",
            subject=model.review_launch_subject(
                d,
                policy.dispatch_context_manifest(d),
                tool_use_id=tool_use,
                task_bytes_sha256=model.sha256_hex(policy.dispatch_task_bytes(d, rs)),
                profile_name=rs["profile"],
            ),
            tool_use_id=tool_use,
            agent_id=agent,
        )
        return engine.TrustedActionPayload(model.canonical_json({"data": {}, "witnesses": [rec]}))

    def collect(self, *, dispatch: dict) -> engine.TrustedActionPayload:
        st = self._load()
        snap = st["snapshot"]
        d = dispatch
        serial = self._next()
        att_bytes = model.canonical_json({"dispatch_id": d["dispatch_id"], "verdict": "clean", "serial": serial})
        att_path = self._file(f"att-{serial}.json", att_bytes)
        self._att_paths[d["dispatch_id"]] = att_path
        ts = model.sha256_hex(f"transcript:{serial}".encode())
        comp = self._witness(
            st,
            kind="review-completion",
            subject=model.review_completion_subject(
                att_bytes,
                tool_transcript_sha256=ts,
                agent_id=d["agent_id"],
            ),
            agent_id=d["agent_id"],
        )
        att = {
            "attestation_id": "",
            "dispatch_id": d["dispatch_id"],
            "assignment_ids": sorted(d["assignment_ids"]),
            "verdict": "clean",
            "finding_ids": [],
            "uncertainties": [],
            "tool_transcript_sha256": ts,
            "evidence_id": "@att",
            "completion_witness_id": comp["witness_id"],
            "audit_result": "clean",
            "snapshot_epoch": snap["epoch"],
            "snapshot_fingerprint": snap["fingerprint"],
        }
        role = st["route_selections"][d["route_selection_id"]]["required_role"]
        builder = self.data_builders.get(role)
        if builder is None:
            data = {"attestations": [att], "findings": []}
            extra = ()
        else:
            data, extra = builder(self, st, d, att)
        envelope = {"data": data, "witnesses": [comp], "transcript_sha256": ts}
        return engine.TrustedActionPayload(
            model.canonical_json(envelope),
            (engine.EvidenceSource("att", "review-attestation", att_path), *extra),
        )


def _map_data_builder(dd: _DispatchDouble, st: dict, dispatch: dict, att: dict):
    snap = st["snapshot"]
    role = st["route_selections"][dispatch["route_selection_id"]]["required_role"]
    entries = [
        {
            "surface": "src/foo.py",
            "category": "behavioral-correctness",
            "hazards": ["h-sem"],
            "consequences": ["security"],
        }
    ]
    map_path = dd._file(f"map-{dd.serial}.json", {"role": role, "entries": entries})
    return (
        {
            "impact_map": {
                "impact_map_id": "",
                "role": role,
                "entries": entries,
                "evidence_id": "@map",
                "snapshot_epoch": snap["epoch"],
                "snapshot_fingerprint": snap["fingerprint"],
            },
            "attestation": att,
            "findings": [],
        },
        (engine.EvidenceSource("map", "impact-map", map_path),),
    )


class _CommandRunnerDouble(_DoubleBase):
    """Executes command intents and returns witnessed check results."""

    # Mirrors the walk's convention: item-preflight runs under run-preflight,
    # item-targeted runs under run-fix-verification.
    _KIND_FOR_ACTION = {
        "run-preflight": ("preflight", "item-preflight"),
        "run-fix-verification": ("targeted", "item-targeted"),
    }

    def run(self, *, action: str, command_intent: dict) -> engine.TrustedActionPayload:
        st = self._load()
        snap = st["snapshot"]
        kind, item_id = self._KIND_FOR_ACTION[action]
        checks = []
        witnesses = []
        sources = []
        for i, intent in enumerate(command_intent["commands"]):
            if intent["policy_item_id"] != item_id:
                continue
            serial = self._next()
            item = next(it for it in self.policies.local_checks.items if it.policy_item_id == intent["policy_item_id"])
            out_path = self._file(
                f"check-{serial}.json",
                {"output": f"{action}:{item.policy_item_id}", "serial": serial},
            )
            out_ev = _evidence_id_for(out_path.read_bytes(), "check-output", snap)
            rec = {
                "check_id": f"check-{action}-{serial}",
                "kind": kind,
                "locus": "local",
                "policy_item_id": item.policy_item_id,
                "name": item.name,
                "command": list(item.command),
                "working_directory": item.working_directory,
                "local_check_policy_sha256": self.policies.local_checks.sha256,
                "command_execution_policy_sha256": self.policies.command_execution.sha256,
                "required": item.required,
                "conclusion": "success",
                "head_sha": snap["head_sha"],
                "source_materialization_sha256": model.sha256_hex(f"mat:{serial}".encode()),
                "toolchain_sha256": model.sha256_hex(f"toolchain:{serial}".encode()),
                "environment_sha256": model.sha256_hex(f"env:{serial}".encode()),
                "sandbox_id": f"sandbox-{serial}",
                "pre_source_sha256": model.sha256_hex(snap["tree_sha"].encode()),
                "post_source_sha256": model.sha256_hex(snap["tree_sha"].encode()),
                "process_tree_terminated": True,
                "evidence_id": out_ev,
                "execution_witness_id": "",
                "snapshot_epoch": snap["epoch"],
                "snapshot_fingerprint": snap["fingerprint"],
            }
            subject = model.command_execution_subject(intent, policy.command_result_subject(rec))
            w = self._witness(
                st,
                kind="command-execution",
                subject=subject,
                tool_use_id=f"toolu-exec-{serial}",
            )
            rec["execution_witness_id"] = w["witness_id"]
            payload_rec = dict(rec)
            payload_rec["evidence_id"] = f"@out{i}"
            checks.append(payload_rec)
            witnesses.append(w)
            sources.append(engine.EvidenceSource(f"out{i}", "check-output", out_path))
        return engine.TrustedActionPayload(
            model.canonical_json({"data": {"checks": checks}, "witnesses": witnesses}),
            tuple(sources),
        )


class _TransitionDouble(_DoubleBase):
    """Draft->ready lifecycle: observe, then apply/confirm with a witness."""

    def observe_lifecycle(self, *, intent: dict) -> engine.TrustedActionPayload:
        return engine.TrustedActionPayload(
            model.canonical_json({"data": {"prior_lifecycle_state": "draft"}, "witnesses": []})
        )

    def apply_or_confirm_ready(self, *, intent: dict) -> engine.TrustedActionPayload:
        st = self._load()
        ready = st["ready_transition"]
        serial = self._next()
        tool_use = f"toolu-ready-{serial}"
        rec = self._witness(
            st,
            kind="remote-transition",
            subject=model.remote_transition_subject(
                ready,
                tool_use_id=tool_use,
                prior_lifecycle_state="draft",
                result_lifecycle_state=ready["expected_lifecycle_state"],
            ),
            tool_use_id=tool_use,
            locator=f"gh:{st['review_id']}/remote-transition/{serial}",
        )
        return engine.TrustedActionPayload(
            model.canonical_json({"data": {"observed_prior_lifecycle": "draft"}, "witnesses": [rec]})
        )


class _ObserverDouble(_DoubleBase):
    """Remote observation for run-remote-ci: witnessed observation payload plus
    the hosted check record bound to it."""

    def observe(self, *, action: str, snapshot: dict) -> engine.TrustedActionPayload:
        st = self._load()
        snap = st["snapshot"]
        serial = self._next()
        manifest_id = st["authority_manifest"]["authority_manifest_id"]
        empty_feedback_sha = model.sha256_hex(model.canonical_json([]))
        observation = {
            "repository_id": "o/r",
            "pr_number": 7,
            "pr_url": "https://github.com/o/r/pull/7",
            "head_sha": snap["head_sha"],
            "lifecycle_state": "ready",
            "authority_manifest_sha256": manifest_id,
            "unresolved_feedback_sha256": empty_feedback_sha,
            "check_runs": [
                {
                    "check_run_id": f"cr-{serial}",
                    "name": "ci",
                    "app_id": "github-actions",
                    "status": "completed",
                    "conclusion": "success",
                    "head_sha": snap["head_sha"],
                }
            ],
            "workflow_runs": [
                {
                    "workflow_run_id": f"wr-{serial}",
                    "name": "ci",
                    "status": "completed",
                    "conclusion": "success",
                    "head_sha": snap["head_sha"],
                    "run_attempt": serial,
                    "run_number": serial,
                    "event": "pull_request",
                }
            ],
            "observed_at": "2026-09-12T00:00:00+00:00",
        }
        obs_path = self._file(f"obs-{serial}.json", observation)
        w = self._witness(
            st,
            kind="remote-observation",
            subject=model.remote_observation_subject(observation),
            locator=f"gh:{st['review_id']}/remote-observation/{serial}",
        )
        hosted = {
            "check_id": f"check-remote-ci-{serial}",
            "kind": "remote-ci",
            "locus": "hosted",
            "policy_item_id": "ci",
            "name": "ci",
            "required": True,
            "conclusion": "success",
            "head_sha": snap["head_sha"],
            "app_id": "github-actions",
            "workflow_id": "wf-1",
            "workflow_path": ".github/workflows/ci.yml",
            "workflow_definition_ref": "refs/heads/x",
            "workflow_definition_sha": "d" * 40,
            "event": "pull_request",
            "trigger_subject": "pr",
            "policy_inputs_sha256": "0" * 64,
            "configuration_sha256": "0" * 64,
            "check_run_id": f"cr-{serial}",
            "workflow_run_id": f"wr-{serial}",
            "run_attempt": serial,
            "evidence_id": "@obs",
            "remote_observation_witness_id": w["witness_id"],
        }
        return engine.TrustedActionPayload(
            model.canonical_json(
                {
                    "data": {"remote_observation": observation, "checks": [hosted]},
                    "witnesses": [w],
                }
            ),
            (engine.EvidenceSource("obs", "remote-observation", obs_path),),
        )


def _test_ingestion() -> store.EvidenceIngestionPolicy:
    return store.EvidenceIngestionPolicy(
        source_id="test-ingestion",
        source_version="1",
        sha256="0" * 64,
        per_kind_max_bytes={kind: 1 << 20 for kind in model.EVIDENCE_KINDS},
        transaction_max_bytes=8 << 20,
        review_max_bytes=64 << 20,
        windows_allowed_trustee_sids=(),
        posix_directory_mode=0o700,
        posix_file_mode=0o600,
    )


def _persist_state(tmp_path: Path, state: dict) -> Path:
    path = tmp_path / "review-state.json"
    path.write_bytes(model.canonical_json(state) + b"\n")
    return path


def _walk_sources(w: helpers._Walk, state_path: Path, tmp_path: Path, **kw) -> engine.WitnessSources:
    return engine.WitnessSources(
        policies=w.policies,
        evidence_ingestion_policy=_test_ingestion(),
        **kw,
    )


# ---------------------------------------------------------------------------
# CLI contract


class TestCliContract:
    def test_help_exits_0_and_labels_experimental(self):
        r = _ctl("--help")
        assert r.returncode == 0
        assert "experimental" in r.stdout
        for verb in (
            "init",
            "status",
            "next",
            "dispatch",
            "complete",
            "block",
            "resume",
            "validate",
        ):
            assert verb in r.stdout

    def test_check_exits_0_and_creates_no_files(self, tmp_path):
        r = _ctl("--check", cwd=tmp_path)
        assert r.returncode == 0
        assert list(tmp_path.iterdir()) == []

    def test_check_with_command_never_mutates(self, tmp_path):
        state = tmp_path / "s.json"
        r = _ctl(
            "--check",
            "init",
            "--state",
            str(state),
            "--review-id",
            "x",
            "--scratch-dir",
            str(tmp_path),
            "--apply",
        )
        assert r.returncode == 0
        assert not state.exists()

    def test_no_command_is_usage_error(self):
        r = _ctl()
        assert r.returncode == 2

    def test_unknown_command_is_usage_error(self):
        r = _ctl("present", "--state", "x")
        assert r.returncode == 2

    def test_init_check_mode_writes_nothing(self, tmp_path):
        state = tmp_path / "s.json"
        r = _ctl(
            "init",
            "--state",
            str(state),
            "--review-id",
            "r",
            "--scratch-dir",
            str(tmp_path),
        )
        assert r.returncode == 0, r.stderr
        assert not state.exists()

    def test_init_apply_creates_validated_intake_state(self, tmp_path):
        state = _init_state(tmp_path)
        loaded = store.load_state(state)
        assert loaded["generation"] == 0
        assert loaded["stage"] == "intake"
        assert loaded["snapshot"] is None
        assert loaded["schema_version"] == 2
        model.validate_state(loaded)

    def test_init_apply_refuses_existing_state(self, tmp_path):
        state = _init_state(tmp_path)
        again = _ctl(
            "init",
            "--state",
            str(state),
            "--review-id",
            "r2",
            "--scratch-dir",
            str(tmp_path),
            "--apply",
        )
        assert again.returncode == 1
        assert "state-exists" in again.stderr

    def test_status_next_validate_roundtrip(self, tmp_path):
        state = _init_state(tmp_path)
        before = state.read_bytes()
        for verb in ("status", "next", "validate"):
            r = _ctl(verb, "--state", str(state))
            assert r.returncode == 0, (verb, r.stderr)
        assert state.read_bytes() == before  # read-only verbs never mutate
        nxt = _ctl("next", "--state", str(state), "--json")
        payload = json.loads(nxt.stdout)
        assert payload["action"] == "freeze-review-input"

    def test_validate_rejects_v1_state(self, tmp_path):
        v1 = _v1_state(tmp_path)
        r = _ctl("validate", "--state", str(v1))
        assert r.returncode == 1
        assert "version-1" in r.stderr

    def test_validate_missing_state(self, tmp_path):
        r = _ctl("validate", "--state", str(tmp_path / "nope.json"))
        assert r.returncode == 1
        assert "state-missing" in r.stderr

    @pytest.mark.parametrize(
        "verb,extra",
        [
            ("dispatch", ["--action", "run-fast-review"]),
            ("complete", ["--action", "seal-green"]),
            ("block", ["--class", "tool-blocked", "--reason", "x"]),
            ("resume", ["--blocker-id", "b"]),
        ],
    )
    def test_mutations_require_apply(self, tmp_path, verb, extra):
        state = _init_state(tmp_path)
        r = _ctl(verb, "--state", str(state), *extra)
        assert r.returncode == 2
        assert "--apply" in r.stderr

    def test_json_emits_single_object(self, tmp_path):
        state = _init_state(tmp_path)
        r = _ctl("status", "--state", str(state), "--json")
        assert r.returncode == 0
        payload = json.loads(r.stdout)
        assert payload["schema_version"] == 2

    def test_doctor_reports_runtime(self):
        r = _ctl("doctor")
        assert r.returncode == 0
        assert "devin-desktop" in r.stdout

    def test_complete_rejects_caller_verdict_on_seal(self, tmp_path):
        state = _init_state(tmp_path)
        verdict = tmp_path / "verdict.json"
        verdict.write_text('{"green": true}', encoding="utf-8")
        r = _ctl(
            "complete",
            "--state",
            str(state),
            "--action",
            "seal-green",
            "--data-file",
            str(verdict),
            "--apply",
        )
        assert r.returncode == 1
        assert "caller-provenance" in r.stderr

    def test_complete_rejects_caller_evidence_on_source_action(self, tmp_path):
        state = _init_state(tmp_path)
        obs = tmp_path / "obs.json"
        obs.write_text('{"lifecycle_state": "ready"}', encoding="utf-8")
        r = _ctl(
            "complete",
            "--state",
            str(state),
            "--action",
            "run-remote-ci",
            "--evidence-file",
            f"obs=remote-observation={obs}",
            "--apply",
        )
        assert r.returncode == 1
        assert "caller-provenance" in r.stderr

    def test_dispatch_blocks_missing_witness_source(self, tmp_path):
        state = _init_state(tmp_path)
        before = state.read_bytes()
        r = _ctl(
            "dispatch",
            "--state",
            str(state),
            "--action",
            "run-fast-review",
            "--apply",
        )
        assert r.returncode == 1
        assert "missing-witness-source" in r.stdout + r.stderr
        assert state.read_bytes() == before

    def test_complete_blocks_missing_dispatch_source(self, tmp_path):
        state = _init_state(tmp_path)
        before = state.read_bytes()
        r = _ctl(
            "complete",
            "--state",
            str(state),
            "--action",
            "run-fast-review",
            "--apply",
        )
        assert r.returncode == 1
        assert "missing-witness-source" in r.stdout + r.stderr
        assert state.read_bytes() == before

    def test_complete_blocks_missing_command_runner(self, tmp_path):
        state = _init_state(tmp_path)
        before = state.read_bytes()
        r = _ctl(
            "complete",
            "--state",
            str(state),
            "--action",
            "run-preflight",
            "--apply",
        )
        assert r.returncode == 1
        assert "missing-witness-source" in r.stdout + r.stderr
        assert state.read_bytes() == before

    def test_complete_blocks_missing_authority_source(self, tmp_path):
        state = _init_state(tmp_path)
        before = state.read_bytes()
        r = _ctl(
            "complete",
            "--state",
            str(state),
            "--action",
            "freeze-review-input",
            "--apply",
        )
        assert r.returncode == 1
        assert "missing-witness-source" in r.stdout + r.stderr
        assert state.read_bytes() == before

    def test_complete_blocks_missing_remote_transition(self, tmp_path):
        state = _init_state(tmp_path)
        before = state.read_bytes()
        r = _ctl(
            "complete",
            "--state",
            str(state),
            "--action",
            "mark-ready-for-ci",
            "--apply",
        )
        assert r.returncode == 1
        # At intake the action is unlawful; either way no mutation occurs.
        assert state.read_bytes() == before

    def test_block_resume_roundtrip_via_cli(self, tmp_path):
        # Blockers require an installed snapshot, so drive a real freeze first.
        _walk_to(tmp_path, stop="freeze")
        state = tmp_path / "review-state.json"
        r = _ctl(
            "block",
            "--state",
            str(state),
            "--class",
            "tool-blocked",
            "--reason",
            "sandbox unavailable",
            "--apply",
        )
        assert r.returncode == 0, r.stderr
        loaded = store.load_state(state)
        assert loaded["status"] == "blocked"
        blocker_id = next(iter(loaded["blockers"]))
        # Post-freeze resume must present current resolution evidence.
        res = tmp_path / "resolution.txt"
        res.write_text("restored", encoding="utf-8")
        r = _ctl(
            "resume",
            "--state",
            str(state),
            "--blocker-id",
            blocker_id,
            "--evidence-file",
            f"resolution=check-output={res}",
            "--apply",
        )
        assert r.returncode == 0, r.stderr
        loaded = store.load_state(state)
        assert loaded["status"] == "active"
        assert loaded["blockers"][blocker_id]["active"] is False

    def test_block_at_intake_requires_snapshot_via_cli(self, tmp_path):
        state = _init_state(tmp_path)
        before = state.read_bytes()
        r = _ctl(
            "block",
            "--state",
            str(state),
            "--class",
            "tool-blocked",
            "--reason",
            "sandbox unavailable",
            "--apply",
        )
        assert r.returncode == 1
        assert "missing-snapshot" in r.stderr
        assert state.read_bytes() == before

    def test_unsupported_runtime_is_inert(self, tmp_path):
        state = tmp_path / "s.json"
        r = _ctl(
            "init",
            "--state",
            str(state),
            "--review-id",
            "r",
            "--scratch-dir",
            str(tmp_path),
            "--apply",
            runtime="codex-cli",
        )
        assert r.returncode == 1
        assert "unsupported-runtime" in r.stderr
        assert not state.exists()
        r = _ctl("doctor", runtime="codex-cli")
        assert r.returncode == 1
        assert "inert" in r.stdout

    def test_unsupported_runtime_blocks_late_mutations(self, tmp_path):
        state = _init_state(tmp_path)
        before = state.read_bytes()
        r = _ctl(
            "block",
            "--state",
            str(state),
            "--class",
            "tool-blocked",
            "--reason",
            "x",
            "--apply",
            runtime="openai-compatible",
        )
        assert r.returncode == 1
        assert "unsupported-runtime" in r.stderr
        assert state.read_bytes() == before

    def test_duplicate_keys_in_data_file_rejected(self, tmp_path):
        state = _init_state(tmp_path)
        bad = tmp_path / "dup.json"
        bad.write_text('{"obligations": [], "obligations": []}', encoding="utf-8")
        r = _ctl(
            "complete",
            "--state",
            str(state),
            "--action",
            "plan-coverage",
            "--data-file",
            str(bad),
            "--apply",
        )
        assert r.returncode == 1
        assert "invalid-json" in r.stderr or "duplicate" in r.stderr

    def test_failed_transaction_leaves_state_unchanged(self, tmp_path):
        state = _init_state(tmp_path)
        before = state.read_bytes()
        bad = tmp_path / "bad.json"
        bad.write_text('{"obligations": "not-a-list"}', encoding="utf-8")
        r = _ctl(
            "complete",
            "--state",
            str(state),
            "--action",
            "plan-coverage",
            "--data-file",
            str(bad),
            "--apply",
        )
        assert r.returncode == 1
        assert state.read_bytes() == before


# ---------------------------------------------------------------------------
# Engine transactions


def _engine_state(tmp_path: Path) -> Path:
    path = tmp_path / "review-state.json"
    # review_id must match the helpers' witness verifier scope.
    engine.init_review(path, review_id="review-test", scratch_dir=tmp_path, apply=True)
    return path


def _walk_to(tmp_path: Path, *, stop: str) -> helpers._Walk:
    """Drive a _Walk up to (not including) `stop`, persist, and return it."""
    w = helpers._Walk(tmp_path)
    w.freeze()
    if stop == "freeze":
        pass
    else:
        w.ascent()
        if stop == "ascent":
            pass
        else:
            w.final()
            w.closure()
            if stop == "ready":
                pass
            elif stop == "remote-ci":
                w.ready()
                w.transition()
            else:
                raise AssertionError(f"unknown stop {stop!r}")
    _persist_state(tmp_path, w.state)
    return w


class TestEngineTransactions:
    def test_init_review_writes_generation_0(self, tmp_path):
        path = _engine_state(tmp_path)
        loaded = store.load_state(path)
        assert loaded["generation"] == 0
        assert loaded["review_id"] == "review-test"

    def test_init_review_check_writes_nothing(self, tmp_path):
        path = tmp_path / "s.json"
        result = engine.init_review(path, review_id="r", scratch_dir=tmp_path, apply=False)
        assert result.decision.allowed
        assert not path.exists()

    def test_init_review_refuses_existing(self, tmp_path):
        path = _engine_state(tmp_path)
        with pytest.raises(store.StoreError):
            engine.init_review(path, review_id="r2", scratch_dir=tmp_path, apply=True)

    def test_next_action_for_reports_freeze_first(self, tmp_path):
        path = _engine_state(tmp_path)
        result = engine.next_action_for(path, policies=engine.load_witness_sources().policies)
        assert result.decision.action == "freeze-review-input"

    def test_validate_state_file_rejects_v1(self, tmp_path):
        v1 = _v1_state(tmp_path)
        with pytest.raises(store.StoreError):
            engine.validate_state_file(v1)

    def test_freeze_via_authority_source(self, tmp_path):
        w = helpers._Walk(tmp_path)  # policies/registry only; state unused
        path = _engine_state(tmp_path)
        registry = w.registry
        policies = w.policies
        auth = _AuthorityDouble(path, registry, tmp_path, policies)
        sources = engine.WitnessSources(
            policies=policies,
            evidence_ingestion_policy=_test_ingestion(),
            authority_discovery=auth,
        )
        result = engine.complete_transaction(
            path,
            action="freeze-review-input",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=sources,
        )
        assert result.decision.allowed, result.decision.reason
        loaded = store.load_state(path)
        assert loaded["generation"] == 1
        assert loaded["snapshot"]["epoch"] == 1
        assert loaded["authority_manifest"] is not None
        assert len(loaded["witness_records"]) == 1
        assert len(loaded["history"]) == 1
        assert loaded["history"][0]["event"] == "freeze-review-input"
        # The discovery witness verifies against its locator-bound record.
        wrec = next(iter(loaded["witness_records"].values()))
        policy.verify_witness(loaded, policies, wrec)

    def test_freeze_blocked_without_authority_source(self, tmp_path):
        path = _engine_state(tmp_path)
        before = path.read_bytes()
        result = engine.complete_transaction(
            path,
            action="freeze-review-input",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=engine.WitnessSources(
                policies=engine.load_witness_sources().policies,
                evidence_ingestion_policy=_test_ingestion(),
            ),
        )
        assert not result.decision.allowed
        assert "missing-witness-source:authority-discovery" in result.decision.missing
        assert path.read_bytes() == before

    def test_dispatch_launch_collect_map_action(self, tmp_path):
        w = helpers._Walk(tmp_path)
        policies = w.policies
        path = _engine_state(tmp_path)
        registry = w.registry
        auth = _AuthorityDouble(path, registry, tmp_path, policies)
        dispatch = _DispatchDouble(
            path,
            registry,
            tmp_path,
            policies,
            data_builders={"impact-mapper-semantic": _map_data_builder},
        )
        sources = engine.WitnessSources(
            policies=policies,
            evidence_ingestion_policy=_test_ingestion(),
            authority_discovery=auth,
            reviewer_dispatch=dispatch,
        )
        engine.complete_transaction(
            path,
            action="freeze-review-input",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=sources,
        )
        r1 = engine.register_dispatch_transaction(path, action="map-impact-semantic", sources=sources)
        assert r1.decision.allowed
        st = store.load_state(path)
        assert st["generation"] == 2
        d = next(iter(st["dispatches"].values()))
        assert d["status"] == "pending"
        assert d["launch_witness_id"] is None
        did = d["dispatch_id"]
        r2 = engine.launch_transaction(path, dispatch_id=did, sources=sources)
        assert r2.decision.allowed
        st = store.load_state(path)
        assert st["generation"] == 3
        assert st["dispatches"][did]["launch_witness_id"] is not None
        r3 = engine.complete_transaction(
            path,
            action="map-impact-semantic",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=sources,
        )
        assert r3.decision.allowed, r3.decision.reason
        st = store.load_state(path)
        assert st["generation"] == 4
        assert st["dispatches"][did]["status"] == "reported"
        assert len(st["impact_maps"]) == 1
        assert len(st["reviews"]) == 1
        events = [h["event"] for h in st["history"]]
        assert events == [
            "freeze-review-input",
            "register-dispatch",
            "record-launch",
            "map-impact-semantic",
        ]

    def test_register_dispatch_idempotent_replay(self, tmp_path):
        w = helpers._Walk(tmp_path)
        policies = w.policies
        path = _engine_state(tmp_path)
        registry = w.registry
        auth = _AuthorityDouble(path, registry, tmp_path, policies)
        dispatch = _DispatchDouble(path, registry, tmp_path, policies)
        sources = engine.WitnessSources(
            policies=policies,
            evidence_ingestion_policy=_test_ingestion(),
            authority_discovery=auth,
            reviewer_dispatch=dispatch,
        )
        engine.complete_transaction(
            path,
            action="freeze-review-input",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=sources,
        )
        engine.register_dispatch_transaction(path, action="map-impact-semantic", sources=sources)
        before = path.read_bytes()
        again = engine.register_dispatch_transaction(path, action="map-impact-semantic", sources=sources)
        assert again.decision.allowed
        assert "already registered" in again.decision.reason
        assert path.read_bytes() == before  # same bytes, same generation

    def test_complete_blocks_until_dispatch_launched(self, tmp_path):
        w = helpers._Walk(tmp_path)
        policies = w.policies
        path = _engine_state(tmp_path)
        registry = w.registry
        auth = _AuthorityDouble(path, registry, tmp_path, policies)
        dispatch = _DispatchDouble(path, registry, tmp_path, policies)
        sources = engine.WitnessSources(
            policies=policies,
            evidence_ingestion_policy=_test_ingestion(),
            authority_discovery=auth,
            reviewer_dispatch=dispatch,
        )
        engine.complete_transaction(
            path,
            action="freeze-review-input",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=sources,
        )
        engine.register_dispatch_transaction(path, action="map-impact-semantic", sources=sources)
        before = path.read_bytes()
        result = engine.complete_transaction(
            path,
            action="map-impact-semantic",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=sources,
        )
        # Pending but unlaunched: the completion path refuses to fabricate a
        # launch witness retroactively.
        assert not result.decision.allowed
        assert "dispatch-launch" in result.decision.missing
        assert path.read_bytes() == before

    def test_caller_provenance_rejected_for_dispatch_action(self, tmp_path):
        path = _engine_state(tmp_path)
        with pytest.raises(model.StateValidationError) as exc:
            engine.complete_transaction(
                path,
                action="run-fast-review",
                caller_data_bytes=b'{"attestations": [], "findings": []}',
                caller_evidence=(),
                sources=engine.WitnessSources(
                    policies=engine.load_witness_sources().policies,
                    evidence_ingestion_policy=_test_ingestion(),
                ),
            )
        assert exc.value.code == "caller-provenance"

    def test_failed_transaction_commits_nothing(self, tmp_path):
        path = _engine_state(tmp_path)
        before = path.read_bytes()
        with pytest.raises(model.StateValidationError):
            engine.complete_transaction(
                path,
                action="plan-coverage",
                caller_data_bytes=b'{"obligations": [{"bad": true}]}',
                caller_evidence=(),
                sources=engine.WitnessSources(
                    policies=engine.load_witness_sources().policies,
                    evidence_ingestion_policy=_test_ingestion(),
                ),
            )
        assert path.read_bytes() == before
        assert store.load_state(path)["generation"] == 0

    def test_block_at_intake_requires_snapshot(self, tmp_path):
        # Blockers bind a live snapshot epoch, so they cannot be opened before
        # freeze installs one. The failure is clean and rolls back.
        path = _engine_state(tmp_path)
        before = path.read_bytes()
        sources = engine.WitnessSources(
            policies=engine.load_witness_sources().policies,
            evidence_ingestion_policy=_test_ingestion(),
        )
        with pytest.raises(model.StateValidationError) as exc:
            engine.block_transaction(
                path,
                blocker_class="tool-blocked",
                reason="sandbox unavailable",
                evidence=(),
                sources=sources,
            )
        assert exc.value.code == "missing-snapshot"
        assert path.read_bytes() == before

    def test_block_resume_with_evidence_post_freeze(self, tmp_path):
        w = _walk_to(tmp_path, stop="freeze")
        path = tmp_path / "review-state.json"
        gen0 = store.load_state(path)["generation"]
        ev = tmp_path / "note.txt"
        ev.write_text("detail", encoding="utf-8")
        sources = engine.WitnessSources(
            policies=w.policies,
            evidence_ingestion_policy=_test_ingestion(),
        )
        result = engine.block_transaction(
            path,
            blocker_class="tool-blocked",
            reason="sandbox unavailable",
            evidence=(engine.EvidenceSource("note", "check-output", ev),),
            sources=sources,
        )
        assert result.decision.allowed
        st = store.load_state(path)
        assert st["status"] == "blocked"
        blocker_id = next(iter(st["blockers"]))
        blocker = st["blockers"][blocker_id]
        assert blocker["snapshot_epoch"] == 1
        assert len(blocker["evidence_ids"]) == 1
        # Evidence bound to the live epoch: registered, content-addressed.
        ev_id = blocker["evidence_ids"][0]
        assert st["evidence"][ev_id]["snapshot_epoch"] == 1
        res = tmp_path / "res.txt"
        res.write_text("restored", encoding="utf-8")
        result = engine.resume_transaction(
            path,
            blocker_id=blocker_id,
            resolution_evidence=(engine.EvidenceSource("res", "check-output", res),),
            sources=sources,
        )
        assert result.decision.allowed
        st = store.load_state(path)
        assert st["status"] == "active"
        assert store.load_state(path)["generation"] == gen0 + 2

    def test_resume_rejects_empty_evidence_post_freeze(self, tmp_path):
        w = _walk_to(tmp_path, stop="freeze")
        path = tmp_path / "review-state.json"
        sources = engine.WitnessSources(
            policies=w.policies,
            evidence_ingestion_policy=_test_ingestion(),
        )
        engine.block_transaction(
            path,
            blocker_class="tool-blocked",
            reason="blocked",
            evidence=(),
            sources=sources,
        )
        st = store.load_state(path)
        blocker_id = next(iter(st["blockers"]))
        before = path.read_bytes()
        with pytest.raises(model.StateValidationError) as exc:
            engine.resume_transaction(
                path,
                blocker_id=blocker_id,
                resolution_evidence=(),
                sources=sources,
            )
        assert exc.value.code == "missing-field"
        assert path.read_bytes() == before

    def test_ready_transition_two_phase(self, tmp_path):
        w = _walk_to(tmp_path, stop="ready")
        path = tmp_path / "review-state.json"
        transition = _TransitionDouble(path, w.registry, tmp_path, w.policies)
        sources = _walk_sources(w, path, tmp_path, remote_transition=transition)
        result = engine.complete_transaction(
            path,
            action="mark-ready-for-ci",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=sources,
        )
        assert result.decision.allowed, result.decision.reason
        st = store.load_state(path)
        ready = st["ready_transition"]
        assert ready["status"] == "completed"
        assert ready["prior_lifecycle_state"] == "draft"
        assert st["ci_candidate"] is not None
        events = [h["event"] for h in st["history"]]
        assert events[-2:] == ["mark-ready-for-ci", "finalize-ready-transition"]

    def test_finalize_ready_refused_while_blocked(self, tmp_path):
        """A blocker opened between intent registration and finalization
        must stop phase 2: _current does not catch blockers (no epoch
        advance), so the explicit blocked-status guard has to."""
        w = _walk_to(tmp_path, stop="ready")
        path = tmp_path / "review-state.json"
        transition = _TransitionDouble(path, w.registry, tmp_path, w.policies)
        sources = _walk_sources(w, path, tmp_path, remote_transition=transition)
        registered = engine.register_ready_transition_transaction(path, sources=sources)
        assert registered.decision.allowed, registered.decision.reason
        rid = store.load_state(path)["ready_transition"]["ready_transition_id"]
        engine.block_transaction(
            path,
            blocker_class="tool-blocked",
            reason="blocked",
            evidence=(),
            sources=sources,
        )
        result = engine.finalize_ready_transition_transaction(path, ready_transition_id=rid, sources=sources)
        assert not result.decision.allowed
        assert result.decision.status == "blocked"
        st = store.load_state(path)
        assert st["ready_transition"]["status"] == "pending"
        assert st["ci_candidate"] is None

    def test_remote_ci_and_seal_via_engine(self, tmp_path):
        w = _walk_to(tmp_path, stop="remote-ci")
        path = tmp_path / "review-state.json"
        observer = _ObserverDouble(path, w.registry, tmp_path, w.policies)
        sources = _walk_sources(w, path, tmp_path, remote_observer=observer)
        result = engine.complete_transaction(
            path,
            action="run-remote-ci",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=sources,
        )
        assert result.decision.allowed, result.decision.reason
        st = store.load_state(path)
        hosted = [c for c in st["checks"].values() if c["locus"] == "hosted"]
        assert len(hosted) == 1
        result = engine.complete_transaction(
            path,
            action="seal-green",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=sources,
        )
        assert result.decision.allowed, result.decision.reason
        st = store.load_state(path)
        assert st["green_seal"] is not None
        assert st["stage"] == "green-candidate"

    def test_seal_blocked_without_remote_observer(self, tmp_path):
        w = _walk_to(tmp_path, stop="remote-ci")
        path = tmp_path / "review-state.json"
        observer = _ObserverDouble(path, w.registry, tmp_path, w.policies)
        sources = _walk_sources(w, path, tmp_path, remote_observer=observer)
        engine.complete_transaction(
            path,
            action="run-remote-ci",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=sources,
        )
        sealed = _walk_sources(w, path, tmp_path)  # no remote_observer wired
        before = path.read_bytes()
        result = engine.complete_transaction(
            path,
            action="seal-green",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=sealed,
        )
        assert not result.decision.allowed
        assert "missing-witness-source:remote-observer" in result.decision.missing
        assert path.read_bytes() == before

    def test_preflight_via_command_runner(self, tmp_path):
        _walk_to(tmp_path, stop="ascent")
        # _walk_to("ascent") runs the whole ascent including preflight; rebuild
        # to just before preflight instead.
        w2 = helpers._Walk(tmp_path / "pre")
        w2.freeze()
        w2.ascent_to_exemption()
        w2.exemption()
        path2 = _persist_state(tmp_path / "pre", w2.state)
        runner = _CommandRunnerDouble(path2, w2.registry, tmp_path / "pre", w2.policies)
        sources = engine.WitnessSources(
            policies=w2.policies,
            evidence_ingestion_policy=_test_ingestion(),
            command_runner=runner,
        )
        result = engine.complete_transaction(
            path2,
            action="run-preflight",
            caller_data_bytes=b"",
            caller_evidence=(),
            sources=sources,
        )
        assert result.decision.allowed, result.decision.reason
        st = store.load_state(path2)
        preflights = [c for c in st["checks"].values() if c["kind"] == "preflight"]
        assert len(preflights) == 1
        assert preflights[0]["conclusion"] == "success"


# ---------------------------------------------------------------------------
# Version boundary: legacy surfaces cannot touch v2 state


class TestVersionBoundary:
    def test_next_node_refuses_v2_state_every_mode(self, tmp_path):
        state = _init_state(tmp_path)
        before = state.read_bytes()
        for args in (
            ("--state", str(state)),
            ("--state", str(state), "--propose", "ready"),
            ("--state", str(state), "--status"),
            ("--state", str(state), "--resync", "--apply"),
        ):
            r = _run(NEXT_NODE, *args)
            assert r.returncode == 1, (args, r.stdout, r.stderr)
            assert "version-2 state is controlled only by reviewctl.py" in r.stderr
        assert state.read_bytes() == before
        assert b'"green_seal":null' in before

    def test_next_node_metrics_and_propose_rejected(self, tmp_path):
        metrics = tmp_path / "review-metrics.json"
        metrics.write_text("{}", encoding="utf-8")
        r = _run(NEXT_NODE, "--metrics", str(metrics), "--propose", "ready")
        assert r.returncode == 2
        assert "cannot be combined" in r.stderr

    def test_next_node_v1_ready_blocked(self, tmp_path):
        v1 = _v1_state(tmp_path)
        before = v1.read_bytes()
        r = _run(NEXT_NODE, "--state", str(v1), "--propose", "ready")
        assert r.returncode == 1
        assert "version-1 review state cannot produce a trustworthy-green" in r.stderr
        assert v1.read_bytes() == before

    def test_compile_metrics_refuses_v2_state(self, tmp_path):
        state = _init_state(tmp_path)
        before = state.read_bytes()
        metrics = tmp_path / "review-metrics.json"
        r = _run(
            COMPILE_METRICS,
            "--state",
            str(state),
            "--metrics",
            str(metrics),
        )
        assert r.returncode == 1
        assert "version-2 state is controlled only by reviewctl.py" in r.stderr
        assert not metrics.exists()
        assert state.read_bytes() == before
        assert '"green_seal":null' in state.read_text(encoding="utf-8")

    def test_resolved_ledger_cannot_mutate_v2_state(self, tmp_path):
        state = _init_state(tmp_path)
        before = state.read_bytes()
        metrics = tmp_path / "review-metrics.json"
        metrics.write_text('{"rounds_per_finding": [], "regressions": [], "pr": {}}', encoding="utf-8")
        ledger = tmp_path / "ledger.md"
        r = _run(
            RESOLVED_LEDGER,
            "--metrics",
            str(metrics),
            "--ledger",
            str(ledger),
            "--apply",
        )
        assert r.returncode == 0, r.stderr
        assert state.read_bytes() == before

    def test_next_node_does_not_write_to_v2_state_dir(self, tmp_path):
        state = _init_state(tmp_path)
        metrics = tmp_path / "review-metrics.json"
        metrics.write_text('{"rounds_per_finding": [], "regressions": [], "pr": {}}', encoding="utf-8")
        before = state.read_bytes()
        _run(NEXT_NODE, "--metrics", str(metrics))
        # metrics-mode discovery is read-only; the state file is untouched.
        assert state.read_bytes() == before


# ---------------------------------------------------------------------------
# Live acquisition verbs: enumerate -> complete --acquired


class TestEnumerateCompleteFlow:
    """Drives reviewctl.main() in-process with fake git/gh runners so the
    two-command witnessed acquisition flow is exercised end to end."""

    def _live(self, monkeypatch, git=None, gh=None, runtime="devin-desktop"):
        import reviewctl

        monkeypatch.setenv(engine.RUNTIME_ENV_VAR, runtime)
        if git is not None:
            monkeypatch.setattr(reviewctl, "_run_git", lambda a, cwd=None: git(a))
        if gh is not None:
            monkeypatch.setattr(reviewctl, "_run_gh", lambda a, cwd=None: gh(a))
        return reviewctl

    def _init(self, reviewctl, tmp_path, review_id="rev-1"):
        state = tmp_path / "review-state.json"
        scratch = tmp_path / "scratch"
        rc = reviewctl.main(
            [
                "init",
                "--state",
                str(state),
                "--review-id",
                review_id,
                "--scratch-dir",
                str(scratch),
                "--apply",
            ]
        )
        assert rc == 0
        return state, scratch

    def test_enumerate_then_complete_freeze_end_to_end(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(
            monkeypatch,
            git=helpers.FakeGit({"AGENTS.md": "# law"}),
            gh=helpers.FakeGh(),
        )
        state, scratch = self._init(reviewctl, tmp_path)
        rc = reviewctl.main(
            [
                "enumerate",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
            ]
        )
        assert rc == 0
        out = capsys.readouterr().out
        assert "enumeration-id:" in out
        acquire_dir = scratch / "acquire" / "latest"
        enum_id = json.loads((acquire_dir / "enumeration.json").read_text())["enumeration_id"]
        helpers.acq_transcript_with_marker(scratch, enum_id, out_dir=acquire_dir)
        rc = reviewctl.main(
            [
                "complete",
                "--state",
                str(state),
                "--action",
                "freeze-review-input",
                "--acquired",
                str(acquire_dir),
                "--apply",
            ]
        )
        assert rc == 0
        loaded = store.load_state(state)
        assert loaded["snapshot"]["epoch"] == 1
        assert loaded["authority_manifest"] is not None
        assert loaded["authorities"]
        assert len(loaded["witness_records"]) == 1
        wrec = next(iter(loaded["witness_records"].values()))
        assert wrec["kind"] == "authority-discovery"
        rc = reviewctl.main(["validate", "--state", str(state)])
        assert rc == 0
        capsys.readouterr()
        rc = reviewctl.main(["status", "--state", str(state), "--json"])
        assert rc == 0
        status = json.loads(capsys.readouterr().out)
        assert status["stage"] != "intake"

    def test_complete_acquired_witness_error_is_clean_failure(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(
            monkeypatch,
            git=helpers.FakeGit({"AGENTS.md": "# law"}),
            gh=helpers.FakeGh(),
        )
        state, scratch = self._init(reviewctl, tmp_path)
        capsys.readouterr()
        rc = reviewctl.main(["enumerate", "--state", str(state), "--repo", str(tmp_path), "--pr", "7"])
        assert rc == 0
        capsys.readouterr()
        acquire_dir = scratch / "acquire" / "latest"
        enum_id = json.loads((acquire_dir / "enumeration.json").read_text())["enumeration_id"]
        helpers.acq_transcript_with_marker(scratch, enum_id, out_dir=acquire_dir)
        # Corrupt the witness log before complete: the chain-invalid failure
        # must surface as a clean witness-error line, not a traceback.
        log_path = scratch / "witness" / "witness-log.jsonl"
        log_path.parent.mkdir(parents=True, exist_ok=True)
        log_path.write_text('{"bogus": true}' + chr(10), encoding="utf-8")
        rc = reviewctl.main(
            [
                "complete",
                "--state",
                str(state),
                "--action",
                "freeze-review-input",
                "--acquired",
                str(acquire_dir),
                "--apply",
            ]
        )
        assert rc == 1
        err = capsys.readouterr().err
        assert "witness-error" in err
        assert "Traceback" not in err

    def test_complete_acquired_refuses_with_data_file(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(monkeypatch)
        state, scratch = self._init(reviewctl, tmp_path)
        data_file = tmp_path / "d.json"
        data_file.write_text("{}")
        rc = reviewctl.main(
            [
                "complete",
                "--state",
                str(state),
                "--action",
                "freeze-review-input",
                "--acquired",
                str(scratch / "acquire" / "latest"),
                "--data-file",
                str(data_file),
                "--apply",
            ]
        )
        assert rc == 2
        assert "mutually exclusive" in capsys.readouterr().err

    def test_freeze_without_acquisition_fails_source_absent(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(monkeypatch)
        state, scratch = self._init(reviewctl, tmp_path)
        rc = reviewctl.main(
            [
                "complete",
                "--state",
                str(state),
                "--action",
                "freeze-review-input",
                "--acquired",
                str(scratch / "acquire" / "latest"),
                "--apply",
            ]
        )
        assert rc == 1
        out = capsys.readouterr().out
        assert "missing-witness-source" in out

    def test_refresh_flow_advances_epoch_and_invalidates(self, tmp_path, monkeypatch, capsys):
        git1 = helpers.FakeGit({"AGENTS.md": "# law"})
        gh1 = helpers.FakeGh()
        reviewctl = self._live(monkeypatch, git=git1, gh=gh1)
        state, scratch = self._init(reviewctl, tmp_path)
        reviewctl.main(
            [
                "enumerate",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
            ]
        )
        capsys.readouterr()
        acquire_dir = scratch / "acquire" / "latest"
        enum1 = json.loads((acquire_dir / "enumeration.json").read_text())["enumeration_id"]
        helpers.acq_transcript_with_marker(scratch, enum1, out_dir=acquire_dir)
        rc = reviewctl.main(
            [
                "complete",
                "--state",
                str(state),
                "--action",
                "freeze-review-input",
                "--acquired",
                str(acquire_dir),
                "--apply",
            ]
        )
        assert rc == 0
        # New head + tree on the next enumeration.
        git2 = helpers.FakeGit({"AGENTS.md": "# law"}, head="9" * 40, tree="8" * 40, diff="new-diff")
        gh2 = helpers.FakeGh(pr=helpers.acq_pr_meta(headRefOid="9" * 40))
        monkeypatch.setattr(reviewctl, "_run_git", lambda a, cwd=None: git2(a))
        monkeypatch.setattr(reviewctl, "_run_gh", lambda a, cwd=None: gh2(a))
        rc = reviewctl.main(
            [
                "enumerate",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
            ]
        )
        assert rc == 0
        enum2 = json.loads((acquire_dir / "enumeration.json").read_text())["enumeration_id"]
        helpers.acq_transcript_with_marker(scratch, enum2, session="s2", tool_use="exec_2", out_dir=acquire_dir)
        rc = reviewctl.main(
            [
                "complete",
                "--state",
                str(state),
                "--action",
                "refresh-review-input",
                "--acquired",
                str(acquire_dir),
                "--apply",
            ]
        )
        assert rc == 0
        loaded = store.load_state(state)
        assert loaded["snapshot"]["epoch"] == 2
        assert loaded["snapshot"]["head_sha"] == "9" * 40
        assert loaded["coverage_inventory"] is None
        assert loaded["ready_transition"] is None
        assert loaded["ci_candidate"] is None
        assert loaded["green_seal"] is None

    def test_refresh_identical_inputs_refused_no_drift(self, tmp_path, monkeypatch, capsys):
        git = helpers.FakeGit({"AGENTS.md": "# law"})
        gh = helpers.FakeGh()
        reviewctl = self._live(monkeypatch, git=git, gh=gh)
        state, scratch = self._init(reviewctl, tmp_path)
        reviewctl.main(
            [
                "enumerate",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
            ]
        )
        capsys.readouterr()
        acquire_dir = scratch / "acquire" / "latest"
        enum1 = json.loads((acquire_dir / "enumeration.json").read_text())["enumeration_id"]
        helpers.acq_transcript_with_marker(scratch, enum1, out_dir=acquire_dir)
        rc = reviewctl.main(
            [
                "complete",
                "--state",
                str(state),
                "--action",
                "freeze-review-input",
                "--acquired",
                str(acquire_dir),
                "--apply",
            ]
        )
        assert rc == 0
        # Same inputs: re-enumerate epoch 2 (identical bytes) then refresh.
        rc = reviewctl.main(
            [
                "enumerate",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
            ]
        )
        assert rc == 0
        enum2 = json.loads((acquire_dir / "enumeration.json").read_text())["enumeration_id"]
        assert enum2 != enum1  # epoch differs, so the subject differs
        helpers.acq_transcript_with_marker(scratch, enum2, session="s2", tool_use="exec_2", out_dir=acquire_dir)
        rc = reviewctl.main(
            [
                "complete",
                "--state",
                str(state),
                "--action",
                "refresh-review-input",
                "--acquired",
                str(acquire_dir),
                "--apply",
            ]
        )
        assert rc == 1
        assert "no-drift" in capsys.readouterr().err

    def test_enumerate_inert_off_devin(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(monkeypatch)
        state, scratch = self._init(reviewctl, tmp_path)
        # init is runtime-gated too; flip to a non-Devin runtime after the
        # v2 state exists, then prove enumerate stays inert.
        monkeypatch.setenv(engine.RUNTIME_ENV_VAR, "generic-openai")
        rc = reviewctl.main(
            [
                "enumerate",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
            ]
        )
        assert rc == 1
        assert "unsupported-runtime" in capsys.readouterr().err

    def test_freeze_alias_runs_witnessed_flow(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(
            monkeypatch,
            git=helpers.FakeGit({"AGENTS.md": "# law"}),
            gh=helpers.FakeGh(),
        )
        state, scratch = self._init(reviewctl, tmp_path)
        rc = reviewctl.main(
            [
                "enumerate",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
            ]
        )
        assert rc == 0
        capsys.readouterr()
        acquire_dir = scratch / "acquire" / "latest"
        enum_id = json.loads((acquire_dir / "enumeration.json").read_text())["enumeration_id"]
        helpers.acq_transcript_with_marker(scratch, enum_id, out_dir=acquire_dir)
        rc = reviewctl.main(
            [
                "freeze",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
                "--apply",
            ]
        )
        assert rc == 0
        loaded = store.load_state(state)
        assert loaded["snapshot"]["epoch"] == 1
        assert loaded["authority_manifest"] is not None

    def test_freeze_alias_refuses_without_enumeration(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(monkeypatch)
        state, scratch = self._init(reviewctl, tmp_path)
        rc = reviewctl.main(
            [
                "freeze",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
                "--apply",
            ]
        )
        assert rc == 1
        assert "stale-acquisition" in capsys.readouterr().err

    def test_freeze_alias_refuses_on_head_mismatch(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(
            monkeypatch,
            git=helpers.FakeGit({"AGENTS.md": "# law"}),
            gh=helpers.FakeGh(),
        )
        state, scratch = self._init(reviewctl, tmp_path)
        rc = reviewctl.main(
            [
                "enumerate",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
            ]
        )
        assert rc == 0
        capsys.readouterr()
        acquire_dir = scratch / "acquire" / "latest"
        enum_id = json.loads((acquire_dir / "enumeration.json").read_text())["enumeration_id"]
        helpers.acq_transcript_with_marker(scratch, enum_id, out_dir=acquire_dir)
        # checked-out HEAD moved after enumerate: the enumeration is stale.
        moved = helpers.FakeGit({"AGENTS.md": "# law"}, head="f" * 40)
        monkeypatch.setattr(reviewctl, "_run_git", lambda a, cwd=None: moved(a))
        rc = reviewctl.main(
            [
                "freeze",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
                "--apply",
            ]
        )
        assert rc == 1
        assert "stale-acquisition" in capsys.readouterr().err

    def test_enumerate_json_stdout_is_pure_json(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(
            monkeypatch,
            git=helpers.FakeGit({"AGENTS.md": "# law"}),
            gh=helpers.FakeGh(),
        )
        state, scratch = self._init(reviewctl, tmp_path)
        capsys.readouterr()
        rc = reviewctl.main(
            [
                "enumerate",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
                "--json",
            ]
        )
        assert rc == 0
        summary = json.loads(capsys.readouterr().out)
        enum_id = json.loads((scratch / "acquire" / "latest" / "enumeration.json").read_text())["enumeration_id"]
        assert summary["enumeration_id"] == enum_id

    def test_alias_tolerates_repo_path_normalization(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(
            monkeypatch,
            git=helpers.FakeGit({"AGENTS.md": "# law"}),
            gh=helpers.FakeGh(),
        )
        state, scratch = self._init(reviewctl, tmp_path)
        rc = reviewctl.main(
            [
                "enumerate",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
            ]
        )
        assert rc == 0
        capsys.readouterr()
        acquire_dir = scratch / "acquire" / "latest"
        enum_file = acquire_dir / "enumeration.json"
        enum_rec = json.loads(enum_file.read_text())
        enum_rec["inputs"]["repo_root"] = enum_rec["inputs"]["repo_root"].replace(os.sep, "/")
        enum_file.write_text(json.dumps(enum_rec))
        helpers.acq_transcript_with_marker(scratch, enum_rec["enumeration_id"], out_dir=acquire_dir)
        rc = reviewctl.main(
            [
                "freeze",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
                "--apply",
            ]
        )
        assert rc == 0

    def test_freeze_alias_malformed_enumeration_fails_stale(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(monkeypatch)
        state, scratch = self._init(reviewctl, tmp_path)
        acquire_dir = scratch / "acquire" / "latest"
        acquire_dir.mkdir(parents=True)
        (acquire_dir / "enumeration.json").write_text("[]", encoding="utf-8")
        rc = reviewctl.main(
            [
                "freeze",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
                "--apply",
            ]
        )
        assert rc == 1
        err = capsys.readouterr().err
        assert "stale-acquisition" in err
        assert "Traceback" not in err

    def test_freeze_alias_nul_repo_root_fails_stale(self, tmp_path, monkeypatch, capsys):
        # A tampered enumeration.json carrying a NUL-byte repo_root must hit
        # the typed stale-acquisition refusal, not an unexpected ValueError.
        reviewctl = self._live(monkeypatch)
        state, scratch = self._init(reviewctl, tmp_path)
        acquire_dir = scratch / "acquire" / "latest"
        acquire_dir.mkdir(parents=True)
        (acquire_dir / "enumeration.json").write_text(
            json.dumps({"inputs": {"repo_root": "foo\x00bar", "pr_number": 7}}),
            encoding="utf-8",
        )
        rc = reviewctl.main(
            [
                "freeze",
                "--state",
                str(state),
                "--repo",
                str(tmp_path),
                "--pr",
                "7",
                "--apply",
            ]
        )
        assert rc == 1
        err = capsys.readouterr().err
        assert "stale-acquisition" in err
        assert "Traceback" not in err

    def test_complete_missing_data_file_is_typed_failure(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(monkeypatch)
        state, _scratch = self._init(reviewctl, tmp_path)
        capsys.readouterr()
        rc = reviewctl.main(
            [
                "complete",
                "--state",
                str(state),
                "--action",
                "freeze-review-input",
                "--data-file",
                str(tmp_path / "absent.json"),
                "--apply",
            ]
        )
        assert rc == 1
        err = capsys.readouterr().err
        assert "missing-source" in err
        assert "io-error" not in err
        assert "Traceback" not in err

    def test_freeze_alias_git_missing_is_tool_blocked(self, tmp_path, monkeypatch, capsys):
        reviewctl = self._live(
            monkeypatch,
            git=helpers.FakeGit({"AGENTS.md": "# law"}),
            gh=helpers.FakeGh(),
        )
        state, scratch = self._init(reviewctl, tmp_path)
        rc = reviewctl.main(["enumerate", "--state", str(state), "--repo", str(tmp_path), "--pr", "7"])
        assert rc == 0
        capsys.readouterr()

        def no_git(_a, cwd=None):
            raise FileNotFoundError("git")

        monkeypatch.setattr(reviewctl, "_run_git", no_git)
        rc = reviewctl.main(["freeze", "--state", str(state), "--repo", str(tmp_path), "--pr", "7", "--apply"])
        assert rc == 1
        err = capsys.readouterr().err
        assert "tool-blocked" in err
        assert "Traceback" not in err


class TestHooksRenderAndJsonFlag:
    """hooks.v1.json rendering is platform-aware; --json is argparse-native."""

    def test_hooks_install_renders_platform_interpreter(self, tmp_path, monkeypatch, capsys):
        import reviewctl

        monkeypatch.setenv(engine.RUNTIME_ENV_VAR, "devin-desktop")
        scratch = tmp_path / "scratch"
        monkeypatch.setattr(sys, "platform", "linux")
        rc = reviewctl.main(["hooks", "install", "--scratch-dir", str(scratch)])
        assert rc == 0
        capsys.readouterr()
        cfg = json.loads((scratch / "hooks" / "hooks.v1.json").read_text(encoding="utf-8"))
        commands = [h["command"] for group in cfg["hooks"].values() for m in group for h in m["hooks"]]
        assert commands and all(c.startswith("python3 ") for c in commands)

        scratch2 = tmp_path / "scratch2"
        monkeypatch.setattr(sys, "platform", "win32")
        rc = reviewctl.main(["hooks", "install", "--scratch-dir", str(scratch2)])
        assert rc == 0
        capsys.readouterr()
        cfg = json.loads((scratch2 / "hooks" / "hooks.v1.json").read_text(encoding="utf-8"))
        commands = [h["command"] for group in cfg["hooks"].values() for m in group for h in m["hooks"]]
        assert commands and all(c.startswith("py -3 ") for c in commands)

    def test_json_flag_in_both_positions(self, tmp_path, monkeypatch, capsys):
        import reviewctl

        scratch = tmp_path / "scratch"
        rc = reviewctl.main(["doctor", "--scratch-dir", str(scratch), "--json"])
        assert rc in (0, 1)
        out = capsys.readouterr().out.strip()
        assert out.startswith("{")
        rc = reviewctl.main(["--json", "doctor", "--scratch-dir", str(scratch)])
        assert rc in (0, 1)
        out = capsys.readouterr().out.strip()
        assert out.startswith("{")

    def test_hooks_parent_json_flag(self, tmp_path, monkeypatch, capsys):
        import reviewctl

        monkeypatch.setenv(engine.RUNTIME_ENV_VAR, "devin-desktop")
        scratch = tmp_path / "scratch"
        rc = reviewctl.main(["hooks", "--json", "install", "--scratch-dir", str(scratch)])
        assert rc == 0
        out = capsys.readouterr().out.strip()
        assert out.startswith("{")

    def test_json_token_as_flag_value_is_argparse_error(self, capsys):
        import reviewctl

        # `--reason --json` must not silently strip the token: argparse sees
        # it as an option and reports the missing --reason value.
        with pytest.raises(SystemExit):
            reviewctl.main(["block", "--state", "x", "--class", "c", "--reason", "--json"])

    def test_doctor_missing_tools_report_failed_rows(self, tmp_path):
        import reviewctl

        def missing(_argv, cwd=None):
            raise FileNotFoundError("no such file: git")

        rows, verdict = reviewctl._doctor_rows(
            runtime=engine.RUNTIME_DEVIN_DESKTOP,
            repo=tmp_path,
            run_cmd=missing,
        )
        by_name = {r["name"]: r for r in rows}
        for name in ("git-present", "repo-non-shallow", "gh-authenticated"):
            assert by_name[name]["status"] == "fail", by_name
            assert "no such file" in by_name[name]["detail"]
        assert verdict == "capability-floor-failed"
