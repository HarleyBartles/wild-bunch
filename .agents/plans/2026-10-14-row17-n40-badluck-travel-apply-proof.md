# N-40 Correct Travel Event Apply Coverage

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make the travel event Apply tests accurately describe their coverage, retain independent absolute and additive payload behavior, and prove the currently untested `JourneyEncounterResolved` Apply effects.

**Architecture:** Keep production behavior and event contracts unchanged. Exercise isolated `GameSession.Apply` methods with meaningful event payloads, assert the resulting state at the aggregate boundary, and use reversible mutations to establish that each new assertion observes its intended effect.

**Tech Stack:** C# 14, .NET 10, xUnit, Wild Bunch Domain event sourcing.

**Spec:** The [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), row 17 of the [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), N-18 in the [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), the [test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), the [event-sourcing integrity doctrine](../doctrine/event-sourcing-integrity.md), and ADR-0028.

**Execution Strategy:** `executing-plans` (Native inline). The payload assertions, mutation proofs, and disposition updates form one bounded test-ownership change; the standing independent whole-branch review supplies the second context.

## Global Constraints

- N-39 merged to `develop` at `0.1.0-dev.157`; this PR owns the next unique development version, `0.1.0-dev.158`, in `Directory.Build.props`.
- Preserve strict event sourcing and the documented distinction between absolute snapshot fields and additive effects. This plan changes tests and their evidence only.
- Do not change gameplay behavior, event contracts, feature promises, ADRs, or implementation status. Do not claim full field parity or direct-mutation parity from isolated Apply tests.
- Keep assertions about the independently observable effects of `JourneyEncounterResolved`: absolute health, wallet, heat and journey snapshot; additive ammo expenditure; and additive stolen RevolverAmmo removal.
- In the first substantive commit, retire only the fully delivered N-39 plan, record its PR, review, CI, merge, and cleanup facts, and select N-40 in the roadmap.
- Deliver from this fresh worktree through an independently reviewed PR to `develop`, require passing exact-head hosted CI and develop-push CI, then fast-forward the primary checkout and retire this worktree and its paired refs.

## Review Focus

- The class documentation states only the event/application behavior these tests actually prove.
- The retained `TravelDayAdvanced` scenario uses an advanced snapshot and one nonzero health delta, and proves clock, journey progress, health and absolute pursuit heat together without duplicating two tests for the same Apply behavior.
- `JourneyEncounterResolved` Apply coverage distinguishes absolute fields set by the event from additive ammunition effects, using a started journey and values that differ from the pre-Apply state.
- The ammo-spend and stolen-ammo assertions use valid, independently observable event cases; neither case relies on two mutually exclusive command outcomes being combined into an artificial event.
- Deliberately suppressing each newly asserted production effect makes its focused test fail; restore production byte-for-byte and rerun the focused tests successfully.
- The test disposition and dated investigation note close only the documented Apply-test gap. ADR-0028 and the feature matrix remain unchanged because no durable decision or player-visible feature changes.

---

## Task 1: Record N-39 delivery and commit the N-40 plan

**Files:**
- Create: `.agents/plans/2026-10-14-row17-n40-badluck-travel-apply-proof.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-14-row17-n39-quiet-journey-completion-replay.md`

**Interfaces:**
- Consumes: merged N-39 source `13c0780c049e8701e7fa7bff82af8352d3098376`, PR #342, merge `69b78caa6f8f19a4bf748337d4ff81f5ace22d81`, exact-head gate run `38523063303`, develop-push gate run `38523510200`, and the current develop branch.
- Produces: an active N-40 plan, an N-39 delivery record and retirement in the roadmap, and the single authored application version `0.1.0-dev.158`.

- [ ] Verify PR #342 merged the exact N-39 branch head to `develop`, both hosted canonical gates passed, the primary checkout is at merge `69b78caa6f8f19a4bf748337d4ff81f5ace22d81`, and the local and live upstream N-39 branch refs are absent. Record any bounded cleanup limitation accurately.
- [ ] Classify N-39 as delivered: its only scope was consolidation of quiet-journey completion, successful arrival acknowledgment and exact event-history replay into one bounded behavior test, all merged in PR #342. Record its review, validation, merge and cleanup evidence, select N-40 in row 17, and set `Directory.Build.props` to `0.1.0-dev.158`.
- [ ] Commit this plan, N-39 retirement, roadmap delivery update and version transition before editing tests.

## Task 2: Correct and complete isolated travel Apply tests

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/EventSourcing/TravelEventApplyTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`

**Interfaces:**
- Consumes: `TravelTestFactory.CreateEasyShortJourney()`, `GameSession.Apply(JourneyStarted)`, `GameSession.Apply(TravelDayAdvanced)`, `GameSession.Apply(JourneyEncounterResolved)`, `TravelJourney.ToSnapshot(...)`, and the existing `GameSession` resource accessors.
- Produces: accurate isolated Apply test descriptions; one retained `TravelDayAdvanced` case for snapshot, clock, health delta and heat; and direct behavioral coverage for the absolute and additive fields applied from `JourneyEncounterResolved`.

- [ ] Replace the class comment's unsupported claims of six event types, exact full state, and field-for-field command-path parity with a concise statement that these are isolated aggregate Apply payload-effect tests.
- [ ] Consolidate the two `TravelDayAdvanced` tests into one scenario. Build an advanced journey snapshot through the existing travel fixture, apply an event with a nonzero negative `HealthDelta` and a non-default absolute heat value, then assert the event's absolute day/turn and journey progress alongside the resulting health and heat. Remove the duplicate test after preserving each independent outcome.
- [ ] Add a `JourneyEncounterResolved` Apply case that starts an active journey, supplies a progressed snapshot, seeds ammunition, and applies meaningful absolute health, wallet and heat values plus positive `AmmoSpent`. Assert each resulting value and journey progress at the aggregate boundary.
- [ ] Add a distinct `JourneyEncounterResolved` Apply case for stolen RevolverAmmo. Start with a positive ammunition quantity, apply an event with `StolenItemKind = RevolverAmmo`, positive `StolenItemQuantity`, zero `AmmoSpent`, and assert the remaining quantity. Keep the payload combinations consistent with the separate spend and theft outcomes emitted by the command path.
- [ ] For each newly added behavior assertion, temporarily mutate only its owning Apply effect so the corresponding focused test fails: suppress `HealthDelta` application for the consolidated day test, suppress `SpendFirearmAmmo` for the spend case, and suppress stolen-ammo removal for the theft case. Restore production byte-for-byte after each mutation and rerun the focused cases.
- [ ] Update the test disposition to name the truthful coverage and the dated investigation note to record the observed mutation failures/restorations. Close only this Apply-test finding; preserve the remaining N-18 travel-effect and generator findings. Compare ADR-0028 and the feature matrix; record why neither changes.
- [ ] Run `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~TravelEventApplyTests` and `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`; then commit through the normal check-only hook and require its `py -3 tools/run.py ci --check` staged-candidate gate to pass.

## Task 3: Review and deliver N-40

- [ ] Build the whole-branch review package against the recorded `develop` base and have a fresh independent reviewer inspect the exact committed diff, row-17 scope, ADR-0028, N-18, test dispositions and feature matrix. The user has given standing authorization for an independent reviewer on each plan in this epic; do not ask again.
- [ ] Resolve any Critical or Important finding with a scoped failing/passing test and re-review the exact fix; retain documented disposition for non-blocking observations.
- [ ] Open a Draft PR to `develop`, verify its body and exact head, mark it ready after the review is clean, and require the exact-head canonical hosted gate to pass.
- [ ] Squash-merge the reviewed PR, verify the merge commit and passing develop-push canonical gate, then fast-forward the primary checkout to `origin/develop`.
- [ ] Remove the clean N-40 worktree before deleting its exact local/upstream refs, and verify the worktree registration and both refs are absent. If the helper reports a locked directory, stop retrying it and record the bounded host-cleanup residue without touching unrelated worktrees.
