# Correct travel bribe heat reporting

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Failed bribes during hostile trail encounters record no pursuit-heat increase in their persisted encounter-resolution diary facts.

**Architecture:** `JourneyEncounterResolutionEngine` decides the outcome, and `JourneyLoop` records the resulting typed event and journey snapshot through the aggregate command path. The event's absolute pursuit heat and nested encounter-resolution metadata must agree with ADR-0029; replay reconstructs those recorded facts without rerunning the bribe decision.

**Tech Stack:** C#/.NET Domain aggregate, typed domain events and snapshots, xUnit behavior tests, PostgreSQL-backed canonical repository gate.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, Travel PG-007 and the event/history boundary. This correction preserves the accepted travel behavior and does not alter a product promise or feature disposition.

**Decision records:** ADR-0029 remains authoritative: trail encounters do not change heat. No ADR change is required because this implementation correction restores conformance to the existing decision.

**Execution Strategy:** `executing-plans` - the nested diary fact, event, live aggregate and replay are one state transition through the same bribe command. A single behavior test and minimal decision correction keep the event-backed invariant reviewable as one slice.

## Global Constraints

- Heat represents lawman and town attention; trail encounters do not change it (ADR-0029).
- Domain commands record facts through typed events and apply them to aggregate state; replay reconstructs recorded state and does not reroll decisions.
- Do not add schema changes, compatibility shims, replays that rerun commands, or changes to bribe economics, retaliation, theft, health or wallet behavior.
- `Directory.Build.props` is the sole authored application version; this PR advances it once to `0.1.0-dev.79` and the build regenerates the web identity.
- Each plan uses a fresh worktree from refreshed `origin/develop`, retires eligible predecessor artifacts in its first substantive commit, and merges through a PR to `develop` after exact-head hosted CI passes.

## Review Focus

- A retaliatory failed bribe currently leaves absolute pursuit heat unchanged while its nested persisted `HeatIncrease` claims `1`; the behavior test must fail on that disagreement before the fix.
- The asserted behavior must be observed in the emitted event, live aggregate snapshot and replayed aggregate, not merely by checking the plan constructor's chosen literal.
- A non-retaliatory failed bribe also returns a plan and must not acquire heat; cover it only if the existing focused test seam can exercise it without probabilistic retries or widening the slice.

---

### Task 1: Keep failed bribe diary facts consistent with heat

**Files:** Modify `tests/WildBunch.Domain.Tests/TravelResourceSettlementTests.cs` and `src/WildBunch.Domain/Travel/JourneyEncounterResolutionEngine.cs`.

**Consumes:** Existing `BribeRetaliationRecordsTheResourceActuallyStolen` scenario starts an interrupted journey, forces a deterministic retaliation, and verifies the exact event, live resource state and replayed resource state for Food and HorseFeed theft.

**Produces:** The same scenario verifies absolute pursuit heat remains unchanged and the persisted current-day encounter-resolution `HeatIncrease` is zero in the `JourneyEncounterResolved` event, live aggregate and replay.

- [ ] **Step 1: Extend the existing behavior test.** In `BribeRetaliationRecordsTheResourceActuallyStolen`, capture the starting pursuit heat before resolving the bribe. Assert the emitted event's absolute `PursuitHeat` equals that value and its single current-day encounter resolution reports `HeatIncrease == 0`. After rehydration, assert replayed absolute heat and nested resolution metadata equal the event facts. Keep the existing theft, health and wallet assertions intact.

- [ ] **Step 2: Prove the test fails for the intended reason.** Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~BribeRetaliationRecordsTheResourceActuallyStolen`. Expected: the new nested `HeatIncrease` assertion fails because the retaliatory plan records `1`, while event pursuit heat remains unchanged. Confirm the failure identifies the metadata mismatch, not a fixture or environment failure.

- [ ] **Step 3: Correct the domain decision.** In `JourneyEncounterResolutionEngine.ResolveBribe`, set the heat increase to zero for both unsuccessful-bribe branches. Preserve all existing success, offer payment, lockout, theft, health, wallet and encounter behavior. Do not remove or reshape persisted fields because they remain part of event history and snapshots.

- [ ] **Step 4: Verify focused behavior and replay.** Re-run the focused test and then `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore`. Both must pass. Inspect the event production and `Apply` path to confirm the event still carries absolute heat and the persisted snapshot carries the zero increase without rerolling.

### Task 2: Retire the completed plan and deliver the correction

**Files:** Update `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; update `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`; remove `.agents/plans/2026-10-11-travel-encounter-facts.md`; update `Directory.Build.props`.

**Consumes:** PR #263 is merged at `37347826b93e00ce563a1822915f360ce4161289`; its exact-head hosted gate 38114597493 and develop-push gate 38114912244 passed. The `.78` plan is complete and eligible for retirement.

**Produces:** The roadmap records PR #263's merge and gate evidence, links this successor plan and records the `.79` target; the investigation dates DN-16's bribe-heat correction without rewriting the original finding; the `.78` plan is retired in Git history; and the authored version is `.79`.

- [ ] **Step 1: Update the roadmap evidence.** In row 12, preserve the existing PR #260-263 scope and record PR #263's merge SHA, exact-head gate and develop-push gate. Link this plan as the active row-12 slice, set its target to `0.1.0-dev.79`, and describe the narrow remaining correction. Add a dated delivery note for PR #263 consistent with adjacent row-12 records.

- [ ] **Step 2: Date the investigation disposition.** Under DN-16, preserve the original audit text and add a dated note that failed bribe plans no longer report trail heat increases, ADR-0029 remains authoritative, and the correction does not change event schema or supported history policy.

- [ ] **Step 3: Retire the completed `.78` plan and advance the version.** Remove `.agents/plans/2026-10-11-travel-encounter-facts.md`; update `Directory.Build.props` from `.78` to `.79`. Do not edit the accepted baseline or feature matrix because their behavior and disposition do not change.

- [ ] **Step 4: Run the complete local gate and commit deliberately.** Ensure PostgreSQL with `.\tools\postgres-dev.ps1 ensure` when required by CI lanes, run `py -3 tools/run.py ci --check`, inspect the generated build identity and full diff, then commit through the normal check-only hook. Do not bypass hooks or include generated build artifacts.

- [ ] **Step 5: Review and publish.** Run plan completion-readiness and request a fresh independent whole-branch review against the plan, spec, ADR-0029 and backend review profile. Resolve important findings with a witnessed red/green cycle and rerun the full gate. Open a PR to `develop`; verify hosted exact-head CI passes before merging, then verify the develop-push gate, fast-forward the main checkout to latest `develop`, and confirm it is clean.

## Exit Criteria

- Retaliatory failed bribes preserve unchanged absolute pursuit heat and record zero heat increase in event, live and replayed journey facts.
- Existing bribe payment, theft, health, wallet, lockout and encounter behavior remains covered and passing.
- ADR-0029 remains true; no schema, migration, compatibility, feature-matrix or accepted-spec change is introduced.
- The `.78` plan is removed in the successor PR and remains visible in Git history; the roadmap captures merged PR #263 evidence and points to this plan.
- The implementation PR advances the product version exactly once to `0.1.0-dev.79`, and local plus exact-head hosted canonical gates pass before merge.
