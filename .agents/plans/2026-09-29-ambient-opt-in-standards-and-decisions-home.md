# Ambient Opt-In Standards and Decisions Home Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Migrate Wild Bunch to explicit Marketplace standards and ambient capabilities, retire the generated index mesh, and establish `docs/decisions/` as the ADR home.

**Architecture:** Keep Wild Bunch's canonical `tools/run.py ci --apply/--check` interface while replacing the all-in-one projected runner with only the seven explicitly adopted deployed standards. Refresh skills from the pinned Marketplace submodule and preserve repository-owned validation; resolve refresh helper dependencies before pruning ambient projections. In the same ordered slice, remove mesh machinery and move decision records and freshness maintenance to `docs/decisions/`.

**Tech Stack:** Python 3, PowerShell and shell wrappers, Git submodule and JSON contracts, Markdown, tracked pre-commit hook, GitHub Actions, .NET, and the existing web toolchain.

**Spec:** `.agents/specs/2026-09-29-ambient-opt-in-standards-and-decisions-home.md`

**Execution Strategy:** `executing-plans` ; the runner, staged hook, standards deployment, and skill refresh share one compatibility boundary; pruning ambient projections before that boundary works would break the same canonical gate. Inline sequential execution preserves shared state and lets the implementer validate the whole transition continuously. `subagent-driven-development` is the nearest alternative, but per-task handoffs add context reconstruction across these dependent changes without enabling meaningful parallel implementation.

## Global Constraints

- Remove subscriptions and refreshed projections for `agent-operating-model`, `repo-worker-pack`, `superpowers-plus`, `mcp-usage-pack`, `unslop-plus`, and `writing-pack`.
- Retain `dotnet-pack`, `architecture-pack`, `frontend-pack`, local `game-studio`, and exact `repo.local_skills` entries.
- Adopt exactly `marketplace-skill-management`, `root-agent-router`, `runbook-composition`, `playbook-composition`, `tracked-validation-hook`, `markdown-formatting`, and `completed-artifact-custody`.
- Set exactly the three legacy mapping exceptions `review-entry`, `contributing-entry`, and `root-gitignore` before previewing the legacy mapper.
- Keep `tools/run.py ci --apply` and `tools/run.py ci --check` as the canonical commands; preserve repository-owned script, .NET, web, and whitespace checks.
- Keep the tracked staged-snapshot hook and hosted CI equivalent; neither may depend on ambient plugins or installed skill projections for standard execution.
- Do not create an index mesh replacement, an `INDEX.json`, or a `docs/decisions/` compatibility surface.
- Do not change gameplay, application architecture, asset production, or browser behavior, and do not change Marketplace source or publish Marketplace artifacts.
- Preserve fail-closed behavior when a required workflow capability has no suitable provider.
- Follow `.agents/runbooks/planning.md`, `.agents/doctrine/validation-policy.md`, and the repository test-ownership rules. Avoid tautological and change-detector tests.

## Review Focus

- Fresh refresh with the six ambient subscriptions absent but the three retained packs and local plugin present: the pinned refresh helper must locate its vendor-profile deployer and preserve correct ownership/provenance.
- Hosted staged-snapshot execution with no Codex runtime or ambient skill projections: selected standards execute from pinned deployed resources and no unselected standard is inferred.
- Removed generated skill projections with local-owned skills still present: refresh/prune ownership must preserve `repo.local_skills` content and provenance.
- ADR table with zero, one, and multiple records and renamed cross-links: freshness maintenance reports status without regenerating mesh navigation.
- Required versus optional capability unavailable: dependent work stops for required capabilities and reports a skip for optional capabilities, without relying on ambient skill names.

## Implementation Tasks

### Task 1: Pin the accepted Marketplace source and prepare the explicit standards deployment

**Files:** `.agents/plugins/marketplace-source` (gitlink), `.agents/contracts/agent-operating-model.json`, new `.agents/contracts/operating-standards.json`, `.agents/standards/` deployed resources and provenance.

- [x] Inspect the pinned Marketplace source revision and the current `origin/main` in the submodule; verify it contains the ambient-plugin model, standards registry, deploy/preview commands, and refresh helper referenced by the spec. Update only the submodule gitlink to the accepted source revision.
- [x] In the legacy operating-model contract, declare exceptions for exactly `review-entry`, `contributing-entry`, and `root-gitignore`, with reasons grounded in the spec. Keep this contract only as migration input until the new runtime no longer reads it.
- [x] Run the upstream legacy mapping preview and confirm the result is exactly the seven standards in the spec. Stop and diagnose if there are extra, missing, or duplicate standards; do not accept inferred defaults.
- [x] Add `.agents/contracts/operating-standards.json` using the upstream schema and declare exactly the seven selected IDs. Run the upstream selected-resource deployment preview, inspect resource paths and hashes, then deploy from the pinned revision into `.agents/standards/` using its `--apply` path.
- [x] Verify deployment provenance records the pinned source revision and resource hashes and that the selected checker/runtime input set contains no unselected standards.
- [x] Preserve the existing consumer command declaration and current runner until the new deployment and transition bridge are ready; do not refresh/prune ambient subscriptions yet.

**Exit:** The gitlink points to the reviewed Marketplace revision, both previews are exact, and the seven standards are deployed with verifiable provenance while the old gate remains runnable.

### Task 2: Cut over refresh and canonical validation without ambient runtime dependencies

**Files:** `.agents/contracts/repo-standards-commands.json`, `tools/run.py`, `githooks/pre-commit`, `.github/workflows/ci.yml`, selected `.agents/standards/` runtime configuration, and a Wild Bunch-owned refresh/validation adapter if required by the pinned helper.

- [x] Trace the deployed standard command contract and wire `tools/run.py` to dispatch only the seven IDs from the explicit operating-standards declaration. Preserve `ci --apply`, `ci --check`, diagnostics, shared-checkout safety, and failure reporting.
- [x] Change the refresh invocation to use the pinned submodule's `refreshing-installed-skills` implementation with `--no-roll-marketplace-source`; preserve its declared ownership/provenance and safe apply/check semantics.
- [x] Bridge the pinned helper's vendor-profile deployment dependency before ambient skill pruning. The inspected helper currently looks for `repo-shape/scripts/deploy_vendor_profiles.py` under an installed `.agents/skills/repo-shape` or a `codex-marketplace/` path, while this repository stores canonical source at `.agents/plugins/marketplace-source/codex-marketplace/`. Add the narrowest consumer-owned adapter or supported invocation that resolves this path without changing Marketplace source or restoring an ambient subscription. Prove both `--check` and `--apply` refresh paths can invoke it.
- [x] Preserve repository-owned script validation currently reached through projected repo-shape tooling. Inventory its actual checks and move only the necessary behavior to a Wild Bunch-owned path or selected deployed runtime. Keep it scoped to repo-owned/retained skill scripts; do not copy unrelated repo-shape behavior.
- [x] Update `.agents/contracts/repo-standards-commands.json` generated paths to include refreshed `.agents/skills/**`, deployed `.agents/standards/**`, and the ADR freshness README when touched; remove `**/INDEX.md` and `**/INDEX.json`.
- [x] Preserve the tracked hook's staged-snapshot behavior and ensure hosted CI invokes the same checked-in standards and consumer command declaration without relying on ambient skills or Codex. Keep game and web validation lanes intact.
- [x] Update focused command, deployment, staged-snapshot, refresh, and script-validation behavior tests at their owning existing test modules. Add a test only for a demonstrated behavior gap; do not add tautological assertions about file names or implementation text.
- [x] Run the focused suites for changed runner, standard dispatch, refresh adapter, and hook behavior while old projections still exist. Run `py -3 tools/run.py ci --check` before moving to pruning if the transition is not yet committed.

**Exit:** The canonical gate and hosted hook contract work using the new explicit standards deployment and pinned-source refresh path, while no required capability is supplied only by an ambient plugin.

### Task 3: Remove ambient subscriptions and refresh owned skill projections

**Files:** `.agents/plugins/marketplace.json`, `repo.local_skills`, `.agents/skills/` generated projections and provenance, `.agents/runbooks/`, `.agents/playbooks/`, repository-owned guidance, refresh and capability behavior tests.

- [x] Remove exactly the six ambient plugin subscriptions from `.agents/plugins/marketplace.json`; preserve the three retained packs and local `game-studio` definition. Keep `repo.local_skills` unchanged unless source inspection proves a stale entry is owned by one of the removed plugins.
- [x] Inventory runbooks, playbooks, tests, and active guidance that names ambient provider skills or projected paths. Rewrite those dependencies as `Required capabilities` or `Optional capabilities`, describe stop/skip behavior, and retain exact names only for skills owned by Wild Bunch.
- [x] Use the pinned refresh helper with `--no-roll-marketplace-source` to preview then apply the subscription change. Confirm removed plugin projections and their provenance are pruned while retained packs, local plugin, and local skills remain intact. Keep the refresh helper bridge from Task 2 active.
- [x] Remove obsolete `.agents/contracts/agent-operating-model.json` and any legacy runner/refresh references only after a repository-wide reference scan shows the new contract, runner, and refresh path own all active behavior.
- [x] Update existing capability/refresh tests to exercise required-provider absence, optional-provider absence, retained-source refresh, and local-skill ownership at behavior boundaries.
- [x] Run focused tests and the canonical `py -3 tools/run.py ci --check` gate after pruning. Search active tracked files for the six unsubscribed plugin names and projected skill paths; distinguish historical/changelog context from operative dependencies.

**Exit:** Ambient plugins are absent from subscriptions and projections; retained and repo-owned skills refresh correctly; runbooks are provider-agnostic and capability behavior is covered.

### Task 4: Retire the index mesh and move ADR custody to `docs/decisions/`

**Files:** `docs/adr/**` (renamed to `docs/decisions/**`), all in-repository references, ADR freshness updater/check, `tools/run.py`, mesh scripts/wrappers/tests, generated `INDEX.md` files, generated-path contracts, and current repository guidance/doctrine.

- [x] Move all ADR records, README, and template from `docs/decisions/` to `docs/decisions/`; update maintained links and active path references repository-wide, including `.agents/doctrine/`, current plans/specs where relevant, scripts, tests, and cross-linked ADRs. Leave no compatibility directory.
- [x] Move the ADR status/freshness table into `docs/decisions/README.md`. Retain a focused updater and read-only check for that table under a repository-owned location; preserve its date/status semantics and ensure it neither imports nor invokes mesh generation.
- [x] Remove mesh generation, validation, post-processing, wrapper entrypoints, runner commands, and mesh-only tests from `tools/run.py`, `scripts/`, and the owning tests. Remove every tracked mesh-generated `INDEX.md` from the main repository, including `.agents/`, `docs/`, root, and scripts trees. Do not touch Marketplace submodule content as if it were Wild Bunch generated output.
- [x] Promote surviving source-of-truth, skill-custody, completed-artifact, and document-placement guidance into the appropriate current doctrine or README. Remove mesh-specific freshness requirements and stale claims that indexes are required or operative.
- [x] Update repository shape/document contract logic only where needed so ordinary `AGENTS.md`, runbook, and playbook routing works without generated indexes; do not add a new inventory or directory-index system.
- [x] Update or remove existing tests based on behavior ownership. Add focused cases for ADR link/freshness behavior and for the absence of runner dependence only where there is a demonstrated runtime contract gap.
- [x] Verify all maintained relative links to moved decision docs resolve. Search tracked, non-submodule operative files for `docs/decisions/`, mesh generator/validator commands, and `INDEX.md`/`INDEX.json` requirements; classify any historical mentions rather than rewriting unrelated archived history.

**Exit:** Decision records and freshness maintenance work from `docs/decisions/`; mesh runtime and tracked generated indexes are gone; no active workflow depends on either retired surface.

### Task 5: Run migration validation and prepare review evidence

**Files:** changed runner/standards/refresh tests, `.agents/contracts/`, `.agents/standards/`, `githooks/pre-commit`, `.github/workflows/ci.yml`, moved docs, and all changed repository guidance.

- [x] Run changed behavior suites and relevant platform wrapper checks using the repository's documented commands. Do not use grep-only file-presence checks as a substitute for behavior.
- [x] Run `py -3 tools/run.py ci --check` against the completed tree, including the full existing .NET, web, repository-owned script, Markdown, and whitespace lanes.
- [ ] Validate the tracked hook from a staged snapshot and confirm its generated-path staging captures skill removals/additions, standards deployments, and the freshness README but no mesh indexes. Confirm hosted CI uses the same pinned deployed standards and does not need Codex or ambient plugins.
- [ ] Inspect final diff for exact subscription/standard sets, deployment provenance, no Marketplace source edits, resolved decision links, no replacement mesh, and preserved game/web checks.
- [ ] Commit through the normal tracked hook; do not bypass it. Push the branch, open a Draft PR against `main`, attach the PR to this task, and verify GitHub reports the expected head SHA, base, and Draft state. Review the resulting hosted checks and report any pending/running checks accurately.

**Exit:** The migration is reviewable in a Draft PR with local canonical/hook evidence and verified GitHub publication state.
