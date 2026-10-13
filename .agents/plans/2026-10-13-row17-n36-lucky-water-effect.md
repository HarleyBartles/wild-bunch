# N-36 Prove Hard Mounted Lucky Water Seep Effects Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Replace the hard mounted Lucky Water Seep payload-only test with proof that Challenging travel actually applies recovered canteen water and reconstructs it from the event stream.

**Architecture:** Keep the change in the Domain aggregate behavior test. Give the existing deterministic fixture room for a real reward, observe actual `TrailEventApplied` facts and live resource state, derive the final expected canteen charge from normal daily upkeep plus recorded applied deltas, and compare event replay. Do not change gameplay or event production.

**Tech Stack:** C# 14, .NET 10, xUnit, Wild Bunch Domain event sourcing.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), PG-007 and row 17 of the [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), and N-18 in the [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md).

**Execution Strategy:** Native inline execution via `executing-plans`. One deterministic command scenario, its behavior mutation, and its dated test-audit correction share the same fixture and oracle; splitting these dependent steps across implementation handoffs would repeat setup without adding a useful independent review boundary.

## Global Constraints

- N-35 merged as `0.1.0-dev.153`; advance the sole authored application version in `Directory.Build.props` to `0.1.0-dev.154` exactly once.
- Preserve strict CQRS, event-sourced mutation, immutable event facts, deterministic event application, and event-stream-only replay reconstruction.
- Keep the change test-only; do not alter gameplay, event identity or shape, ADRs, feature promises, or generator behavior.
- Retire the completed N-35 plan only in this successor's first substantive commit; keep this N-36 plan through its completing PR.
- Use a fresh worktree from `origin/develop`, target the PR at `develop`, pass independent review and exact-head hosted CI before merge, verify the develop-push gate, then remove the worktree and its paired local/upstream refs.

## Task 1: Record N-35 delivery and commit the N-36 successor plan

**Files:**
- Create: `.agents/plans/2026-10-13-row17-n36-lucky-water-effect.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n35-lucky-resource-effects.md`

- [x] Verify N-35 PR #338 merged as `190b2df0524dfd6555d2f73c2807c0df5ae64d66` from reviewed source `8e96f7541d69459b2f0c92d2664f5243328d9f5d`, both trees match `3bc95738cfa8e38fa04519adfa8199e7343ffa4c`, independent review found no actionable findings, exact-head gate 38476164242 and develop-push gate 38476825282 passed, the primary checkout is current, and the exact N-35 worktree and branch refs are absent.
- [x] Retire the N-35 plan, record its PR, source/merge/tree identities, review and gate outcomes in row 17, name this N-36 plan as the active successor, and set `Directory.Build.props` to `0.1.0-dev.154`.
- [x] Review the staged plan-transition diff and commit it before beginning the behavior-test task.

## Task 2: Prove the hard mounted water recovery is actually applied

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

- [ ] Rename `AdvanceJourneyDayCanTriggerALuckyWaterSeepEventOnHardMountedHillsRoutes` to describe applied canteen recovery and replay. Its only caller of `CreateHardMountedHorseSession` permits the fixture to start with an explicit partially filled `CanteenState(7, 10)`, instead of a full canteen whose Lucky payload can produce no actual reward.
- [ ] Keep the normal Challenging mounted Hills/River journey and actual `AdvanceJourneyDay` command. Assert the initial canteen state, reached `LuckyWaterSeep`, and generated reward equals `session.TravelRules.LuckyTrailWaterRecovery`.
- [ ] Collect the day's `TrailEventApplied` facts for `LuckyWaterSeep` without asserting an event count. Require at least one matching fact with a positive actual canteen delta. This must fail if the event payload remains positive while event application leaves canteen state unchanged.
- [ ] Independently calculate daily canteen upkeep with `JourneyUpkeepRules.ApplyDailyUpkeep` for the fixture's Hills/River route, healthy horse, initial canteen, zero horse feed, and current travel rules. Add all recorded actual canteen deltas from that command, applying the canteen capacity bound, then assert the resulting absolute charges in the returned/live journey, player inventory, and the same state after `GameSession.RehydrateFromEvents`. Preserve any additional same-day applied facts without assuming one event per day.
- [ ] Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~AdvanceJourneyDayAppliesHardMountedLuckyWaterSeepToTheCanteenAndReplays -v minimal`, then temporarily suppress `_journey.SetCanteenCharges(nextCharges)` in `JourneyLoop.ApplyTrailEvent`; verify the new test fails on the missing actual applied delta or resource result, and restore the production file byte-for-byte.
- [ ] Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~TravelResolverTests -v minimal`; expect the complete class to pass with no skips.
- [ ] Add a dated N-36 disposition and update the `TravelResolverTests.cs` inventory row to close only the hard mounted Lucky Water Seep application/replay gap. State that the prior full-canteen payload-only test did not prove resource gain, record the mutation result, and leave the remaining BadLuck, completion, and generator findings open.
- [ ] Review the focused test and audit diff, then commit through the staged-candidate hook.

## Task 3: Review and deliver N-36

- [ ] Compare the final diff with PG-007, N-18, ADR-0028, and the feature matrix through their playbooks; apply the backend-architecture unslop profile and testing doctrine, and state why gameplay, current promise, and durable event decisions remain truthful.
- [ ] Build the whole-branch review package and dispatch a fresh-context independent reviewer; resolve material findings with a witnessed failing test and focused correction.
- [ ] Open a Draft PR to `develop`, read back and verify its exact head and description, then mark it ready after independent review so hosted CI runs on the reviewed head.
- [ ] Wait for exact-head hosted CI to pass, squash-merge the N-36 PR, verify its merge tree and successful develop-push gate, then fast-forward the primary checkout.
- [ ] Remove the clean N-36 worktree, delete only its matching local and upstream branch refs, and verify all three are absent before starting another plan.

## Review Focus

- A positive generated canteen delta does not prove resource gain when the canteen begins full; the fixture must have capacity for the effect.
- Daily upkeep and one or more same-day trail effects can both alter canteen charges; independently compute the upkeep baseline and use actual applied event deltas without freezing event count.
- The replay source is the original complete event stream; compare the reconstructed canteen to the independently derived command result.
- Keep event IDs, event payloads, gameplay behavior, PG-007 truth, and ADR-0028 truth unchanged; close only the N-18 test gap for this hard mounted effect.
