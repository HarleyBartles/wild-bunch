# Row 17 N-22 Journal Trail-Day Projection Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Prove that the player journal dates a `TrailEventApplied` narration on the absolute day recorded by the immediately following `TravelDayAdvanced` event.

**Architecture:** Keep the event stream as the sole history authority and extend `JournalLogProjectorTests` at its existing application projection owner. The production domain emits `TrailEventApplied` before the corresponding `TravelDayAdvanced`; the projector must use that following event's absolute `Day` for the trail narration without changing event order or production behavior.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.Application.Tests`.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality findings](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), “Finish gaps at their existing behavioral owners”; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), `JournalLogProjectorTests`.

**Execution Strategy:** `executing-plans`, inline and sequential. This slice adds one precise behavior assertion and its dated disposition at the same projection owner; a separate implementation context would duplicate the event-order analysis without independent work to parallelize.

## Review Focus

- The test must use the actual event order produced by `JourneyLoop`: `TrailEventApplied` before `TravelDayAdvanced`.
- The asserted trail entry must carry the next event's absolute day and the trail event's journey sequence; a projector that leaves the trail entry on the previous day must fail.

## Global Constraints

- Start from refreshed `origin/develop` and deliver one PR to `develop`.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.140`; it remains the sole authored application version.
- Retire N-21 only after its complete scope is verified in PR #324; preserve this active N-22 plan through its completing PR.
- Do not change production behavior, event payloads, feature promises, ADR decisions, or unrelated projector behavior.
- Follow the check-only staged-snapshot hook and fail-fast canonical validation; do not use `--no-verify`.
- Before merge, pass fresh whole-branch review and exact-head hosted CI. After merge, verify develop-push CI, fast-forward the primary checkout, then remove this clean worktree and its exact local/upstream branch refs together.
- Preserve unrelated worktrees and branch refs.

---

### Task 1: Retire N-21 and install N-22

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n21-map-output-behavior.md`
- Add: `.agents/plans/2026-10-13-row17-n22-journal-trail-day.md`

**Interfaces:**
- Consumes: N-21 PR #324 source `c601db04d0e16abc5bae2ec042f8ba23a8a20781`, squash merge `1c0c5d02e02a5a2b135b361b00b38eb7bbed8f12`, identical tree `2087db62f0dda6d7ba2cf62f2c59b1482e7225ab`, exact-head hosted gate `38389160945`, develop-push gate `38389708157`, approved independent review, and verified paired worktree/local/upstream branch cleanup.
- Produces: active N-22 plan at development version `0.1.0-dev.140`, N-21 retirement, and dated N-21 delivery evidence in row 17.

- [x] **Step 1: Record N-21 delivery and select this successor.** Add PR #324 to row 17 with its reviewed source, merge identity/tree, hosted checks, review, and paired cleanup evidence. Point the active row to N-22, advance the version to `.140`, and say N-21 closed four map-output test findings while this successor addresses the remaining trail-event journal day finding.
- [x] **Step 2: Retire N-21 and commit the plan before test edits.** Confirm N-21's complete scope is shipped and its durable findings remain in the investigation/disposition documents. Delete its completed plan, add this plan, validate version and links, and commit only these planning/version changes before changing tests.

### Task 2: Prove trail-event narration uses the recorded travel day

**Files:**
- Modify: `tests/WildBunch.Application.Tests/Projections/JournalLogProjectorTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

**Interfaces:**
- Consumes: `JourneyLoop.ApplyTrailEvent`, which appends `TrailEventApplied` before `HandleOngoingTravelDay` or `HandleCompletedTravelDay` appends `TravelDayAdvanced`; `JournalLogProjector.Project` uses the immediate next event's absolute day for the trail narration.
- Produces: an exact projected entry sequence that distinguishes the previous day from the next recorded day and confirms the travel journey sequence, plus dated closure of this journal-projection finding while leaving the dev-context and Domain-arrival findings open.

- [x] **Step 1: Add the focused failing projection test.** Add this test to `JournalLogProjectorTests`; use the existing `GameStartedEvent` and `JourneySnapshot` helpers:

```csharp
[Fact]
public void TrailEventApplied_BeforeTravelDayAdvanced_UsesAdvancedDayInJournal()
{
    var journey = JourneySnapshot(1);
    IDomainEvent[] events =
    [
        GameStartedEvent(),
        new JourneyStarted { JourneySnapshot = journey, DiaryMessage = "The road opens.", PursuitHeat = 0 },
        new TrailEventApplied
        {
            JourneySnapshot = journey,
            TrailEventKind = JourneyTrailEventKind.Lucky,
            TrailEventId = JourneyTrailEventId.LuckyCoinCache,
            Title = "A forgotten cache",
            Message = "I find biscuits beside the trail.",
            WalletDelta = 0m,
            WalletCash = 25m,
            FoodDelta = 0,
            CanteenChargeDelta = 0,
            HorseHungerDelta = 0,
            HorseThirstDelta = 0,
            HorseExhaustionDelta = 0,
            DelayDays = 0,
            HeatIncrease = 0,
            PursuitHeat = 0,
            TravelModeChangedTo = null,
            DiaryMessage = "I find biscuits beside the trail.",
            HorseLostMessage = string.Empty
        },
        new TravelDayAdvanced
        {
            Day = 2,
            JourneySnapshot = journey,
            HealthDelta = 0,
            PursuitHeat = 0,
            DayOutcome = TravelDayOutcome.Ongoing,
            DiaryMessage = "Another day on the trail.",
            HorseLostMessage = string.Empty
        }
    ];

    var entries = new JournalLogProjector().Project(events);

    Assert.Equal(
        new GameLogEntry[]
        {
            new(GameLogEntryKind.Opening, "The hunt begins in Pinecross.", 1, 0),
            new(GameLogEntryKind.Travel, "The road opens.", 1, 0, 1),
            new(GameLogEntryKind.Travel, "I find biscuits beside the trail.", 2, 0, 1),
            new(GameLogEntryKind.Travel, "Another day on the trail.", 2, 0, 1)
        },
        entries);
}
```

The exact sequence makes a projector that leaves trail narration on day 1 fail specifically at the trail entry, while also requiring the separate day-advance narration and journey identity.
- [x] **Step 2: Prove RED and GREEN at the actual behavior.** Temporarily remove the projector's immediate-following `TravelDayAdvanced` look-ahead so `TrailEventApplied` uses the current day; run only the new test and require it to fail because the trail entry is day 1 instead of day 2. Restore production code and require the same test to pass.
- [x] **Step 3: Record the finding disposition.** Add a dated entry stating that `JourneyLoop` emits trail-event facts before the absolute travel-day fact, the exact journal test now proves the trail narration receives that next event's day, and the mutation failed as intended. Update the `JournalLogProjectorTests` disposition; preserve the remaining dev mapper and Domain arrival/context findings as open. Do not describe this projection behavior as a product feature change or an ADR change.
- [ ] **Step 4: Run owning validation and commit.** Run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter FullyQualifiedName~JournalLogProjectorTests`, inspect the test and dated disposition diff, then commit. The commit hook runs the full canonical gate; do not redundantly run the full gate immediately before committing.

### Task 3: Review, publish, merge, and close out

**Files:**
- Review: the complete branch diff and staged-candidate validation result.

- [ ] **Step 1: Obtain a fresh whole-branch review.** Build a review package from the refreshed develop merge base to final HEAD and review the code, tests, dispositions, N-21 retirement, and roadmap against the plan, ADR-0028, and repository review runbook. Correct actionable findings with witnessed behavior tests and rerun affected validation.
- [ ] **Step 2: Publish and merge.** Push this branch and open one PR to `develop`. Verify the PR head equals local `HEAD`, it targets `develop`, and exact-head hosted CI passes before merging. The next successor records this PR's delivery and paired cleanup evidence in row 17.
- [ ] **Step 3: Verify and retire the paired checkout.** Confirm develop-push CI passed, fast-forward the primary checkout, verify merge identity and resulting tree, and remove the clean worktree before deleting this plan's exact local and upstream branch refs. Confirm the worktree and both refs are absent; preserve unrelated worktrees and refs.
