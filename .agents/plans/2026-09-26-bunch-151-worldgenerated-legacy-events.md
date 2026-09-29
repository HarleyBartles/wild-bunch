# BUNCH-151 WorldGenerated Legacy Events Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Load older `WorldGenerated` event payloads without `caseFile` while preserving the original case file and rejecting unrecoverable event streams.

**Architecture:** Use the existing `PersistedPayloadLoader` and `PayloadUpcasterRegistry` funnel. A `WorldGenerated` v1-to-v2 upcaster makes an absent `caseFile` explicit as null, while preserving populated payloads. Replay leaves case-file state untouched for that legacy event and takes the original state from the following `CaseFileGenerated` event; a stream with neither case-file source fails clearly.

**Tech Stack:** C#/.NET 10, System.Text.Json, xUnit, EF Core/PostgreSQL.

**Spec:** [BUNCH-151](https://linear.app/harleys-workspace/issue/BUNCH-151/worldgenerated-event-deserialization-breaks-on-older-event-stream); `.agents/doctrine/event-sourcing-integrity.md`; `docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`.

**Execution Strategy:** `executing-plans` because event decoding, replay behavior, and persistence proof are sequential changes to one stream contract.

## Source-grounded decision

- At the starting commit `d63c3826cc63db88b2bcad618a3909d1b174b34a`, `WorldGenerated.CaseFile` is required, every stored event is loaded through `PersistedPayloadLoader`, and there are no registered production upcasters.
- Git commit `c5e8b92` (4 July 2026) shows the original `WorldGenerated` without `CaseFile`; `StartSetup` wrote a separate `CaseFileGenerated` event immediately after it. Commit `a65ca6c` (10 July 2026) added `CaseFile` to `WorldGenerated` without an event-version bump. Both historical and current shapes can therefore be stored as v1.
- The upcaster must tolerate both v1 shapes. For a payload with `caseFile`, preserve that value. For one without it, add JSON `caseFile: null`; do not generate a new case file, derive one from the seed, or use the current snapshot as historical truth.
- `CaseFileGenerated` supplies the original case file in the old stream. Replay must reject a `WorldGenerated` with no case file if the stream also lacks `CaseFileGenerated`, so its temporary placeholder cannot escape as real state.
- Existing writers continue emitting a populated case file in `WorldGenerated` and `CaseFileGenerated`. The schema bump changes only the event envelope version for future writes; no database column or migration is needed.

## Global Constraints

- Keep `GameSession` the command aggregate root and preserve event-stream reconstruction without snapshot dependence.
- Upcast only in the existing persisted-payload funnel. Do not add a serializer or repository bypass.
- Reject unknown future event versions and malformed payloads. Do not turn a missing case file into an empty or fabricated one.
- Keep setup behavior, player-facing projections, and hidden culprit truth unchanged. Development exception presentation is outside BUNCH-151.
- Follow `.agents/runbooks/planning.md`, `.agents/runbooks/implementing.md`, `.agents/playbooks/testing.md`, and `.agents/doctrine/event-sourcing-integrity.md` during execution.

## File ownership

- `src/WildBunch.Persistence/Versioning/WorldGeneratedV1ToV2Upcaster.cs`: pure JSON shape transition for both historical v1 variants.
- `src/WildBunch.Persistence/DependencyInjection.cs`: register the new upcaster in the existing registry.
- `src/WildBunch.Domain/Events/WorldGenerated.cs` and `src/WildBunch.Domain/Game/GameSession.cs`: represent legacy absence and apply only a present event case file.
- `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`: reject a stream that cannot recover its original case file.
- `tests/WildBunch.Integration.Tests/Versioning/WorldGeneratedLegacyEventTests.cs`: prove the actual persisted load, replay, and setup-status query behavior with historical JSON.
- `tests/WildBunch.Domain.Tests/Game/GameSessionEventReplayTests.cs`: prove the unrecoverable stream fails rather than retaining the placeholder.
- `.agents/doctrine/event-sourcing-integrity.md` or the event versioning section of ADR-0028: record the first real upcaster and the two v1 historical shapes, without duplicating the full implementation plan.

## Review Focus

- Old v1 payload without `caseFile` followed by `CaseFileGenerated`: original suspect, culprit, and clue state survive repository load and full replay (Task 2).
- v1 payload already containing `caseFile`: the upcaster does not replace it (Task 1).
- Missing both case-file sources: replay fails explicitly instead of returning the placeholder (Task 1).
- An active legacy session does not prevent `GetByStatusAsync(GameStatus.Active)` from loading sessions during new-game setup (Task 2).
- v2 writes are stamped with the new version and current payloads still round-trip (Task 2).

---

### Task 1: Recover the historical event shape and enforce replay truth

**Files:**
- Create: `src/WildBunch.Persistence/Versioning/WorldGeneratedV1ToV2Upcaster.cs`
- Modify: `src/WildBunch.Persistence/DependencyInjection.cs`
- Modify: `src/WildBunch.Domain/Events/WorldGenerated.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`
- Test: `tests/WildBunch.Integration.Tests/Versioning/WorldGeneratedLegacyEventTests.cs`
- Test: `tests/WildBunch.Domain.Tests/Game/GameSessionEventReplayTests.cs`

**Interfaces:**
- Consumes: `IEventUpcaster`, `PayloadUpcasterRegistry`, `WorldGenerated`, `CaseFileGenerated`, and `GameSession.RehydrateFromEvents`.
- Produces: `WorldGeneratedV1ToV2Upcaster` with `PayloadType == nameof(WorldGenerated)`, `FromVersion == 1`, and `Upcast(string)`; the registry reports v2 for `WorldGenerated`.

- [x] **Step 1: Characterize both v1 payloads and the missing-case-file failure.** In the versioning test, serialize a current `WorldGenerated`, remove only the top-level `caseFile` with `JsonNode`, and assert the current loader throws `JsonException`. Keep the populated v1 payload as the control case. In the domain replay test, construct a stream with `WorldGenerated` but no `CaseFileGenerated` and assert it cannot be accepted once the guard is added. Run the focused tests and record the expected red result.
- [x] **Step 2: Implement the version transition.** Parse the event payload as a JSON object; if the `caseFile` key is absent, add it with JSON null. If the key exists, preserve its value, including an explicit null. Reject a non-object payload with a clear exception. Register the upcaster through `CreateDefaultUpcasters()`; do not modify `PayloadUpcasterRegistry` or `DeserializeEvent`.

  ```csharp
  var root = JsonNode.Parse(payloadJson) as JsonObject
      ?? throw new JsonException("WorldGenerated payload must be an object.");
  if (!root.ContainsKey("caseFile"))
      root["caseFile"] = null;
  return root.ToJsonString();
  ```

- [x] **Step 3: Apply the legacy event without inventing a case file.** Make `WorldGenerated.CaseFile` nullable and have `GameSession.Apply(WorldGenerated)` assign `CaseFile` only when the event value is present. Keep both current `WorldGenerated` writers setting a non-null snapshot. In `RehydrateFromEvents`, before accepting the replay result, reject a stream containing a `WorldGenerated` with null case file and no `CaseFileGenerated` event. Preserve direct-start streams that have no `WorldGenerated`.

  ```csharp
  if (e.CaseFile is { } caseFile)
      CaseFile = caseFile.ToDomain();
  // Before accepting replay, reject only the legacy shape without a
  // later CaseFileGenerated event. GameStarted-only streams stay valid.
  ```
- [x] **Step 4: Prove focused behavior.** Assert the upcasted legacy JSON deserializes, the populated v1 payload retains its case file, the registered event version is 2, and the no-source replay throws. Run `dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter FullyQualifiedName~WorldGeneratedLegacyEventTests` and the focused domain replay test. Commit this independently reviewable event-contract change.

### Task 2: Prove persisted session loading and the setup path

**Files:**
- Extend: `tests/WildBunch.Integration.Tests/Versioning/WorldGeneratedLegacyEventTests.cs`
- Check or extend: `tests/WildBunch.Integration.Tests/FullReplayEqualityTests.cs` only if the focused integration fixture cannot prove snapshot/replay equality itself
- Update: `.agents/doctrine/event-sourcing-integrity.md` or `docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`

**Interfaces:**
- Consumes: Task 1's registered upcaster and nullable legacy event behavior; `EfGameSessionRepository.GetByIdAsync`, `GetEventStreamAsync`, `GetByStatusAsync`, and the existing full-replay test fixture.
- Produces: persisted-load proof that the historical event shape works without losing original case-file truth.

- [x] **Step 1: Add a real persistence test.** Store a normal setup/active session through the existing EF test fixture. In its stored `WorldGenerated` row, remove top-level `caseFile` and leave `SchemaVersion = 1`; keep the following stored `CaseFileGenerated` row intact. Assert `GetByIdAsync` and `GetEventStreamAsync` load successfully and that full event replay matches the original case file, including true culprit and clue identities. This is a behavior test, not a source-text or change-detector test.
- [x] **Step 2: Exercise the user-visible trigger.** In the same fixture, call `GetByStatusAsync(GameStatus.Active)` with the legacy row present. Assert the session is returned and can be archived through the existing setup lifecycle path, or use the setup handler fixture if it already exposes this path without large scaffolding. Verify the old row no longer turns the new-game setup query into a deserialization failure.
- [x] **Step 3: Check current writes and document the contract.** Assert a newly stored `WorldGenerated` has schema version 2 and retains its populated `caseFile`. Inspect other `required` event properties for an evidenced compatibility gap and record any concrete separate follow-up in the return, without changing their schemas in this slice. Record in the chosen doctrine/ADR why the upcaster handles both historical v1 shapes and why a missing `CaseFileGenerated` fails closed. Do not broaden this task to other event schemas or Development error presentation.
- [ ] **Step 4: Verify and commit the tested tree.** Start shared PostgreSQL with `.\tools\postgres-dev.ps1 ensure` if the integration lane needs it. Run `dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter FullyQualifiedName~WorldGeneratedLegacyEventTests`, `dotnet tool restore`, and `dotnet ef migrations list --project src\WildBunch.Persistence --startup-project src\WildBunch.Api`. Stage the exact intended files and commit normally; the tracked hook runs the canonical apply/check gate on the staged tree. If the hook is unavailable, run `py -3 tools/run.py ci --check` against the intended tree and report that limitation. Report any skipped or environment-dependent checks precisely.

## Completion evidence

- Return the branch, starting main SHA, final head SHA, changed files, focused tests and canonical gate results, and any skipped checks.
- Show that the case file after legacy replay equals the original `CaseFileGenerated` payload; a mere absence of an exception is insufficient.
- Obtain fresh review of the finished branch before calling the fix ready. Leave BUNCH-151 In Progress until the implementation and publication proof are available.
