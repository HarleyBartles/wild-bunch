# ADR-0037 Ambient Capabilities, Opt-In Standards, and Decision Documentation

## Status

`partially superseded`

## Dated Status History

- 2026-09-29 - live: Wild Bunch adopts seven Marketplace operating standards explicitly, treats ambient plugin skills as capabilities rather than subscriptions, retires generated index mesh navigation, and moves the ADR home to `docs/decisions/`.
- 2026-10-02 - superseded in part: `.agents/contracts/operating-standards.json` and `.agents/contracts/standards-certification.md` replace the seven-item deployment model with six immutable self-certification references. Repository-owned checks replace the Marketplace deployment runtime. The native Codex plugin catalog remains separate. The ADR location and retirement of generated index navigation remain live.
- 2026-10-07 - clarified: the decision catalogue is authored, and the generated freshness table was retired because the latest history date does not establish semantic review.
- 2026-10-08 - superseded in part: ADR-0043 establishes the command bus as the sole agent-facing home for supported repository validation and makes the canonical CI gate check-only, fail-fast, and ordered from cheapest to most expensive. The historical deployment and routing decisions above remain as recorded.

## Decision Type

process, documentation

## Related ADRs

- `supersedes`: ADR-0033 (repository documentation mesh posture)
- `partially superseded by`: ADR-0043 (repository command bus ownership and gate order)
- `related to`: ADR-0001 (Markdown ADR log)

## Context

Marketplace now distinguishes ambient capabilities from installed plugin subscriptions and supports explicit selection of operating standards. Wild Bunch had subscribed to workflow plugins primarily to make their skills available, used a legacy all-in-one standards deployment, and maintained generated `INDEX.md` navigation across the repository. Those mechanisms coupled repository validation and guidance to ambient skill projections and imposed a broad index maintenance obligation.

The decision log also lived at `docs/adr/`, while the Marketplace model treats durable repository standards and documentation as opt-in, explicitly owned resources. The migration needs durable policy for the standards Wild Bunch depends on, a human-facing location for decisions, and a capability contract that remains clear when no ambient provider is available.

## Decision

At the time of this decision, Wild Bunch explicitly adopted these Marketplace standards:

- `marketplace-skill-management`
- `root-agent-router`
- `runbook-composition`
- `playbook-composition`
- `tracked-validation-hook`
- `markdown-formatting`
- `completed-artifact-custody`

The repository then removed subscriptions to `agent-operating-model`, `repo-worker-pack`, `superpowers-plus`, `mcp-usage-pack`, `unslop-plus`, and `writing-pack`. It retained `dotnet-pack`, `architecture-pack`, `frontend-pack`, the local `game-studio` plugin, and repository-owned skills listed in `repo.local_skills`.

Runbooks and playbooks state required and optional workflow capabilities independently of provider names. A workflow stops when a required capability has no suitable provider and skips optional work when its capability is unavailable. Exact skill names remain appropriate for repository-owned skills.

The generated index mesh and its generators, validation, wrappers, and runner integration are retired. Wild Bunch does not replace the mesh with another generated directory inventory. Root and scoped `AGENTS.md`, runbooks, playbooks, and human-facing READMEs remain the maintained routing surfaces.

The ADR home is `docs/decisions/`. Its README owns the human-readable decision log guidance and a generated status/review-date table maintained by a focused updater. That updater does not produce navigation indexes.

At the time, the tracked pre-commit hook and hosted CI used the same checked-in standards deployment and consumer command declaration. The successor migration removes that deployment and its source submodule. The repository-owned script, .NET, web, and whitespace validation lanes remain in `tools/run.py ci --apply/--check`.

## Consequences

- Standards are selected explicitly and can be audited from one contract and its deployment provenance.
- Removing ambient subscriptions no longer removes a hidden runtime dependency from the canonical validation path.
- Workflow guidance describes required and optional capabilities while remaining provider-agnostic.
- The repository no longer maintains generated `INDEX.md` files or a replacement mesh.
- At the time, ADR records and their generated freshness table shared `docs/decisions/README.md`; the table was retired on 2026-10-07 because history dates do not establish semantic review.
- Repository routing remains authored and intentionally scoped; contributors do not need to update a generated whole-tree inventory.

## Evidence Surface

Implementation is represented by `.agents/contracts/operating-standards.json`, `.agents/plugins/marketplace.json`, `.agents/runbooks/`, `.agents/playbooks/`, `tools/run.py`, the tracked hook, hosted CI, and `docs/decisions/README.md`.
