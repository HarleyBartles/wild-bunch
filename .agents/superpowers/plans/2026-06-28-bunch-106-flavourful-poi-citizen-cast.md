# Flavourful POI Citizen Cast Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the generic "a town clerk from {town}" citizen fallback with a varied source-backed citizen cast. Each citizen has a named role (butcher, mortician, doctor, etc.) and a distinguishing feature drawn from the same shared feature vocabulary as suspects — so a citizen can plausibly be mistaken for a suspect. During POI lookaround, identity stays concealed: the player sees only "a stranger with {feature}." The citizen's role is revealed only after a mistaken take-in, when the sheriff identifies them, releases them, and fines the player. Dev overlay can force a specific available citizen role to be the next POI encounter.

**Architecture:** A new `CitizenCast` static content catalog in `WildBunch.Domain.Game` defines named town roles. Citizen distinguishing features are NOT a separate pool — they are drawn from the same feature vocabulary as suspects. `GameSession.LookAroundSaloon()` collects the suspect feature descriptions from `CaseFile.Suspects` (their `SuspectProfile.IdentifyingFacts`) and passes them to `CitizenCast.Select(townId, day, turn, visitNumber, featureDescriptions)`, which deterministically picks a citizen role and a feature from the provided suspect feature vocabulary. This preserves mistaken-identity play: a citizen can have the same visible feature as a wanted suspect, and the player cannot tell them apart by feature alone. The lookaround descriptor becomes "a stranger with {feature}" (concealment), matching the suspect descriptor pattern. The citizen's role key is carried in the `SaloonPersonOfInterestSpotted` event and stored in `TownVisitTownState` (accessed via `CurrentTownVisit.CurrentTownState`) alongside the descriptor. The `BountyLoopCoordinator` citizen confrontation path reads the stored role and builds a reveal narration: "The sheriff identifies them as {role}, releases them, and fines you ${fineAmount}." The `DevSaloonOverride` record is extended with an optional forced citizen role key. The `SaloonDevContextDto` / `CitizenInfoDto` is updated to expose the available citizen roles. The frontend `SaloonDevPanel` gets a citizen role selector when forcing a Citizen override. The non-role-revealing guardrail verifies that suspect feature descriptions (the shared vocabulary) do not contain citizen role names — but does NOT create a separate citizen-only feature vocabulary.

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
- Citizen distinguishing features are NOT a separate pool. Citizens draw from the same feature vocabulary as suspects — the `IdentifyingFacts` descriptions from `CaseFile.Suspects`. This preserves mistaken-identity play: a citizen can have the same visible feature as a wanted suspect, and the player cannot tell them apart by feature alone. The citizen is innocent because of their role, not because of a separate civilian-only feature set.
- `WildBunch.Domain` cannot reference `WildBunch.GameContent` (dependency direction is `GameContent → Domain`). The shared feature vocabulary flows from `GameContent` into `Domain` at seed time through `CaseFile.Suspects[].Profile.IdentifyingFacts`. `GameSession` collects these descriptions and passes them to `CitizenCast.Select(...)` as a parameter. No cross-project reference is needed.
- POI lookaround does not reveal the citizen role/name; it exposes only the distinguishing feature via "a stranger with {feature}" copy.
- The citizen role is revealed only in the sheriff mistaken-arrest resolution narration.
- The `DevSaloonOverride` consume-once lifecycle is preserved. A forced citizen role is consumed by the next `LookAroundSaloon()` call.
- Worker environment uses PowerShell; do not use `&&` for command chaining.
- Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent validation.
- styled-components for component styling; reference design tokens via `var(--token-name)`. No plain CSS classes.
- Adding a nullable field to an existing event record is backward-compatible for JSON event deserialization (missing fields default to null).
- The `ActiveSaloonPersonOfInterestDescriptor` string in `TownVisitTownState` (defined in `TownSourceVisitState.cs`, accessed via `CurrentTownVisit.CurrentTownState`) remains the player-facing concealment descriptor. A new `ActiveSaloonCitizenRole` string? field on `TownVisitTownState` stores the role key for the later reveal, separate from the descriptor. Note: `TownSourceVisitState` (line 13) is the per-source state class; `TownVisitTownState` (line 68) is the per-town-visit state class that owns the active saloon POI. The role belongs on `TownVisitTownState`, not `TownSourceVisitState`.

### Citizen cast content contract

The `CitizenCast` catalog defines:
- **Roles**: A static list of `CitizenRole` records, each with a `Key` (stable identifier), `DisplayName` (e.g. "the town butcher"), and `ShortName` (e.g. "butcher"). At least 12 roles to prove a full flavour cast.
- **No separate feature pool**: Citizens do NOT have a separate `CitizenFeature` pool. Citizen distinguishing features are drawn from the same feature vocabulary as suspects — the `IdentifyingFacts` descriptions from `CaseFile.Suspects`, which originate from `CaseSuspectFeaturePool` at seed time. This is the core design intent: a citizen can plausibly be mistaken for a suspect because they share the same visible feature vocabulary (limps, scars, earrings, hats, etc.).
- **CitizenEncounter**: A record carrying `CitizenRole Role` + `string FeatureDescription` (a feature description string from the shared suspect vocabulary, not a separate citizen feature type).
- **Select(townId, day, turn, visitNumber, IReadOnlyList<string> featureDescriptions)**: Deterministic pick of a `CitizenEncounter` (role + feature) based on a stable manual hash of `townId + day + turn + visitNumber`. The role is picked from `Roles` and the feature is picked from the provided `featureDescriptions` (the suspect feature vocabulary from the `CaseFile`). Using all four inputs provides substantially more variety than `townId + turn` alone. Still avoids `string.GetHashCode()` (not stable across process restarts); use a manual stable hash (e.g. sum of char codes with a prime multiplier). If `featureDescriptions` is empty, fall back to a neutral descriptor like "an unfamiliar face" (edge case — should not occur in normal play since the case always has suspects with features).
- **SelectByRoleKey(roleKey, IReadOnlyList<string> featureDescriptions)**: Look up a specific citizen by role key and pick a feature from the provided descriptions (for dev overlay forcing). The feature pick is deterministic based on the role key + feature descriptions.
- **GetRoleByKey(roleKey)**: Look up a `CitizenRole` by key only — no feature, no `featureDescriptions` parameter. Used by the confrontation reveal path, which only needs the role display name and already has the concealment descriptor from active state. Throw `ArgumentException` if the role key is not found.
- **ResolveDescriptor(encounter)**: Returns `"a stranger with {NormalizeFeatureDescriptor(encounter.FeatureDescription)}"` — the concealment descriptor shown during lookaround. Reuses the same normalization logic as `SaloonPersonOfInterestDescriptor.NormalizeFeatureDescriptor` (strip "has a"/"wears a" prefixes → "a"/"an"). Extract a shared helper or duplicate the small normalization.
- **ResolveRevealName(encounter)**: Returns the role display name (e.g. "the town butcher") — used in contexts where an encounter is already available. The confrontation reveal path uses `GetRoleByKey(roleKey).DisplayName` directly instead, since it does not have or need an encounter.

### Shared feature vocabulary (mistaken-identity invariant)

Citizens and suspects share the same distinguishing-feature vocabulary. This is the core design intent: a citizen can plausibly be mistaken for a suspect because they share the same visible feature vocabulary.

- Citizen features are drawn from `CaseFile.Suspects[].Profile.IdentifyingFacts[].Description` — the same feature descriptions that suspects have.
- There is NO separate citizen feature pool, NO disjointness requirement, and NO disjointness test.
- A citizen may have the same feature as a suspect in the current case. This is intentional — it creates mistaken-identity play.
- Within a generated case / active encounter set, the feature used for the currently surfaced citizen must still be distinguishable enough for the player to tell POIs apart visually, but citizens and suspects are allowed (and expected) to use the same kind of feature vocabulary.
- The feature pool supports mistaken identity, not prevents it.

### Non-role-revealing feature guardrail (shared vocabulary)

Since citizens now draw from the suspect feature vocabulary, the guardrail verifies that the shared feature descriptions (the suspect `IdentifyingFacts` from `CaseSuspectFeaturePool`) do not contain citizen role names. This prevents a feature like "wears a butcher's apron" from leaking the citizen's role through the concealment descriptor.

This guardrail does NOT create a separate citizen-only feature vocabulary. It only verifies that the shared vocabulary (suspect features) is safe for citizen concealment.

A test (`SharedFeatures_DoNotRevealCitizenRoleNames`) asserts that no feature description from `CaseSuspectFeaturePool.FeaturePool` contains any citizen role key, short name, or display name token. The citizen role names are drawn from `CitizenCast.Roles`. This test lives in `WildBunch.GameContent.Tests` (where both `CaseSuspectFeaturePool` and `CitizenCast` are accessible — `CitizenCast` is in `WildBunch.Domain` which `GameContent` references).

### Feature uniqueness contract (replaced — shared vocabulary)

The previous "disjoint from suspect features" contract has been removed. Citizens and suspects share the same feature vocabulary. There is:
- NO "no key overlap" test.
- NO "no description overlap" test.
- NO "separate pools by construction" language.

The correct invariant is:
- Within a generated case / active encounter set, the feature used for the currently surfaced citizen must still be unique enough for the player to distinguish the visible POI.
- But citizens and suspects are allowed, and in fact expected, to use the same kind of feature vocabulary.
- The feature pool should support mistaken identity, not prevent it.

### Concealment falsification proof

Tests at both domain aggregate and integration levels must prove that:
1. The `SaloonPersonOfInterestSpotted` event's `Descriptor` (player-facing) is "a stranger with {feature}" and does NOT contain the role name or role display name.
2. The `SaloonPersonOfInterestSpotted` event's `Message` is "You look around the saloon and spot a stranger with {feature}." and does NOT contain the role name.
3. The `ActiveSaloonPersonOfInterestDescriptor` stored in `TownVisitTownState` is the concealment descriptor, not the role.
4. The `ActiveSaloonCitizenRole` stored in `TownVisitTownState` is the role key, not shown in player-facing DTOs.
5. The player-facing `ActiveSaloonPersonOfInterestDto.Descriptor` is the concealment descriptor.
6. The `SaloonPersonOfInterestConfronted` event's `Message` DOES contain the role reveal (only after mistaken take-in).
7. The `SaloonPersonOfInterestConfronted` event's `TargetName` is the concealment descriptor (what the player saw), not the role.

---

## File Structure

### Domain layer (src/WildBunch.Domain/)

| File | Responsibility |
|------|----------------|
| `Game/CitizenCast.cs` | New static content catalog: `CitizenRole` record, `CitizenEncounter` record (role + feature description string), `CitizenCast.Roles` static list, `CitizenCast.Select(townId, day, turn, visitNumber, featureDescriptions)`, `CitizenCast.SelectByRoleKey(roleKey, featureDescriptions)`, `CitizenCast.GetRoleByKey(roleKey)`, `CitizenCast.ResolveDescriptor(encounter)`, `CitizenCast.ResolveRevealName(encounter)`. No separate `CitizenFeature` pool — features come from the shared suspect vocabulary passed as a parameter. |
| `Game/GameSession.cs` (modify) | Replace `DescribeTownCitizen()` with `CitizenCast.Select(townId, day, turn, visitNumber, featureDescriptions)` calls in `LookAroundSaloon()` (both normal fallback path and dev-override citizen path). Collect `featureDescriptions` from `CaseFile.Suspects[].Profile.IdentifyingFacts[].Description`. Emit `CitizenRole` in `SaloonPersonOfInterestSpotted` event. Store role in `TownVisitTownState` via `SetActiveSaloonCitizenPersonOfInterest(descriptor, role)`. |
| `Game/GameSession.BountyLoopCoordinator.cs` (modify) | Update citizen confrontation path to read `ActiveSaloonCitizenRole` from `TownVisitTownState` and build role-reveal narration in `ProduceSaloonConfrontedEvent` for citizen wrong-declaration outcome |
| `Game/TownSourceVisitState.cs` (modify) | Add `ActiveSaloonCitizenRole` (string?) property to `TownVisitTownState` (line 68+). Update `TownVisitTownState` constructor to accept `activeSaloonCitizenRole`. Update `SetActiveSaloonCitizenPersonOfInterest(descriptor, role)` to accept and store the role. Update `ClearActiveSaloonPersonOfInterest()` to clear the role. The `TownSourceVisitState` class (line 13) is NOT modified. |
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
| `tests/WildBunch.Domain.Tests/CitizenCastTests.cs` | New: verify cast has ≥12 roles, no duplicate role keys, no duplicate role display names, `Select(townId, day, turn, visitNumber, featureDescriptions)` is deterministic and varied, `SelectByRoleKey(roleKey, featureDescriptions)` resolves correctly, `ResolveDescriptor()` produces "a stranger with {feature}" and does NOT contain the role name, `Select()` with empty feature descriptions falls back gracefully |
| `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs` (modify) | Update citizen tests to verify: lookaround descriptor is "a stranger with {feature}" (not "a town clerk"), `ActiveSaloonCitizenRole` is set, confrontation message reveals the role, player-facing DTO descriptor is the concealment descriptor, citizen feature comes from the suspect feature vocabulary |
| `tests/WildBunch.Domain.Tests/DevSaloonOverrideTests.cs` (modify) | Update citizen override tests to verify forced citizen role key is consumed and the correct citizen is spotted |
| `tests/WildBunch.GameContent.Tests/CaseCharacterRosterTests.cs` (modify) | Add `SharedFeatures_DoNotRevealCitizenRoleNames` guardrail: no `CaseSuspectFeaturePool.FeaturePool` description contains any `CitizenCast.Roles` key, short name, or display name token. This verifies the shared vocabulary is safe for citizen concealment — NOT a disjointness test. |
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
  - `public sealed record CitizenEncounter(CitizenRole Role, string FeatureDescription)` — the feature description is a string from the shared suspect vocabulary (passed to `Select`), NOT a separate citizen feature type.
  - `public static class CitizenCast` with:
    - `Roles` — static readonly list of ≥12 `CitizenRole` records: butcher, mortician, doctor, blacksmith, schoolteacher, preacher, seamstress, hotel-keeper, banker, newspaperman, stable-hand, telegraph-operator, barber, undertaker, prospector, cook, stagecoach-agent, gunsmith, town-clerk
    - NO `Features` list. NO `CitizenFeature` record. NO `RoleFeaturePairs`. Citizen features come from the shared suspect vocabulary passed as a parameter to `Select`.
    - `Select(TownId townId, int day, int turn, int visitNumber, IReadOnlyList<string> featureDescriptions)` — deterministic pick of a role + a feature from the provided `featureDescriptions`. Role index: `StableHash(townId.Value, day, turn, visitNumber) % Roles.Count`. Feature index: `StableHash(townId.Value, day, turn, visitNumber, "feature") % featureDescriptions.Count`. Using all four inputs provides substantially more variety than `townId + turn` alone. Do NOT use `string.GetHashCode()` (not stable across process restarts); use a manual `StableHash` helper (e.g. sum of char codes with a prime multiplier over the concatenated string representation). If `featureDescriptions` is empty, return an encounter with `FeatureDescription = null` (edge case — `ResolveDescriptor` falls back to "an unfamiliar face").
    - `SelectByRoleKey(string roleKey, IReadOnlyList<string> featureDescriptions)` — look up the role by key, pick a feature from `featureDescriptions` deterministically (using the role key as the hash input). Throw `ArgumentException` if the role key is not found. Used by the dev overlay forcing path (which has featureDescriptions available from the CaseFile).
    - `GetRoleByKey(string roleKey)` — look up a `CitizenRole` by key only. No feature, no `featureDescriptions` parameter. Throw `ArgumentException` if the role key is not found. Used by the confrontation reveal path (`BuildCitizenRevealNarration`), which only needs the role display name and already has the concealment descriptor from active state. Does NOT call `Select(...)`, does NOT re-pick a feature.
    - `ResolveDescriptor(CitizenEncounter encounter)` — if `encounter.FeatureDescription` is null/empty, return `"an unfamiliar face"`. Otherwise: `$"a stranger with {NormalizeFeatureDescriptor(encounter.FeatureDescription)}"`. Reuse the same normalization logic as `SaloonPersonOfInterestDescriptor.NormalizeFeatureDescriptor` (strip "has a"/"wears a" prefixes → "a"/"an"). Extract a shared helper or duplicate the small normalization.
    - `ResolveRevealName(CitizenEncounter encounter)` — `encounter.Role.DisplayName` (e.g. "the town butcher"). Used in contexts where an encounter is already available. The confrontation reveal path uses `GetRoleByKey(roleKey).DisplayName` directly instead.
    - `ResolveRevealNarration(CitizenEncounter encounter, decimal fineAmount)` — `$"You bring {ResolveDescriptor(encounter)} to the sheriff. The sheriff identifies them as {encounter.Role.DisplayName}, releases them, and fines you ${fineAmount:0.00}."`. (Note: the actual confrontation path in `BountyLoopCoordinator` does NOT use this method — it uses `GetRoleByKey` + the stored concealment descriptor. This method is a convenience helper for other contexts if needed.)

- [ ] 1.2 Create `tests/WildBunch.Domain.Tests/CitizenCastTests.cs` with tests:
  - `CitizenCast_HasAtLeastTwelveRoles` — `Assert.True(CitizenCast.Roles.Count >= 12)`
  - `CitizenCast_NoDuplicateRoleKeys` — all role keys are distinct
  - `CitizenCast_NoDuplicateRoleDisplayNames` — all display names are distinct
  - `CitizenCast_SelectIsDeterministic` — same town + day + turn + visitNumber + same featureDescriptions → same encounter
  - `CitizenCast_SelectDifferentInputsProduceVariedEncounters` — at least 5 distinct role picks across a range of town/day/turn/visitNumber inputs (using a fixed featureDescriptions list), proving the 4-input key provides meaningful variety
  - `CitizenCast_Select_PicksFeatureFromProvidedDescriptions` — the encounter's `FeatureDescription` is one of the provided descriptions
  - `CitizenCast_Select_WithEmptyFeatureDescriptions_FallsBackGracefully` — returns an encounter with null `FeatureDescription`; `ResolveDescriptor` returns "an unfamiliar face"
  - `CitizenCast_SelectByRoleKey_ResolvesCorrectly` — each role key resolves to the correct role, with a feature from the provided descriptions
  - `CitizenCast_SelectByRoleKey_ThrowsForUnknownKey` — unknown key throws `ArgumentException`
  - `CitizenCast_GetRoleByKey_ResolvesCorrectly` — each role key resolves to the correct `CitizenRole` with the correct display name
  - `CitizenCast_GetRoleByKey_ThrowsForUnknownKey` — unknown key throws `ArgumentException`
  - `CitizenCast_GetRoleByKey_DoesNotRequireFeatureDescriptions` — `GetRoleByKey` can be called with only a role key, no feature descriptions parameter
  - `CitizenCast_ResolveDescriptor_ProducesConcealmentFormat` — starts with "a stranger with " and does NOT contain the role display name or short name
  - `CitizenCast_ResolveRevealName_ProducesRoleDisplayName` — returns the role display name
  - `CitizenCast_ResolveRevealNarration_ContainsRoleAndFine` — contains the role display name and the fine amount, and contains "sheriff identifies them as"

- [ ] 1.3 Update `src/WildBunch.Domain/Game/INDEX.md` to add `CitizenCast.cs` entry.

- [ ] 1.4 Update `tests/WildBunch.Domain.Tests/INDEX.md` to add `CitizenCastTests.cs` entry.

- [ ] 1.5 Run `dotnet build` and `dotnet test` for the new test project to verify the catalog compiles and tests pass.

### Task 2: Shared-features non-role-revealing guardrail test

**Files:**
- `tests/WildBunch.GameContent.Tests/CaseCharacterRosterTests.cs` (modify)

**Steps:**

- [ ] 2.1 Add a test `SharedFeatures_DoNotRevealCitizenRoleNames` to `CaseCharacterRosterTests.cs`:
  - Collect all `CaseSuspectFeaturePool.FeaturePool` descriptions (both primary and accessory features).
  - Collect all `CitizenCast.Roles` keys, short names, and display name tokens.
  - Assert that no suspect feature description contains any citizen role key, short name, or display name token (case-insensitive).
  - This verifies that the shared feature vocabulary (suspect features) is safe for citizen concealment — a suspect feature like "wears a butcher's apron" would leak the citizen's role through the concealment descriptor. This is NOT a disjointness test; it is a non-role-revealing guardrail on the shared vocabulary.
  - Note: `CitizenCast` is in `WildBunch.Domain` which `WildBunch.GameContent` references, so `CaseCharacterRosterTests` can access both `CaseSuspectFeaturePool` and `CitizenCast.Roles`.

- [ ] 2.2 Run `dotnet test` for `WildBunch.GameContent.Tests` to verify the guardrail test passes.

### Task 3: Domain — extend TownVisitTownState with citizen role

**Files:**
- `src/WildBunch.Domain/Game/TownSourceVisitState.cs` (modify — `TownVisitTownState` class only, line 68+)

**Steps:**

- [ ] 3.1 Add `public string? ActiveSaloonCitizenRole { get; private set; }` property to `TownVisitTownState` (line 68+), adjacent to `ActiveSaloonPersonOfInterestDescriptor`. Do NOT modify the `TownSourceVisitState` class (line 13).

- [ ] 3.2 Update the `TownVisitTownState` constructor (line 72-80) to accept a new `string? activeSaloonCitizenRole = null` parameter, and assign it: `ActiveSaloonCitizenRole = activeSaloonCitizenRole;`. Place it after `activeSaloonPersonOfInterestKind` in the parameter list.

- [ ] 3.3 Update `SetActiveSaloonCitizenPersonOfInterest(string descriptor)` → `SetActiveSaloonCitizenPersonOfInterest(string descriptor, string? citizenRole)`:
  ```csharp
  public void SetActiveSaloonCitizenPersonOfInterest(string descriptor, string? citizenRole)
  {
      ActiveSaloonPersonOfInterestId = null;
      ActiveSaloonPersonOfInterestDescriptor = descriptor;
      ActiveSaloonPersonOfInterestKind = SaloonPersonOfInterestKind.Citizen;
      ActiveSaloonCitizenRole = citizenRole;
  }
  ```

- [ ] 3.4 Update `ClearActiveSaloonPersonOfInterest()` to also clear `ActiveSaloonCitizenRole = null`.

- [ ] 3.5 Run `dotnet build` to verify compilation. Existing callers of `SetActiveSaloonCitizenPersonOfInterest(descriptor)` will need updating (Task 5). The `TownVisitTownState` constructor change is backward-compatible because the new parameter has a default of `null`.

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
  var featureDescriptions = CollectSuspectFeatureDescriptions();
  CitizenEncounter? forcedEncounter = null;
  if (pendingDevOverride.ForcedCitizenRoleKey is not null)
  {
      forcedEncounter = CitizenCast.SelectByRoleKey(pendingDevOverride.ForcedCitizenRoleKey, featureDescriptions);
  }
  else
  {
      forcedEncounter = CitizenCast.Select(CurrentTown.TownId, Clock.Day, Clock.Turn, CurrentTownVisit.CurrentTownState.VisitNumber, featureDescriptions);
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
  var featureDescriptions = CollectSuspectFeatureDescriptions();
  var citizenEncounter = CitizenCast.Select(CurrentTown.TownId, Clock.Day, Clock.Turn, CurrentTownVisit.CurrentTownState.VisitNumber, featureDescriptions);
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

- [ ] 6.3 Add a private helper `CollectSuspectFeatureDescriptions()` to `GameSession`:
  ```csharp
  private IReadOnlyList<string> CollectSuspectFeatureDescriptions()
      => CaseFile.Suspects
          .SelectMany(s => s.Profile.IdentifyingFacts)
          .Select(f => f.Description)
          .Where(d => !string.IsNullOrWhiteSpace(d))
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .ToList();
  ```
  This collects the shared feature vocabulary from the case's suspects — the same `IdentifyingFacts` descriptions that `SaloonPersonOfInterestDescriptor.Describe` uses for suspect descriptors. Citizens draw from this same vocabulary.

- [ ] 6.4 Remove or mark obsolete the `DescribeTownCitizen` method (~line 3298-3299). If no other callers remain, delete it. Check for references in plans/other files — plan references are informational only and do not block deletion.

- [ ] 6.5 Run `dotnet build` to verify compilation.

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
  Where `BuildCitizenRevealNarration` is a new helper that resolves the role by key only — it does NOT call `Select(...)`, does NOT re-pick a feature, and does NOT require `featureDescriptions`. The reveal already has the concealment descriptor from active state; it only needs the citizen role display name:
  ```csharp
  private static string BuildCitizenRevealNarration(string concealmentDescriptor, string? roleKey, decimal fineAmount)
  {
      if (string.IsNullOrWhiteSpace(roleKey))
      {
          // Backward-compatible fallback: no role stored (old sessions or edge cases).
          return $"You bring {concealmentDescriptor} to the sheriff, but the declaration is wrong. The sheriff releases them and fines you ${fineAmount:0.00}.";
      }
      var role = CitizenCast.GetRoleByKey(roleKey);
      return $"You bring {concealmentDescriptor} to the sheriff. The sheriff identifies them as {role.DisplayName}, releases them, and fines you ${fineAmount:0.00}.";
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

### Task 9: Persistence — serialize ActiveSaloonCitizenRole via TownVisitTownStateSnapshot

**Files:**
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs` (modify)

**Steps:**

The active saloon POI state is serialized through the private `TownVisitTownStateSnapshot` record (line 711-742 of `GameSessionJsonSerializer.Components.cs`), NOT through `TownSourceVisitStateSnapshot` (line 744+, which serializes per-source state). The role field must be routed through `TownVisitTownStateSnapshot`.

- [ ] 9.1 Add `string? ActiveSaloonCitizenRole` to the `TownVisitTownStateSnapshot` record signature (line 711-718), after `ActiveSaloonPersonOfInterestKind`:
  ```csharp
  private sealed record TownVisitTownStateSnapshot(
      string TownId,
      int VisitNumber,
      IReadOnlyList<TownSourceVisitStateSnapshot>? SourceStates,
      int WantedPostersLastCheckedVisitNumber,
      string? ActiveSaloonPersonOfInterestId,
      string? ActiveSaloonPersonOfInterestDescriptor,
      SaloonPersonOfInterestKind? ActiveSaloonPersonOfInterestKind,
      string? ActiveSaloonCitizenRole)
  ```

- [ ] 9.2 Update `TownVisitTownStateSnapshot.FromDomain(TownVisitTownState townState)` (line 720-731) to map the new field:
  ```csharp
  public static TownVisitTownStateSnapshot FromDomain(TownVisitTownState townState)
      => new(
          townState.TownId.Value,
          townState.VisitNumber,
          townState.SourceStates
              .OrderBy(sourceState => sourceState.SourceKind)
              .Select(TownSourceVisitStateSnapshot.FromDomain)
              .ToArray(),
          townState.WantedPostersLastCheckedVisitNumber,
          townState.ActiveSaloonPersonOfInterestId?.Value,
          townState.ActiveSaloonPersonOfInterestDescriptor,
          townState.ActiveSaloonPersonOfInterestKind,
          townState.ActiveSaloonCitizenRole);
  ```

- [ ] 9.3 Update `TownVisitTownStateSnapshot.ToDomain()` (line 733-741) to pass the role through the updated `TownVisitTownState` constructor:
  ```csharp
  public TownVisitTownState ToDomain()
      => new(
          new TownId(TownId),
          VisitNumber,
          SourceStates?.Select(snapshot => snapshot.ToDomain()),
          wantedPostersSpent: WantedPostersLastCheckedVisitNumber == VisitNumber,
          activeSaloonPersonOfInterestId: ActiveSaloonPersonOfInterestId is null ? null : new SuspectId(ActiveSaloonPersonOfInterestId),
          activeSaloonPersonOfInterestDescriptor: ActiveSaloonPersonOfInterestDescriptor,
          activeSaloonPersonOfInterestKind: ActiveSaloonPersonOfInterestKind,
          activeSaloonCitizenRole: ActiveSaloonCitizenRole);
  ```
  This routes the role directly through the constructor (added in Task 3.2). No `GameSessionRehydrator.SetBackingField` hack is needed — the constructor accepts the role as a normal parameter.

- [ ] 9.4 Old snapshots with a missing `ActiveSaloonCitizenRole` field deserialize as `null` via System.Text.Json's default behavior for missing nullable fields. This preserves the old generic mistaken-arrest narration fallback (the `BuildCitizenRevealNarration` helper in Task 7 handles `null` role keys by falling back to the old narration format).

- [ ] 9.5 The `DevSaloonOverride` JSON serialization automatically picks up the new `ForcedCitizenRoleKey` field via System.Text.Json — no manual changes needed for the override.

- [ ] 9.6 Run `dotnet build` to verify compilation.

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
      string DisplayName);
  ```
  Update the doc comment: "Citizens are drawn from a source-backed cast of named town roles. Citizen distinguishing features come from the same shared vocabulary as suspects — the role selector chooses the citizen role, not a separate citizen-only visual feature taxonomy."

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
      Descriptor: "a stranger with a distinguishing feature from the shared suspect vocabulary",
      HasNamedArchetypes: true,
      AvailableArchetypes: CitizenCast.Roles.Select(role =>
          new CitizenArchetypeDto(role.Key, role.DisplayName)).ToList());
  ```
  Note: the archetype DTO carries only the role key and display name — no feature description. The feature is chosen at lookaround time from the shared suspect vocabulary, not fixed per role. The role selector chooses the citizen role; it does not imply a separate citizen-only visual feature taxonomy.

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
  }

  export interface CitizenInfoDto {
    descriptor: string;
    hasNamedArchetypes: boolean;
    availableArchetypes: CitizenArchetypeDto[];
  }
  ```
  Note: no `featureDescription` field — the feature is chosen at lookaround time from the shared suspect vocabulary, not fixed per role.

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
                  {a.displayName}
                </option>
              ))}
            </Select>
          </Field>
          <CitizenNote>
            Source-backed cast of {data.citizenInfo.availableArchetypes.length} citizen roles.
            Citizen features come from the shared suspect vocabulary — the role selector
            chooses the citizen role, not a separate visual feature. The feature is
            concealed during lookaround and revealed only after mistaken take-in.
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

---

## Realignment: Simplified Saloon POI Selection (mid-implementation)

> **Date:** 2026-06-28. The original plan preserved the old saloon POI eligibility logic (town presence, known warrant, poster state gates). The product direction has changed: saloon POI selection is now much simpler. This section supersedes the eligibility-related parts of the original plan.

### New product rule

Any non-culprit suspect can walk into any saloon. Any citizen can walk into any saloon. The saloon POI opportunity can be:
- a suspect,
- a citizen,
- or nobody of interest.

Do NOT gate ordinary suspect or citizen saloon POI eligibility on town presence, known warrant poster state, viewed poster state, clue visibility, town source state, or whether the suspect has a local presence. A suspect or citizen being in the saloon is just a rolled opportunity.

The only special case is the true killer. The true killer remains gated behind the existing killer-release gameplay gate. Do not fix the broader killer-release model in BUNCH-106.

### Implementation changes

1. **`IsEligibleSaloonPersonOfInterestCandidate`**: Simplified to only exclude the unreleased true killer. No warrant, presence, or poster checks.
2. **`GetSaloonPoiIneligibilityReason`**: Simplified to only report the unreleased true killer reason.
3. **`TryGetConfrontableSaloonPersonOfInterestCandidateInTown`**: Renamed/repurposed to `TryGetEligibleSaloonSuspectCandidate` — iterates suspects, skips only the unreleased true killer.
4. **Normal `LookAroundSaloon` path**: Build candidate pool from non-culprit suspects + citizen cast + nobody outcome. Roll deterministically using the salt source. The pool is: each eligible suspect, each citizen role, and a "nobody" slot. The roll picks one deterministically.
5. **`DevSaloonPoiKind`**: Add `None` for forcing "nobody of interest."
6. **`DevSaloonOverride`**: Add `ForNone()` factory.
7. **`ForceDevSaloonOverride` validation**: Only reject the unreleased true killer for specific suspect force. No warrant/presence checks.
8. **Tests**: Use dev override seams to force specific outcomes. Do NOT remove suspects or rely on ineligibility to get citizen/nobody outcomes.

### Test expectations (realigned)

- A non-culprit suspect can be selected as saloon POI without town presence (no `SetWantedSuspectPresenceState` call).
- A citizen can be selected as saloon POI via dev override without suspect ineligibility hacks.
- Nobody of interest is a possible saloon outcome (force via dev override).
- Unreleased true killer is not selected by ordinary saloon POI roll.
- Dev override can force citizen/suspect/nobody cleanly.
- Citizen concealment/reveal tests from the original plan are preserved.

### Out of scope (reported as follow-up)

- The existing sheriff turn-in rule where the UI lets you name a suspect but the turn-in path blocks with "you did not have enough information" is wrong as a product rule. If the game lets the player name a suspect for confrontation/take-in, that should be enough to perform the take-in attempt. This is NOT fixed in BUNCH-106 unless it blocks tests. Report as a separate issue candidate.
- The killer-release model redesign (clue-count → gang-members-taken-in count) is out of scope.

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
   - `CaseCharacterRosterTests` — shared-features non-role-revealing guardrail (`SharedFeatures_DoNotRevealCitizenRoleNames`).
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

### Shared vocabulary proof (replaces uniqueness proof)

1. A `WildBunch.GameContent.Tests` test (`SharedFeatures_DoNotRevealCitizenRoleNames`) asserts that no `CaseSuspectFeaturePool.FeaturePool` description contains any `CitizenCast.Roles` key, short name, or display name token. This verifies the shared vocabulary is safe for citizen concealment.
2. A domain test asserts that the citizen's `FeatureDescription` (in the `CitizenEncounter`) is one of the descriptions from `CaseFile.Suspects[].Profile.IdentifyingFacts` — proving citizens draw from the same vocabulary as suspects, not a separate pool.

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

- **`string.GetHashCode()` instability:** `GetHashCode()` is not stable across .NET runtimes/process restarts. The `CitizenCast.Select(townId, day, turn, visitNumber)` method must use a manual stable hash (e.g. sum of char codes with a prime multiplier) over the concatenated string representation of all four inputs to ensure deterministic selection across process restarts and event replay. If this is not done, rehydrated sessions may resolve a different citizen on the next look-around than the one stored in the event stream — but since the role is stored in the event and `TownVisitTownState`, replay correctness is preserved. The hash only affects new look-around selections, not replay. Still, use a stable hash for consistency.

- **Backward compatibility for old sessions:** Old sessions persisted before this change will not have `ActiveSaloonCitizenRole` in their `TownVisitTownStateSnapshot`. System.Text.Json deserializes missing nullable fields as `null`, so the `TownVisitTownState.ToDomain()` constructor receives `null` for the role. The `BuildCitizenRevealNarration` fallback handles this: if `citizenRoleKey` is null, the old narration format is used. This is a deliberate compatibility seam, not a shim — old sessions simply don't have the role and get the generic narration. New sessions always have the role.

- **Event field addition:** Adding `CitizenRole` (string?) to `SaloonPersonOfInterestSpotted` and `SaloonPersonOfInterestConfronted` is backward-compatible for JSON deserialization (missing fields default to null). Old events in the stream will have `CitizenRole = null` after deserialization, which is correct (they predate the citizen cast).

- **`DevSaloonOverride` record signature change:** Adding `ForcedCitizenRoleKey` to the record changes its constructor signature. All construction sites must be updated. The JSON serializer will handle old snapshots that lack the field (defaulting to null). This is acceptable per the repo's "current mainline model correctness wins over old-save compatibility" posture.

- **Shared feature vocabulary edge case:** `CitizenCast.Select(...)` receives `featureDescriptions` from `CaseFile.Suspects[].Profile.IdentifyingFacts`. In normal play, the case always has suspects with features, so this list is non-empty. If it is somehow empty (edge case, test fixture, or future case shape), `ResolveDescriptor` falls back to "an unfamiliar face" — the lookaround still works, just without a specific feature. This is a graceful degradation, not a crash.

- **Mistaken-identity by design:** Citizens and suspects share the same feature vocabulary. A citizen may have the same visible feature as a wanted suspect. This is intentional — the player cannot tell them apart by feature alone, and the citizen is innocent because of their role, not their feature. The confrontation path checks `ActiveSaloonPersonOfInterestKind` (Citizen vs WantedSuspect), not feature matching, so a citizen with the same feature as a suspect is still correctly resolved as a wrong declaration.

---

## Out of Scope

- Tracking encountered citizens across visits to prevent repeats (scope creep — current behavior doesn't track this).
- Adding citizen-specific confrontation outcomes beyond the existing wrong-declaration fine.
- Adding citizen names (only roles are added; citizens remain unnamed strangers until the sheriff reveal).
- Adding citizens to the UUID seed codec (citizens are selected at runtime, not seed time).
- Adding citizen-specific clues, journal entries, or wanted posters.
- Frontend player-facing POI UI changes beyond what naturally flows from the new descriptor format (the `SaloonPlace.tsx` already displays `personOfInterest.descriptor`, which will now be "a stranger with {feature}" instead of "a town clerk from {town}").
- Removing "town clerk" entirely from the cast (it remains as one of many roles in `CitizenCast.Roles`).
