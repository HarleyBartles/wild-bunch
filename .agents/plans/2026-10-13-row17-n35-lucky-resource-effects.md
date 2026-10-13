# N-35 Prove Lucky Trail Resource Effects Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make Domain behavior tests prove Lucky Water Seep and Lucky Coin Cache change the player's actual canteen and wallet state and reconstruct those changes from the event stream.

**Architecture:** Strengthen the existing `TravelResolverTests` command scenarios at the `GameSession.AdvanceJourneyDay` owner. Assert the selected trail event, its recorded `TrailEventApplied` fact, the resulting player resource, and full event replay without changing production behavior.

**Tech Stack:** C# 14, .NET 10, xUnit, Wild Bunch Domain event sourcing.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), row 17 of the [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), and N-18 in the [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md).

**Execution Strategy:** Native inline execution via `executing-plans`. Both event cases exercise the same command, application, and replay boundary in one test owner, while the dated audit and roadmap updates share the same evidence. One continuous implementation context plus one fresh whole-branch review is more useful than per-task implementer/reviewer handoffs for these coupled assertions.

## Global Constraints

- N-34 merged as `0.1.0-dev.152`; advance the sole authored application version in `Directory.Build.props` to `0.1.0-dev.153` exactly once.
- Retain strict CQRS, event sourcing, immutable event facts, deterministic event application, and event-stream-only replay reconstruction.
- Keep this a test-quality correction; do not change game behavior, event identity or shape, feature promises, or architecture decisions.
- Retire the completed N-34 plan only in this successor's first substantive commit; keep this N-35 plan through its completing PR.
- Use a fresh worktree from `origin/develop`, target the PR at `develop`, pass exact-head review and hosted CI before merge, then verify the develop-push gate and paired worktree/local/upstream branch cleanup.

## Task 1: Record N-34 delivery and commit the N-35 successor plan

**Files:**
- Create: `.agents/plans/2026-10-13-row17-n35-lucky-resource-effects.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n34-retire-orphan-trail-event-catalog.md`

- [x] Verify N-34 PR #337 merged as `d0e8ed6525909ddc6a5377cd6c8d88ca2d005343`, its tree matches reviewed source `31258d6f9100e8ae9955f595d469c83ab4aaabfe`, both hosted gates passed, the primary checkout is current, and the exact N-34 worktree and branch refs are absent.
- [x] Retire the N-34 plan, record its PR, source/merge/tree identities, review and hosted-gate outcomes in row 17, name this N-35 plan as the active successor, and set `Directory.Build.props` to `0.1.0-dev.153`.
- [x] Review the staged plan-transition diff and commit it before beginning the behavior-test task.

## Task 2: Prove Lucky water and cash effects through event application and replay

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

- [x] In `AdvanceJourneyDayCanTriggerAnAdditionalLuckyWaterSeepEventOnEasyDryRoutes`, record and assert the fixture's initial canteen state, require `LuckyWaterSeep`, select the unique matching `TrailEventApplied` fact with a positive actual canteen-charge delta, then assert its post-event snapshot, the expected live journey/inventory charges, and the same reconstructed charges after `GameSession.RehydrateFromEvents`. The deterministic fixture currently records two further water-seep applications at full capacity with zero actual delta; preserve them without asserting a false single-application shape.
- [x] In `AdvanceJourneyDayCanTriggerALuckyCoinCacheOnHardBadlandsRoutes`, assert starting cash, require `LuckyCoinCache`, select the unique matching `TrailEventApplied` fact with a positive wallet delta, then assert its recorded delta and absolute cash, the resulting player wallet, and full replay wallet agree with the independently computed starting-cash-plus-applied-delta result. The deterministic day also records a separate Water Seep fact; preserve it without asserting a false single-event shape.
- [x] Run the canteen scenario with `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~AdvanceJourneyDayCanTriggerAnAdditionalLuckyWaterSeepEventOnEasyDryRoutes" --logger "console;verbosity=minimal"`, then prove it detects the effect by temporarily omitting `_journey.SetCanteenCharges(nextCharges)` in `JourneyLoop.ApplyTrailEvent`; expect the unique-positive-fact, snapshot, or resulting-charge assertion to fail and restore the production file byte-for-byte.
- [x] Run the wallet scenario with `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~AdvanceJourneyDayCanTriggerALuckyCoinCacheOnHardBadlandsRoutes" --logger "console;verbosity=minimal"`, then prove it detects the effect by temporarily making `runtime.WalletCash += trailEvent.WalletDelta` in `JourneyLoop.ApplyTrailEvent` a no-op; expect the wallet-result assertion to fail and restore the production file byte-for-byte.
- [x] Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~TravelResolverTests" --logger "console;verbosity=minimal"`; expect the complete `TravelResolverTests` class to pass with no skips.
- [x] Add a dated N-35 disposition under N-18 and in the `TravelResolverTests.cs` inventory row, state that only Lucky canteen/cash consumer and replay proof is closed, and leave unrelated travel effects and generator findings open.
- [x] Review the focused test and audit diff, then commit through the staged-candidate hook.

## Task 3: Review and deliver N-35

- [ ] Compare the final diff with the baseline spec, N-18 audit, current feature matrix, applicable ADRs, and selected Domain/test-quality unslop profiles; state why the product promise and durable event decisions remain truthful.
- [ ] Build the whole-branch review package and dispatch a fresh-context independent reviewer; resolve any material finding with a witnessed failing test and focused correction.
- [ ] Open a Draft PR to `develop`, read back and verify its exact head and description, then mark it ready after independent review so hosted CI runs on the reviewed head.
- [ ] Wait for exact-head hosted CI to pass, squash-merge the N-35 PR, verify its merge tree and successful develop-push gate, then fast-forward the primary checkout.
- [ ] Remove the clean N-35 worktree, delete only its matching local and upstream branch refs, and verify all three are absent before starting another plan.

## Review Focus

- A positive event payload alone does not prove that `GameSession.AdvanceJourneyDay` changed the resource: assert the applied event fact and resulting player resource.
- Daily upkeep also changes canteen charges on the water fixture: assert the initial state, applied seep delta, absolute post-event snapshot, live state, and replay state as separate facts.
- A cash cache must survive event replay as wallet state: compare the result to an independently captured starting wallet plus the applied event delta.
- Keep event IDs, event payloads, production behavior, PG-007 truth, and ADR truth unchanged; the new coverage closes only the test gap recorded under N-18.
