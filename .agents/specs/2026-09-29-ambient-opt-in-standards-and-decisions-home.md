# Ambient Plugins, Opt-In Standards, and Decision Documentation

**Status:** Approved design

## Goal

Move Wild Bunch from ambient plugin subscriptions and the legacy all-in-one
repository standards runner to the Marketplace's ambient-capability and
opt-in operating-standards model. In the same slice, retire the generated
index mesh and move the architecture decision record home from `docs/adr/` to
`docs/decisions/`, updating links throughout the repository.

## Decisions

### Ambient capabilities and plugin subscriptions

Treat these Marketplace plugins as ambient capabilities and remove their
repository subscriptions and refreshed skill projections:

- `agent-operating-model`
- `repo-worker-pack`
- `superpowers-plus`
- `mcp-usage-pack`
- `unslop-plus`
- `writing-pack`

Keep the explicit subscriptions to `dotnet-pack`, `architecture-pack`, and
`frontend-pack`; keep the local `game-studio` plugin and all exact names in
`repo.local_skills`. Refresh must preserve these selected projections and
repository-owned skills while pruning projections and provenance owned by the
removed subscriptions.

When a runbook or playbook depends on an ambient capability, describe it under
`Required capabilities` or `Optional capabilities`, without naming an ambient
provider skill. At runtime, the agent selects an available provider. If a
required capability has no suitable provider, work stops before its dependent
action; an unavailable optional capability may be skipped and reported. Exact
skill names remain appropriate for skills genuinely owned by this repository.

### Adopted operating standards

Declare these Marketplace standards in
`.agents/contracts/operating-standards.json`:

- `marketplace-skill-management`
- `root-agent-router`
- `runbook-composition`
- `playbook-composition`
- `tracked-validation-hook`
- `markdown-formatting`
- `completed-artifact-custody`

These standards protect the source pin and subscription declaration, agent
entry routing, workflow composition, staged-snapshot validation, maintained
Markdown, and retirement of completed planning artifacts. Do not adopt
`review-entrypoint`, `contribution-entrypoint`, or `root-gitignore-hygiene` in
this migration; their current value is limited to boilerplate presence or
stale-tooling cleanup. The game build, test, and web checks remain
Wild-Bunch-owned CI checks rather than Marketplace standards.

The upstream legacy mapper infers adopted standards from enabled legacy
surfaces. Before using it, record exceptions for exactly `review-entry`,
`contributing-entry`, and `root-gitignore` in the legacy
`.agents/contracts/agent-operating-model.json`, then preview the mapping and
confirm it yields exactly the seven selected standards. Do not activate the
mapper's default inference without this check.

Deploy each selected standard's checker inputs and runtime from the pinned
Marketplace source into the consumer-owned `.agents/standards/` tree. Record
source revision and resource hashes using the upstream deployment contract.
The complete standard selection is the consumer's explicit adoption state;
plugin installation state must neither add nor remove standards.

### Validation and refresh ownership

Keep `tools/run.py ci --apply` and `tools/run.py ci --check` as Wild Bunch's
canonical commands. They will dispatch only the adopted standards through the
deployed runtime, refresh skill projections from the pinned Marketplace
submodule without rolling that source pin, run repository-owned script checks,
and retain the existing .NET, web, and whitespace validation lanes.

The tracked pre-commit hook remains the local and hosted full-gate entrypoint.
Its declared generated paths must cover refreshed skill additions and
deletions, deployed standards, and the ADR freshness README when updated. It
must not stage or expect generated `INDEX.md` or `INDEX.json` files. Hosted CI
uses the checked-in standard implementations and does not depend on Codex,
ambient skills, or installed projections.

Migrate using the upstream compatibility sequence: preview legacy-to-new
standard mapping after setting the three legacy exceptions above; preview
selected-resource deployment; deploy selected implementations from the
pinned source; apply the new standards declaration; verify the declaration
and deployment; then switch the consumer runner and hook contract. Refresh
skills through the pinned submodule's
`refreshing-installed-skills` implementation with
`--no-roll-marketplace-source`. Do not invoke legacy operating-model or mesh
scripts after their projections have been removed.

### Documentation home and index mesh retirement

Rename `docs/decisions/` to `docs/decisions/`. Preserve its ADR records, README, and
template. Update repository guidance, scripts, tests, ADR cross-links, and
other references so current links resolve under `docs/decisions/`; do not
leave a compatibility directory or redirect at `docs/decisions/`.

Remove the generated index mesh completely. Delete tracked mesh-generated
`INDEX.md` files and the mesh generation, validation, custom post-processing,
wrappers, and runner wiring. Remove mesh-only generated-path declarations and
update guidance that currently calls for index regeneration. The mesh audit
found that its other index sections are generated file or directory listings
and do not carry durable policy.

Preserve the ADR freshness table because it reports decision status and last
review dates, which are distinct from mesh navigation. Move that table into
`docs/decisions/README.md` and retain a narrowly scoped ADR freshness updater
and check. It must not generate indexes or depend on the retired mesh skill.

Promote surviving policy from mesh-specific guidance to its current owner:
repository skill custody belongs in repository-skill doctrine, completed
artifact custody in its doctrine, and document placement in the appropriate
repository guidance. Remove mesh freshness rules and stale claims that
generated indexes are operative or required.

## Non-goals

- Do not change gameplay, application architecture, asset production, or
  browser behavior.
- Do not remove the locally authored game-studio plugin or repository-owned
  skills.
- Do not remove the explicitly retained .NET, architecture, or frontend skill
  packs.
- Do not create a replacement index generator, directory inventory, or
  `docs/decisions/` compatibility surface.
- Do not change Marketplace source or publish Marketplace artifacts; this is a
  consumer-repository migration.
- Do not weaken the staged-snapshot hook, hosted parity, repository-owned
  validation, or the rule that missing required workflow capabilities stop
  dependent work.

## Affected surfaces

- `.agents/plugins/marketplace-source` submodule gitlink and
  `.agents/plugins/marketplace.json`
- `.agents/contracts/operating-standards.json`, legacy operating-model
  contract, and `.agents/standards/` deployed resources and provenance
- `.agents/contracts/repo-standards-commands.json`, `tools/run.py`,
  `githooks/pre-commit`, and `.github/workflows/ci.yml`
- `.agents/skills/` refresh outputs and provenance, with authored local skills
  preserved according to `repo.local_skills`
- `.agents/runbooks/`, `.agents/playbooks/`, and current repository guidance
  that names ambient provider skills or generated indexes
- `docs/decisions/` to `docs/decisions/`, all references to the old home, ADR
  freshness maintenance, and all mesh-generated `INDEX.md` files
- Mesh-specific scripts, wrappers, tests, generated-path declarations, and
  repository guidance

## Acceptance criteria

1. The Marketplace submodule is pinned to the current Marketplace main revision
   that contains the accepted ambient-plugin and opt-in-standards migration.
2. The six ambient plugins are absent from Wild Bunch's subscription list and
   their generated projections are pruned; the three retained Marketplace
   packs, local plugin, and exact local skill declarations remain valid and
   refreshable.
3. Workflows that depend on ambient capabilities declare them as required or
   optional without depending on ambient skill names or copied projection
   paths. Missing required capabilities have an explicit stop condition;
   exact names remain valid for repository-owned skills.
4. The operating-standards declaration selects exactly the seven standards
   listed above. Their deployed checker resources and provenance agree with
   the pinned Marketplace revision.
5. The canonical apply/check commands, tracked hook, and hosted CI operate
   without ambient plugins or installed skill projections for standards
   execution, and dispatch no unselected Marketplace standards.
6. Existing Wild Bunch validation remains composed into the canonical gate,
   including repository-owned checks, .NET build/tests, web typecheck/tests/
   build, and diff whitespace validation.
7. The tracked index mesh, its generator/validator/post-processor, wrappers,
   and runner integration are removed. No workflow, contract, or guidance
   requires `INDEX.md` or `INDEX.json` generation.
8. All ADR records and templates live under `docs/decisions/`; all current
   repository links and maintained path references point there, with no
   `docs/decisions/` compatibility tree.
9. `docs/decisions/README.md` retains the ADR status/freshness table and its
   focused updater/check without recreating mesh navigation.
10. Surviving source-of-truth, skill-custody, completed-artifact, and document
    placement rules have current owners and do not depend on retired mesh
    guidance.

## Validation evidence

Use the consumer's focused migration checks while composing the new runner,
then the canonical `py -3 tools/run.py ci --check` gate after the complete
consumer transition. Validate the staged-snapshot hook and hosted CI path
against the same pinned deployed standards. Confirm all renamed decision links
resolve and that no operative source retains an obsolete `docs/decisions/` path or
index-mesh dependency. Preserve the existing repository test ownership rules;
do not add tautological or change-detector tests.

## Planning handoff

Implementation is expected to proceed as one consumer migration with ordered
compatibility steps: source refresh and preview, selected-standard deployment,
subscription refresh, runner/hook cutover, capability-guidance migration,
index-mesh retirement and decisions-home rename, then focused and canonical
validation. The implementation plan must preserve this order so the old
runner is not invoked after its ambient skill projections are pruned.
