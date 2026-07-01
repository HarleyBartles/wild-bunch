# BUNCH-121 Audit GameSession Decomposition and Document Aggregate-Boundary Guidance — Preflight Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Linear issue:** [BUNCH-121](https://linear.app/harleys-workspace/issue/BUNCH-121/audit-gamesession-decomposition-and-document-aggregate)
**Route state:** `preflight_needed` → this plan is the preflight artifact. After approval and merge, route state becomes `approved_plan_execution_ready`.
**Branch (plan-only PR):** `harleydbartles/bunch-121-audit-gamesession-decomposition-and-document-aggregate`
**Worktree:** `C:\WORK\repo-workspace\wild-bunch\.worktrees\bunch-121`
**Base commit:** `99970d8` (origin/main tip as of plan authoring)

## Goal

Capstone the GameSession decomposition sequence (BUNCH-112 BountyLoop, BUNCH-119 JourneyLoop, BUNCH-120 InvestigationLoop + ActionContextTracker + StoreLoop) by (1) auditing the remaining `GameSession` shape, (2) committing a durable audit report under `.agents/docs/`, (3) updating architecture guidance with child-component boundary rules, (4) cleaning up stale BUNCH-67/68/72-era guidance, and (5) applying only small, clearly safe cleanup changes. No new child components are extracted in this issue.

## Architecture posture (binding)

This plan is bound to the following repo-resident architecture constraints. Every task's requirements implicitly include this section.

- **ADR-0002** — `GameSession` is the live-play command aggregate root. Real session-owned aggregate/subaggregate boundaries may exist inside the session for cohesive state and invariants; they do not become separate command roots or repositories by default.
- **ADR-0020** — Aggregates are first-class domain authorities. The root coordinates persistence/transaction posture but does not gain authority to mutate or override another aggregate's internal rules. Cross-aggregate effects travel as explicit facts/events routed through aggregate-owned APIs. No direct reach-through mutation across boundaries.
- **ADR-0028** — All flows are event-sourced. `Apply(...)` is the single mutation path. Typed domain events are immutable facts; the event stream is the source of history; snapshots are cache. Command methods validate intent, produce a typed event, call `Apply`, and record it. Replay reconstructs state. Do not add direct mutation beside event application.
- **`.agents/architecture-hygiene.md`** — `GameSession` remains the live-play aggregate root. Do not move gameplay mutation out of `GameSession` just to satisfy abstraction goals. Internally cohesive components must stay tidy so the aggregate does not become a catch-all. Keep overloaded files/classes split by coherent responsibility before they become catch-alls.
- **`.agents/unslop/backend-architecture.md`** — Aggregate authority, application boundary, persistence boundary, projection boundary, hidden-truth leakage, runtime persistence shape, repository proliferation, and generic-backend-noun drift modes are all in force. The audit and guidance must name these drift modes and the review-failure conditions that catch them.
- **Repo skills** — `wild-bunch-dotnet-architecture`, `wild-bunch-domain-modeling`, `architecture-superpowers`, `cqrs-event-sourcing`, `event-driven-architecture`, `clean-architecture` are the controlling architecture surfaces. The audit report and the AGENTS.md guidance must be consistent with their core rules.
- **Scope discipline (AGENTS.md)** — Do only the requested slice. No opportunistic broad refactors. No unrelated feature work. If a needed design decision is missing, return `BLOCKED` or `AMBER` rather than inventing broad architecture.

## Tech Stack

C# / .NET 10, EF Core on PostgreSQL, xUnit, repo-local `postgres-dev.ps1` validation lane. No new dependencies.

## File Structure

This plan touches documentation and guidance surfaces plus, optionally, tiny safe code cleanups. No new production source files are created.

**Create:**
- `.agents/docs/game-session-decomposition-audit.md` — the durable audit report (committed artifact, not a Linear comment).

**Modify (guidance):**
- `AGENTS.md` — add a "GameSession child-component boundary rules" subsection under Architecture Guardrails naming when to add behavior to `GameSession` vs. when to create/extend a child component, and what a lawful child boundary looks like.
- `docs/adr/ADR-0002-gamesession-is-the-command-aggregate-root.md` — update the "Related Stable Source Surfaces" and/or "Implementation Status or Plan" section to cite the new audit report and the established child-component pattern (BountyLoop/JourneyLoop/InvestigationLoop/ActionContextTracker/StoreLoop). Mark BUNCH-67/68/72-era "future sub-aggregate splits" language as historical/superseded by the concrete child-component pattern that has now landed.
- `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md` — mark the BUNCH-67 "sub-aggregate splits" references as historical where they describe future work that has now been concretely superseded by the child-component extraction pattern. Do not change the event-sourcing posture.

**Modify (optional, only if clearly safe — see Task 5):**
- `src/WildBunch.Domain/Game/GameSession.cs` and/or child component files — only for dead-code removal, stale comment fixes, or duplicate-helper collapse that the audit identifies as clearly safe. No behavior changes. No public API, DTO, event payload, or snapshot shape changes.

**Index mesh:**
- `python scripts/generate_index_mesh.py` must be run after adding `.agents/docs/game-session-decomposition-audit.md` so the new INDEX.md entries are committed in the same PR.

## Current decomposition snapshot (audit baseline)

Source inspection at `99970d8` shows the following child components already extracted inside `GameSession`:

| Child component | File | Lines | Owns | Issue |
| --- | --- | --- | --- | --- |
| `BountyLoop` | `src/WildBunch.Domain/Game/BountyLoop.cs` | ~1013 | Wanted-suspect presence ledger, unrelated-criminal parity ledger, saloon POI confrontation decision logic, dev saloon override state | BUNCH-112 |
| `JourneyLoop` | `src/WildBunch.Domain/Game/JourneyLoop.cs` | ~1577 | Travel/journey state, travel-diary days, completed journey history, journey command decision logic, dev travel override state | BUNCH-119 |
| `InvestigationLoop` | `src/WildBunch.Domain/Game/InvestigationLoop.cs` | ~367 | Stateless investigation source resolution and clue/warrant surfacing decision logic | BUNCH-120 |
| `StoreLoop` | `src/WildBunch.Domain/Game/StoreLoop.cs` | ~124 | Stateless store purchase decision logic | BUNCH-120 |
| `ActionContextTracker` | `src/WildBunch.Domain/Game/ActionContextTracker.cs` | ~127 | Town action-context state and turn-advancement tracking | BUNCH-120 |

`GameSession.cs` itself is ~2486 lines plus a ~213-line `GameSessionEventReplay.cs` partial. The audit (Task 1) will produce the authoritative grouped method inventory; the snapshot here is the baseline for the trajectory table in the report.

All five child components follow the same lawful shape established by BUNCH-112: `internal sealed`, receive narrow context records, return results plus events-to-produce, do NOT reference `GameSession`, do NOT produce events directly, do NOT enter action context, do NOT mutate unrelated owners, and do NOT own infrastructure/persistence. `GameSession` produces the events, dispatches `Apply`, and owns cross-component orchestration and the persistence boundary.

## Tasks

### Task 1: Audit the remaining GameSession shape

**Files:**
- Read: `src/WildBunch.Domain/Game/GameSession.cs`, `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`, `src/WildBunch.Domain/Game/BountyLoop.cs`, `src/WildBunch.Domain/Game/JourneyLoop.cs`, `src/WildBunch.Domain/Game/InvestigationLoop.cs`, `src/WildBunch.Domain/Game/StoreLoop.cs`, `src/WildBunch.Domain/Game/ActionContextTracker.cs`
- Produce: `.agents/docs/game-session-decomposition-audit.md` (created in Task 2; this task produces the audit content as the report body)

**Interfaces:**
- Consumes: the binding architecture constraints in the "Architecture posture" section above.
- Produces: the grouped method inventory, child-component list, acceptable-orchestration explanation, future-extraction-candidates list, and line-count trajectory table that Task 2 commits.

- [ ] **Step 1: Inventory remaining public/internal methods on `GameSession`**

Read `GameSession.cs` and `GameSessionEventReplay.cs` end to end. Group every `public` and `internal` member into one of these responsibility buckets:

1. **Session lifecycle** — `StartSetup`, `CompleteGameStart`, `StartNew` (both overloads), `ViewPrologue`, `ArchivePlaythrough`, `StartFlowPhase`, `Status`, constructor, `RehydrateFromEvents`, `ApplyCommittedEvents`.
2. **Event-sourcing infrastructure** — `ProduceEvent`, `ApplyProducedEvent`, `Apply` overloads, `SetCommittedEvents`, `MarkEventsCommitted`, `UncommittedEvents`, `CommittedEvents`, `AllEvents`, `Version`.
3. **Cross-component orchestration** — methods that coordinate across child components or own session-level concerns (e.g., `EnterActionContext`, `CanConfrontWantedSuspectInCurrentContext`, `RefreshTownVisit`, `ResetActionContextForTownChange`, `RefillCanteenAfterArrival`, `CreateTravelDayGenerationContext`, the static pressure-band/band factory helpers if they are shared orchestration rather than journey-internal).
4. **Dev overrides** — `ForceDevTravelOverride`, `ClearDevTravelOverride`, `ForceDevSaloonOverride`, `ClearDevSaloonOverride`, `ForceDevSaltSource`, `ClearDevSaltSource`, `ForceDevDifficulty`, `SetDevEntropy`, the `PendingDev*` getters, the `Restore*` rehydration helpers.
5. **Bounty/saloon/sheriff orchestration** — `LookAroundSaloon`, `ConfrontSaloonPersonOfInterest`, `ConfrontSaloonWantedSuspect`, `ResolveWantedSuspectConfrontation`, `AssessSheriffTurnIn`, `SettleSheriffTurnIn`, `SettleUnrelatedCriminalTurnIn`, `ReadWantedPosters`, presence-state getters/setters, `BuildUnrelatedCriminalLedger`, the saloon-eligibility helpers.
6. **Investigation orchestration** — `FollowTelegraphLeads`, `GatherLocalGossip`, `InspectNoticeBoard`, `CheckSheriffRecords`.
7. **Store orchestration** — `Purchase`.
8. **Journey orchestration** — `StartJourney`, `AdvanceJourneyDay`, `AcknowledgeJourneyArrival`, `ResolveJourneyEncounter` (all overloads), `AppendTravelDiaryDay`, `UpdateLatestTravelDiaryDay`, `ReplaceTravelDiaryDays`, `SyncPlayerFromJourneySnapshot`.
9. **Read-only state projections on the root** — the `=>` getters that delegate to child components (`Journey`, `TravelDiaryDays`, `CompletedJourneyHistory`, `WantedSuspectPresenceEntries`, `UnrelatedCriminalLedger`, `CurrentActionContext`, `CurrentActionContextTownId`, `PendingDevTravelOverride`, `PendingDevSaloonOverride`).
10. **Warrant/suspect pure helpers** — `MatchesKnownWarrant`, `TryGetKnownWarrantForSuspect`, `DescribeWarrantDisposition`, `DescribeConfrontationNarration`, `CollectSuspectFeatureDescriptions`, `TryGetEligibleSaloonSuspectCandidate`, `IsEligibleSaloonPersonOfInterestCandidate`, `GetSaloonPoiIneligibilityReason`, `ResolveSaloonPersonOfInterestCompatibilityResult`, `SpendFirearmAmmo`, `IsJourneyModal`.

For each method, record: signature, line range, bucket, and a one-line note on whether it is (a) acceptable orchestration that belongs on `GameSession`, (b) a delegate that already forwards to a child component, or (c) a future extraction candidate (and if so, why it is not extracted now).

- [ ] **Step 2: List current child components with their owned state and command surface**

For each of `BountyLoop`, `JourneyLoop`, `InvestigationLoop`, `StoreLoop`, `ActionContextTracker`, record: file, line count, owned state fields, public/internal command methods, the context-record types they consume, and the result/outcome types they return. Confirm each one conforms to the lawful child boundary (no `GameSession` reference, no direct event production, no `EnterActionContext`, no cross-owner mutation, no infrastructure/persistence ownership).

- [ ] **Step 3: Explain remaining acceptable orchestration**

Write the "Acceptable orchestration" section explaining why the methods in buckets 1, 3, 4, and the orchestration entry points in buckets 5–8 legitimately belong on `GameSession`: they coordinate across child components, own session-level concerns (clock, pursuit, player, world, case file), produce events through `ProduceEvent`, dispatch `Apply`, and own the persistence/rehydration boundary. Cite ADR-0002 ("`GameSession` owns the live play state, the command methods, and the invariant boundary for session mutation") and ADR-0020 ("the root coordinates persistence or transaction posture for that boundary, but it does not gain authority to mutate or override another aggregate's internal domain rules").

- [ ] **Step 4: Document future extraction candidates (if any) with rationale for not extracting now**

For each method in bucket 10 (pure helpers) and any orchestration method that has grown large enough to be a candidate, record: candidate name, cohesive state+rules it would own, the event/state family it would receive/return, and the explicit rationale for NOT extracting it in this issue (scope discipline: this is a capstone, not a new extraction track). If no candidates exist, state that explicitly.

- [ ] **Step 5: Produce the line-count trajectory table**

Record the line counts for `GameSession.cs`, `GameSessionEventReplay.cs`, and each child component file at the current commit. Where prior commits' line counts are recoverable from git history (BUNCH-112/119/120 merge commits), include before/after pairs to show the decomposition trajectory. Use `git log --oneline --follow` and `git show <sha>:path | Measure-Object -Line` for historical counts where available. Do not fabricate counts — if a historical count is not recoverable, mark it "n/a".

### Task 2: Commit the durable audit report

**Files:**
- Create: `.agents/docs/game-session-decomposition-audit.md`
- Modify: `.agents/docs/INDEX.md` (regenerated by the index-mesh script)

**Interfaces:**
- Consumes: the audit content produced in Task 1.
- Produces: a committed artifact at `.agents/docs/game-session-decomposition-audit.md` that future workers and reviewers can cite.

- [ ] **Step 1: Write the audit report**

Create `.agents/docs/game-session-decomposition-audit.md` with this structure:

```markdown
# GameSession Decomposition Audit

> Capstone audit for BUNCH-121. Source snapshot: `99970d8` (origin/main, 2026-07-01).
> This is a committed artifact, not a Linear comment. Update it when the
> decomposition posture changes materially.

## Posture

`GameSession` remains the live-play aggregate root, command entry point,
event-production boundary, apply-dispatch owner, and persistence boundary
(ADR-0002, ADR-0020, ADR-0028). It may orchestrate cross-component behavior.
It should not directly accumulate all game rules. New cohesive gameplay loops
should become internal child domain components when they own state plus rules,
have a clear event family or state family, and can receive narrow context
records.

## Child components

(table from Task 1 Step 2)

## Remaining GameSession public/internal methods by responsibility

(grouped inventory from Task 1 Step 1)

## Acceptable orchestration

(section from Task 1 Step 3)

## Future extraction candidates

(section from Task 1 Step 4)

## Decomposition trajectory

(line-count table from Task 1 Step 5)

## Lawful child boundary

A lawful child component inside the GameSession boundary:
- is `internal sealed` and lives under `src/WildBunch.Domain/Game/`;
- receives narrow context records (not the parent aggregate);
- returns results plus events-to-produce (it does NOT produce events directly);
- does NOT reference `GameSession`;
- does NOT call `EnterActionContext` (it is not the action-context owner);
- does NOT mutate owners it does not own (CaseFile, TownVisitState, Player,
  Clock, PursuitState);
- does NOT own infrastructure or persistence;
- has its owned state restored during snapshot rehydration via a
  `Restore*` helper on `GameSession` that delegates to the child.

## Drift modes this audit must catch

(name the drift modes from `.agents/unslop/backend-architecture.md` that the
child-component pattern exists to prevent: aggregate bypass, event-sourcing
drift, repository proliferation, generic-backend-noun drift, architecture-name
compliance theater)
```

Fill every section with the content produced in Task 1. No placeholders.

- [ ] **Step 2: Regenerate the index mesh**

Run: `python scripts/generate_index_mesh.py`
Expected: the generator walks the live tree and updates `.agents/docs/INDEX.md` (and any other affected INDEX.md files) to include the new audit report. Inspect the diff to confirm the new file appears and no unrelated INDEX.md files changed.

- [ ] **Step 3: Commit the audit report and index mesh**

```bash
git add .agents/docs/game-session-decomposition-audit.md .agents/docs/INDEX.md
# add any other INDEX.md files the generator touched
git commit -m "BUNCH-121: add GameSession decomposition audit report"
```

### Task 3: Update architecture guidance with child-component boundary rules

**Files:**
- Modify: `AGENTS.md` (root)
- Modify: `docs/adr/ADR-0002-gamesession-is-the-command-aggregate-root.md`
- Modify: `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`

**Interfaces:**
- Consumes: the lawful-child-boundary definition from Task 2 and the binding architecture constraints.
- Produces: durable guidance so future workers know when to add behavior directly to `GameSession` vs. when to create/extend a child component, and what a lawful child boundary looks like.

- [ ] **Step 1: Add child-component boundary rules to root `AGENTS.md`**

Under the existing "Architecture Guardrails" section, add a new subsection. The wording should be close to the issue's suggested guidance, tightened to repo language:

```markdown
### GameSession child-component boundaries
- `GameSession` remains the session aggregate root, command entry point,
  event-production boundary, apply-dispatch owner, and persistence boundary.
  It may orchestrate cross-component behavior. It should not directly
  accumulate all game rules.
- Add behavior directly to `GameSession` when it coordinates across child
  components, owns a session-level concern (clock, pursuit, player, world,
  case file), or is the event-production/apply-dispatch/persistence seam.
- Create or extend an internal child domain component when the behavior owns
  state plus rules, has a clear event family or state family, and can receive
  narrow context records. A lawful child component is `internal sealed`,
  lives under `src/WildBunch.Domain/Game/`, receives narrow context records
  (not the parent aggregate), returns results plus events-to-produce, does
  NOT reference `GameSession`, does NOT produce events directly, does NOT
  call `EnterActionContext`, does NOT mutate owners it does not own, and does
  NOT own infrastructure or persistence. Owned state is restored during
  snapshot rehydration via a `Restore*` helper on `GameSession` that
  delegates to the child.
- See `.agents/docs/game-session-decomposition-audit.md` for the current
  child-component inventory and the decomposition trajectory.
```

Do not duplicate the full audit report in `AGENTS.md` — cite it.

- [ ] **Step 2: Update ADR-0002 to cite the audit and mark BUNCH-67/68/72-era language as historical**

In `docs/adr/ADR-0002-gamesession-is-the-command-aggregate-root.md`:
- In "Related Stable Source Surfaces", add `.agents/docs/game-session-decomposition-audit.md` and the five child component files.
- In "Implementation Status or Plan", add a dated entry: `2026-07-01 - live: the child-component extraction pattern (BountyLoop, JourneyLoop, InvestigationLoop, StoreLoop, ActionContextTracker) is now concrete. See .agents/docs/game-session-decomposition-audit.md.`
- In "Review Triggers" or a new "Historical Notes" subsection, mark the BUNCH-67/68/72-era "future sub-aggregate splits" language as historical/superseded by the concrete child-component pattern. Do not reopen those tracks.

- [ ] **Step 3: Mark BUNCH-67 references in ADR-0028 as historical**

In `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`, the lines referencing `BUNCH-67 (refactor GameSession into domain aggregates)` and `BUNCH-67 handles sub-aggregate splits` (around lines 27, 57, 124, 140, 161, 202) describe future work that has now been concretely superseded by the child-component extraction pattern. Add a brief historical note (do not delete the lines — they are part of the ADR's reasoning record) stating that BUNCH-67/68/72 are closed as historical/superseded and the concrete pattern is recorded in `.agents/docs/game-session-decomposition-audit.md` and the root `AGENTS.md` child-component boundary rules. Do not change the event-sourcing posture.

- [ ] **Step 4: Regenerate the index mesh if any INDEX.md would change**

The ADR and AGENTS.md edits do not add/remove files, so the index mesh should not change. Run `python scripts/generate_index_mesh.py --check` to confirm. If it fails, run `python scripts/generate_index_mesh.py` and commit the regenerated files alongside the guidance edits.

- [ ] **Step 5: Commit the guidance updates**

```bash
git add AGENTS.md docs/adr/ADR-0002-gamesession-is-the-command-aggregate-root.md docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md
# add any regenerated INDEX.md files if Step 4 required it
git commit -m "BUNCH-121: document GameSession child-component boundary rules"
```

### Task 4: Clean up stale BUNCH-67/68/72-era guidance

**Files:**
- Read: every file matched by `grep -r "BUNCH-67\|BUNCH-68\|BUNCH-72"` (already enumerated: `docs/adr/ADR-0028*.md`, the BUNCH-77 plan files, the BUNCH-72 plan file).
- Modify: only files that contain operative agent law or current architecture guidance that is now misleading. Plan files under `.agents/superpowers/plans/` are historical execution records and are NOT modified — they document what was planned at the time.

**Interfaces:**
- Consumes: the historical-note edits from Task 3 (ADR-0028) and the audit report.
- Produces: a guidance surface that is not misleading about the current decomposition posture.

- [ ] **Step 1: Enumerate every BUNCH-67/68/72 reference and classify each**

For each match, classify it as:
- (a) **historical reasoning inside an ADR** — leave in place; Task 3 Step 3 added the superseding note.
- (b) **operative agent law that is now misleading** — repair or remove.
- (c) **historical plan execution record under `.agents/superpowers/plans/`** — leave unchanged (historical record).

Expected outcome: the only operative-law references are in ADR-0002 and ADR-0028, both already handled by Task 3. The BUNCH-77 and BUNCH-72 plan files are class (c). If any class (b) reference is found outside those, repair it in this step.

- [ ] **Step 2: Commit any class (b) repairs (if any)**

If Step 1 found class (b) references, repair them and commit:
```bash
git add <repaired files>
git commit -m "BUNCH-121: clean up stale BUNCH-67/68/72-era guidance"
```
If no class (b) references were found, record that finding in the audit report's "Drift modes this audit must catch" section (or a new "Stale guidance cleanup" subsection) and skip the commit.

### Task 5: Apply small safe cleanup changes (only if clearly safe)

**Files:**
- Modify (only if clearly safe): `src/WildBunch.Domain/Game/GameSession.cs` and/or child component files.

**Interfaces:**
- Consumes: the audit from Task 1.
- Produces: dead-code removal, stale-comment fixes, or duplicate-helper collapse with NO behavior change, NO public API change, NO DTO/event-payload/snapshot-shape change.

**Scope guard:** This task is gated. If the audit finds no clearly safe cleanup, this task is a no-op and the worker records that finding in the audit report. Do NOT attempt broad refactors. Do NOT extract new child components. Do NOT change behavior.

- [ ] **Step 1: Identify clearly safe cleanup candidates from the audit**

Scan the audit for:
- dead code (unused private methods, unreachable branches, stale `const` values);
- stale comments referencing deleted coordinators or superseded issues;
- duplicate pure helpers that exist on both `GameSession` and a child component where the `GameSession` copy is now dead.

For each candidate, record: location, why it is clearly safe (no caller, no behavior change, no shape change), and the exact edit.

- [ ] **Step 2: Apply each clearly safe cleanup**

For each candidate, make the edit. Do not batch unrelated cleanups into one edit — keep each cleanup atomic so a reviewer could reject one while approving another.

- [ ] **Step 3: Build and run the domain test suite**

Run: `dotnet build WildBunch.sln`
Expected: success, 0 errors.

Run: `dotnet test tests/WildBunch.Domain.Tests`
Expected: all domain tests pass. If any test fails, revert the cleanup that caused the failure — it was not clearly safe.

- [ ] **Step 4: Commit the cleanups (if any)**

```bash
git add <cleaned files>
git commit -m "BUNCH-121: remove dead code / fix stale comments (small safe cleanup)"
```
If no cleanups were applied, record that finding in the audit report and skip the commit.

### Task 6: Full validation and route-state update

**Files:**
- Read: the audit report, the guidance edits, the cleanup commits (if any).
- Modify: Linear issue BUNCH-121 (route-state block only — via the Linear connector, not a GitHub mutation).

**Interfaces:**
- Consumes: all prior tasks.
- Produces: validation evidence, a clean worktree, branch head proof, PR publication, and a Linear route-state update.

- [ ] **Step 1: Run the full build**

Run: `dotnet build WildBunch.sln`
Expected: success, 0 errors. Record the exact output tail.

- [ ] **Step 2: Run the PostgreSQL-backed validation lane**

Run: `.\scripts\postgres-dev.ps1 ensure`
Then: `.\scripts\postgres-dev.ps1 validate`
Expected: EF migrations list succeeds; `dotnet test` passes across domain + integration lanes. Record the exact output tail. If PostgreSQL port 5434 is closed or setup fails, report the exact command and output after running the repo-local setup/status lane instead of treating it as a product regression.

- [ ] **Step 3: Run the index-mesh CI check**

Run: `python scripts/generate_index_mesh.py --check`
Expected: success (the committed INDEX.md files match the generator output from the current tree). If it fails, run `python scripts/generate_index_mesh.py`, commit the regenerated files, and rerun the check.

- [ ] **Step 4: Confirm a clean worktree and branch head proof**

Run: `git status` (expected: clean), `git log --oneline -10` (record head SHA and commit list), `git rev-parse origin/main` (record remote head for the falsification check that the branch is ahead of main with the plan-only commits).

- [ ] **Step 5: Push the branch and open the plan-only PR**

This is a preflight plan. The PR is plan-only: it contains the audit report, the guidance edits, the stale-guidance cleanup, and any small safe code cleanups — but it does NOT extract new child components and does NOT change behavior. Push the branch and open the PR:

```bash
git push -u origin harleydbartles/bunch-121-audit-gamesession-decomposition-and-document-aggregate
gh pr create --title "BUNCH-121: Audit GameSession decomposition and document aggregate-boundary guidance" --body "$(cat <<'EOF'
## Summary
- Adds `.agents/docs/game-session-decomposition-audit.md` (durable audit report)
- Documents GameSession child-component boundary rules in root `AGENTS.md`
- Updates ADR-0002 and ADR-0028 to cite the audit and mark BUNCH-67/68/72-era language as historical
- Applies small safe cleanup (if any found by the audit)

## Plan
- `.agents/superpowers/plans/2026-07-01-bunch-121-audit-gamesession-decomposition.md`

#### Test plan
- [ ] `dotnet build WildBunch.sln` passes
- [ ] `.\scripts\postgres-dev.ps1 validate` passes (EF + domain + integration)
- [ ] `python scripts/generate_index_mesh.py --check` passes
- [ ] Audit report exists at `.agents/docs/game-session-decomposition-audit.md`
- [ ] No behavior change, no public API/DTO/event-payload/snapshot-shape change

Generated with [Devin](https://devin.ai)
EOF
)"
```

Record the PR URL.

- [ ] **Step 6: Update Linear route state**

Via the Linear connector (read the `using-linear` mutate-save reference first), post a comment on BUNCH-121 with a route-state block recording:

- route-state: `preflight_complete_pending_approval`
- plan path: `.agents/superpowers/plans/2026-07-01-bunch-121-audit-gamesession-decomposition.md`
- plan PR: <PR URL from Step 5>
- plan branch: `harleydbartles/bunch-121-audit-gamesession-decomposition-and-document-aggregate`
- plan head: <SHA from Step 4>
- base: `99970d8` (origin/main)
- validation: build + postgres validate + index-mesh check results
- next: approval + merge → route state becomes `approved_plan_execution_ready`

Do NOT close the Linear issue. Do NOT mutate GitHub issue state. Stop after the route-state update.

## Self-Review

**1. Spec coverage:**
- Audit the remaining GameSession shape → Task 1.
- Produce a durable audit report at `.agents/docs/game-session-decomposition-audit.md` → Task 2.
- Update architecture guidance with child-component boundary rules → Task 3.
- Clean up stale BUNCH-67/68/72-era guidance → Task 4.
- Small safe cleanup changes only if clearly safe → Task 5 (gated).
- `dotnet build` + `dotnet test` pass → Task 6.
- Linear issue updated with final state → Task 6 Step 6.
- Deliverables checklist from the issue: audit report committed (Task 2), AGENTS.md updated (Task 3), remaining methods grouped (Task 1 → Task 2), child components listed (Task 1 → Task 2), acceptable orchestration explained (Task 1 → Task 2), future candidates documented (Task 1 → Task 2), stale guidance removed/marked historical (Task 3 + Task 4), small safe cleanup applied (Task 5), build+test pass (Task 6), Linear updated (Task 6 Step 6).

**2. Placeholder scan:** No "TBD", "TODO", "implement later", or "add appropriate X" in any step. Every step names the exact file, the exact edit, and the exact command with expected output.

**3. Type consistency:** This plan does not introduce new types. The child-component names (`BountyLoop`, `JourneyLoop`, `InvestigationLoop`, `StoreLoop`, `ActionContextTracker`) and the context-record/outcome types referenced in the audit match the source files inspected at `99970d8`.

**4. Scope discipline:** This plan does NOT extract new child components. It does NOT change behavior, public API, DTOs, event payloads, or snapshot shape. Task 5 is explicitly gated to dead-code/stale-comment/duplicate-helper cleanup only. If the audit finds no clearly safe cleanup, Task 5 is a no-op.

**5. Architecture conformance:** The audit report and guidance edits are bound to ADR-0002, ADR-0020, ADR-0028, `.agents/architecture-hygiene.md`, `.agents/unslop/backend-architecture.md`, and the repo-resident architecture skills. The lawful-child-boundary definition in Task 2 is consistent with all of them. The drift-mode naming in the audit report is consistent with `.agents/unslop/backend-architecture.md`.

## Execution Handoff

After this plan is approved and merged to `main`, route state becomes `approved_plan_execution_ready`. The executing worker should use `superpowers:executing-plans` to implement the plan task-by-task in a fresh worktree branched from the merged `main` tip.
