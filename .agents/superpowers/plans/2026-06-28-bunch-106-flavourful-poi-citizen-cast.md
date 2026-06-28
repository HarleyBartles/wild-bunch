# Flavourful POI Citizen Cast Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the generic "a town clerk from {town}" citizen fallback with a varied source-backed citizen cast. Each citizen has a named role (butcher, mortician, doctor, etc.) and a globally unique distinguishing feature. During POI lookaround, identity stays concealed: the player sees only "a stranger with {feature}." The citizen's role is revealed only after a mistaken take-in, when the sheriff identifies them, releases them, and fines the player. Dev overlay can force a specific available citizen role to be the next POI encounter.

**Architecture:** A new `CitizenCast` static content catalog in `WildBunch.Domain.Game` defines named town roles and citizen-specific distinguishing features. `GameSession.LookAroundSaloon()` selects a citizen from the cast (deterministic pick based on town + clock state) instead of calling the hardcoded `DescribeTownCitizen()`. The lookaround descriptor becomes "a stranger with {feature}" (concealment), matching the suspect descriptor pattern. The citizen's role key is carried in the `SaloonPersonOfInterestSpotted` event and stored in `TownSourceVisitState` alongside the descriptor. The `BountyLoopCoordinator` citizen confrontation path reads the stored role and builds a reveal narration: "The sheriff identifies them as {role}, releases them, and fines you ${fineAmount}." The `DevSaloonOverride` record is extended with an optional forced citizen role key. The `SaloonDevContextDto` / `CitizenInfoDto` is updated to expose the available citizen roles. The frontend `SaloonDevPanel` gets a citizen role selector when forcing a Citizen override. The citizen feature pool is disjoint from the suspect feature pool by construction (different keys, different descriptions), with a cross-cutting test asserting no overlap.

**Tech Stack:** C#/.NET 10, ASP.NET Core Minimal APIs, EF Core, xUnit, React 18, TanStack Query, styled-components, Vitest.

## Global Constraints

- `GameSession` is the live-play aggregate root; all gameplay mutation flows through it.
- Typed domain events are plain sealed records implementing `IDomainEvent`; `Apply` is the single mutation path.
- Dev endpoints live under `/api/dev/` and are gated by `DevRoleGuard.EnsureDevAccess()`.
- Dev DTOs are separate types from player DTOs (per ADR-0030 §7).
- Normal player APIs must remain clean of dev-only state and must not gain dev mutation powers.
- Do not leak hidden culprit truth.
- Preserve clue, journal, wanted-poster, suspect, and culprit flows unless directly required by this issue.
- Preserve current wanted-suspect confrontation behavior.
- The culprit is always a gang member; this issue does not touch culprit/seed logic.
- Do not implement a frontend-only citizen roster or frontend-only role labels.
- The citizen cast is source-backed in `WildBunch.Domain.Game.CitizenCast`, not a frontend list.
- Citizen features are disjoint from suspect features by construction (separate pools, different keys). A test asserts no key or description overlap.
- POI lookaround does not reveal the citizen role/name; it exposes only the distinguishing feature via "a stranger with {feature}" copy.
- The citizen role is revealed only in the sheriff mistaken-arrest resolution narration.
- The `DevSaloonOverride` consume-once lifecycle is preserved. A forced citizen role is consumed by the next `LookAroundSaloon()` call.
- Worker environment uses PowerShell; do not use `&&` for command chaining.
- Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent validation.
- styled-components for component styling; reference design tokens via `var(--token-name)`. No plain CSS classes.
- Adding a nullable field to an existing event record is backward-compatible for JSON event deserialization (missing fields default to null).
- The `ActiveSaloonPersonOfInterestDescriptor` string in `TownSourceVisitState` remains the player-facing concealment descriptor. A new `ActiveSaloonCitizenRole` string? field stores the role key for the later reveal, separate from the descriptor.

### Citizen cast content contract

The `CitizenCast` catalog defines:
- **Roles**: A static list of `CitizenRole` records, each with a `Key` (stable identifier), `DisplayName` (e.g. "the town butcher"), and `ShortName` (e.g. "butcher"). At least 10 roles to prove a full flavour cast.
- **Features**: A static list of `CitizenFeature` records, each with a `Key` (stable identifier) and `Description` (e.g. "Wears a wide-brimmed hat with a rattlesnake rattle"). At least 10 features. Each role is paired with one feature by index, ensuring unique role→feature mapping.
- **Select(townId, turn)**: Deterministic pick of a `CitizenEncounter` (role + feature) based on a stable hash of town ID + clock turn. This provides variety across visits and towns without requiring seed-source retention.
- **SelectByRoleKey(roleKey)**: Look up a specific citizen by role key (for dev overlay forcing).
- **ResolveDescriptor(encounter)**: Returns `"a stranger with {NormalizeFeatureDescriptor(feature.Description)}"` — the concealment descriptor shown during lookaround.
- **ResolveRevealName(encounter)**: Returns the role display name (e.g. "the town butcher") — used in the sheriff reveal narration.

### Feature uniqueness contract

- Citizen feature keys are disjoint from `CaseSuspectFeaturePool` feature keys by construction.
- Citizen feature descriptions are disjoint from suspect feature descriptions by construction.
- A cross-cutting test in `WildBunch.GameContent.Tests` asserts no key overlap and no description overlap between `CitizenCast.Features` and `CaseSuspectFeaturePool.FeaturePool`.
- Within the citizen cast, each role has exactly one feature, and no two roles share a feature.

### Concealment falsification proof

Tests at both domain aggregate and integration levels must prove that:
1. The `SaloonPersonOfInterestSpotted` event's `Descriptor` (player-facing) is "a stranger with {feature}" and does NOT contain the role name or role display name.
2. The `SaloonPersonOfInterestSpotted` event's `Message` is "You look around the saloon and spot a stranger with {feature}." and does NOT contain the role name.
3. The `ActiveSaloonPersonOfInterestDescriptor` stored in `TownSourceVisitState` is the concealment descriptor, not the role.
4. The `ActiveSaloonCitizenRole` stored in `TownSourceVisitState` is the role key, not shown in player-facing DTOs.
5. The player-facing `ActiveSaloonPersonOfInterestDto.Descriptor` is the concealment descriptor.
6. The `SaloonPersonOfInterestConfronted` event's `Message` DOES contain the role reveal (only after mistaken take-in).
7. The `SaloonPersonOfInterestConfronted` event's `TargetName` is the concealment descriptor (what the player saw), not the role.

---

## File Structure

### Domain layer (src/WildBunch.Domain/)

| File | Responsibility |
|------|----------------|
| `Game/CitizenCast.cs` | New static content catalog: `CitizenRole` record, `CitizenFeature` record, `CitizenEncounter` record, `CitizenCast.Roles` / `CitizenCast.Features` static lists, `CitizenCast.Select(townId, turn)`, `CitizenCast.SelectByRoleKey(roleKey)`, `CitizenCast.ResolveDescriptor(encounter)`, `CitizenCast.ResolveRevealName(encounter)` |
| `Game/GameSession.cs` (modify) | Replace `DescribeTownCitizen()` with `CitizenCast.Select()` calls in `LookAroundSaloon()` (both normal fallback path and dev-override citizen path). Emit `CitizenRole` in `SaloonPersonOfInterestSpotted` event. Store role in `TownSourceVisitState`. |
| `Game/GameSession.BountyLoopCoordinator.cs` (modify) | Update citizen confrontation path to read `ActiveSaloonCitizenRole` and build role-reveal narration in `ProduceSaloonConfrontedEvent` for citizen wrong-declaration outcome |
| `Game/TownSourceVisitState.cs` (modify) | Add `ActiveSaloonCitizenRole` (string?) property. Update `SetActiveSaloonCitizenPersonOfInterest(descriptor, role)` to accept and store the role. Update `ClearActiveSaloonPersonOfInterest()` to clear the role. |
| `Game/DevSaloonOverride.cs` (modify) | Add `ForcedCitizenRoleKey` (string?) to the record. Add `ForCitizen(string? roleKey)` overload. Update `ForCitizen()` to call `ForCitizen(null)`. |
| `Events/SaloonPersonOfInterestSpotted.cs` (modify) | Add `CitizenRole` (string?) field — the citizen role key, null for suspect/repeat POIs |
| `Events/SaloonPersonOfInterestConfronted.cs` (modify) | Add `CitizenRole` (string?) field — the revealed citizen role key, null for suspect confrontations |
| `Game/GameSessionEventReplay.cs` (modify) | No new event types — existing `SaloonPersonOfInterestSpotted` / `SaloonPersonOfInterestConfronted` cases already handle Apply. No changes needed unless Apply signature changes. |

### Application layer (src/WildBunch.Application/)

| File | Responsibility |
|------|----------------|
| `Dev/Models/SaloonDevContextDto.cs` (modify) | Update `CitizenInfoDto` to carry `HasNamedArchetypes = true`, `AvailableArchetypes` = list of citizen role display names + keys |
| `Dev/Mapping/SaloonDevContextMapper.cs` (modify) | Map `CitizenCast.Roles` into `CitizenInfoDto.AvailableArchetypes`. Replace hardcoded `"a town clerk from {town}"` descriptor with `CitizenCast` reference. |
| `Dev/Commands/ForceSaloonOverrideHandler.cs` (modify) | Pass `ForcedCitizenRoleKey` from command to `DevSaloonOverride.ForCitizen(roleKey)` |
| `Dev/Commands/ForceSaloonOverrideCommand.cs` (modify) | Add `ForcedCitizenRoleKey` (string?) to the command record |
| `Games/Models/GameDtos.cs` (no change) | `ActiveSaloonPersonOfInterestDto` already carries `Descriptor` + `Kind` — no new player-facing fields needed. The role is NOT exposed in player DTOs. |
| `Games/Mapping/GameSessionMapper.cs` (no change) | The mapper already passes through `ActiveSaloonPersonOfInterestDescriptor` as the player-facing descriptor. No role mapping needed — role is dev-only and confrontation-internal. |

### API layer (src/WildBunch.Api/)

| File | Responsibility |
|------|----------------|
| `Dev/DevEndpoints.cs` (modify) | Pass `ForcedCitizenRoleKey` from `ForceSaloonOverrideRequestDto` to `ForceSaloonOverrideCommand` |
| `Dev/Models/ForceSaloonOverrideRequestDto.cs` (modify) | Add `ForcedCitizenRoleKey` (string?) to the request DTO |

### Persistence layer (src/WildBunch.Persistence/)

| File | Responsibility |
|------|----------------|
| `Serialization/GameSessionJsonSerializer.Components.cs` (modify) | Serialize `ActiveSaloonCitizenRole` alongside `ActiveSaloonPersonOfInterestDescriptor` in the town visit state snapshot. The `DevSaloonOverride` JSON serialization automatically picks up the new `ForcedCitizenRoleKey` field via System.Text.Json. |

### Frontend (src/WildBunch.Web/src/)

| File | Responsibility |
|------|----------------|
| `dev/panels/SaloonDevPanel.tsx` (modify) | When `forcedKind === "Citizen"` and `citizenInfo.hasNamedArchetypes` is true, show a role selector dropdown populated from `citizenInfo.availableArchetypes`. Send `forcedCitizenRoleKey` in the force request. |
| `dev/types.ts` (modify) | Add `forcedCitizenRoleKey?: string | null` to `ForceSaloonOverrideRequestDto`. Update `CitizenInfoDto` to include role keys alongside display names. |
| `dev/devApi.ts` (modify) | Pass `forcedCitizenRoleKey` in the `forceSaloonOverride` request body |

### Tests (backend)

| File | Responsibility |
|------|----------------|
| `tests/WildBunch.Domain.Tests/CitizenCastTests.cs` | New: verify cast has ≥10 roles, each role has a unique feature, no duplicate role keys, no duplicate feature keys, `Select()` is deterministic, `SelectByRoleKey()` resolves correctly, `ResolveDescriptor()` produces "a stranger with {feature}" and does NOT contain the role name |
| `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs` (modify) | Update citizen tests to verify: lookaround descriptor is "a stranger with {feature}" (not "a town clerk"), `ActiveSaloonCitizenRole` is set, confrontation message reveals the role, player-facing DTO descriptor is the concealment descriptor |
| `tests/WildBunch.Domain.Tests/DevSaloonOverrideTests.cs` (modify) | Update citizen override tests to verify forced citizen role key is consumed and the correct citizen is spotted |
| `tests/WildBunch.GameContent.Tests/CaseCharacterRosterTests.cs` (modify) | Add cross-cutting assertion: no `CitizenCast.Features` key matches any `CaseSuspectFeaturePool.FeaturePool` key, no description overlap |
| `tests/WildBunch.Application.Tests/Dev/GetSaloonDevContextHandlerTests.cs` (modify) | Verify `CitizenInfoDto.HasNamedArchetypes` is true and `AvailableArchetypes` is non-empty |
| `tests/WildBunch.Application.Tests/Dev/ForceSaloonOverrideHandlerTests.cs` (modify) | Verify forced citizen role key is persisted and consumed |
| `tests/WildBunch.Application.Tests/SaloonPersonOfInterestDescriptorParityTests.cs` (modify) | Update citizen parity test to verify descriptor is "a stranger with {feature}" |

### Tests (frontend)

| File | Responsibility |
|------|----------------|
| `src/tests/SaloonDevPanel.test.tsx` (modify) | Verify citizen role selector appears when `forcedKind === "Citizen"` and `hasNamedArchetypes` is true. Verify `forcedCitizenRoleKey` is sent in the force request. |

---

## Task Breakdown

### Task 1: Citizen cast content catalog

**Files:**
- `src/WildBunch.Domain/Game/CitizenCast.cs` (new)
- `src/WildBunch.Domain/Game/INDEX.md` (modify — add entry)
- `tests/WildBunch.Domain.Tests/CitizenCastTests.cs` (new)
- `tests/WildBunch.Domain.Tests/INDEX.md` (modify — add entry)

**Steps:**

- [ ] 1.1 Create `src/WildBunch.Domain/Game/CitizenCast.cs` with:
  - `public sealed record CitizenRole(string Key, string DisplayName, string ShortName)` — e.g. `new("butcher", "the town butcher", "butcher")`
  - `public sealed record CitizenFeature(string Key, string Description)` — e.g. `new("wide-brim-rattlesnake-hat", "Wears a wide-brimmed hat with a rattlesnake rattle on the band")`
  - `public sealed record CitizenEncounter(CitizenRole Role, CitizenFeature Feature)`
  - `public static class CitizenCast` with:
    - `Roles` — static readonly list of ≥12 `CitizenRole` records: butcher, mortician, doctor, blacksmith, schoolteacher, preacher, seamstress, hotel-keeper, banker, newspaperman, stable-hand, telegraph-operator, barber, undertaker, prospector, cook, stagecoach-agent, gunsmith, town-clerk
    - `Features` — static readonly list of ≥12 `CitizenFeature` records, each visually distinct and NOT overlapping with suspect feature descriptions. Citizen features are general physical markers (hats, jewelry, clothing items, grooming) that do NOT reveal a trade/role. Examples: "Wears a wide-brimmed hat with a rattlesnake rattle on the band", "Has a silver pocket watch chain dangling from the vest", "Wears a red bandana around the neck", "Has a long braided beard tied with a leather cord", "Wears a pair of spurs that jingle when walking", "Has a missing front tooth", "Wears a dusty cavalry coat with brass buttons", "Has a burn scar across the back of one hand", "Wears a pawnbroker's loupe on a leather cord", "Has a tar-stained pipe clenched in the teeth", "Wears a woman's riding gloves of doeskin", "Has a tin star pinned to the vest" — none of these reveal a specific trade.
    - `RoleFeaturePairs` — static readonly list of `CitizenEncounter` records, pairing each role with one feature by index. Roles and Features lists must have the same count.
    - `Select(TownId townId, int turn)` — deterministic pick: `var index = Math.Abs((townId.Value, turn).GetHashCode()) % RoleFeaturePairs.Count; return RoleFeaturePairs[index];`. Note: `string.GetHashCode()` is not stable across runtimes; use a simple manual hash (e.g. sum char codes) for determinism across process restarts. Use a `StableHash` helper.
    - `SelectByRoleKey(string roleKey)` — `RoleFeaturePairs.FirstOrDefault(e => e.Role.Key == roleKey)` or throw if not found.
    - `ResolveDescriptor(CitizenEncounter encounter)` — `$"a stranger with {NormalizeFeatureDescriptor(encounter.Feature.Description)}"`. Reuse the same normalization logic as `SaloonPersonOfInterestDescriptor.NormalizeFeatureDescriptor` (strip "has a"/"wears a" prefixes → "a"/"an"). Extract a shared helper or duplicate the small normalization.
    - `ResolveRevealName(CitizenEncounter encounter)` — `encounter.Role.DisplayName` (e.g. "the town butcher").
    - `ResolveRevealNarration(CitizenEncounter encounter, decimal fineAmount)` — `$"You bring a stranger with {NormalizeFeatureDescriptor(encounter.Feature.Description)} to the sheriff. The sheriff identifies them as {encounter.Role.DisplayName}, releases them, and fines you ${fineAmount:0.00}."`.

- [ ] 1.2 Create `tests/WildBunch.Domain.Tests/CitizenCastTests.cs` with tests:
  - `CitizenCast_HasAtLeastTwelveRoles` — `Assert.True(CitizenCast.Roles.Count >= 12)`
  - `CitizenCast_HasAtLeastTwelveFeatures` — `Assert.True(CitizenCast.Features.Count >= 12)`
  - `CitizenCast_RolesAndFeaturesHaveSameCount` — `Assert.Equal(CitizenCast.Roles.Count, CitizenCast.Features.Count)`
  - `CitizenCast_NoDuplicateRoleKeys` — all role keys are distinct
  - `CitizenCast_NoDuplicateFeatureKeys` — all feature keys are distinct
  - `CitizenCast_NoDuplicateRoleDisplayNames` — all display names are distinct
  - `CitizenCast_SelectIsDeterministic` — same town + turn → same encounter
  - `CitizenCast_SelectDifferentTownsOrTurnsProducesVariedEncounters` — at least 3 distinct encounters across a range of inputs
  - `CitizenCast_SelectByRoleKey_ResolvesCorrectly` — each role key resolves to the correct encounter
  - `CitizenCast_SelectByRoleKey_ThrowsForUnknownKey` — unknown key throws `ArgumentException`
  - `CitizenCast_ResolveDescriptor_ProducesConcealmentFormat` — starts with "a stranger with " and does NOT contain the role display name or short name
  - `CitizenCast_ResolveRevealName_ProducesRoleDisplayName` — returns the role display name
  - `CitizenCast_ResolveRevealNarration_ContainsRoleAndFine` — contains the role display name and the fine amount, and contains "sheriff identifies them as"

- [ ] 1.3 Update `src/WildBunch.Domain/Game/INDEX.md` to add `CitizenCast.cs` entry.

- [ ] 1.4 Update `tests/WildBunch.Domain.Tests/INDEX.md` to add `CitizenCastTests.cs` entry.

- [ ] 1.5 Run `dotnet build` and `dotnet test` for the new test project to verify the catalog compiles and tests pass.

### Task 2: Cross-cutting feature uniqueness test

**Files:**
- `tests/WildBunch.GameContent.Tests/CaseCharacterRosterTests.cs` (modify)

**Steps:**

- [ ] 2.1 Add a test `CitizenFeatures_AreDisjointFromSuspectFeatures` to `CaseCharacterRosterTests.cs`:
  - Collect all `CitizenCast.Features` keys and descriptions.
  - Collect all `CaseSuspectFeaturePool.FeaturePool` keys and descriptions (both primary and accessory features).
  - Assert no citizen feature key matches any suspect feature key (case-insensitive).
  - Assert no citizen feature description matches any suspect feature description (case-insensitive, after trimming).
  - This test proves the disjointness invariant by construction + test.

- [ ] 2.2 Run `dotnet test` for `WildBunch.GameContent.Tests` to verify the disjointness test passes.

### Task 3: Domain — extend TownSourceVisitState with citizen role

**Files:**
- `src/WildBunch.Domain/Game/TownSourceVisitState.cs` (modify)

**Steps:**

- [ ] 3.1 Add `public string? ActiveSaloonCitizenRole { get; private set; }` property to `TownSourceVisitState`, adjacent to `ActiveSaloonPersonOfInterestDescriptor`.

- [ ] 3.2 Update `SetActiveSaloonCitizenPersonOfInterest(string descriptor)` → `SetActiveSaloonCitizenPersonOfInterest(string descriptor, string? citizenRole)`:
  ```csharp
  public void SetActiveSaloonCitizenPersonOfInterest(string descriptor, string? citizenRole)
  {
      ActiveSaloonPersonOfInterestId = null;
      ActiveSaloonPersonOfInterestDescriptor = descriptor;
      ActiveSaloonPersonOfInterestKind = SaloonPersonOfInterestKind.Citizen;
      ActiveSaloonCitizenRole = citizenRole;
  }
  ```

- [ ] 3.3 Update `ClearActiveSaloonPersonOfInterest()` to also clear `ActiveSaloonCitizenRole = null`.

- [ ] 3.4 Update the constructor or factory method that sets `ActiveSaloonPersonOfInterestDescriptor` to also accept and set `ActiveSaloonCitizenRole`. Check the `TownSourceVisitState` constructor signature and the rehydration path.

- [ ] 3.5 Run `dotnet build` to verify compilation. Existing callers of `SetActiveSaloonCitizenPersonOfInterest(descriptor)` will need updating (Task 4).

### Task 4: Domain — extend events with CitizenRole field

**Files:**
- `src/WildBunch.Domain/Events/SaloonPersonOfInterestSpotted.cs` (modify)
- `src/WildBunch.Domain/Events/SaloonPersonOfInterestConfronted.cs` (modify)

**Steps:**

- [ ] 4.1 Add `public string? CitizenRole { get; init; }` to `SaloonPersonOfInterestSpotted` — the citizen role key, null for suspect/repeat POIs. This is the role key (e.g. "butcher"), not the display name.

- [ ] 4.2 Add `public string? CitizenRole { get; init; }` to `SaloonPersonOfInterestConfronted` — the revealed citizen role key, null for suspect confrontations.

- [ ] 4.3 Run `dotnet build` to verify compilation. The new nullable fields default to null, so existing event construction sites remain valid.

### Task 5: Domain — update GameSession.Apply for spotted event

**Files:**
- `src/WildBunch.Domain/Game/GameSession.cs` (modify — `Apply(SaloonPersonOfInterestSpotted)`)

**Steps:**

- [ ] 5.1 Update `Apply(SaloonPersonOfInterestSpotted e)` to pass `e.CitizenRole` when setting a citizen POI:
  ```csharp
  else if (e.Descriptor is not null)
  {
      CurrentTownVisit.CurrentTownState.SetActiveSaloonCitizenPersonOfInterest(e.Descriptor, e.CitizenRole);
  }
  ```
  The suspect path (`e.SuspectId is not null && e.Descriptor is not null`) is unchanged — it calls `SetActiveSaloonPersonOfInterest(e.SuspectId.Value, e.Descriptor)` which doesn't set a citizen role.

- [ ] 5.2 Run `dotnet build` to verify compilation.

### Task 6: Domain — update LookAroundSaloon citizen paths

**Files:**
- `src/WildBunch.Domain/Game/GameSession.cs` (modify — `LookAroundSaloon` method, ~lines 2750-2858)

**Steps:**

- [ ] 6.1 Replace the dev-override citizen path (~line 2793-2810). Instead of:
  ```csharp
  var forcedCitizenDescriptor = DescribeTownCitizen(CurrentTown);
  ```
  Change to:
  ```csharp
  CitizenEncounter? forcedEncounter = null;
  if (pendingDevOverride.ForcedCitizenRoleKey is not null)
  {
      forcedEncounter = CitizenCast.SelectByRoleKey(pendingDevOverride.ForcedCitizenRoleKey);
  }
  else
  {
      forcedEncounter = CitizenCast.Select(CurrentTown.TownId, Clock.Turn);
  }
  var forcedCitizenDescriptor = CitizenCast.ResolveDescriptor(forcedEncounter);
  var forcedCitizenMessage = $"You look around the saloon and spot {forcedCitizenDescriptor}.";
  ProduceEvent(new SaloonPersonOfInterestSpotted
  {
      SourceKind = InvestigationSourceKind.SaloonLookAround,
      TownId = CurrentTown.TownId,
      Message = forcedCitizenMessage,
      Descriptor = forcedCitizenDescriptor,
      PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
      CitizenRole = forcedEncounter.Role.Key,
      RecordLog = false
  });
  ```

- [ ] 6.2 Replace the normal fallback citizen path (~line 2846-2858). Instead of:
  ```csharp
  var citizenDescriptor = DescribeTownCitizen(CurrentTown);
  ```
  Change to:
  ```csharp
  var citizenEncounter = CitizenCast.Select(CurrentTown.TownId, Clock.Turn);
  var citizenDescriptor = CitizenCast.ResolveDescriptor(citizenEncounter);
  var citizenMessage = $"You look around the saloon and spot {citizenDescriptor}.";
  var citizenEvent = new SaloonPersonOfInterestSpotted
  {
      SourceKind = InvestigationSourceKind.SaloonLookAround,
      TownId = CurrentTown.TownId,
      Message = citizenMessage,
      Descriptor = citizenDescriptor,
      PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
      CitizenRole = citizenEncounter.Role.Key,
      RecordLog = false
  };
  ```

- [ ] 6.3 Remove or mark obsolete the `DescribeTownCitizen` method (~line 3298-3299). If no other callers remain, delete it. Check for references in plans/other files — plan references are informational only and do not block deletion.

- [ ] 6.4 Run `dotnet build` to verify compilation.

### Task 7: Domain — update BountyLoopCoordinator citizen confrontation

**Files:**
- `src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs` (modify — citizen confrontation path, ~lines 200-223)

**Steps:**

- [ ] 7.1 Update the citizen wrong-declaration path to read the stored role and build a reveal narration. Instead of:
  ```csharp
  var citizenTargetName = activeSaloonPersonOfInterestDescriptor ?? throw new InvalidOperationException("...");
  var citizenNarration = $"You bring {citizenTargetName} to the sheriff, but the declaration is wrong. The sheriff releases them and fines you ${fineAmount:0.00}.";
  ```
  Change to:
  ```csharp
  var citizenTargetName = activeSaloonPersonOfInterestDescriptor ?? throw new InvalidOperationException("A citizen person of interest descriptor is required.");
  var citizenRoleKey = _session.CurrentTownVisit.CurrentTownState.ActiveSaloonCitizenRole;
  var citizenNarration = BuildCitizenRevealNarration(citizenTargetName, citizenRoleKey, fineAmount);
  ```
  Where `BuildCitizenRevealNarration` is a new helper:
  ```csharp
  private static string BuildCitizenRevealNarration(string concealmentDescriptor, string? roleKey, decimal fineAmount)
  {
      if (string.IsNullOrWhiteSpace(roleKey))
      {
          // Backward-compatible fallback: no role stored (old sessions or edge cases).
          return $"You bring {concealmentDescriptor} to the sheriff, but the declaration is wrong. The sheriff releases them and fines you ${fineAmount:0.00}.";
      }
      var encounter = CitizenCast.SelectByRoleKey(roleKey);
      var revealName = CitizenCast.ResolveRevealName(encounter);
      return $"You bring {concealmentDescriptor} to the sheriff. The sheriff identifies them as {revealName}, releases them, and fines you ${fineAmount:0.00}.";
  }
  ```

- [ ] 7.2 Update `ProduceSaloonConfrontedEvent` call for the citizen path to pass `CitizenRole = citizenRoleKey` in the event:
  ```csharp
  ProduceSaloonConfrontedEvent(
      citizenNarration,
      declaredWantedIdentityHandle,
      targetName: citizenTargetName,
      personOfInterestKind: SaloonPersonOfInterestKind.Citizen,
      outcome: SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration,
      fineAmount: fineAmount,
      walletBefore: walletBefore,
      isCitizen: true,
      citizenRole: citizenRoleKey);
  ```

- [ ] 7.3 Add `string? citizenRole = null` parameter to `ProduceSaloonConfrontedEvent` and pass it to the `SaloonPersonOfInterestConfronted` event construction:
  ```csharp
  var e = new SaloonPersonOfInterestConfronted
  {
      // ... existing fields ...
      CitizenRole = citizenRole
  };
  ```

- [ ] 7.4 Run `dotnet build` to verify compilation.

### Task 8: Domain — extend DevSaloonOverride with forced citizen role

**Files:**
- `src/WildBunch.Domain/Game/DevSaloonOverride.cs` (modify)

**Steps:**

- [ ] 8.1 Add `ForcedCitizenRoleKey` (string?) to the `DevSaloonOverride` record:
  ```csharp
  public sealed record DevSaloonOverride(
      DevSaloonPoiKind ForcedKind,
      SuspectId? ForcedSuspectId,
      string? ForcedCitizenRoleKey)
  ```

- [ ] 8.2 Update `ForSuspect(SuspectId suspectId)` → `new(DevSaloonPoiKind.Suspect, suspectId, null)`

- [ ] 8.3 Update `ForAnySuspect()` → `new(DevSaloonPoiKind.Suspect, null, null)`

- [ ] 8.4 Update `ForCitizen()` → `new(DevSaloonPoiKind.Citizen, null, null)` (generic — random/deterministic citizen)

- [ ] 8.5 Add `ForCitizen(string roleKey)` → `new(DevSaloonPoiKind.Citizen, null, roleKey)` (force a specific citizen role)

- [ ] 8.6 Update `ForceDevSaloonOverride` in `GameSession.cs` (~line 1046) to validate the forced citizen role key if provided:
  ```csharp
  if (overrideValue.ForcedKind is DevSaloonPoiKind.Citizen && overrideValue.ForcedCitizenRoleKey is not null)
  {
      // Validate that the role key exists in the cast.
      if (!CitizenCast.Roles.Any(r => r.Key == overrideValue.ForcedCitizenRoleKey))
      {
          throw new InvalidOperationException(
              $"Unknown citizen role key: {overrideValue.ForcedCitizenRoleKey}. Cannot force a saloon override for a citizen role that does not exist.");
      }
  }
  ```

- [ ] 8.7 Update `Apply(DevSaloonOverrideForced e)` in `GameSession.cs` (~line 697) to carry the new field:
  ```csharp
  internal void Apply(DevSaloonOverrideForced e)
  {
      _pendingDevSaloonOverride = new DevSaloonOverride(
          e.ForcedKind,
          e.ForcedSuspectId,
          e.ForcedCitizenRoleKey);
      _version++;
  }
  ```

- [ ] 8.8 Update `DevSaloonOverrideForced` event to add `ForcedCitizenRoleKey` (string?) field. Check `src/WildBunch.Domain/Events/DevSaloonOverrideForced.cs`.

- [ ] 8.9 Update `ForceDevSaloonOverride` to pass `ForcedCitizenRoleKey` in the `DevSaloonOverrideForced` event:
  ```csharp
  ProduceEvent(new DevSaloonOverrideForced
  {
      ForcedKind = overrideValue.ForcedKind,
      ForcedSuspectId = overrideValue.ForcedSuspectId,
      ForcedCitizenRoleKey = overrideValue.ForcedCitizenRoleKey
  });
  ```

- [ ] 8.10 Run `dotnet build` to verify compilation. All existing `DevSaloonOverride` construction sites need updating to pass the new parameter (null for suspect overrides).

### Task 9: Persistence — serialize ActiveSaloonCitizenRole

**Files:**
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs` (modify)

**Steps:**

- [ ] 9.1 Find the snapshot record that serializes `ActiveSaloonPersonOfInterestDescriptor` (likely a `TownSourceVisitStateSnapshot` or similar inner record). Add `string? ActiveSaloonCitizenRole` to that snapshot record.

- [ ] 9.2 Update the `FromDomain` mapping to read `ActiveSaloonCitizenRole` from the domain `TownSourceVisitState`.

- [ ] 9.3 Update the `ToDomain` mapping to set `ActiveSaloonCitizenRole` on the rehydrated `TownSourceVisitState`. This may require a `GameSessionRehydrator.SetBackingField` call or a new setter on `TownSourceVisitState`.

- [ ] 9.4 The `DevSaloonOverride` JSON serialization automatically picks up the new `ForcedCitizenRoleKey` field via System.Text.Json — no manual changes needed for the override.

- [ ] 9.5 Run `dotnet build` to verify compilation.

### Task 10: Application — update dev DTOs and mapper

**Files:**
- `src/WildBunch.Application/Dev/Models/SaloonDevContextDto.cs` (modify)
- `src/WildBunch.Application/Dev/Mapping/SaloonDevContextMapper.cs` (modify)
- `src/WildBunch.Application/Dev/Commands/ForceSaloonOverrideCommand.cs` (modify)
- `src/WildBunch.Application/Dev/Commands/ForceSaloonOverrideHandler.cs` (modify)

**Steps:**

- [ ] 10.1 Update `CitizenInfoDto` in `SaloonDevContextDto.cs`:
  ```csharp
  public sealed record CitizenInfoDto(
      string Descriptor,
      bool HasNamedArchetypes,
      IReadOnlyList<CitizenArchetypeDto> AvailableArchetypes);

  public sealed record CitizenArchetypeDto(
      string RoleKey,
      string DisplayName,
      string FeatureDescription);
  ```
  Update the doc comment: "Citizens are drawn from a source-backed cast of named town roles, each with a unique distinguishing feature."

- [ ] 10.2 Update `SaloonDevContextMapper.cs` `ToDto` method (~line 62-67). Replace:
  ```csharp
  var citizenDescriptor = $"a town clerk from {session.CurrentTown.TownName}";
  var citizenInfo = new CitizenInfoDto(
      Descriptor: citizenDescriptor,
      HasNamedArchetypes: false,
      AvailableArchetypes: Array.Empty<string>());
  ```
  With:
  ```csharp
  var citizenInfo = new CitizenInfoDto(
      Descriptor: "a stranger with a unique distinguishing feature",
      HasNamedArchetypes: true,
      AvailableArchetypes: CitizenCast.Roles.Select(role =>
      {
          var encounter = CitizenCast.SelectByRoleKey(role.Key);
          return new CitizenArchetypeDto(
              role.Key,
              role.DisplayName,
              encounter.Feature.Description);
      }).ToList());
  ```

- [ ] 10.3 Add `ForcedCitizenRoleKey` (string?) to `ForceSaloonOverrideCommand` record.

- [ ] 10.4 Update `ForceSaloonOverrideHandler.HandleAsync` (~line 32-41) to pass `ForcedCitizenRoleKey`:
  ```csharp
  DevSaloonPoiKind.Citizen
      => DevSaloonOverride.ForCitizen(command.ForcedCitizenRoleKey),
  ```
  Where `ForCitizen(null)` → generic citizen, `ForCitizen("butcher")` → specific citizen.

- [ ] 10.5 Run `dotnet build` to verify compilation.

### Task 11: API — update dev endpoints

**Files:**
- `src/WildBunch.Api/Dev/DevEndpoints.cs` (modify)
- `src/WildBunch.Api/Dev/Models/ForceSaloonOverrideRequestDto.cs` (modify — check if this exists as a separate file or inline)

**Steps:**

- [ ] 11.1 Find the `ForceSaloonOverrideRequestDto` definition (may be in `DevEndpoints.cs` or a separate models file). Add `public string? ForcedCitizenRoleKey { get; init; }` to it.

- [ ] 11.2 Update `ForceSaloonOverrideAsync` in `DevEndpoints.cs` (~line 201) to pass `ForcedCitizenRoleKey`:
  ```csharp
  await handler.HandleAsync(new ForceSaloonOverrideCommand(
      id, request.ForcedKind, request.ForcedSuspectId, request.ForcedCitizenRoleKey),
      cancellationToken);
  ```

- [ ] 11.3 Run `dotnet build` to verify compilation.

### Task 12: Frontend — update dev types and API

**Files:**
- `src/WildBunch.Web/src/dev/types.ts` (modify)
- `src/WildBunch.Web/src/dev/devApi.ts` (modify)

**Steps:**

- [ ] 12.1 Update `CitizenInfoDto` in `types.ts`:
  ```typescript
  export interface CitizenArchetypeDto {
    roleKey: string;
    displayName: string;
    featureDescription: string;
  }

  export interface CitizenInfoDto {
    descriptor: string;
    hasNamedArchetypes: boolean;
    availableArchetypes: CitizenArchetypeDto[];
  }
  ```

- [ ] 12.2 Update `ForceSaloonOverrideRequestDto` in `types.ts`:
  ```typescript
  export interface ForceSaloonOverrideRequestDto {
    forcedKind: string;
    forcedSuspectId?: string | null;
    forcedCitizenRoleKey?: string | null;
  }
  ```

- [ ] 12.3 Update `forceSaloonOverride` in `devApi.ts` to include `forcedCitizenRoleKey` in the request body when provided.

- [ ] 12.4 Run frontend typecheck to verify: `cd src/WildBunch.Web && npx tsc --noEmit`.

### Task 13: Frontend — update SaloonDevPanel with citizen role selector

**Files:**
- `src/WildBunch.Web/src/dev/panels/SaloonDevPanel.tsx` (modify)

**Steps:**

- [ ] 13.1 Add state for selected citizen role key:
  ```typescript
  const [selectedCitizenRoleKey, setSelectedCitizenRoleKey] = useState<string>("");
  ```

- [ ] 13.2 Update the `handleForce` function to pass `forcedCitizenRoleKey`:
  ```typescript
  await forceSaloonOverride(gameId, {
    forcedKind,
    forcedSuspectId: forcedKind === "Suspect" && selectedSuspectId !== ""
      ? selectedSuspectId
      : null,
    forcedCitizenRoleKey: forcedKind === "Citizen" && selectedCitizenRoleKey !== ""
      ? selectedCitizenRoleKey
      : null,
  });
  ```

- [ ] 13.3 Update the `forcedKind` select `onChange` to also reset `selectedCitizenRoleKey`:
  ```typescript
  onChange={(e) => {
    setForcedKind(e.target.value as PoiKind);
    setSelectedSuspectId("");
    setSelectedCitizenRoleKey("");
  }}
  ```

- [ ] 13.4 Replace the `CitizenNote` section (~line 232-240) with a citizen role selector when `hasNamedArchetypes` is true:
  ```tsx
  {forcedKind === "Citizen" && (
    <CitizenSection>
      {data?.citizenInfo?.hasNamedArchetypes ? (
        <>
          <Field>
            <Label>Citizen role:</Label>
            <Select
              value={selectedCitizenRoleKey}
              onChange={(e) => setSelectedCitizenRoleKey(e.target.value)}
              data-testid="force-citizen-role-select"
            >
              <option value="">Any citizen (deterministic pick)</option>
              {data.citizenInfo.availableArchetypes.map((a) => (
                <option key={a.roleKey} value={a.roleKey}>
                  {a.displayName} — {a.featureDescription}
                </option>
              ))}
            </Select>
          </Field>
          <CitizenNote>
            Source-backed cast of {data.citizenInfo.availableArchetypes.length} citizen roles.
            Each citizen has a unique distinguishing feature concealed during lookaround.
          </CitizenNote>
        </>
      ) : (
        <CitizenNote>
          Generic citizen POI — {data?.citizenInfo?.descriptor ?? "no named archetypes."}
        </CitizenNote>
      )}
    </CitizenSection>
  )}
  ```

- [ ] 13.5 Update the "Active saloon POI" citizen display (~line 130-134) to show the stored role (dev-only, not player-facing):
  ```tsx
  {!data.activeSaloonPoi.suspectId && !data.activeSaloonPoi.suspectName && (
    <MutedText>
      Citizen POI — {data.activeSaloonPoi.descriptor}
      {data.activeSaloonPoi.citizenRole && ` (role: ${data.activeSaloonPoi.citizenRole})`}
    </MutedText>
  )}
  ```
  Note: This requires adding `citizenRole` to `ActiveSaloonPoiDto` in the dev DTO. Update `ActiveSaloonPoiDto` in `SaloonDevContextDto.cs` to add `string? CitizenRole` and map it in `SaloonDevContextMapper.cs`.

- [ ] 13.6 Run frontend typecheck and tests to verify.

### Task 14: Update existing domain tests

**Files:**
- `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs` (modify)
- `tests/WildBunch.Domain.Tests/DevSaloonOverrideTests.cs` (modify)
- `tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs` (modify)

**Steps:**

- [ ] 14.1 In `GameSessionSaloonPersonOfInterestTests.cs`, update the citizen lookaround test (`LookAroundSaloonCanSurfaceATownCitizenAndWrongDeclarationCapsTheFineAtTheAvailableWallet` ~line 239):
  - Assert the lookaround result message contains "a stranger with" and does NOT contain "town clerk".
  - Assert the `SaloonPersonOfInterestSpotted` event's `Descriptor` starts with "a stranger with".
  - Assert the `SaloonPersonOfInterestSpotted` event's `CitizenRole` is not null.
  - Assert the confrontation result message contains "sheriff identifies them as" and the role display name.
  - Assert the `SaloonPersonOfInterestConfronted` event's `CitizenRole` matches the spotted event's role.

- [ ] 14.2 In `GameSessionSaloonPersonOfInterestTests.cs`, update any test that asserts the citizen descriptor is "a town clerk from {town}" — replace with "a stranger with" assertion.

- [ ] 14.3 In `DevSaloonOverrideTests.cs`, update `LookAroundSaloon_WithCitizenOverride_ConsumesOverrideAndSpotsCitizen` (~line 166):
  - Assert the spotted event's `Descriptor` starts with "a stranger with".
  - Assert the spotted event's `CitizenRole` is not null.

- [ ] 14.4 Add a new test `LookAroundSaloon_WithCitizenRoleOverride_ConsumesOverrideAndSpotsForcedCitizen`:
  - Force `DevSaloonOverride.ForCitizen("butcher")`.
  - Call `LookAroundSaloon`.
  - Assert the spotted event's `CitizenRole` is "butcher".
  - Assert the descriptor is the butcher's concealment descriptor.

- [ ] 14.5 In `BountySaloonEventSourcingTests.cs`, update `LookAroundSaloonCitizenProducesSpottedEventWithNoLog` (~line 44):
  - Assert the spotted event's `Descriptor` starts with "a stranger with".
  - Assert `CitizenRole` is not null.

- [ ] 14.6 Run `dotnet test` for `WildBunch.Domain.Tests` to verify all updated tests pass.

### Task 15: Update application tests

**Files:**
- `tests/WildBunch.Application.Tests/Dev/GetSaloonDevContextHandlerTests.cs` (modify)
- `tests/WildBunch.Application.Tests/Dev/ForceSaloonOverrideHandlerTests.cs` (modify)
- `tests/WildBunch.Application.Tests/SaloonPersonOfInterestDescriptorParityTests.cs` (modify)

**Steps:**

- [ ] 15.1 In `GetSaloonDevContextHandlerTests.cs`, update or add tests:
  - Assert `CitizenInfo.HasNamedArchetypes` is true.
  - Assert `CitizenInfo.AvailableArchetypes` is non-empty and contains role keys + display names.
  - Assert each archetype has a `FeatureDescription`.

- [ ] 15.2 In `ForceSaloonOverrideHandlerTests.cs`, add a test for forced citizen role key:
  - Force with `ForcedKind = "Citizen"` and `ForcedCitizenRoleKey = "butcher"`.
  - Assert the persisted `PendingDevSaloonOverride.ForcedCitizenRoleKey` is "butcher".

- [ ] 15.3 In `SaloonPersonOfInterestDescriptorParityTests.cs`, update the citizen parity test (~line 67):
  - Assert the citizen session's lookaround descriptor starts with "a stranger with".
  - Assert the mapped DTO descriptor matches the event descriptor.

- [ ] 15.4 Run `dotnet test` for `WildBunch.Application.Tests` to verify all updated tests pass.

### Task 16: Update frontend tests

**Files:**
- `src/WildBunch.Web/src/tests/SaloonDevPanel.test.tsx` (modify)

**Steps:**

- [ ] 16.1 Update the mock `SaloonDevContextDto` to include `citizenInfo` with `hasNamedArchetypes: true` and a non-empty `availableArchetypes` array.

- [ ] 16.2 Add a test: when `forcedKind` is "Citizen" and `hasNamedArchetypes` is true, the citizen role selector is rendered with options from `availableArchetypes`.

- [ ] 16.3 Add a test: selecting a citizen role and clicking "Force next POI" sends `forcedCitizenRoleKey` in the request body.

- [ ] 16.4 Add a test: when `forcedKind` is "Citizen" and `hasNamedArchetypes` is false, the fallback generic citizen note is shown (no selector).

- [ ] 16.5 Run frontend tests: `cd src/WildBunch.Web && npx vitest run`.

### Task 17: Update INDEX.md files and regenerate index mesh

**Files:**
- Various `INDEX.md` files (modify via generator)
- `scripts/generate_index_mesh.py` (no change expected unless new gitignored dirs appeared)

**Steps:**

- [ ] 17.1 Run `python scripts/generate_index_mesh.py` to regenerate all `INDEX.md` files.

- [ ] 17.2 Run `python scripts/generate_index_mesh.py --check` to verify the index mesh is clean.

- [ ] 17.3 If `--check` fails, inspect the diff and commit the regenerated `INDEX.md` files.

### Task 18: Full validation

**Steps:**

- [ ] 18.1 Run `.\scripts\postgres-dev.ps1 ensure` to ensure the PostgreSQL service is healthy.

- [ ] 18.2 Run `dotnet build WildBunch.sln` — verify zero errors, zero warnings (or document warnings separately).

- [ ] 18.3 Run `.\scripts\postgres-dev.ps1 test -- dotnet test WildBunch.sln` — verify all tests pass.

- [ ] 18.4 Run `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api` — verify no migration changes needed (this issue does not add EF migrations; persistence is JSON snapshot only).

- [ ] 18.5 Run frontend typecheck: `cd src/WildBunch.Web && npx tsc --noEmit`.

- [ ] 18.6 Run frontend tests: `cd src/WildBunch.Web && npx vitest run`.

- [ ] 18.7 Run frontend build: `cd src/WildBunch.Web && npm run build`.

- [ ] 18.8 Grep proof: search for "town clerk" in `src/` — verify it no longer appears as a citizen descriptor in production code (only in `CitizenCast` as one of many roles, or in plan/test files).

- [ ] 18.9 Grep proof: search for "DescribeTownCitizen" in `src/` — verify the method is removed or only referenced in plans.

---

## Validation Plan

### Backend validation

1. **`dotnet build WildBunch.sln`** — zero errors.
2. **`dotnet test WildBunch.sln`** (via `.\scripts\postgres-dev.ps1 test`) — all tests pass, including:
   - `CitizenCastTests` — catalog integrity, determinism, concealment.
   - `CaseCharacterRosterTests` — feature disjointness.
   - `GameSessionSaloonPersonOfInterestTests` — citizen lookaround concealment + confrontation reveal.
   - `DevSaloonOverrideTests` — forced citizen role forcing.
   - `GetSaloonDevContextHandlerTests` — dev DTO shape.
   - `ForceSaloonOverrideHandlerTests` — forced citizen role persistence.
   - `SaloonPersonOfInterestDescriptorParityTests` — descriptor parity.
3. **`dotnet ef migrations list`** — no new migrations (JSON snapshot only).

### Frontend validation

1. **`npx tsc --noEmit`** — zero type errors.
2. **`npx vitest run`** — all tests pass, including updated `SaloonDevPanel.test.tsx`.
3. **`npm run build`** — production build succeeds.

### Grep proof

1. `rg "town clerk" src/ --glob '!**/plans/**'` — no production code references "town clerk" as a universal citizen descriptor. It may appear in `CitizenCast.cs` as one of many roles.
2. `rg "DescribeTownCitizen" src/` — the method is removed from `GameSession.cs`.

### Concealment proof

1. A domain test asserts the `SaloonPersonOfInterestSpotted` event's `Descriptor` for a citizen POI starts with "a stranger with" and does NOT contain the role display name or short name.
2. A domain test asserts the `SaloonPersonOfInterestConfronted` event's `Message` for a citizen wrong-declaration DOES contain "sheriff identifies them as" and the role display name.

### Uniqueness proof

1. A `WildBunch.GameContent.Tests` test asserts no `CitizenCast.Features` key matches any `CaseSuspectFeaturePool` feature key.
2. The same test asserts no `CitizenCast.Features` description matches any `CaseSuspectFeaturePool` feature description.

---

## Browser Proof Plan

Browser proof is required because the dev overlay and POI UI are touched. The canonical browser proof demonstrates:

1. **Forced citizen POI concealment:**
   - Start a session, enter a town, open the dev overlay.
   - In the Saloon dev panel, select `forcedKind = Citizen`, select a specific role (e.g. "the town butcher"), click "Force next POI".
   - Click "Look around" in the saloon.
   - Verify the POI descriptor shown to the player is "a stranger with {feature}" — NOT "the town butcher" or "a town clerk from {town}".
   - Screenshot the saloon surface showing the concealed descriptor.

2. **Mistaken-arrest role reveal + fine:**
   - With the citizen POI active, read wanted posters at the sheriff office (if not already done).
   - Return to saloon, declare a wanted identity, and take the citizen to the sheriff.
   - Verify the confrontation message says "The sheriff identifies them as the town butcher, releases them, and fines you ${amount}."
   - Verify the player's wallet decreased by the fine amount.
   - Screenshot the confrontation result showing the role reveal and fine.

3. **Dev overlay forced-citizen control:**
   - Screenshot the Saloon dev panel showing the citizen role selector with the source-backed archetype list.
   - Screenshot the active saloon POI section showing the citizen role (dev-only, not player-facing).

4. **Normal (non-forced) citizen variety:**
   - Clear the dev override.
   - Travel to a different town or advance the clock.
   - Look around the saloon — verify a different citizen appears (different feature, different role after take-in).
   - Screenshot at least two different citizen encounters.

Screenshots are saved under `.agents/superpowers/output/screenshots/` (git-ignored) and cited in the PR body by filename/path.

---

## AMBER Seams

- **`string.GetHashCode()` instability:** `GetHashCode()` is not stable across .NET runtimes/process restarts. The `CitizenCast.Select(townId, turn)` method must use a manual stable hash (e.g. sum of char codes with a prime multiplier) to ensure deterministic selection across process restarts and event replay. If this is not done, rehydrated sessions may resolve a different citizen on the next look-around than the one stored in the event stream — but since the role is stored in the event and visit state, replay correctness is preserved. The hash only affects new look-around selections, not replay. Still, use a stable hash for consistency.

- **Backward compatibility for old sessions:** Old sessions persisted before this change will not have `ActiveSaloonCitizenRole` in their snapshot. The `BuildCitizenRevealNarration` fallback handles this: if `citizenRoleKey` is null, the old narration format is used. This is a deliberate compatibility seam, not a shim — old sessions simply don't have the role and get the generic narration. New sessions always have the role.

- **Event field addition:** Adding `CitizenRole` (string?) to `SaloonPersonOfInterestSpotted` and `SaloonPersonOfInterestConfronted` is backward-compatible for JSON deserialization (missing fields default to null). Old events in the stream will have `CitizenRole = null` after deserialization, which is correct (they predate the citizen cast).

- **`DevSaloonOverride` record signature change:** Adding `ForcedCitizenRoleKey` to the record changes its constructor signature. All construction sites must be updated. The JSON serializer will handle old snapshots that lack the field (defaulting to null). This is acceptable per the repo's "current mainline model correctness wins over old-save compatibility" posture.

---

## Out of Scope

- Tracking encountered citizens across visits to prevent repeats (scope creep — current behavior doesn't track this).
- Adding citizen-specific confrontation outcomes beyond the existing wrong-declaration fine.
- Adding citizen names (only roles are added; citizens remain unnamed strangers until the sheriff reveal).
- Adding citizens to the UUID seed codec (citizens are selected at runtime, not seed time).
- Adding citizen-specific clues, journal entries, or wanted posters.
- Frontend player-facing POI UI changes beyond what naturally flows from the new descriptor format (the `SaloonPlace.tsx` already displays `personOfInterest.descriptor`, which will now be "a stranger with {feature}" instead of "a town clerk from {town}").
- Removing "town clerk" entirely from the cast (it remains as one of many roles in `CitizenCast.Roles`).
