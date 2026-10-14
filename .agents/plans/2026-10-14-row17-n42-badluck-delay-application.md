# N-42 BadLuck Delay Application and Replay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Prove that a reached `BadLuckWashout` delay is recorded in the travel event stream, changes the live journey, and reconstructs from the exact original event history.

**Architecture:** Strengthen the existing deterministic low-risk aggregate scenario at the `GameSession` travel command owner. Keep production code unchanged unless the behavior test demonstrates a defect; use the typed event and absolute journey snapshots as the replay authority rather than reconstructing or rerolling the outcome.

**Tech Stack:** .NET 10, xUnit, Wild Bunch Domain travel fixtures, deterministic entropy, typed domain events and event replay.

**Spec:** The [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), row 17 of the [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), the [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), the [test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), validation and event-sourcing doctrine, and ADR-0028.

**Execution Strategy:** `executing-plans` (Native inline execution; one sequential behavior-test slice).

## Global Constraints

- Keep the existing low-risk Mountains/Creek route, Standard difficulty and fixed salt source; it deterministically reaches `BadLuckWashout`.
- Assert established outcomes through the command result, `TrailEventApplied`, `TravelDayAdvanced`, live journey and exact ordered `AllEvents` replay.
- Do not turn an arbitrary tuning value into a permanent fixture; assert the positive delay and that the recorded absolute state reflects that applied delay.
- Do not change production behavior, event contracts, ADRs, feature promises or unrelated travel/generator findings unless the new behavior assertion demonstrates a required defect.
- Advance the single authored development version to `0.1.0-dev.160` in this slice.
- Use the fresh canonical worktree and PR to `develop`; require independent review, exact-head hosted CI and develop-push CI; retire the worktree before its paired local and remote branch refs.

## Review Focus

- The fixed fixture reaches the specific `BadLuckWashout`, so the effect and replay checks cannot pass by observing a different BadLuck event or no event.
- The evented command records the delay and its journey snapshot; the live journey reflects a positive delay after advancing one travel day.
- Rehydrating the exact ordered `AllEvents` stream reproduces the command's delay, remaining route progress, journey status, clock and relevant inventory.
- This closes only the missing specific event-fact and replay proof for the existing low-risk BadLuck scenario; it does not claim all travel events or generators are covered.

---

## Task 1: Record N-41 delivery and commit the N-42 plan

**Files:**
- Create: `.agents/plans/2026-10-14-row17-n42-badluck-delay-application.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-14-row17-n41-travel-effect-order.md`

**Interfaces:**
- Consumes: merged N-41 source `f4cc0d235c26adf2d2658bef5ff2230351a28d5d`, PR #344, squash merge `a77c5c2aa84b22ee40bbb4c08cadf3b68b668063`, exact-head gate run `38531350891`, develop-push gate run `38531933613`, clean N-41 worktree/ref retirement, and current `develop`.
- Produces: an active N-42 plan, successor retirement and dated delivery record for N-41, and application version `0.1.0-dev.160`.

- [x] Verify PR #344 merged the exact N-41 branch head to `develop`, both hosted gates passed, the primary checkout is at merge `a77c5c2aa84b22ee40bbb4c08cadf3b68b668063`, and the N-41 worktree plus local/upstream refs are absent.
- [x] Record N-41's independent review, test and hosted-gate outcomes in row 17; select N-42 and advance `Directory.Build.props` to `0.1.0-dev.160`.
- [x] Commit the N-41 retirement, N-42 plan, roadmap update and version transition before changing tests.

## Task 2: Prove low-risk BadLuck delay facts and exact replay

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`

**Interfaces:**
- Consumes: `CreateLuckyFootSession`, canonical `StartJourney` and `AdvanceJourneyDay`, fixed Classic entropy and salt source, `BadLuckWashout`, `TrailEventApplied`, `TravelDayAdvanced`, and exact-stream `GameSession.RehydrateFromEvents`.
- Produces: a deterministic aggregate behavior test proving the selected washout is recorded and its delay survives command state and event replay; an accurately narrowed test-audit disposition.

- [x] Replace the low-risk smoke assertions with a specific `BadLuckWashout` assertion while retaining the actual journey start and day-advance commands.
- [x] Require the matching `TrailEventApplied` fact and positive delay; require its journey snapshot, the `TravelDayAdvanced` snapshot and live journey to reflect the delay and remaining progress after one day advances.
- [x] Rehydrate from the original ordered `AllEvents` stream and compare delay, remaining route distance/days, status, days travelled, clock and relevant inventory with command state.
- [x] Prove the added oracle's bite by temporarily removing the washout delay from the established command outcome; the focused test must fail on the delayed journey state. Restore production byte-for-byte.
- [x] Update the test dispositions and investigation to close only the low-risk washout event-fact/replay gap. Compare ADR-0028 and the feature matrix; record why neither changes.
- [x] Run the focused test and complete Domain test project, then commit through the check-only hook and require its fail-fast `py -3 tools/run.py ci --check` staged-candidate gate to pass.

## Task 3: Review and deliver N-42

- [ ] Build the whole-branch review package against the recorded `develop` base and have a fresh independent reviewer inspect the committed diff, row-17 scope, ADR-0028, N-18, dispositions and feature matrix. The user has given standing authorization for an independent reviewer on every plan in this epic.
- [ ] Resolve any Critical or Important finding with a scoped failing/passing test and re-review the exact fix; preserve documented disposition for nonblocking observations.
- [ ] Open a Draft PR to `develop`, verify its body and exact head, mark ready after clean review, and require exact-head canonical hosted CI.
- [ ] Squash-merge the reviewed PR, verify the merge commit and passing develop-push canonical CI, then fast-forward the primary checkout to `origin/develop`.
- [ ] Remove the clean N-42 worktree before deleting its exact local/upstream refs, and verify the worktree registration and both refs are absent.
