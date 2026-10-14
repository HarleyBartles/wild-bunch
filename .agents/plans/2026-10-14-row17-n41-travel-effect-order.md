# N-41 Travel Upkeep Before Event Generation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Prove that a horse made unfit by daily upkeep is removed from the horse-required travel-event candidate set before that day’s plan is generated.

**Architecture:** Keep the Domain loop and generator unchanged unless a behavior defect is demonstrated. Add a deterministic aggregate behavior test at the travel command owner, backed by a fixed generator case proving the selected seed would reach the horse-required event if generation observed the pre-upkeep state. This makes the sequence observable without asserting private call order or duplicating production logic.

**Tech Stack:** .NET 10, xUnit, Wild Bunch Domain test fixtures and seeded travel generation.

**Spec:** The [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), row 17 of the [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), the [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), the [test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), event-sourcing integrity doctrine, and ADR-0028.

**Execution Strategy:** `executing-plans` (Native inline execution; this is one sequential behavior-test slice).

## Global Constraints

- Preserve the user's rule that a horse that dies on the trail is no longer in inventory, while its saddle remains.
- Keep event history factual and replay authoritative; test the original ordered `AllEvents` stream.
- Do not change production behavior, event contracts, ADRs, feature promises, or unrelated generator findings unless the test reveals a defect required by this acceptance behavior.
- Advance the single authored development version to `0.1.0-dev.159` in this slice.
- Use a fresh canonical worktree and PR to `develop`; require independent review, exact-head hosted CI and develop-push CI; retire the worktree before its paired local and remote branch refs.

## Review Focus

- The selected seed reaches `BadLuckSpookedHorse` when generation sees a living horse, so a passing negative assertion is not explained by an unreachable event.
- Daily upkeep crosses the Challenging mountain-route horse from alive to dead before the day plan is generated; the command’s generated day must not apply a horse-required event to that dead horse.
- Replay from the exact session event history reproduces the horse/inventory and journey result without rerolling.

---

## Task 1: Record N-40 delivery and commit the N-41 plan

**Files:**
- Create: `.agents/plans/2026-10-14-row17-n41-travel-effect-order.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-14-row17-n40-badluck-travel-apply-proof.md`

**Interfaces:**
- Consumes: merged N-40 source `b82307a0e8f2489b72b00f3672937c01ffec92ca`, PR #343, merge `8e8544a3d3a3c1e81d874f28d8d563bf114288f0`, exact-head gate run `38526496026`, develop-push gate run `38527176432`, the clean paired worktree/ref retirement, and current `develop`.
- Produces: an active N-41 plan, N-40 successor retirement and dated delivery record, and application version `0.1.0-dev.159`.

- [x] Verify PR #343 merged the exact N-40 branch head to `develop`, both hosted gates passed, the primary checkout is at merge `8e8544a3d3a3c1e81d874f28d8d563bf114288f0`, and the N-40 worktree plus local/upstream refs are absent.
- [x] Classify N-40 as delivered: its only scope was truthful isolated Apply coverage for `TravelDayAdvanced` and `JourneyEncounterResolved`. Record review, test, hosted-gate, merge and cleanup evidence; select N-41 in row 17 and set `Directory.Build.props` to `0.1.0-dev.159`.
- [x] Commit the N-40 retirement, N-41 plan, roadmap update and version transition before editing tests.

## Task 2: Prove upkeep-dependent horse-event eligibility

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`

**Interfaces:**
- Consumes: `CreateProgressionSession`, canonical `StartJourney` and `AdvanceJourneyDay`, Challenging travel rules, mountain upkeep, `TravelDayPlanGenerator`, `TrailEventApplied`, and exact-stream `GameSession.RehydrateFromEvents`.
- Produces: a deterministic aggregate behavior proof that day upkeep can kill a previously mounted horse before generation and that the reachable `BadLuckSpookedHorse` outcome is then excluded; replay restores the same settled state.

- [ ] Construct a Challenging Mountains journey with a living horse at exhaustion 2. Prove the start preview is mounted and daily upkeep crosses the horse's death threshold before generation.
- [ ] Discover one fixed game seed using the existing generator and context builders where the same eligible pre-upkeep context reaches `BadLuckSpookedHorse`; use that seed in the aggregate scenario and demonstrate that the post-upkeep command path excludes it.
- [ ] Assert the resulting command state removes the dead horse but retains the saddle, the day result contains no `BadLuckSpookedHorse` application, and the command event stream records the actual settled journey state.
- [ ] Rehydrate from the original ordered `AllEvents` stream and assert the complete relevant journey, horse inventory and clock state matches the command result.
- [ ] Prove the test's bite by temporarily disabling the horse-required candidate exclusion or supplying the pre-upkeep horse eligibility to generation; the focused test must fail for an attempted spooked-horse outcome. Restore production byte-for-byte.
- [ ] Update the two audit dispositions to close only the ordering/eligibility gap. Compare ADR-0028 and the feature matrix; record why neither changes.
- [ ] Run the focused `TravelResolverTests` and complete Domain test project, then commit through the check-only hook and require its fail-fast `py -3 tools/run.py ci --check` staged-candidate gate to pass.

## Task 3: Review and deliver N-41

- [ ] Build the whole-branch review package against the recorded `develop` base and have a fresh independent reviewer inspect the committed diff, row-17 scope, ADR-0028, N-18, dispositions, investigation and feature matrix. The user has given standing authorization for an independent reviewer on every plan in this epic.
- [ ] Resolve any Critical or Important finding with a scoped failing/passing test and re-review the exact fix; preserve documented disposition for nonblocking observations.
- [ ] Open a Draft PR to `develop`, verify body and exact head, mark ready after clean review, and require exact-head canonical hosted CI.
- [ ] Squash-merge the reviewed PR, verify the merge commit and passing develop-push canonical CI, then fast-forward the primary checkout to `origin/develop`.
- [ ] Remove the clean N-41 worktree before deleting its exact local/upstream refs, and verify the worktree registration and both refs are absent.
