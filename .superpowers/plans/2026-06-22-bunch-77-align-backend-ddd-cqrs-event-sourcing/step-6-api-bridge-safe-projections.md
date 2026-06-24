# Step 6 — API Bridge: Safe Player-Facing Projections Only

> Parent plan: `../2026-06-22-bunch-77-align-backend-ddd-cqrs-event-sourcing.md`
> Acceptance criteria covered: **AC-007** (UI-facing bridge ready for the next slice; source truth remains the event stream/projection pipeline).

## Goal

Expose the **safe** projected player events from Step 5 (`CommandProjectionResult` — diary + HUD only) through the existing synchronous API endpoints as an additive bridge. The existing `Message` field and current response shape are preserved for backward compatibility. **No raw domain events, no raw payloads, no full audit entries, and no case-file internal truth are exposed to player-facing API responses.** Hidden culprit boundaries (ADR-0007) are preserved.

This is the key safety correction from the rejected draft, which exposed raw events and full audit to the player-facing API.

## Files

- Modify: `src/WildBunch.Application/Games/Models/GameDtos.cs` — extend the command result DTOs with **safe** projection fields only (diary + HUD). Existing `Message` and current fields stay.
- Modify: `src/WildBunch.Api/` — the minimal API endpoints (per ADR-0015) that return command results. Each migrated endpoint maps `CommandProjectionResult` to the extended DTO.
- Add: `src/WildBunch.Application/Games/Models/ProjectionDtos.cs` — DTO shapes for `PlayerDiaryEntryDto` and `HudFeedEntryDto` only. No `AuditEntryDto`, no `GameEventDto`, no `CaseFileViewEntryDto` (unless Harley explicitly approves a safe subset).
- Add: `tests/WildBunch.Api.Tests/` (or extend existing) — endpoint tests asserting safe fields are present and unsafe fields are absent.

## DTO extension shape

The existing command result DTO gains **safe fields only**:

```csharp
public sealed class StartNewGameResponse
{
    // Existing fields preserved:
    public Guid SessionId { get; init; }
    public string Message { get; init; }

    // New safe bridge fields — player-facing projections only:
    public IReadOnlyList<PlayerDiaryEntryDto> DiaryEntries { get; init; }
    public IReadOnlyList<HudFeedEntryDto> HudEntries { get; init; }

    // NOT present:
    // - No IReadOnlyList<GameEventDto> Events
    // - No IReadOnlyList<AuditEntryDto> AuditEntries
    // - No IReadOnlyList<CaseFileViewEntryDto> CaseFileEntries (unless safe subset approved)
    // - No payload JSON
    // - No raw domain event types
}
```

The same extension applies to `PurchaseStoreItemResponse`. The worker confirms actual DTO names from `GameDtos.cs`.

## Safe projection DTO shapes

```csharp
public sealed record PlayerDiaryEntryDto
{
    public required int Sequence { get; init; }
    public required int Day { get; init; }
    public required string Narrative { get; init; }  // first-person past tense, safe
}

public sealed record HudFeedEntryDto
{
    public required int Sequence { get; init; }
    public required string Notice { get; init; }     // second-person present tense, safe
    public required string Severity { get; init; }   // "info" | "warning" | "danger"
}
```

These are the **only** projection DTOs exposed to the player-facing API. They carry no envelope metadata, no event type strings, no payload JSON, no correlation/causation IDs. The player sees diary narratives and HUD notices — nothing else.

## What is NOT exposed to the player-facing API

- **Raw domain events** (`IDomainEvent` instances, typed event records) — not in the response.
- **Full audit entries** (`AuditEntry`) — developer/replay surface only, not player-facing.
- **Case file view entries** (`CaseFileViewEntry`) — not exposed by default. If Harley approves a safe subset in a future step, only public clues/warrants are included, never hidden culprit truth (ADR-0007).
- **Event envelope metadata** (EventId, StreamId, Sequence as infrastructure, OccurredAtUtc, SchemaVersion, CorrelationId, CausationId) — not in the response.
- **Payload JSON** — not in the response.

The API response is a **safe projection bridge**, not an event dump. Source truth remains the event stream + projections (recorded in ADR-0028 and enforced by the handler orchestration in Step 5).

## Backward compatibility

- The existing `Message` field is preserved on every response. The frontend (BUNCH-56 SPA shell) continues to work without consuming the new fields.
- New fields are additive and default to empty lists.
- No field is removed or renamed.
- The frontend typed API client (ADR-0019) is updated in a follow-up UI slice, not in this campaign.

## Tasks

- [ ] **Task 1: Read the current command response DTOs in `GameDtos.cs` and the API endpoints.** Confirm actual names.
- [ ] **Task 2: Add `ProjectionDtos.cs`** with `PlayerDiaryEntryDto` and `HudFeedEntryDto` only. No audit DTO, no event DTO, no case-file DTO.
- [ ] **Task 3: Extend each migrated command response DTO** (`StartNewGameResponse`, `PurchaseStoreItemResponse`) with `DiaryEntries` and `HudEntries` only. Preserve existing fields. Default new fields to empty lists.
- [ ] **Task 4: Update each migrated API endpoint** to map `CommandProjectionResult` to the extended DTO. The mapping is a pure function that copies diary + HUD entries and preserves the existing `Message`.
- [ ] **Task 5: Write API endpoint tests.** For each migrated flow, assert: (a) existing fields unchanged, (b) `DiaryEntries` and `HudEntries` populated, (c) **no** `Events`, `AuditEntries`, `CaseFileEntries`, or payload fields exist on the response DTO (reflection check — safety enforcement).
- [ ] **Task 6: Write a backward-compatibility test.** Assert a client ignoring the new fields still gets the existing `Message` and other fields with the same values.
- [ ] **Task 7: Update ADR-0028's §API safety section** with the actual DTO field names and the safety contract.

## Validation

- [ ] **V1: `dotnet build`** passes.
- [ ] **V2: `dotnet test`** (full suite) passes.
- [ ] **V3: `.\scripts\postgres-dev.ps1 test -- dotnet test`** — PostgreSQL-backed tests pass end-to-end.
- [ ] **V4: No domain/handler/persistence changes.** `git status` shows only `WildBunch.Api/**`, `WildBunch.Application/Games/Models/**`, and test files.
- [ ] **V5: Safety enforcement.** The reflection test in Task 5 confirms no unsafe fields exist on the response DTOs.
- [ ] **V6: API contract is additive.** No field removed/renamed.

## Acceptance mapping

- **AC-007 (UI-facing bridge ready for the next slice):** fully satisfied. Synchronous command responses expose safe projected player events (diary + HUD) as a bridge. Source truth remains the event stream + projections. No raw events, no audit, no payloads, no hidden truth. The frontend type compatibility is preserved (additive fields).

## Non-goals for this step

- No frontend changes (follow-up UI slice).
- No SignalR transport.
- No projections query endpoint (future step).
- No removal of the existing `Message` field.
- No domain/handler/persistence changes.
- No case-file view exposure (unless Harley explicitly approves a safe subset in a future step).
- No audit exposure to player-facing API (audit is developer/replay only).
- No raw event exposure.

## Self-Review

**Spec coverage:** Step 6 covers AC-007 fully with the safety correction. The bridge is additive, backward-compatible, and exposes only safe projections. Source truth remains the event stream + projections.

**Placeholder scan:** DTO names to be confirmed from `GameDtos.cs`. No TBDs.

**Type consistency:** Projection DTOs mirror the Step 4 projection models (safe subset only). Mapping follows the existing mapper convention.

**Non-goals:** All eight non-goals preserved.
