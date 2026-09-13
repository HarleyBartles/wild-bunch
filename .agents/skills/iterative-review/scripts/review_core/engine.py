#!/usr/bin/env python3
"""Version-2 review engine: composition root and durable transactions.

Every mutation wraps exactly one ``locked_state_transaction``: the engine
loads the candidate, registers trusted payload evidence, installs witness
records, applies the pure ``policy`` transition, verifies freshly-installed
witnesses, appends history, and calls ``commit()`` exactly once. External
side effects never predate their recoverable intent: dispatch and
ready-transition flows persist a pending intent in one transaction, then the
witnessed call in the next, so a crash leaves a visible pending record at
the last durable phase and retry of the same intent is idempotent.

``load_witness_sources`` is the Plan-1 composition root: builtin fail-closed
policies and no live sources. Authority discovery, reviewer dispatch,
command execution, remote transition, and remote observation all report
``missing-witness-source`` until later plans inject them.
"""

from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path
from typing import Protocol, runtime_checkable

from . import model, policy, store


# ---------------------------------------------------------------------------
# Runtime detection
#
# The version-2 workflow is Devin-Desktop-only: it depends on hook-emitted
# transcripts, subagent profile loading, and workspace confinement that only
# that surface provides. Other runtimes are detected so the CLI can stay
# inert rather than fail midway through a mutation.

RUNTIME_ENV_VAR = "REVIEWCTL_RUNTIME"
RUNTIME_DEVIN_DESKTOP = "devin-desktop"
RUNTIME_CODEX = "codex-cli"
RUNTIME_OPENAI = "openai-compatible"
RUNTIME_UNKNOWN = "unknown"
KNOWN_RUNTIMES = frozenset({RUNTIME_DEVIN_DESKTOP, RUNTIME_CODEX, RUNTIME_OPENAI, RUNTIME_UNKNOWN})


def _devin_surface_dirs(env) -> list[Path]:
    dirs: list[Path] = []
    appdata = env.get("APPDATA")
    if appdata:
        dirs.append(Path(appdata) / "devin" / "agents")
    home = env.get("HOME") or env.get("USERPROFILE")
    if home:
        dirs.append(Path(home) / ".config" / "devin" / "agents")
        dirs.append(Path(home) / ".devin" / "agents")
    return dirs


def detect_runtime(environ=None) -> str:
    """Detect the hosting runtime. ``REVIEWCTL_RUNTIME`` overrides detection so
    tests and diagnostics can pin a verdict deterministically."""
    env = os.environ if environ is None else environ
    override = env.get(RUNTIME_ENV_VAR)
    if override:
        return override if override in KNOWN_RUNTIMES else RUNTIME_UNKNOWN
    if any(d.is_dir() for d in _devin_surface_dirs(env)):
        return RUNTIME_DEVIN_DESKTOP
    if env.get("CODEX_HOME") or env.get("CODEX_CI"):
        return RUNTIME_CODEX
    if env.get("OPENAI_API_KEY"):
        return RUNTIME_OPENAI
    return RUNTIME_UNKNOWN


# ---------------------------------------------------------------------------
# Trusted payloads and sources


@dataclass(frozen=True)
class EvidenceSource:
    alias: str
    kind: str
    path: Path


@dataclass(frozen=True)
class TrustedActionPayload:
    """Bytes plus evidence files produced by a trusted source adapter.

    ``raw_data`` decodes to an envelope ``{"data": {...}, "witnesses": [...]}``;
    ``data`` carries the action payload (or flow-specific fields) and
    ``witnesses`` carries witness records to install. Evidence aliases
    (``@name``) may appear inside ``data`` and resolve against
    ``evidence_sources`` before record validation.
    """

    raw_data: bytes
    evidence_sources: tuple[EvidenceSource, ...] = ()


@runtime_checkable
class AuthorityDiscoverySource(Protocol):
    def acquire(self, *, action: str, current_snapshot: dict | None) -> TrustedActionPayload: ...


@runtime_checkable
class ReviewerDispatchSource(Protocol):
    def resolve_profile(self, *, action: str, recipe: policy.ActionRecipe) -> TrustedActionPayload: ...

    def launch(self, *, dispatch: dict) -> TrustedActionPayload: ...

    def collect(self, *, dispatch: dict) -> TrustedActionPayload: ...


@runtime_checkable
class CommandRunnerSource(Protocol):
    def run(self, *, action: str, command_intent: dict) -> TrustedActionPayload: ...


@runtime_checkable
class RemoteTransitionSource(Protocol):
    def observe_lifecycle(self, *, intent: dict) -> TrustedActionPayload: ...

    def apply_or_confirm_ready(self, *, intent: dict) -> TrustedActionPayload: ...


@runtime_checkable
class RemoteObserverSource(Protocol):
    def observe(self, *, action: str, snapshot: dict) -> TrustedActionPayload: ...


@dataclass(frozen=True)
class WitnessSources:
    policies: policy.PolicyBundle
    evidence_ingestion_policy: store.EvidenceIngestionPolicy
    authority_discovery: AuthorityDiscoverySource | None = None
    reviewer_dispatch: ReviewerDispatchSource | None = None
    command_runner: CommandRunnerSource | None = None
    remote_transition: RemoteTransitionSource | None = None
    remote_observer: RemoteObserverSource | None = None


@dataclass(frozen=True)
class EngineResult:
    decision: policy.Decision
    generation: int
    state_path: Path


# ---------------------------------------------------------------------------
# Builtin fail-closed policies for the Plan-1 composition root


class _FailClosedWitnessPolicy:
    @property
    def sha256(self) -> str:
        return model.sha256_hex(b"review-core-witness-policy:fail-closed:1")

    @property
    def sources(self) -> tuple:
        return ()

    def permits(self, *, kind: str, source: str, locator: str) -> bool:
        return False


class _FailClosedWitnessVerifier:
    """Verifier used when no live witness sources are wired: every record is
    unverifiable, so the witnesses green predicate can never pass."""

    def __init__(self):
        self._policy = _FailClosedWitnessPolicy()

    @property
    def policy(self) -> _FailClosedWitnessPolicy:
        return self._policy

    def verify(self, **kwargs):
        raise policy.WitnessVerificationError("missing-source", "no live witness sources are wired in Plan 1")


class _BuiltinLocalChecks:
    """No required local checks are declared by the kernel itself; a reviewed
    project's check policy is injected by a later plan. The empty item set
    keeps the policy digest stable and fail-closed: no command-runner source
    exists to execute items anyway."""

    @property
    def source_id(self) -> str:
        return "review-core-local-checks"

    @property
    def source_version(self) -> str:
        return "1"

    @property
    def sha256(self) -> str:
        return model.sha256_hex(b"review-core-local-checks:empty:1")

    @property
    def items(self) -> tuple:
        return ()


class _BuiltinReviewAssignments:
    """Derives each dispatch's requirement from role and obligation floors."""

    @property
    def source_id(self) -> str:
        return "review-core-assignments"

    @property
    def source_version(self) -> str:
        return "1"

    @property
    def sha256(self) -> str:
        return model.sha256_hex(b"review-core-assignments:1")

    def requirement(self, *, state: dict, role: str, assignment_ids: tuple) -> policy.RoleRequirement:
        floor_t, floor_r = policy._role_floor(role)
        if role == "obligation-reviewer":
            floor_t, floor_r = "fast", "low"
            for aid in assignment_ids:
                o = state["obligations"].get(aid)
                if o is None:
                    continue
                t, r = policy.obligation_floor(o["scope_level"], o["risk"], o["consequences"])
                if model.CAPABILITY_TIERS.index(t) > model.CAPABILITY_TIERS.index(floor_t):
                    floor_t = t
                if model.REASONING_FLOORS.index(r) > model.REASONING_FLOORS.index(floor_r):
                    floor_r = r
        return policy.RoleRequirement(
            capability_tier=floor_t,
            reasoning_floor=floor_r,
            context_mode="fresh",
            distinct_execution_from=(),
            distinct_role_contract_from=(),
        )


class _BuiltinCommandExecution:
    @property
    def source_id(self) -> str:
        return "review-core-command-execution"

    @property
    def source_version(self) -> str:
        return "1"

    @property
    def sha256(self) -> str:
        return model.sha256_hex(b"review-core-command-execution:1")

    def command_intent(self, *, snapshot: dict, item: policy.LocalCheckItem) -> dict:
        return {
            "command": list(item.command),
            "policy_item_id": item.policy_item_id,
            "working_directory": item.working_directory,
            "snapshot_epoch": snapshot["epoch"],
            "snapshot_fingerprint": snapshot["fingerprint"],
        }


class _BuiltinHypotheses:
    """Minimal derivation: each obligation yields one claim/counterexample
    pair. Later plans may inject a richer manifest-driven derivation."""

    @property
    def source_id(self) -> str:
        return "review-core-hypotheses"

    @property
    def source_version(self) -> str:
        return "1"

    @property
    def sha256(self) -> str:
        return model.sha256_hex(b"review-core-hypotheses:1")

    def derive(self, *, obligation: dict) -> tuple:
        oid = obligation["obligation_id"]
        sha = self.sha256
        return (
            {
                "hypothesis_id": f"{oid}:claim",
                "polarity": "claim",
                "statement": f"{obligation['category']} holds for {oid}",
                "derivation_policy_sha256": sha,
            },
            {
                "hypothesis_id": f"{oid}:counterexample",
                "polarity": "counterexample",
                "statement": f"{obligation['category']} fails for {oid}",
                "derivation_policy_sha256": sha,
            },
        )


def _default_ingestion_policy() -> store.EvidenceIngestionPolicy:
    mib = 1024 * 1024
    per_kind = {kind: mib for kind in model.EVIDENCE_KINDS}
    for kind, cap in (
        ("tool-transcript", 8 * mib),
        ("check-output", 4 * mib),
        ("authority-manifest-payload", 2 * mib),
        ("authority", 2 * mib),
    ):
        per_kind[kind] = cap
    return store.EvidenceIngestionPolicy(
        source_id="review-core-evidence-ingestion",
        source_version="1",
        sha256=model.sha256_hex(b"review-core-evidence-ingestion:1"),
        per_kind_max_bytes=per_kind,
        transaction_max_bytes=16 * mib,
        review_max_bytes=64 * mib,
        windows_allowed_trustee_sids=(),
        posix_directory_mode=0o700,
        posix_file_mode=0o600,
    )


def load_witness_sources(
    *,
    scratch_dir: Path | None = None,
    review_id: str | None = None,
    runtime: str | None = None,
    acquisition_dir: Path | None = None,
) -> WitnessSources:
    """Composition root.

    Without live kwargs, or off Devin Desktop, returns the fail-closed
    builtin bundle (unchanged Plan-1 behavior). On Devin with ``scratch_dir``
    and ``review_id``, wires the transcript witness verifier; when a produced
    acquisition dir exists (``acquisition_dir`` or the default
    ``<scratch>/acquire/latest``), also wires the live authority-discovery
    source.
    """
    rt = runtime if runtime is not None else detect_runtime()
    if rt != RUNTIME_DEVIN_DESKTOP or scratch_dir is None or review_id is None:
        return WitnessSources(
            policies=policy.PolicyBundle(
                witness_verifier=_FailClosedWitnessVerifier(),
                local_checks=_BuiltinLocalChecks(),
                review_assignments=_BuiltinReviewAssignments(),
                command_execution=_BuiltinCommandExecution(),
                hypotheses=_BuiltinHypotheses(),
            ),
            evidence_ingestion_policy=_default_ingestion_policy(),
        )
    # Lazy import: acquisition depends on this module (EvidenceSource).
    from . import acquisition, witness_log

    scratch = Path(scratch_dir)
    witness_pol = witness_log.TranscriptWitnessPolicy(
        transcript_root=scratch / "transcripts",
        witness_root=scratch / "witness",
    )
    verifier = witness_log.TranscriptWitnessVerifier(
        witness_pol,
        witness_root=scratch / "witness",
        review_id=review_id,
    )
    acq_dir = Path(acquisition_dir) if acquisition_dir is not None else scratch / "acquire" / "latest"
    discovery = None
    if (acq_dir / "data.json").is_file() and (acq_dir / "enumeration.json").is_file():
        discovery = acquisition.LiveAuthorityDiscovery(
            acquisition_dir=acq_dir,
            witness_log_path=scratch / "witness" / "witness-log.jsonl",
            transcript_root=scratch / "transcripts",
            review_id=review_id,
        )
    return WitnessSources(
        policies=policy.PolicyBundle(
            witness_verifier=verifier,
            local_checks=_BuiltinLocalChecks(),
            review_assignments=_BuiltinReviewAssignments(),
            command_execution=_BuiltinCommandExecution(),
            hypotheses=_BuiltinHypotheses(),
            discovery_policy_origin="base-revision",
        ),
        evidence_ingestion_policy=_default_ingestion_policy(),
        authority_discovery=discovery,
    )


# ---------------------------------------------------------------------------
# Internal helpers


_SOURCE_FOR_ACTION = {
    "freeze-review-input": ("authority_discovery", "authority-discovery"),
    "refresh-review-input": ("authority_discovery", "authority-discovery"),
    "run-preflight": ("command_runner", "command-runner"),
    "run-fix-verification": ("command_runner", "command-runner"),
    "run-remote-ci": ("remote_observer", "remote-observer"),
    "seal-green": ("remote_observer", "remote-observer"),
}


def _blocked(action: str, reason: str, missing: tuple, recipe=None, status: str = "blocked") -> policy.Decision:
    return policy.Decision(False, action, reason, missing=missing, recipe=recipe, status=status)


def _decode_payload(payload: TrustedActionPayload, source: str) -> tuple[dict, list, dict]:
    env = model.strict_json_loads(payload.raw_data, source=source)
    if not isinstance(env, dict):
        raise model.StateValidationError("bad-type", "payload", "source payload must be an object")
    data = env.get("data", {})
    witnesses = env.get("witnesses", [])
    if not isinstance(data, dict):
        raise model.StateValidationError("bad-type", "payload.data", "payload data must be an object")
    if not isinstance(witnesses, list):
        raise model.StateValidationError("bad-type", "payload.witnesses", "payload witnesses must be a list")
    extra = {k: v for k, v in env.items() if k not in ("data", "witnesses")}
    return data, witnesses, extra


def _candidate_snapshot_for(action: str, data: dict) -> dict | None:
    """The snapshot a snapshot action's evidence must bind: the one the payload
    itself installs, fingerprint computed exactly as the handler will."""
    key = "replacement_snapshot" if action == "enter-fixing" else "snapshot"
    snap = data.get(key)
    if not isinstance(snap, dict):
        return None
    snap = dict(snap)
    snap["fingerprint"] = model.snapshot_fingerprint(snap)
    return snap


def _register_payload_evidence(
    tx,
    payload: TrustedActionPayload,
    *,
    action: str,
    ingestion: store.EvidenceIngestionPolicy,
    candidate_snapshot: dict | None = None,
) -> dict:
    if not payload.evidence_sources:
        return {}
    snap = candidate_snapshot if candidate_snapshot is not None else tx.candidate["snapshot"]
    epoch = snap["epoch"] if snap else -1
    fp = snap["fingerprint"] if snap else "0" * 64
    requests = tuple(
        store.EvidenceRequest(
            alias=s.alias,
            kind=s.kind,
            source=Path(s.path),
            snapshot_epoch=epoch,
            snapshot_fingerprint=fp,
        )
        for s in payload.evidence_sources
    )
    context = store.EvidenceIngestionContext(
        action=action,
        candidate_snapshot=candidate_snapshot,
        eligible_sources=tuple(Path(s.path) for s in payload.evidence_sources),
    )
    return store.register_evidence_batch(tx, requests, policy=ingestion, context=context)


def _install_witness_records(st: dict, witnesses: list, policies) -> dict:
    """Install subject-bound witness records (authority-discovery,
    remote-observation, human-decision, profile-resolution). Launch,
    completion, and remote-transition witnesses go through their dedicated
    record_* APIs because they also bind a dispatch or ready transition."""
    for rec in witnesses:
        if not isinstance(rec, dict):
            raise model.StateValidationError("bad-type", "witnesses", "witness record must be an object")
        kind = rec.get("kind")
        if kind in ("review-launch", "review-completion", "remote-transition"):
            raise model.StateValidationError(
                "wrong-kind",
                "witnesses",
                f"{kind} witnesses bind through their dedicated flow",
            )
        st = policy.record_witness(st, witness_bytes=model.canonical_json(rec), kind=kind, policies=policies)
    return st


def _verify_records(st: dict, policies, witness_ids, *, dispatch_id=None) -> None:
    for wid in witness_ids:
        if wid is None:
            continue
        rec = st["witness_records"].get(wid)
        if rec is None:
            raise model.StateValidationError(
                "dangling-ref",
                "witness_records",
                f"witness record {wid!r} is not installed",
            )
        policy.verify_witness(st, policies, rec, dispatch_id=dispatch_id)


def _pending_dispatch(st: dict, role: str) -> dict | None:
    """The single current pending dispatch for a role, if exactly one."""
    found = []
    for did, d in st["dispatches"].items():
        if not policy._current(st, d, did):
            continue
        if d["status"] != "pending":
            continue
        rs = st["route_selections"].get(d["route_selection_id"])
        if rs is None or rs["required_role"] != role:
            continue
        found.append(d)
    if len(found) > 1:
        raise model.StateValidationError("conflict", "dispatches", f"multiple pending dispatches for {role!r}")
    return found[0] if found else None


def _launched_dispatch(st: dict, role: str) -> dict | None:
    """The single current launched-but-uncompleted dispatch for a role."""
    found = []
    for did, d in st["dispatches"].items():
        if not policy._current(st, d, did):
            continue
        if d["launch_witness_id"] is None or d["completion_witness_id"] is not None:
            continue
        rs = st["route_selections"].get(d["route_selection_id"])
        if rs is None or rs["required_role"] != role:
            continue
        found.append(d)
    if len(found) > 1:
        raise model.StateValidationError("conflict", "dispatches", f"multiple launched dispatches for {role!r}")
    return found[0] if found else None


def find_pending_dispatch_id(state: dict, role: str) -> str | None:
    d = _pending_dispatch(state, role)
    return d["dispatch_id"] if d else None


def _commit_if_changed(tx, before: dict, result: EngineResult) -> EngineResult:
    """Commit exactly once when the mutation changed the candidate; an
    identical replay leaves the file and generation untouched."""
    if tx.candidate is before or tx.candidate == before:
        return result
    committed = tx.commit()
    return EngineResult(result.decision, committed["generation"], result.state_path)


# ---------------------------------------------------------------------------
# Transactions


def init_review(state_path: Path, *, review_id: str, scratch_dir: Path, apply: bool) -> EngineResult:
    """Create the validated generation-0 intake state. Without ``apply`` this
    is a check: the would-be state is built and validated but nothing is
    written."""
    sp = Path(state_path)
    state = model.new_state(review_id, Path(scratch_dir))
    model.validate_state(state)
    if not apply:
        return EngineResult(
            policy.Decision(True, "init", "would create version-2 intake state", status="check"),
            0,
            sp,
        )
    store.create_state(sp, state)
    return EngineResult(
        policy.Decision(True, "init", "created version-2 intake state", status="applied"),
        0,
        sp,
    )


def validate_state_file(state_path: Path) -> EngineResult:
    state = store.load_state(state_path)
    return EngineResult(
        policy.Decision(True, "validate", "state is a valid version-2 state"),
        state["generation"],
        Path(state_path),
    )


def next_action_for(state_path: Path, *, policies) -> EngineResult:
    state = store.load_state(state_path)
    decision = policy.next_action(state, policies=policies)
    if decision.recipe is not None and decision.action in policy.ACTION_PAYLOAD_KEYS:
        decision = policy.Decision(
            decision.allowed,
            decision.action,
            decision.reason,
            missing=decision.missing,
            recipe=policy.action_recipe(decision.action, state_path=str(state_path)),
            status=decision.status,
        )
    return EngineResult(decision, state["generation"], Path(state_path))


def register_dispatch_transaction(state_path: Path, *, action: str, sources: WitnessSources) -> EngineResult:
    """Phase 1 of a reviewer dispatch: persist the pending dispatch plus its
    resolved profile identity, verified against the profile-resolution
    witness. Idempotent for the same pending intent."""
    sp = Path(state_path)
    recipe = policy.action_recipe(action)
    if recipe.required_role is None:
        raise model.StateValidationError("unlawful-transition", "action", f"{action!r} is not a dispatch action")
    with store.locked_state_transaction(sp) as tx:
        before = tx.candidate
        st = before
        if _pending_dispatch(st, recipe.required_role) is not None:
            return EngineResult(
                policy.Decision(
                    True,
                    action,
                    "dispatch intent already registered for this role",
                    recipe=recipe,
                ),
                tx.prior_generation,
                sp,
            )
        if sources.reviewer_dispatch is None:
            return EngineResult(
                _blocked(
                    action,
                    "no reviewer-dispatch source is wired",
                    ("missing-witness-source:reviewer-dispatch",),
                    recipe=recipe,
                ),
                tx.prior_generation,
                sp,
            )
        payload = sources.reviewer_dispatch.resolve_profile(action=action, recipe=recipe)
        data, witnesses, _extra = _decode_payload(payload, f"payload:{action}")
        missing_keys = {"route_selection", "dispatch"} - set(data)
        if missing_keys:
            raise model.StateValidationError(
                "missing-field",
                "payload.data",
                f"resolve_profile payload lacks {sorted(missing_keys)}",
            )
        registrations = _register_payload_evidence(
            tx,
            payload,
            action=action,
            ingestion=sources.evidence_ingestion_policy,
        )
        resolved = store.resolve_evidence_aliases(data, registrations)
        st = _install_witness_records(st, witnesses, sources.policies)
        st = policy.register_dispatch(
            st,
            route_selection=resolved["route_selection"],
            dispatch=resolved["dispatch"],
            policies=sources.policies,
        )
        new_ids = set(st["dispatches"]) - set(before["dispatches"])
        if len(new_ids) != 1:
            raise model.StateValidationError(
                "state-invalid",
                "dispatches",
                "register_dispatch did not add exactly one dispatch",
            )
        d = st["dispatches"][next(iter(new_ids))]
        # Verify the freshly bound profile-resolution witness. Its subject
        # covers the route selection, not the dispatch id (the dispatch id
        # derives from the intent that embeds this witness), so no
        # dispatch-scope check applies here.
        _verify_records(st, sources.policies, [d["profile_resolution_witness_id"]])
        st = store.append_history(
            st,
            event="register-dispatch",
            data_sha256=model.sha256_hex(payload.raw_data),
        )
        tx.candidate = st
        committed = tx.commit()
        return EngineResult(
            policy.Decision(
                True,
                action,
                f"registered pending dispatch {d['dispatch_id']}",
                recipe=recipe,
            ),
            committed["generation"],
            sp,
        )


def launch_transaction(state_path: Path, *, dispatch_id: str, sources: WitnessSources) -> EngineResult:
    """Phase 2 of a reviewer dispatch: verify and persist the witnessed
    launch (tool_use_id, verbatim task bytes, resolved profile name)."""
    sp = Path(state_path)
    with store.locked_state_transaction(sp) as tx:
        st = tx.candidate
        d = st["dispatches"].get(dispatch_id)
        if d is None or not policy._current(st, d, dispatch_id):
            raise model.StateValidationError("dangling-ref", "dispatch_id", f"unknown dispatch {dispatch_id!r}")
        if d["launch_witness_id"] is not None:
            return EngineResult(
                policy.Decision(True, "launch", f"dispatch {dispatch_id} already launched"),
                tx.prior_generation,
                sp,
            )
        if sources.reviewer_dispatch is None:
            return EngineResult(
                _blocked(
                    "launch",
                    "no reviewer-dispatch source is wired",
                    ("missing-witness-source:reviewer-dispatch",),
                ),
                tx.prior_generation,
                sp,
            )
        payload = sources.reviewer_dispatch.launch(dispatch=dict(d))
        data, witnesses, _extra = _decode_payload(payload, "payload:launch")
        launch_records = [r for r in witnesses if isinstance(r, dict) and r.get("kind") == "review-launch"]
        if len(launch_records) != 1:
            raise model.StateValidationError(
                "missing-field",
                "payload.witnesses",
                "launch payload must carry exactly one review-launch witness",
            )
        _register_payload_evidence(
            tx,
            payload,
            action="launch",
            ingestion=sources.evidence_ingestion_policy,
        )
        st = policy.record_launch(
            st,
            dispatch_id=dispatch_id,
            launch_witness_bytes=model.canonical_json(launch_records[0]),
            policies=sources.policies,
        )
        _verify_records(
            st,
            sources.policies,
            [st["dispatches"][dispatch_id]["launch_witness_id"]],
            dispatch_id=dispatch_id,
        )
        st = store.append_history(
            st,
            event="record-launch",
            data_sha256=model.sha256_hex(payload.raw_data),
        )
        tx.candidate = st
        committed = tx.commit()
        return EngineResult(
            policy.Decision(True, "launch", f"dispatch {dispatch_id} launched"),
            committed["generation"],
            sp,
        )


def complete_transaction(
    state_path: Path,
    *,
    action: str,
    caller_data_bytes: bytes,
    caller_evidence: tuple[EvidenceSource, ...],
    sources: WitnessSources,
) -> EngineResult:
    """Complete one action inside a single locked transaction.

    Source-backed actions acquire their payload from the wired source;
    caller-supplied data/evidence for them must be empty (caller bytes are
    never provenance). Caller-data actions ingest ``caller_data_bytes`` plus
    declared evidence files. ``mark-ready-for-ci`` delegates to the
    two-phase ready-transition flow, which commits once per phase: one
    CLI call advances generation by two and appends two history records
    (``complete`` for the intent, ``finalize-ready-transition`` for the
    witnessed transition). Each phase is independently durable and
    idempotent, so the double increment is deliberate.
    """
    sp = Path(state_path)
    recipe = policy.action_recipe(action)
    if action == "mark-ready-for-ci":
        if caller_data_bytes not in (b"", b"{}"):
            raise model.StateValidationError("caller-provenance", "data", "mark-ready-for-ci takes no caller data")
        if caller_evidence:
            raise model.StateValidationError(
                "caller-provenance",
                "evidence",
                "mark-ready-for-ci takes no caller evidence",
            )
        registered = register_ready_transition_transaction(sp, sources=sources)
        if not registered.decision.allowed:
            return registered
        state = store.load_state(sp)
        ready = state["ready_transition"]
        if ready is None:
            raise model.StateValidationError("state-invalid", "ready_transition", "register left no intent")
        return finalize_ready_transition_transaction(
            sp, ready_transition_id=ready["ready_transition_id"], sources=sources
        )

    with store.locked_state_transaction(sp) as tx:
        before = tx.candidate
        st = before
        source_name = None
        if recipe.required_role is not None:
            source_name = "reviewer-dispatch"
            src = sources.reviewer_dispatch
        elif action in _SOURCE_FOR_ACTION:
            attr, source_name = _SOURCE_FOR_ACTION[action]
            src = getattr(sources, attr)
        else:
            src = None
        if recipe.required_role is not None or action in _SOURCE_FOR_ACTION:
            if caller_data_bytes not in (b"", b"{}"):
                raise model.StateValidationError(
                    "caller-provenance",
                    "data",
                    f"{action} takes no caller data; its payload is source-witnessed",
                )
            if caller_evidence:
                raise model.StateValidationError(
                    "caller-provenance",
                    "evidence",
                    f"{action} takes no caller evidence; its payload is source-witnessed",
                )
            if src is None:
                return EngineResult(
                    _blocked(
                        action,
                        f"no {source_name} source is wired",
                        (f"missing-witness-source:{source_name}",),
                        recipe=recipe,
                    ),
                    tx.prior_generation,
                    sp,
                )

        witness_ids: list[str] = []
        if recipe.required_role is not None:
            d = _launched_dispatch(st, recipe.required_role)
            if d is None:
                return EngineResult(
                    _blocked(
                        action,
                        f"no launched dispatch awaits completion for role {recipe.required_role!r}",
                        ("dispatch-launch",),
                        recipe=recipe,
                    ),
                    tx.prior_generation,
                    sp,
                )
            payload = sources.reviewer_dispatch.collect(dispatch=dict(d))
            data, witnesses, extra = _decode_payload(payload, f"payload:{action}")
            completions = [r for r in witnesses if isinstance(r, dict) and r.get("kind") == "review-completion"]
            if len(completions) != 1:
                raise model.StateValidationError(
                    "missing-field",
                    "payload.witnesses",
                    "collect payload must carry exactly one review-completion witness",
                )
            registrations = _register_payload_evidence(
                tx,
                payload,
                action=action,
                ingestion=sources.evidence_ingestion_policy,
            )
            resolved = store.resolve_evidence_aliases(data, registrations)
            st = policy.record_completion(
                st,
                dispatch_id=d["dispatch_id"],
                completion_witness_bytes=model.canonical_json(completions[0]),
                transcript_sha256=extra.get("transcript_sha256"),
                policies=sources.policies,
            )
            witness_ids.append(st["dispatches"][d["dispatch_id"]]["completion_witness_id"])
            st = policy.complete_action(
                st,
                action=action,
                raw_data=model.canonical_json(resolved),
                policies=sources.policies,
            )
        elif action in _SOURCE_FOR_ACTION:
            attr, _kind = _SOURCE_FOR_ACTION[action]
            src = getattr(sources, attr)
            if action == "seal-green":
                # The seal derives only from state; remote_observer must exist
                # because sealing stays blocked until Plan 6 wires it, but no
                # presentation-time observation is consumed here.
                payload = TrustedActionPayload(b'{"data": {}, "witnesses": []}')
            elif attr == "authority_discovery":
                payload = src.acquire(action=action, current_snapshot=st["snapshot"])
            elif attr == "command_runner":
                intents = [
                    sources.policies.command_execution.command_intent(snapshot=st["snapshot"], item=i)
                    for i in sources.policies.local_checks.items
                ]
                payload = src.run(
                    action=action,
                    command_intent={"action": action, "commands": intents},
                )
            else:
                payload = src.observe(action=action, snapshot=st["snapshot"])
            data, witnesses, _extra = _decode_payload(payload, f"payload:{action}")
            candidate_snapshot = _candidate_snapshot_for(action, data)
            registrations = _register_payload_evidence(
                tx,
                payload,
                action=action,
                ingestion=sources.evidence_ingestion_policy,
                candidate_snapshot=candidate_snapshot,
            )
            resolved = store.resolve_evidence_aliases(data, registrations)
            if attr == "authority_discovery":
                # Freeze/refresh witnesses bind the snapshot the payload
                # installs, so the handler installs them inside the same
                # validated transition rather than before it.
                resolved = {**resolved, "witnesses": witnesses}
            else:
                # Other source payloads reference their witness ids, so the
                # witnesses must land first.
                st = _install_witness_records(st, witnesses, sources.policies)
            st = policy.complete_action(
                st,
                action=action,
                raw_data=model.canonical_json(resolved),
                policies=sources.policies,
            )
            witness_ids.extend(set(st["witness_records"]) - set(before["witness_records"]))
        else:
            payload = TrustedActionPayload(caller_data_bytes, tuple(caller_evidence))
            data = model.strict_json_loads(bytes(caller_data_bytes), source=f"data:{action}")
            if not isinstance(data, dict):
                raise model.StateValidationError("bad-type", "data", "action payload must be an object")
            candidate_snapshot = _candidate_snapshot_for(action, data)
            registrations = _register_payload_evidence(
                tx,
                payload,
                action=action,
                ingestion=sources.evidence_ingestion_policy,
                candidate_snapshot=candidate_snapshot,
            )
            resolved = store.resolve_evidence_aliases(data, registrations)
            st = policy.complete_action(
                st,
                action=action,
                raw_data=model.canonical_json(resolved),
                policies=sources.policies,
            )

        _verify_records(st, sources.policies, witness_ids)
        tx.candidate = st
        return _commit_if_changed(
            tx,
            before,
            EngineResult(
                policy.Decision(
                    True,
                    action,
                    f"action {action} recorded",
                    recipe=recipe,
                    status="applied",
                ),
                tx.prior_generation,
                sp,
            ),
        )


def register_ready_transition_transaction(state_path: Path, *, sources: WitnessSources) -> EngineResult:
    """Phase 1 of mark-ready-for-ci: observe the current remote lifecycle,
    then persist the exact pending intent for this snapshot."""
    sp = Path(state_path)
    action = "mark-ready-for-ci"
    recipe = policy.action_recipe(action)
    with store.locked_state_transaction(sp) as tx:
        st = tx.candidate
        ready = st["ready_transition"]
        if (
            ready is not None
            and ready["status"] == "pending"
            and policy._current(st, ready, ready["ready_transition_id"])
        ):
            return EngineResult(
                policy.Decision(
                    True,
                    action,
                    "ready-transition intent already registered",
                    recipe=recipe,
                ),
                tx.prior_generation,
                sp,
            )
        if action not in policy.lawful_actions(st, policies=sources.policies):
            raise model.StateValidationError(
                "unlawful-transition",
                "action",
                "mark-ready-for-ci is not lawful in the current state",
            )
        if sources.remote_transition is None:
            return EngineResult(
                _blocked(
                    action,
                    "no remote-transition source is wired",
                    ("missing-witness-source:remote-transition",),
                    recipe=recipe,
                ),
                tx.prior_generation,
                sp,
            )
        snap = st["snapshot"]
        intent = {
            "idempotency_key": policy.ready_idempotency_key(st),
            "repository_id": snap["repository_id"],
            "pr_number": snap["pr_number"],
            "head_sha": snap["head_sha"],
            "expected_lifecycle_state": "ready",
        }
        observed = sources.remote_transition.observe_lifecycle(intent=intent)
        data, witnesses, _extra = _decode_payload(observed, "payload:mark-ready-for-ci")
        prior = data.get("prior_lifecycle_state")
        if not isinstance(prior, str) or not prior:
            raise model.StateValidationError(
                "missing-field",
                "payload.data.prior_lifecycle_state",
                "observe_lifecycle must report the observed prior lifecycle",
            )
        registrations = _register_payload_evidence(
            tx,
            observed,
            action=action,
            ingestion=sources.evidence_ingestion_policy,
        )
        if registrations:
            store.resolve_evidence_aliases(data, registrations)
        st = _install_witness_records(st, witnesses, sources.policies)
        st = policy.complete_action(
            st,
            action=action,
            raw_data=model.canonical_json({"prior_lifecycle_state": prior}),
            policies=sources.policies,
        )
        tx.candidate = st
        committed = tx.commit()
        return EngineResult(
            policy.Decision(True, action, "ready-transition intent registered", recipe=recipe),
            committed["generation"],
            sp,
        )


def finalize_ready_transition_transaction(
    state_path: Path, *, ready_transition_id: str, sources: WitnessSources
) -> EngineResult:
    """Phase 2 of mark-ready-for-ci: perform or confirm the remote
    transition and persist its witnessed outcome, materializing the CI
    candidate. Accepts a verified already-ready no-op for the same
    idempotency key and head.

    No fresh lawful-action check happens here by design: the pending
    intent was registered inside a lawful-action window (phase 1), and
    this phase only executes that bound intent on its recorded head.
    This is sound because state changes that should block a pending
    intent are all caught before execution: new findings, invalidated
    proofs, and snapshot drift advance ``snapshot_epoch``/
    ``snapshot_fingerprint`` or invalidate the ``ready_transition`` id
    through a repair cut, so ``_current`` below rejects any intent
    minted before such a change; an opened blocker does not advance
    the epoch, so the explicit ``status == "blocked"`` guard below
    refuses finalization while a blocker is active.
    """
    sp = Path(state_path)
    action = "mark-ready-for-ci"
    with store.locked_state_transaction(sp) as tx:
        st = tx.candidate
        ready = st["ready_transition"]
        if (
            ready is None
            or ready["ready_transition_id"] != ready_transition_id
            or not policy._current(st, ready, ready_transition_id)
        ):
            raise model.StateValidationError(
                "dangling-ref",
                "ready_transition_id",
                f"unknown ready transition {ready_transition_id!r}",
            )
        if ready["status"] == "completed":
            return EngineResult(
                policy.Decision(
                    True,
                    action,
                    f"ready transition {ready_transition_id} already completed",
                ),
                tx.prior_generation,
                sp,
            )
        if st["status"] == "blocked":
            return EngineResult(
                _blocked(
                    action,
                    "review is blocked; resume requires blocker-resolution evidence",
                    ("active-blocker",),
                ),
                tx.prior_generation,
                sp,
            )
        if sources.remote_transition is None:
            return EngineResult(
                _blocked(
                    action,
                    "no remote-transition source is wired",
                    ("missing-witness-source:remote-transition",),
                ),
                tx.prior_generation,
                sp,
            )
        payload = sources.remote_transition.apply_or_confirm_ready(intent=dict(ready))
        data, witnesses, _extra = _decode_payload(payload, "payload:mark-ready-for-ci")
        transitions = [r for r in witnesses if isinstance(r, dict) and r.get("kind") == "remote-transition"]
        if len(transitions) != 1:
            raise model.StateValidationError(
                "missing-field",
                "payload.witnesses",
                "apply_or_confirm_ready must carry exactly one remote-transition witness",
            )
        prior = data.get("observed_prior_lifecycle")
        if not isinstance(prior, str) or not prior:
            raise model.StateValidationError(
                "missing-field",
                "payload.data.observed_prior_lifecycle",
                "apply_or_confirm_ready must report the observed prior lifecycle",
            )
        _register_payload_evidence(
            tx,
            payload,
            action=action,
            ingestion=sources.evidence_ingestion_policy,
        )
        st = policy.record_ready_transition(
            st,
            transition_witness_bytes=model.canonical_json(transitions[0]),
            observed_prior_lifecycle=prior,
            policies=sources.policies,
        )
        _verify_records(
            st,
            sources.policies,
            [st["ready_transition"]["transition_witness_id"]],
        )
        st = store.append_history(
            st,
            event="finalize-ready-transition",
            data_sha256=model.sha256_hex(payload.raw_data),
        )
        tx.candidate = st
        committed = tx.commit()
        return EngineResult(
            policy.Decision(
                True,
                action,
                f"ready transition {ready_transition_id} completed",
            ),
            committed["generation"],
            sp,
        )


def block_transaction(
    state_path: Path,
    *,
    blocker_class: str,
    reason: str,
    evidence: tuple[EvidenceSource, ...],
    sources: WitnessSources,
) -> EngineResult:
    sp = Path(state_path)
    if blocker_class not in model.BLOCKER_CLASSES:
        raise model.StateValidationError("bad-enum", "class", f"unknown blocker class {blocker_class!r}")
    with store.locked_state_transaction(sp) as tx:
        st = tx.candidate
        payload = TrustedActionPayload(b"{}", tuple(evidence))
        registrations = _register_payload_evidence(
            tx,
            payload,
            action="block-review",
            ingestion=sources.evidence_ingestion_policy,
        )
        evidence_ids = sorted(r.evidence_id for r in registrations.values())
        blocker_id = "blocker:" + model.sha256_json(
            {
                "class": blocker_class,
                "reason": reason,
                "evidence_ids": evidence_ids,
                "seq": len(st["history"]) + 1,
            }
        )
        st = policy.block_review(
            st,
            blocker_id=blocker_id,
            blocker_class=blocker_class,
            reason=reason,
            evidence_ids=evidence_ids,
        )
        tx.candidate = st
        committed = tx.commit()
        return EngineResult(
            policy.Decision(True, "block", f"blocker {blocker_id} opened"),
            committed["generation"],
            sp,
        )


def resume_transaction(
    state_path: Path,
    *,
    blocker_id: str,
    resolution_evidence: tuple[EvidenceSource, ...],
    sources: WitnessSources,
) -> EngineResult:
    """Resume through the resume-review action so the lawful-action gate
    applies. Resolution files are caller evidence resolved to ids."""
    data = {
        "blocker_id": blocker_id,
        "resolution_evidence_ids": [f"@{e.alias}" for e in resolution_evidence],
    }
    return complete_transaction(
        state_path,
        action="resume-review",
        caller_data_bytes=model.canonical_json(data),
        caller_evidence=tuple(resolution_evidence),
        sources=sources,
    )
