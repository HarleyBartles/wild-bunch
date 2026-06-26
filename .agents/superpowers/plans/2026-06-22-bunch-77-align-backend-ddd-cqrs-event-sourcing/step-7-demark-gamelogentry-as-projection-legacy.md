# Step 7 — Demote `GameLogEntry` to Projection-Legacy (Stop the Bleed)

> Parent plan: `../2026-06-22-bunch-77-align-backend-ddd-cqrs-event-sourcing.md`
> Acceptance criteria covered: **AC-005** (existing misleading log path corrected or explicitly deprecated).

## Goal

Mark `GameLogEntry` and the `AddLogEntry` pathway as **legacy projection-only output**, not event-source truth. Stop adding new `AddLogEntry` call sites in domain code. The migrated flows (start game, purchase) now produce typed domain events and derive narration from projectors (Step 4); the legacy `AddLogEntry` calls in those flows are kept temporarily with `#pragma` suppression but are no longer the source of player-facing narration — the diary projector is.

This step is **corrective but non-breaking**. The existing `LogEntries` DTO field and persistence row are preserved. The change is:

1. `[Obsolete]` on `GameLogEntry` and `AddLogEntry` with a redirect to ADR-0028 and the projection pathway.
2. `#pragma warning disable CS0618` around existing `AddLogEntry` call sites with a follow-up-removal comment.
3. A `LegacyLogProjector` that derives `GameLogEntry`-shaped rows from typed domain events, so future work can switch the `LogEntries` DTO to projection-derived without scraping `GameSession`.
4. For the migrated flows, the handler (Step 5) uses the diary projector for player-facing narration, not `LogEntries`.

## Files

- Modify: `src/WildBunch.Domain/Game/GameLogEntry.cs` — add `[Obsolete]` on the type and on `AddLogEntry`.
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` — add `#pragma warning disable CS0618` around existing `AddLogEntry` call sites with follow-up-removal comments.
- Add: `src/WildBunch.Application/Projections/LegacyLogProjector.cs` — derives `LegacyLogEntry`-shaped rows from typed domain events.
- Add: `src/WildBunch.Application/Projections/Models/LegacyLogEntry.cs` — mirrors `GameLogEntry` fields (`Kind`, `Message`, `Day`, `Turn`).
- Modify: `docs/adr/ADR-0028-*.md` — finalize the "GameLogEntry demotion" section.
- Add: `tests/WildBunch.Application.Tests/Projections/LegacyLogProjectorTests.cs`
- Add: `tests/WildBunch.Domain.Tests/GameLogEntryObsoleteMarkerTests.cs` — reflection test asserting `[Obsolete]` is present.

## `LegacyLogProjector` contract

```csharp
public sealed class LegacyLogProjector
{
    public IReadOnlyList<LegacyLogEntry> Project(IReadOnlyList<IDomainEvent> events);
}
```

The projector maps each migrated event to a `LegacyLogEntry` matching the current `AddLogEntry` output:

- `GameStarted` → `LegacyLogEntry { Kind = Opening, Message = "The hunt begins in {townName}.", Day = 1, Turn = 0 }`
- `StoreItemPurchased` → `LegacyLogEntry { Kind = Purchase, Message = "Purchased {qty} {name} for ${total}.", Day = currentDay, Turn = currentTurn }`

For events the current `AddLogEntry` path does not log, the projector skips them. The worker cross-checks against existing `AddLogEntry` call sites.

## `[Obsolete]` strategy

- `[Obsolete("GameLogEntry is legacy projection-only output. Use IPlayerDiaryProjection / IHudFeedProjection from WildBunch.Application.Projections. See ADR-0028.")]` on `GameLogEntry` and `AddLogEntry`.
- `GameSession.cs` wraps existing call sites in `#pragma warning disable CS0618` / `restore` with: `// Legacy log pathway — see ADR-0028. To be removed in follow-up issue.`
- New code calling `AddLogEntry` fails the build (verify warning-as-error posture in `Directory.Build.props`; if not errors, add a BannedApiAnalyzers entry or test).
- The migrated flows (`StartNew`, `Purchase`) keep their `AddLogEntry` calls under `#pragma` for now, but the handler (Step 5) uses the diary projector for player-facing narration. The `LogEntries` DTO field is preserved but is no longer the primary narration source for migrated flows.

## Tasks

- [ ] **Task 1: Verify the repo's warning-as-error posture.** Read `Directory.Build.props` and analyzer config.
- [ ] **Task 2: Add `[Obsolete]` to `GameLogEntry` and `AddLogEntry`** with the ADR-0028 redirect.
- [ ] **Task 3: Add `#pragma warning disable CS0618` around existing `AddLogEntry` call sites in `GameSession.cs`** with follow-up-removal comments.
- [ ] **Task 4: Add `LegacyLogEntry` projection output model** mirroring `GameLogEntry` fields.
- [ ] **Task 5: Implement `LegacyLogProjector`** mapping `GameStarted` and `StoreItemPurchased` to `LegacyLogEntry`.
- [ ] **Task 6: Write `LegacyLogProjectorTests`.** Assert projected entries match current `AddLogEntry` output for migrated flows.
- [ ] **Task 7: Write `GameLogEntryObsoleteMarkerTests`.** Reflection test asserting `[Obsolete]` is present on `GameLogEntry` and `AddLogEntry`.
- [ ] **Task 8: Finalize ADR-0028's "GameLogEntry demotion" section.**
- [ ] **Task 9: Build and confirm no new CS0618 warnings outside `#pragma` blocks.**

## Validation

- [ ] **V1: `dotnet build`** passes. `[Obsolete]` markers do not break the build (existing call sites are `#pragma`-suppressed).
- [ ] **V2: `dotnet test`** passes.
- [ ] **V3: No API/DTO/persistence shape changes.** `git diff` on `GameDtos.cs`, `EfGameSessionRepository.cs`, and API endpoints shows no changes.
- [ ] **V4: `[Obsolete]` enforcement test passes.**
- [ ] **V5: `git status` scope.** Modified: `GameLogEntry.cs`, `GameSession.cs`, ADR-0028. Added: `LegacyLogProjector.cs`, `LegacyLogEntry.cs`, test files.

## Acceptance mapping

- **AC-005:** satisfied by `[Obsolete]` marker, `#pragma` confinement, `LegacyLogProjector` replacement, and ADR-0028 finalized demotion section. Future work cannot treat `GameLogEntry` as event-source truth.

## Non-goals for this step

- No removal of existing `AddLogEntry` call sites (follow-up removes them after `LogEntries` DTO switches to projection-derived).
- No `LogEntries` DTO field removal.
- No `GameSessionLogEntries` table removal.
- No handler/API changes (Step 5/6 already done).
- No `TravelDiaryDayState` demotion (diary projector handles diary derivation; `TravelDiaryDayState` demotion is a follow-up).
- No persistence changes.

## Follow-up issue recommendation

**"Remove legacy `GameLogEntry` pathway and switch `LogEntries` DTO to projection-derived output."** Named in the return evidence for Harley to create.

## Self-Review

**Spec coverage:** Step 7 covers AC-005 by marking the legacy pathway obsolete and providing a projection-derived replacement.

**Placeholder scan:** The follow-up issue id is a recommendation; the worker fills it if Harley creates it during the campaign.

**Type consistency:** `LegacyLogEntry` mirrors `GameLogEntry` fields exactly.

**Non-goals:** All six non-goals preserved.
