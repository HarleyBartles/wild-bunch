# Required Component Cache Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Rebuild malformed current-version required component caches from supported event history on command and query loads, without turning cache recovery into a way to hide invalid history.

**Architecture:** Event history remains authoritative. Required cached components are decoded only as an optimization; when a current-version required component cannot become valid domain state, the repository rebuilds once from the coherent event stream. Query recovery is in-memory and performs no writeback. A later legal command repairs the cache through the existing unit of work. Optional-component absence remains a separate phase-dependent contract.

**Tech Stack:** C#/.NET, EF Core, Npgsql/PostgreSQL, xUnit integration tests.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md#cache-backed-state-and-recovery), [row 07 persistence roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md#07-persistence-restoration-and-retries), [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), [PS-04 test follow-up](../investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md#mapping-every-persistence-finding-to-the-right-proof).

**Execution Strategy:** `executing-plans` inline. The PostgreSQL tests, narrow persisted-component decode classification, repository fallback and same-unit-of-work repair contract share one recovery boundary.

## Global Constraints

- Events record facts that occurred; replay reconstructs the exact state and never rolls randomness again.
- Only a malformed required component cache is eligible for this fallback. Exceptions while decoding or applying authoritative events remain visible to the caller.
- Current-version component corruption is not repaired by query writeback. A later legal command persists reconstructed state through the existing unit of work.
- Do not reinterpret absent optional state, phase-specific setup state, or supported legacy event payloads as malformed required component data.
- Each ordinary PR to `develop` advances `Directory.Build.props` once to the next unique `0.1.0-dev.N` identity.
- Use a fresh worktree from refreshed `origin/develop`, publish a PR to `develop`, and merge only after the exact PR head passes review and local and hosted canonical gates.

## Review Focus

- A parseable but unusable non-Player required component cache recovers to independently known event-established state through aggregate and read-model repositories.
- The cache JSON, component version, stream positions, diary projection and event count remain unchanged during reads; a subsequent legal command repairs the component in its existing save.
- Removing an event required for replay still fails explicitly even when the corresponding cache is malformed.
- Tests use production serialization, registered event decoding, real PostgreSQL repositories and independently captured expected facts. No same-projector-only oracle or timing race.

---

### Task 1: Retire the completed command-load slice and establish this successor

**Files:** `.agents/plans/2026-10-09-command-load-consistent-read.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `Directory.Build.props`.

**Consumes:** Merged PR #205 evidence on `develop`, including its exact source, merge, matching tree, development version and hosted canonical gate.

**Produces:** This plan as row 07's current plan, truthful command-load disposition, completed predecessor retirement and the next authored development version in the first substantive commit.

- [x] Verify PR #205 merged to `develop` from reviewed source `fedac9b935dda22c9300a3b429bfae53221a5403`, the source and merge trees match, and hosted canonical gate run `37870893847` passed.
- [x] In the first substantive commit, set this plan as row 07's current plan, record PR #205's merge SHA, tree, `0.1.0-dev.20` and hosted gate, summarize command-load `RepeatableRead` behavior, and retire the completed predecessor plan.
- [x] Advance `Directory.Build.props` once from `0.1.0-dev.20` to `0.1.0-dev.21`; do not hand-edit generated web version output.
- [x] Keep this plan-only commit first; inspect its path and content before committing.

### Task 2: Prove recovery from malformed required component caches

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`.

**Consumes:** A persisted active session created and saved through production repositories, with expected player, world, case-file, clock and pursuit facts captured before cache damage.

**Produces:** PostgreSQL integration tests for representative parseable malformed non-Player required component payloads across the command aggregate and query read-model paths.

- [x] Extend the production-loader scenario for a required non-Player component whose current-version JSON is syntactically valid but omits or nulls a domain-required fact; choose concrete shapes from the current `WorldSnapshot` and `CaseFileSnapshot` codecs and record the chosen mutations in the test name or local helper.
- [x] Capture expected state before directly mutating only the selected component payload in PostgreSQL; preserve its component version, envelope versions, event rows and diary metadata.
- [x] Assert fresh command aggregate, player read and journal read return the independently captured event-established facts rather than throwing, defaulting or silently losing the facts.
- [x] Assert each read leaves the damaged JSON, component version, stream/snapshot positions, diary metadata and event count unchanged.
- [x] Execute a legal command after recovery, save through the existing unit of work, then fresh-load and assert the repaired component yields the complete expected state.
- [x] Add a negative control that damages the same required cache and removes a required authoritative event; assert command recovery surfaces the replay failure for every tested required component, and query recovery does so for each required component it consumes. Salt-source remains command-side because the player query does not consume it.
- [x] Run the focused PostgreSQL scenarios against the current implementation and observe the malformed-cache path fail before changing production code.

Run RED with `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~ReadModel_CurrentRequiredComponentCacheShapeRebuildsFromEvents"`; the expected failure must identify decode/materialization failure or incorrect recovered facts while event history is intact.

### Task 3: Classify component decode failures and fall back to supported replay

**Files:** `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`; `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`; `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`.

**Consumes:** The failing real-PostgreSQL cases and the current required-component deserializers used by both repository adapters.

**Produces:** A narrow typed invalid-cache boundary for required component decoding and full event replay when that boundary is reached.

- [x] Introduce a persistence-owned invalid required-component cache exception with the component identity and original decode/shape cause; preserve the existing Player recovery behavior through the same classification.
- [x] Validate required fields/collections before constructing usable `World`, `CaseFile`, `GameClock`, `PursuitState` and `SaltSource` values. Reject null or missing required shape; retain existing defaults only where the current persisted format explicitly treats a field as optional.
- [x] Keep exception classification scoped to component JSON decoding and domain conversion; do not catch event upcast, event decode, replay, projection or unrelated infrastructure exceptions as cache damage.
- [x] Route classified required-component shape failures from both command aggregate and query read-model materialization into the coherent full replay path.
- [x] Preserve current behavior for a missing row, stale component-version rebuild, optional component absence, current legal payload, unsupported history and cancellation.
- [x] Rerun the focused PostgreSQL cases and prove the invalid-history negative remains failing at the authoritative history boundary.

### Task 4: Record the bounded PS-04 disposition and deliver

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; this plan; changed source and tests.

**Consumes:** Focused red-green evidence and the exact staged diff.

**Produces:** Dated PS-04 disposition, truthful row 07 remaining scope and reviewed PR.

- [x] Add a dated PS-04 disposition listing the exact required component shapes, production adapters, preservation assertions, later legal-save repair and corrupt-history negative; preserve the original audit finding.
- [x] State precisely which non-Player required component cases are closed and which optional, nested, or other malformed component forms remain open; keep row 07 executing and this plan live through its completing PR.
- [x] Compare the change with ADR-0028, event-sourcing integrity doctrine, architecture guardrails, feature matrix, backend-architecture and code-review unslop profiles; keep ADR-0028 unchanged because the implementation enforces its existing cache and event-authority decisions, and update the feature matrix's current capability assessment.
- [x] Run focused PostgreSQL proof, persistence migration inventory and canonical fail-fast `py -3 tools/run.py ci --check`; confirm generated web identity is `0.1.0-dev.21` and no schema or event payload changes were introduced.
- [x] Complete whole-branch review against this plan, the baseline spec, PS-04, persistence doctrine and code-review runbook; the independent reviewer found one Minor capability-assessment mismatch, corrected in this branch, and the correction must be re-checked against the final PR head before it becomes ready.
- [ ] Publish and attach a Draft PR to `develop`, verify its exact source head, then mark it ready so hosted validation runs; verify the canonical gate passes on that exact SHA before merging under active epic authorization, fast-forward `Z:\wild-bunch`, and clean only this verified merged worktree and branch; retain this plan until its next row 07 successor classifies it.

The local commit hook runs `py -3 tools/run.py ci --check` against the staged candidate. Confirm generated `src/WildBunch.Web/dist/version.json` reports `0.1.0-dev.21`; verify no migration or event payload file changed with `git diff origin/develop -- src/WildBunch.Persistence/Migrations src/WildBunch.Domain/Events`. For publication, the PR head must equal local `HEAD`; hosted validation must pass on that exact SHA before the authorized merge.
