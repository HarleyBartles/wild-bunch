# BUNCH-102: Player Start-Over Settings and Prologue Start Loop — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver one player-facing lifecycle slice: a themed start/prologue flow for beginning a playthrough (start screen → name entry → story so far → starting town selection → game start proper), plus a normal player Game Settings surface that lets Harley start over during playtesting without dev-only controls. Confirming Start Over archives the active playthrough (does not delete it), returns the player to the start flow, and a fresh active playthrough is created only after the start flow is completed. One active playthrough at a time is preserved.

**Architecture:** Player commands flow through Application handlers into `GameSession` aggregate methods that emit typed domain events. Start Over is a player command-side operation: an `ArchivePlaythroughHandler` loads the active `GameSession`, invokes a new `GameSession.ArchivePlaythrough(...)` command method that emits a `PlaythroughArchived` event (typed, replay-friendly, audit-friendly), and stores it through the repository/UoW. The frontend tracks the active session id in `localStorage` under `wild-bunch.current-game-id`; Start Over clears that id and returns to the start flow. A new active playthrough is created only when the player completes the full start flow and the existing `POST /api/games` route is called.

**One-active-playthrough invariant (strong route):** The invariant is enforced at the application/persistence level, not only in browser-local state. `StartNewGameHandler` is extended so that before creating a new session it loads any existing `Active` sessions via a new `IGameSessionRepository.GetByStatusAsync(GameStatus.Active)` query, archives each one through `GameSession.ArchivePlaythrough(...)` + `StoreAsync`, and then creates the new session — all staged on the same EF DbContext and committed in a single `IGameSessionUnitOfWork.CommitAsync` transaction. This guarantees that after any `POST /api/games` call, at most one `Active` session exists in the persisted store, regardless of client state. The explicit `POST /api/games/{id}/archive` Start Over endpoint archives the named session directly; the `StartNewGameHandler` invariant enforcement is the backstop that catches any other Active sessions (e.g. from a stale client, a second browser, or a direct API call). Prologue copy lives in `WildBunch.GameContent` as a new `PrologueContent` content source with flavour variants (code-backed per ADR-0012). The `{trueCulpritMainIdentifier}` surfaced in the prologue is the same player-visible descriptor produced by `SaloonPersonOfInterestDescriptor.Describe(...)` — the prologue does not expose hidden culprit internals. Starting town selection uses a new read endpoint that returns playable map towns (excluding the unnamed incident town), and the existing `StartGameRequest`/`StartNewGameCommand`/`SeededNewGameFactory`/`GameSession.StartNew` chain is extended with an optional player-chosen `StartingTownId` that overrides the seed-derived starting town.

**Tech Stack:** C#/.NET 10, ASP.NET Core Minimal APIs, EF Core, xUnit, React 18, TanStack Query, styled-components, Vitest, TanStack Router.

## Global Constraints

- `GameSession` is the live-play aggregate root; all gameplay mutation flows through it. Archive is a gameplay-lifecycle mutation on `GameSession`.
- Typed domain events are plain sealed records implementing `IDomainEvent`; `Apply` is the single mutation path (ADR-0028).
- Backend remains authoritative for gameplay state; React renders server state instead of inventing it.
- Hidden culprit truth remains internal (ADR-0007). The prologue surfaces only the player-visible `{trueCulpritMainIdentifier}` descriptor, never `TrueCulpritId`, `isTrueCulprit`, or internal suspect ids.
- The culprit is always a gang member; this issue does not touch culprit/seed-eligibility logic.
- Do not delete existing sessions when starting over. Archive = mark + persist metadata, not delete.
- Do not implement archived-games browser or unarchive/restore UI in this issue.
- Do not bundle saloon/person-of-interest/town-presence fixes into this issue.
- Preserve existing gameplay state flows unless directly necessary for start/reset lifecycle.
- Prologue copy variants are flavour-only; all variants preserve the same starting facts. Do not add a far-future alternate-starting-truth abstraction.
- Difficulty/entropy settings beyond preserving existing behavior are out of scope (ADR-0023).
- No dev overlay reset control as the primary path; Start Over is a normal player control.
- No lawman start generation yet; leave a narrow later seam for map-topology selection.
- Worker environment uses PowerShell; do not use `&&` for command chaining.
- Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent validation.
- UUID seed codec: adding a player-chosen starting town does NOT add a new codec field. The seed still encodes the world (towns, trails, culprit, loadout, etc.); the player's town choice is a runtime override passed through the command/factory chain, not a new encoded starting-world field. The seed-derived starting town becomes the default when the player does not pick one.
- One-active-playthrough invariant is enforced at the application/persistence level (strong route): `StartNewGameHandler` archives any existing `Active` sessions in the same UoW transaction before creating a new one. This is not deferred to a future slice. The invariant must be provable against persisted state, not only against browser-local storage.

---

## Preflight Answers (source-grounded)

### Q1: Where does the frontend currently decide whether to show a start screen or active game?

`src/WildBunch.Web/src/flow/GameFlowRouter.tsx:10-31` switches on `useGamePhase().phase`. `src/WildBunch.Web/src/hooks/useGamePhase.ts:28-68` derives phase from `useGameSession().session`: when `session` is null → `"pre-session"` → renders `PreSessionSurface` (`src/WildBunch.Web/src/flow/PreSessionSurface.tsx:4-27`), which renders `StartGamePanel`. When a session exists, phase is `"in-town"` / `"on-trail"` / `"arrival"`.

The active session id is tracked in `localStorage` key `wild-bunch.current-game-id` (`src/WildBunch.Web/src/hooks/useCurrentGameSession.ts:28`) and read by `useCurrentGameSession` (`:61-74`). `sessionQuery` (`:76-81`) fetches `GET /api/games/{id}` when a stored id exists.

### Q2: Which API route/handler currently creates a game session?

`POST /api/games` at `src/WildBunch.Api/Games/GameSessionEndpoints.cs:13-17`, handler `CreateGameAsync` (`:27-42`) calls `StartNewGameHandler.HandleAsync(...)` (`src/WildBunch.Application/Games/Commands/StartNewGameHandler.cs:11-47`). Request DTO: `StartGameRequest(string PlayerName, TravelDifficulty TravelDifficulty = Normal, string? SeedCode = null, AdventureRandomnessPolicy Entropy = Standard)` at `src/WildBunch.Api/Games/Requests/StartGameRequest.cs:5-9`. Command: `StartNewGameCommand` at `src/WildBunch.Application/Games/Commands/StartNewGameCommand.cs:5-9` (same shape). Handler calls `_newGameFactory.Create(...)` then stores via `ExecuteNewSessionAsync` and projects HUD/Diary.

### Q3: Does the current start route already accept player name and starting town?

**Player name: yes** — `StartGameRequest.PlayerName` is required. **Starting town: no** — there is no `StartingTownId` field. The starting town is currently seed-fixed: `SeededNewGameFactory.Create` (`src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs:25-47`) builds a `GameSetupPackage` via `GameSetupPackageBuilder.Build(descriptor)` (`src/WildBunch.GameContent/NewGame/GameSetupPackageBuilder.cs:10-29`), which calls `SeedWorldBuilder.CreateWorld(plan)` (`src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs:18-29`), whose `PickStartingTown` (`:31-46`) deterministically picks a town from the seed's `StartingTownSelectionKey`.

**Smallest safe extension point:** The domain aggregate already supports a player-chosen starting town. `GameSession.StartNew(...)` at `src/WildBunch.Domain/Game/GameSession.cs:700-709` accepts `TownId? startingTownId` and falls back to `world.Towns.First().Id` when null (`:715`). The extension path is: `StartGameRequest` → `StartNewGameCommand` → `INewGameFactory.Create` → `SeededNewGameFactory.Create` → pass the player's `TownId?` through to `GameSession.StartNew`, overriding the seed-derived `setupPackage.StartingTownId`. The seed codec is NOT extended — the player's town choice is a runtime override, not a new encoded field.

### Q4: Are starting towns currently sourced from the backend/world model, static frontend state, or both?

Backend only. Towns come from `SeedWorldCatalog` (`src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs:70-80`) which defines 8 towns (pinecross, redmesa, holloway, sagewell, dryfork, emberfall, hardpan, openpass). The frontend has no independent town list — it only sees towns inside the `GameSessionDto.World.Towns` array after a session is created (`src/WildBunch.Web/src/api/types.ts:270-274`). There is no backend route that returns playable towns independently of a session. A new read endpoint is needed for the starting-town selection screen.

### Q5: Is any existing difficulty/entropy setup coupled to start-game behavior that must be preserved?

Yes — `TravelDifficulty` and `AdventureRandomnessPolicy` (entropy) are accepted by `StartGameRequest`, forwarded through `StartNewGameCommand`, and flow into `SeededNewGameFactory.Create` and `GameSession.StartNew`. They are also encoded in the UUID seed (`GameSetupSeedCodec.ResolveAdventureRandomnessPolicy`, `ResolveDifficulty`). This issue preserves existing difficulty/entropy behavior and does not add new settings (ADR-0023). The start flow continues to pass the existing `TravelDifficulty` and `Entropy` through unchanged.

### Q6: Which components own the current start screen and game shell/HUD?

- Start screen: `PreSessionSurface` (`src/WildBunch.Web/src/flow/PreSessionSurface.tsx`) → `StartGamePanel` (`src/WildBunch.Web/src/components/StartGamePanel.tsx`) → `StartGameOptionsForm` (`src/WildBunch.Web/src/components/StartGameOptionsForm.tsx`).
- Game shell/HUD: `AppShell` (`src/WildBunch.Web/src/shell/AppShell.tsx`) renders `Hud` (`src/WildBunch.Web/src/shell/Hud.tsx`) + `GlobalOverlays` (`src/WildBunch.Web/src/flow/GlobalOverlays.tsx`) + `DevOverlay` (`src/WildBunch.Web/src/dev/DevOverlay.tsx`). Routing via TanStack Router (`Outlet`).

### Q7: What frontend state should be draft-only before final game creation?

Name, story acknowledgement, and selected starting town are draft-only frontend setup state until the player completes the flow and `POST /api/games` is called. The backend creates the new `GameSession` only at the end of the flow. This matches the copy doc: "The first implementation can hold name, story acknowledgement, and selected starting town as frontend draft setup state until final game creation."

### Q8: At what point should the backend create the new GameSession?

After starting-town selection (the final step). The flow is: name entry → story-so-far acknowledgement → starting-town selection → `POST /api/games` with `PlayerName` + `StartingTownId` (+ existing `TravelDifficulty`/`SeedCode`/`Entropy`). This keeps a single backend creation call at the end and avoids a partial setup-session seam (none exists today).

### Q9: How will the story-so-far avoid revealing hidden culprit truth?

The prologue surfaces only the player-visible `{trueCulpritMainIdentifier}` descriptor — the same string produced by `SaloonPersonOfInterestDescriptor.Describe(suspect, caseFile)` (`src/WildBunch.Domain/Cases/SaloonPersonOfInterestDescriptor.cs:7-35`), which returns a public-safe phrase like "a stranger with a scar on his left cheek" derived from warrant known features, suspect profile identifying facts, or trait tags. It never exposes `TrueCulpritId`, `isTrueCulprit`, or internal suspect ids. The prologue content source will hold the copy templates with a `{trueCulpritMainIdentifier}` placeholder; a new application query/projection will resolve the true culprit's player-visible descriptor from the seeded `CaseFile` and substitute it. The descriptor resolution must use the same `SaloonPersonOfInterestDescriptor.Describe` path used elsewhere for clues/suspects so there is one canonical formatter (architecture-hygiene: "Prefer one canonical algorithm or formatter over duplicate versions that can drift").

### Q10: Where does GameContent or the closest content source live, and how should flavour variants be represented there?

`src/WildBunch.GameContent/` is the code-backed content project (ADR-0012: "GameContent in Code Now, DB-Backed Content Later"). Existing flavour content pattern: `src/WildBunch.GameContent/Travel/TravelDiaryFlavours.Content.cs` uses `Entry("diary.day-opening.open-range-1", category, text, tags, terrain)` records with unique keys, categories, tags, and context constraints. Prologue content will follow the same code-backed catalog pattern in a new `src/WildBunch.GameContent/Prologue/` folder with a `PrologueContent` static class exposing the three body-copy variants from the copy doc, keyed by variant id, plus the name-entry, story-so-far heading, and starting-town copy.

### Q11: How will the flow return to the same setup screens after Start Over?

Start Over clears the active session id from `localStorage` and React Query cache, which makes `useGamePhase` return `"pre-session"` and renders `PreSessionSurface` again. The start flow is the same screens used for a first start. No separate "post-archive" route is needed.

### Q12: How does persistence currently identify the active game session?

There is no backend "active session" concept. Sessions are stored by `GameSessionId` (Guid) in the `GameSessions` table (`src/WildBunch.Persistence/GameSessions/GameSessionEntity.cs:3-26`) and looked up by explicit id (`EfGameSessionRepository.LoadStoreAsync` at `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs:166-184`). The frontend tracks the active id in `localStorage`. Multiple sessions can coexist in the table; the "active" one is whichever id the frontend holds.

### Q13: Are multiple sessions already stored? How is the active one selected?

Yes — every `POST /api/games` creates a new `GameSessionEntity` row. The active one is selected client-side by the id in `localStorage` (`wild-bunch.current-game-id`). There is no server-side active/archived distinction today.

### Q14: What is the smallest repo-aligned way to archive the current active session without deleting it?

Add an `Archived` status to the session lifecycle and a `PlaythroughArchived` domain event. Concretely:
- Extend `GameStatus` (`src/WildBunch.Domain/Game/GameStatus.cs:3-8`) with `Archived = 3`.
- Add `GameSession.ArchivePlaythrough(string reason, ...)` command method that validates the session is currently `Active`/`Completed`/`Failed` (not already archived), emits a `PlaythroughArchived` event, and `Apply(PlaythroughArchived)` sets `Status = Archived` and records archive metadata.
- `PlaythroughArchived` event carries: `ArchivedAtUtc`, `ArchiveReason` (e.g. `"player-start-over"`), and snapshot-derived summary fields the copy doc asks for (player name, last town id/name, day/turn, status-before-archive). These are derived from current aggregate state at archive time, not new normalized tables.
- The existing `GameSessionEntity.Status` string column (`src/WildBunch.Persistence/GameSessions/GameSessionEntity.cs:11`) already stores `Status.ToString()`, so `Archived` persists with no schema migration. Archive metadata that is not already on the envelope (e.g. `ArchivedAtUtc`, `ArchiveReason`) is carried by the `PlaythroughArchived` event in the event stream (replay/audit-friendly per ADR-0028) and, if needed for future archive listing, can be projected later. No new tables in this issue (persistence posture: "Do not normalize runtime session state into many DB tables unless explicitly directed").

### Q15: What metadata should be persisted now for future unarchive/restore?

Per the copy doc: session id, player name, created time if available, archived time, archive reason/source, last known town, day/turn, and summary/status. Of these, session id, player name, status, and travel difficulty are already on `GameSessionEntity`. `CreatedAtUtc` is already on the envelope (`GameSessionEntity.cs:7`). Last known town and day/turn are on the snapshot (Player.CurrentTownId, Clock.Day/Turn). The `PlaythroughArchived` event will carry `ArchivedAtUtc`, `ArchiveReason`, `PlayerName`, `LastTownId`, `LastTownName`, `Day`, `Turn`, `StatusBeforeArchive` — enough to reconstruct an archive summary from the event stream later without new tables.

### Q16: How will the implementation enforce one active playthrough at a time?

**Strong route (chosen):** The invariant is enforced at the application/persistence level, not only in browser-local state. `StartNewGameHandler` is extended so that before creating a new session it:
1. Queries all existing `Active` sessions via a new `IGameSessionRepository.GetByStatusAsync(GameStatus.Active)`.
2. For each Active session, calls `session.ArchivePlaythrough("superseded-by-new-playthrough")` and `StoreAsync` — staging the archive on the same EF DbContext.
3. Creates the new session via `_newGameFactory.Create(...)` and `StoreAsync` — also staged on the same DbContext.
4. Commits everything in a single `IGameSessionUnitOfWork.CommitAsync` transaction.

This guarantees that after any `POST /api/games` call, at most one `Active` session exists in the persisted store, regardless of how many clients or direct API calls created sessions. The explicit `POST /api/games/{id}/archive` Start Over endpoint archives the named session directly; the `StartNewGameHandler` invariant enforcement is the backstop that catches any other Active sessions (e.g. from a stale client, a second browser, or a direct API call that bypassed the frontend flow).

This is feasible without unrelated architecture because: the UoW already wraps a single EF DbContext transaction (`EfGameSessionUnitOfWork.CommitAsync` at `src/WildBunch.Persistence/GameSessions/EfGameSessionUnitOfWork.cs:7-33` calls `BeginTransactionAsync` → `SaveChangesAsync` → `CommitAsync`); `EfGameSessionRepository.StoreAsync` stages changes on the DbContext without calling `SaveChangesAsync` itself (line 142: "NO SaveChangesAsync here — the UoW commits"); so multiple `StoreAsync` calls for different aggregates can be staged before one `CommitAsync`. The only new infrastructure is a `GetByStatusAsync` query method on `IGameSessionRepository` (a simple EF `Where(entity => entity.Status == status.ToString())` query) and the `ArchivePlaythrough` domain method (already in this plan). No new tables, no session-account architecture, no broad refactor.

### Q17: What happens if Start Over is confirmed while no active session exists?

The confirmation dialog is only reachable from the Game Settings surface, which is only reachable when a session is active. If the stored id is stale (session deleted/missing), the settings surface should degrade gracefully: the Start Over action is disabled or no-ops with a notice. The backend `ArchivePlaythroughHandler` returns 404 if the session does not exist, and the frontend treats that as "already no active playthrough" and returns to the start flow.

### Q18: Which command expresses player intent for Start Over / Reset Game?

A new `ArchivePlaythroughCommand(GameSessionId SessionId, string ArchiveReason)` handled by `ArchivePlaythroughHandler` (Application layer). This is a player command, not a dev command — it lives under `/api/games/{id}/archive` (player route), not `/api/dev/`.

### Q19: Which aggregate or application coordinator owns archive-old + create-new?

`GameSession.ArchivePlaythrough(...)` owns the archive mutation on the aggregate. The new session is created by the existing `StartNewGameHandler` when the player completes the start flow. `StartNewGameHandler` also owns the one-active-playthrough invariant: before creating the new session, it loads and archives any existing `Active` sessions in the same UoW transaction (see Q16). The explicit `ArchivePlaythroughHandler` (for the `POST /api/games/{id}/archive` Start Over endpoint) archives a named session directly. The Application layer does not bypass domain/persistence truth: both archive and create go through the aggregate + repository/UoW. The two operations (archive-old, create-new) are coordinated by `StartNewGameHandler` in a single transaction, but the player-facing flow still separates them by requiring the player to complete the start flow before `POST /api/games` is called (matching the copy doc: "A fresh active playthrough should only be created once the player completes name entry, story acknowledgement, and starting town selection").

### Q20: What domain event(s) or persisted facts record that a playthrough was archived?

`PlaythroughArchived` (new typed domain event, `IDomainEvent`). `Apply(PlaythroughArchived)` sets `Status = Archived` and records archive facts on the aggregate. The event is appended to the `StoredEvents` stream by `EfGameSessionRepository.StoreAsync` (`src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs:57-90`) and is replay-friendly.

### Q21: How will the implementation avoid mutating an old session into a new one?

Archive and create are separate aggregate instances with separate `GameSessionId`s. Archive mutates the old session's status; create produces a brand-new `GameSession` via `GameSession.StartNew` with a new id. The old session's event stream and snapshot remain intact and distinguishable by status.

### Q22: How will archived session state remain replay/audit-friendly and available for a future unarchive feature?

The `PlaythroughArchived` event stays in the old session's event stream. The snapshot is updated to `Status = Archived`. The session remains loadable by id via `EfGameSessionRepository.GetByIdAsync`. A future archive browser can query `GameSessions` by `Status = "Archived"` and a future unarchive can replay/restore. No data is deleted.

### Q23: What read model/query needs to distinguish active versus archived sessions?

The frontend's active-session lookup (`GET /api/games/{id}`) does not filter by status — it returns whatever session exists. After Start Over, the frontend clears the stored id, so it stops requesting the archived session. A future archive-listing query (out of scope) would filter `Status = "Archived"`. No existing read model needs to change in this issue.

### Q24: Where should Game Settings live in the normal player UI so it is clearly not dev overlay tooling?

Per `src/WildBunch.Web/AGENTS.md`: "Durable play surfaces belong in the HUD/shell or another player-facing route, not in `DebugCockpitRoute`." Game Settings will be a new overlay reachable from the HUD actions bar in `Hud.tsx` (alongside the existing Journal button), using the existing `CockpitOverlayFrame` overlay pattern (`src/WildBunch.Web/src/components/CockpitOverlayFrame.tsx`) and wired through `GlobalOverlays` (`src/WildBunch.Web/src/flow/GlobalOverlays.tsx`). It is explicitly NOT added to `DevOverlay` / `DevPanelRegistry`.

### Q25: What confirmation dialog component/pattern already exists?

There is no dedicated confirm/cancel dialog component today. `CockpitOverlayFrame` (`src/WildBunch.Web/src/components/CockpitOverlayFrame.tsx:1-78`) is a general overlay frame with `open`, `eyebrow`, `title`, `description`, `onClose`, and `children` — it has no confirm/cancel action slots. A new small `ConfirmDialog` component will be added (built on `CockpitOverlayFrame` or a sibling backdrop pattern) with `title`, `body`, `cancelLabel`, `confirmLabel`, `onCancel`, `onConfirm` props. It will be reusable for future confirmations.

### Q26: What copy will make clear that the current playthrough is archived, not deleted?

The copy doc provides the exact copy: confirmation title "Start over?", body "This will archive your current playthrough and return you to the beginning. Your old game will not be deleted. It will be kept for posterity, and later you may be able to restore archived playthroughs. For now, only one playthrough can be active at a time.", cancel "Cancel", confirm "Archive and Start Over", success "Your old playthrough has been archived. Start a new one when you are ready."

### Q27: How does Cancel prove no state mutation happened?

Cancel only closes the dialog (local UI state). It does not call any API. The confirmation dialog's `onCancel` is a pure frontend callback. A test asserts that no mutation API is called when Cancel is clicked.

### Q28: How will the settings surface be structured so later player settings can be added without redesign?

Game Settings is an overlay with a "Playthrough" section containing the Start Over action. The overlay is structured as a small settings shell with section headings so future settings sections (e.g. display, audio) can be added as new sections without redesigning the entry point. The Start Over section is the only section in this issue.

### Q29: Where is the player-visible culprit identifier surfaced today?

`SaloonPersonOfInterestDescriptor.Describe(Suspect, CaseFile)` (`src/WildBunch.Domain/Cases/SaloonPersonOfInterestDescriptor.cs:7-35`) is the canonical player-visible descriptor formatter. It returns "a stranger with {feature}" derived from warrant known features (`caseFile.KnownWarrants`), then suspect profile identifying facts (`suspect.Profile.IdentifyingFacts.FirstOrDefault().Description`), then trait tags. This is the same surface used for saloon persons-of-interest and wanted posters. The prologue will reuse this exact formatter to produce `{trueCulpritMainIdentifier}` for the true culprit, so there is one canonical descriptor and no hidden-truth leak.

### Q30: List the application-layer command/handler pattern.

Raw handlers with explicit DI registration (no Mediator/Wolverine). Commands are sealed records. Session-mutation handlers extend `GameSessionCommandHandler` (`src/WildBunch.Application/Games/Execution/GameSessionCommandHandler.cs:19-99`) which provides `ExecuteWithRetryAsync` (load → command → store → commit with optimistic concurrency retry) and `ExecuteNewSessionAsync` (create path, used by `StartNewGameHandler`). Handlers are registered in `src/WildBunch.Api/DependencyInjection.cs:37-56`. Endpoints inject handlers directly and call `HandleAsync`.

### Q31: List the domain event pattern.

Typed domain events are sealed records implementing `IDomainEvent` (`src/WildBunch.Domain/Events/IDomainEvent.cs:3-8`). Aggregate command methods build an event, construct a placeholder session, call `Apply(event)` (single mutation path), and add the event to `_uncommittedEvents`. Example: `GameStarted` (`src/WildBunch.Domain/Events/GameStarted.cs:10-21`) emitted by `GameSession.StartNew` (`src/WildBunch.Domain/Game/GameSession.cs:722-734`), applied at `:783-798`. Events are appended to `StoredEvents` by `EfGameSessionRepository.StoreAsync` with optimistic concurrency on `StreamVersion` (`:57-90`). Projections (Journal, HUD, Diary, CaseFileView) derive read models from the event stream (ADR-0028).

---

## Implementation Plan

### Phase 1 — Backend: archive lifecycle on GameSession

#### Task 1.1 — Add `Archived` to `GameStatus` and a `PlaythroughArchived` domain event

- [ ] Add `Archived = 3` to `GameStatus` enum at `src/WildBunch.Domain/Game/GameStatus.cs`.
- [ ] Create `src/WildBunch.Domain/Events/PlaythroughArchived.cs` as a sealed record implementing `IDomainEvent` with: `DateTime ArchivedAtUtc`, `string ArchiveReason`, `string PlayerName`, `TownId? LastTownId`, `string? LastTownName`, `int Day`, `string Turn`, `GameStatus StatusBeforeArchive`.
- [ ] Verify build: `dotnet build src/WildBunch.Domain`.

#### Task 1.2 — Add `GameSession.ArchivePlaythrough(...)` command + `Apply(PlaythroughArchived)`

- [ ] Add `ArchivePlaythrough(string archiveReason, DateTime? archivedAtUtc = null)` instance method on `GameSession` (`src/WildBunch.Domain/Game/GameSession.cs`) that: throws if `Status == Archived` (already archived), builds a `PlaythroughArchived` event from current state (Player.Name, CurrentTown id/name, Clock.Day, Clock.Turn, Status), calls `Apply(e)`, and adds `e` to `_uncommittedEvents`.
- [ ] Add `private void Apply(PlaythroughArchived e)` that sets `Status = GameStatus.Archived` and increments `_version`.
- [ ] Unit test in `tests/WildBunch.Domain.Tests/`: `GameSessionArchiveTests.cs` — assert archive sets status, emits event, throws on double-archive, event carries correct derived metadata.
- [ ] Verify: `dotnet test tests/WildBunch.Domain.Tests`.

#### Task 1.3 — Add `ArchivePlaythroughCommand` + `ArchivePlaythroughHandler` (Application)

- [ ] Create `src/WildBunch.Application/Games/Commands/ArchivePlaythroughCommand.cs` (sealed record with `GameSessionId SessionId`, `string ArchiveReason`).
- [ ] Create `src/WildBunch.Application/Games/Commands/ArchivePlaythroughHandler.cs` extending `GameSessionCommandHandler`, using `ExecuteWithRetryAsync` to load the session, call `session.ArchivePlaythrough(command.ArchiveReason)`, and return a small `ArchivePlaythroughResultDto` (session id, archived status, player name, last town, day/turn).
- [ ] Register `ArchivePlaythroughHandler` in `src/WildBunch.Api/DependencyInjection.cs`.
- [ ] Application test in `tests/WildBunch.Application.Tests/`: `ArchivePlaythroughHandlerTests.cs` — assert handler loads, archives, stores, commits, returns dto; assert 404 mapping when session missing (via `GameSessionNotFoundException`).
- [ ] Verify: `dotnet test tests/WildBunch.Application.Tests`.

#### Task 1.4 — Add `POST /api/games/{id}/archive` player endpoint

- [ ] In `src/WildBunch.Api/Games/GameSessionEndpoints.cs`, add `games.MapPost("{id:guid}/archive", ArchiveGameAsync)` calling `ArchivePlaythroughHandler` with `ArchivePlaythroughCommand(new GameSessionId(id), "player-start-over")`. Returns 200 with the result dto, or 404 on `GameSessionNotFoundException`.
- [ ] Integration test in `tests/WildBunch.Integration.Tests/`: `GameApiArchiveTests.cs` — create a session, archive it, assert status becomes Archived, assert session still loadable by id, assert archived session is not deleted.
- [ ] Verify: `.\scripts\postgres-dev.ps1 ensure` then `dotnet test tests/WildBunch.Integration.Tests` (PostgreSQL-backed).

#### Task 1.5 — Add `GetByStatusAsync` to `IGameSessionRepository` + `EfGameSessionRepository`

- [ ] Add `Task<IReadOnlyList<GameSession>> GetByStatusAsync(GameStatus status, CancellationToken cancellationToken = default)` to `IGameSessionRepository` (`src/WildBunch.Application/Abstractions/IGameSessionRepository.cs`).
- [ ] Implement in `EfGameSessionRepository` (`src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`) as a query on `_dbContext.GameSessions.Where(entity => entity.Status == status.ToString())` that loads each matching session via the existing `LoadStoreAsync` + `ToAggregate` path. This is a read + rehydrate query, not a projection — it returns full `GameSession` aggregates so the handler can call `ArchivePlaythrough` on them.
- [ ] This is the minimal infrastructure for the one-active-playthrough invariant (Q16). No new tables, no session-account architecture.
- [ ] Verify: `dotnet build`.

#### Task 1.6 — Extend `StartNewGameHandler` to enforce one-active-playthrough invariant

- [ ] Modify `StartNewGameHandler.HandleAsync` (`src/WildBunch.Application/Games/Commands/StartNewGameHandler.cs`) so that before creating the new session it:
  1. Calls `GameSessionRepository.GetByStatusAsync(GameStatus.Active, cancellationToken)` to load all existing Active sessions.
  2. For each Active session, calls `session.ArchivePlaythrough("superseded-by-new-playthrough")` and `GameSessionRepository.StoreAsync(session, correlationId, cancellationToken)` — staging the archive on the same DbContext.
  3. Creates the new session via `_newGameFactory.Create(...)` and stages it via `StoreAsync`.
  4. Commits everything in a single `GameSessionUnitOfWork.CommitAsync`.
- [ ] This requires restructuring `ExecuteNewSessionAsync` (or inlining its logic in `StartNewGameHandler`) so that the archive-old + create-new steps share one correlation id and one commit. The base class `ExecuteNewSessionAsync` (`src/WildBunch.Application/Games/Execution/GameSessionCommandHandler.cs:81-98`) currently does `createAsync → StoreAsync → CommitAsync`; the extended flow does `archive-old → StoreAsync(each) → create-new → StoreAsync → CommitAsync`. Keep the base class method for other new-session paths if any; inline or add a new base method for the archive-then-create path.
- [ ] Application test in `tests/WildBunch.Application.Tests/`: `StartNewGameOneActivePlaythroughTests.cs` —
  - Assert: when an Active session exists, calling `StartNewGameHandler` archives it (emits `PlaythroughArchived`, status → Archived) and creates the new Active session, all in one UoW commit.
  - Assert: when multiple Active sessions exist (e.g. from direct API calls), all are archived and only the new one remains Active.
  - Assert: when no Active session exists, `StartNewGameHandler` creates the new session with no archive events.
  - Assert: `CommitAsync` is called exactly once (single transaction).
  - Assert: `StoreAsync` is called once per archived session + once for the new session.
- [ ] Integration test in `tests/WildBunch.Integration.Tests/`: `OneActivePlaythroughInvariantTests.cs` —
  - Assert against persisted PostgreSQL state: create session A via `POST /api/games`, create session B via `POST /api/games`, query `GameSessions` table and assert exactly one row has `Status = "Active"` (session B) and session A has `Status = "Archived"`.
  - Assert: session A is still loadable by id with `Archived` status (not deleted).
  - Assert: the `PlaythroughArchived` event is in session A's event stream with `ArchiveReason = "superseded-by-new-playthrough"`.
- [ ] Verify: `.\scripts\postgres-dev.ps1 ensure` then `dotnet test tests/WildBunch.Application.Tests tests/WildBunch.Integration.Tests`.

### Phase 2 — Backend: starting-town selection + prologue content

#### Task 2.1 — Add `StartingTownId` to start-game chain (no codec change)

- [ ] Add `string? StartingTownId` to `StartGameRequest` (`src/WildBunch.Api/Games/Requests/StartGameRequest.cs`).
- [ ] Add `TownId? StartingTownId` to `StartNewGameCommand` (`src/WildBunch.Application/Games/Commands/StartNewGameCommand.cs`).
- [ ] Add `TownId? startingTownId = null` to `INewGameFactory.Create` (`src/WildBunch.GameContent/Abstractions/INewGameFactory.cs`).
- [ ] Update `SeededNewGameFactory.Create` (`src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs`) to accept `TownId? startingTownId` and pass it through to `GameSession.StartNew` instead of `setupPackage.StartingTownId` when the player provides one. When null, keep `setupPackage.StartingTownId` (seed-derived default).
- [ ] Update `StartNewGameHandler.HandleAsync` to forward `command.StartingTownId` (the invariant enforcement from Task 1.6 is already in place; this just adds the town forwarding).
- [ ] Update `GameSessionEndpoints.CreateGameAsync` to forward `validatedRequest.StartingTownId`.
- [ ] Update existing `StartNewGameHandlerTests` and `SeededNewGameFactoryTests` to cover: player-chosen town overrides seed town; null town falls back to seed town; invalid town id throws.
- [ ] Verify: `dotnet build` and `dotnet test tests/WildBunch.Application.Tests tests/WildBunch.GameContent.Tests`.

#### Task 2.2 — Add `GET /api/games/starting-towns` read endpoint

- [ ] Create a read query + handler returning playable starting towns (id, name, services) from `SeedWorldCatalog` for the canonical world variant, excluding the unnamed incident town (which is not in the catalog — all catalog towns are eligible candidates, matching `SeedWorldBuilder.PickStartingTown`'s candidate filter of towns with Supplies or NoticeBoard services). The endpoint returns the same candidate set the seed uses, so the player picks from real playable towns.
- [ ] Add `GET /api/games/starting-towns` to `GameSessionEndpoints.cs` (or a new `StartingTownEndpoints.cs`) returning `IReadOnlyList<StartingTownDto>`.
- [ ] Register handler in DI.
- [ ] Application test: `GetStartingTownsHandlerTests.cs` — assert returns expected towns, all have Supplies or NoticeBoard, incident town is not in list (it isn't in the catalog).
- [ ] Verify: `dotnet test tests/WildBunch.Application.Tests`.

#### Task 2.3 — Add `PrologueContent` content source in `WildBunch.GameContent`

- [ ] Create `src/WildBunch.GameContent/Prologue/PrologueContent.cs` with the three body-copy variants from the copy doc, the name-entry heading/helper/validation copy, the story-so-far heading and primary action, and the starting-town heading/body/empty-state/primary-action/validation copy. Use the existing `Entry(...)`-style code-backed catalog pattern (ADR-0012).
- [ ] Each variant preserves the same facts (dying man, Wild Bunch culprit, `{trueCulpritMainIdentifier}`, sheriff accusation, flight, fugitive, must take in true killer). Variants differ only in flavour wording.
- [ ] Unit test: `PrologueContentTests.cs` — assert all variants contain the required fact phrases (or their semantic equivalents), assert `{trueCulpritMainIdentifier}` placeholder present in each variant, assert variant ids are unique.
- [ ] Verify: `dotnet test tests/WildBunch.GameContent.Tests`.

#### Task 2.4 — Add prologue read endpoint resolving `{trueCulpritMainIdentifier}`

- [ ] Add `GET /api/games/prologue?seedCode={seed}` (or a setup-scoped route) that: resolves the `StartingWorldDescriptor` from the seed, builds the `CaseFile` via `SeedCaseBuilder` (the same path `GameSetupPackageBuilder` uses), finds the true culprit suspect, calls `SaloonPersonOfInterestDescriptor.Describe(trueCulprit, caseFile)` to get the player-visible descriptor, and returns the prologue copy with `{trueCulpritMainIdentifier}` substituted into the chosen (or a random/default) variant.
- [ ] The endpoint must NOT return `TrueCulpritId`, `isTrueCulprit`, or internal suspect ids — only the substituted public descriptor string. Add a hidden-truth guard test.
- [ ] Application test: `PrologueHandlerTests.cs` — assert descriptor is substituted, assert hidden fields absent, assert all three variants are available, assert copy matches `PrologueContent`.
- [ ] Verify: `dotnet test tests/WildBunch.Application.Tests`.

### Phase 3 — Frontend: start flow with name → story → town → game

#### Task 3.1 — Add start flow step state and routing in `PreSessionSurface`

- [ ] Introduce a small start-flow step state (`"name" | "story" | "town" | "creating"`) in `PreSessionSurface` (or a new `useStartFlow` hook in `src/WildBunch.Web/src/hooks/useStartFlow.ts`) holding draft `playerName`, `storyAcknowledged`, `selectedTownId`, plus existing `travelDifficulty`/`seedState`/`entropy` from `useStartGameSeed`.
- [ ] Render the current step's component; advance on continue; allow back navigation.
- [ ] The final step calls the existing `startNewGame` mutation with `playerName` + `startingTownId` + existing fields.
- [ ] Vitest test: `StartFlow.test.tsx` — assert step progression name → story → town → create; assert back navigation; assert draft state preserved across steps; assert `POST /api/games` only called at the final step.

#### Task 3.2 — Name entry step component

- [ ] Create `src/WildBunch.Web/src/components/start-flow/NameEntryStep.tsx` using the copy doc's heading ("Howdy, pard'ner. What name d'you go by?"), helper, validation ("Tell me what name you go by before we ride on."), and primary action ("Continue"). Reuse the shared styled primitives from `src/WildBunch.Web/src/components/ui/sharedStyled.ts` (`FlowSurface`, `Eyebrow`, `Button`, etc.) introduced by BUNCH-95.
- [ ] Vitest test: `NameEntryStep.test.tsx` — assert empty name shows validation, valid name enables Continue, Continue advances step.

#### Task 3.3 — Story-so-far step component

- [ ] Create `src/WildBunch.Web/src/components/start-flow/StorySoFarStep.tsx` that fetches prologue copy from the new backend endpoint (via TanStack Query), picks a variant, renders the substituted body with the `{trueCulpritMainIdentifier}` filled in, and shows the primary action ("I understand. Keep riding."). Acknowledgement advances to town selection.
- [ ] Vitest test: `StorySoFarStep.test.tsx` — assert copy rendered, assert `{trueCulpritMainIdentifier}` substituted (no literal placeholder), assert hidden fields absent, assert Continue advances.

#### Task 3.4 — Starting town selection step component

- [ ] Create `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx` that fetches `GET /api/games/starting-towns`, renders the town list with the copy doc's heading/body/empty-state, and a per-town primary action ("Start in {townName}"). No selection validation ("Pick a town before you ride.") if the player tries to continue without selecting.
- [ ] Selecting a town triggers the final `startNewGame` call with `startingTownId`.
- [ ] Vitest test: `StartingTownStep.test.tsx` — assert towns rendered from backend, assert selecting a town calls `startNewGame` with the town id, assert empty/loading state copy.

#### Task 3.5 — Wire start flow into `PreSessionSurface` and `useCurrentGameSession`

- [ ] Replace the current single-panel `StartGamePanel` render in `PreSessionSurface` with the new stepped start flow. Keep the existing `useStartGameSeed` hook for difficulty/entropy/seed draft state, carried through the steps.
- [ ] Ensure `startNewGame` mutation in `useCurrentGameSession` forwards `startingTownId` (update `StartGameRequest` type in `src/WildBunch.Web/src/api/types.ts` and `createGame` payload).
- [ ] Update `src/WildBunch.Web/src/api/wildBunchApi.ts` with `getStartingTowns()` and `getPrologue(seedCode)` client functions.
- [ ] Vitest test: update `StartGamePanel.test.tsx` (or replace with start-flow tests) to cover the new flow; ensure existing seed/difficulty assertions still hold.

### Phase 4 — Frontend: Game Settings + Start Over confirmation

#### Task 4.1 — Add `ConfirmDialog` component

- [ ] Create `src/WildBunch.Web/src/components/ConfirmDialog.tsx` with props `open`, `title`, `body`, `cancelLabel`, `confirmLabel`, `onCancel`, `onConfirm`, `busy`. Built on the existing backdrop/overlay pattern (reuse `CockpitOverlayFrame` or a sibling styled backdrop using shared primitives from `ui/sharedStyled`). Accessible: focus trap, Escape cancels, aria roles.
- [ ] Vitest test: `ConfirmDialog.test.tsx` — assert Cancel calls `onCancel` and not `onConfirm`; assert Confirm calls `onConfirm`; assert Escape cancels.

#### Task 4.2 — Add Game Settings overlay + Start Over section

- [ ] Add `"game-settings"` to `OverlayKind` in `src/WildBunch.Web/src/flow/GlobalOverlays.tsx` and a new `CockpitOverlayFrame` for "Game Settings" with a "Playthrough" section containing the Start Over action (label "Start Over", helper "Archive this playthrough and begin again from the start.").
- [ ] Add a "Game Settings" button to the HUD actions bar in `Hud.tsx` (alongside Journal), enabled only when a session is active.
- [ ] Start Over opens the `ConfirmDialog` with the copy doc's confirmation copy.
- [ ] Vitest test: `GameSettingsOverlay.test.tsx` — assert overlay opens from HUD, assert Start Over button present, assert opens confirmation dialog.

#### Task 4.3 — Wire Start Over confirm to archive API + return to start flow

- [ ] Add `archiveGame(gameId)` client function in `src/WildBunch.Web/src/api/wildBunchApi.ts` calling `POST /api/games/{id}/archive`.
- [ ] Add an `archivePlaythrough` mutation in `useCurrentGameSession` that: calls `archiveGame`, on success clears `localStorage` `wild-bunch.current-game-id`, clears `storedGameId`, invalidates React Query cache, sets the success notice ("Your old playthrough has been archived. Start a new one when you are ready."), and lets `useGamePhase` fall back to `"pre-session"`.
- [ ] Confirm in the dialog calls the mutation; Cancel only closes.
- [ ] Vitest test: `StartOverConfirmation.test.tsx` — assert Confirm calls `archiveGame` then clears stored id and returns to start flow; assert Cancel does not call `archiveGame` and leaves session state unchanged (no mutation, no localStorage clear).

### Phase 5 — Hidden-truth, regression, and integration guards

#### Task 5.1 — Hidden-truth guard for prologue endpoint

- [ ] Integration/contract test: `PrologueHiddenTruthTests.cs` — assert the prologue response contains no `trueCulpritId`, `isTrueCulprit`, `linkedSuspectIds`, or internal suspect id fields; assert the `{trueCulpritMainIdentifier}` is a public-safe descriptor string.

#### Task 5.2 — One-active-playthrough invariant proof (strong route)

This is the core invariant proof. The tests in Task 1.6 (`StartNewGameOneActivePlaythroughTests.cs` and `OneActivePlaythroughInvariantTests.cs`) are the primary proof. This task is the summary and falsification checklist:

- [ ] Application test (`StartNewGameOneActivePlaythroughTests.cs` from Task 1.6): assert that `StartNewGameHandler` archives all pre-existing Active sessions and creates exactly one new Active session in a single UoW commit, with `CommitAsync` called exactly once.
- [ ] Integration test (`OneActivePlaythroughInvariantTests.cs` from Task 1.6): assert against persisted PostgreSQL state that after two consecutive `POST /api/games` calls, exactly one row has `Status = "Active"` and the other has `Status = "Archived"` with a `PlaythroughArchived` event in its stream.
- [ ] Falsification check: directly insert a second Active session via the API (bypassing the frontend), then call `POST /api/games` again, and assert the invariant still holds (only one Active row after).
- [ ] Falsification check: call `POST /api/games/{id}/archive` on the active session, then call `POST /api/games`, and assert the archived session remains Archived and the new session is Active (no resurrection).
- [ ] Archived sessions remain persisted and distinguishable: assert archived sessions are loadable by id with `Archived` status and their event streams contain `PlaythroughArchived` (covered by `GameApiArchiveTests.cs` from Task 1.4 + `OneActivePlaythroughInvariantTests.cs` from Task 1.6).

#### Task 5.3 — Regression: reset/start-over is not exposed only through dev overlay

- [ ] Test/assertion: confirm Start Over is reachable from the normal player Game Settings overlay and NOT only from `DevOverlay`/`DevPanelRegistry`. (Frontend test in 4.2 covers this; add an explicit assertion that the Game Settings button is in `Hud`, not in `DevOverlay`.)

### Phase 6 — Docs, ADR freshness, validation

#### Task 6.1 — ADR freshness check + update

- [ ] Read `docs/adr/INDEX.md` and every ADR. The ADR log now contains ADR-0001 through ADR-0033 (as of main @ 30c3b58). ADR-0032 (event-sourced dev saloon controls) and ADR-0033 (repo documentation mesh posture) were added by BUNCH-90/BUNCH-96. This issue introduces `GameStatus.Archived` and a `PlaythroughArchived` event and the one-active-playthrough invariant. If any ADR documents `GameStatus` values or session lifecycle, update it in this PR. Specifically check ADR-0002 (GameSession aggregate root), ADR-0003 (composed persistence), ADR-0028 (event sourcing). Update the `Last checked` timestamps in `docs/adr/INDEX.md` for each ADR verified.
- [ ] If a new ADR is warranted for the archive/start-over lifecycle decision, add it as ADR-0034: "Playthrough Archive Lifecycle and One-Active-Playthrough Invariant" and update the index. (ADR-0032 and ADR-0033 are already taken.)

#### Task 6.2 — Update docs if new lifecycle/persistence semantics introduced

- [ ] If `GameStatus.Archived` or the archive endpoint changes documented behavior in `docs/INDEX.md`-referenced docs, update them. Keep changes narrow and reference-shaped (architecture-hygiene: source docs are durable reference, not issue tracking).

#### Task 6.3 — Full validation

- [ ] `dotnet build WildBunch.sln`
- [ ] `.\scripts\postgres-dev.ps1 ensure`
- [ ] `dotnet test WildBunch.sln` (PostgreSQL-backed lane)
- [ ] `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api` (confirm no new migration needed — `Archived` is a new enum value stored as string in the existing `Status` column)
- [ ] Frontend: `npm run build` and `npm test` in `src/WildBunch.Web`
- [ ] Manual/playtest: screenshots for first-start flow (name → story → town → game) and reset-from-active-session flow (Game Settings → Start Over → confirm → start flow). Use the repo-local browser-check playbook (`.agents/ui-browser-check-playbook.md`).

---

## Definition of Done → Proof Mapping

| DOD item | Proof |
| --- | --- |
| Player can complete the start flow and reach the game proper with chosen name and starting town | Frontend start-flow test + manual screenshot of full flow |
| Story-so-far screen explains the fixed premise | `StorySoFarStep.test.tsx` + `PrologueContentTests.cs` asserting all fact phrases present |
| Starting town choices come from playable map towns; incident town unavailable | `GetStartingTownsHandlerTests.cs` + `StartingTownStep.test.tsx` |
| Game Settings reachable from normal play UI | `GameSettingsOverlay.test.tsx` asserting HUD entry, not DevOverlay |
| Start Over opens confirmation; Cancel safe | `ConfirmDialog.test.tsx` + `StartOverConfirmation.test.tsx` asserting no mutation on Cancel |
| Confirming Start Over archives, returns to start flow, new playthrough only after setup | `StartOverConfirmation.test.tsx` + `GameApiArchiveTests.cs` |
| Archived sessions remain stored and distinguishable from the active session | `GameApiArchiveTests.cs` + `OneActivePlaythroughInvariantTests.cs` asserting archived session loadable by id with `Archived` status and `PlaythroughArchived` event in stream; active session has `Active` status |
| One active playthrough at a time (strong route, persisted-level invariant) | `StartNewGameOneActivePlaythroughTests.cs` (application: single UoW commit, all pre-existing Active archived) + `OneActivePlaythroughInvariantTests.cs` (integration: exactly one `Active` row in PostgreSQL after consecutive creates; falsification checks for direct-API and archive-then-create paths) |
| Automated tests cover start flow, town selection, copy source, cancel, confirm, one-active invariant, persistence | Test files listed per task above |
| Manual/playtest proof | Screenshots for first-start and reset-from-active flows |
| PR includes updated docs or durable route notes | ADR freshness update + any doc updates from Task 6.2 |

---

## Out of Scope (explicit non-goals from issue + copy doc)

- No archived-games browser.
- No unarchive/restore feature.
- No difficulty/entropy settings beyond preserving existing behavior.
- No dev overlay reset control as the primary path.
- No saloon/person-of-interest/town-presence fixes.
- No far-future alternate-starting-truth abstraction; flavour variants only.
- No lawman start generation; leave a narrow later seam for map-topology selection.
- No UUID seed codec changes (player town choice is a runtime override, not a new encoded field).
- No new persistence tables/migrations (archive uses existing `Status` string column + event stream).

---

## Open Questions for Harley (none blocking preflight)

None. The preflight questions are all answered from current source. The plan makes two default decisions worth flagging:

1. **One-active-playthrough route (strong, chosen):** The invariant is enforced at the application/persistence level — `StartNewGameHandler` archives all pre-existing `Active` sessions in the same UoW transaction before creating the new one. This was chosen over the narrow route because repo inspection confirmed it requires only one new repository query method (`GetByStatusAsync`) and the existing UoW already supports staging multiple `StoreAsync` calls before a single `CommitAsync`. No session-account architecture or broad refactor is needed. The narrow route (browser-local only) was rejected because it would under-claim the invariant the issue requires.

2. **Prologue endpoint resolves the true culprit descriptor from the seed without yet creating a session**, so the player sees the story-so-far before committing to a town. If Harley prefers the story-so-far to come only after a setup-session is created, that is a separate slice (none exists today and the copy doc explicitly allows frontend draft state until final creation).

## Staleness check

- Plan originally written against main @ `1af4053`.
- Refreshed staleness check against current main @ `30c3b58` (PR #109 base).
- 3 commits between `1af4053` and `30c3b58`: BUNCH-95 (frontend styling consolidation), BUNCH-96 (session audit summaries), BUNCH-90 (saloon POI dev controls).
- **Core backend extension points unchanged:** `GameStatus.cs`, `StartNewGameCommand.cs`, `StartNewGameHandler.cs`, `GameSessionEndpoints.cs`, `StartGameRequest.cs`, `IGameSessionRepository.cs`, `IGameSessionUnitOfWork.cs`, `EfGameSessionUnitOfWork.cs`, `SeededNewGameFactory.cs`, `INewGameFactory.cs`, `SeedWorldBuilder.cs`, `SeedWorldCatalog.cs` — none changed.
- **`EfGameSessionRepository.cs` changed** (BUNCH-90 added saloon dev override component handling in `StoreAsync` and `LoadStoreAsync`). The staging pattern ("NO SaveChangesAsync here — the UoW commits") is unchanged. The new `GetByStatusAsync` method (Task 1.5) is additive and does not conflict.
- **Frontend styling consolidated** (BUNCH-95): `CockpitOverlayFrame.tsx`, `StartGamePanel.tsx`, `StartGameOptionsForm.tsx`, `PreSessionSurface.tsx`, `AppShell.tsx` changed. Shared styled primitives extracted to `src/WildBunch.Web/src/components/ui/sharedStyled.ts` (`FlowSurface`, `FlowNotice`, `FlowError`, `Eyebrow`, `PanelSubtitle`, `Button`). `AppShell.tsx` now wraps in `DevSurfaceProvider`. The styled-components + CSS variables pattern is preserved. The overlay/HUD/routing structure my plan references is structurally the same. Implementation should use the shared primitives from `ui/sharedStyled` for new components.
- **ADR log updated:** ADR-0032 (event-sourced dev saloon controls) and ADR-0033 (repo documentation mesh posture) added. ADR-0030 and ADR-0031 timestamps updated. Task 6.1 updated to account for these.
- **`GlobalOverlays.tsx`, `Hud.tsx`, `useCurrentGameSession.ts`, `wildBunchApi.ts`, `types.ts`** — unchanged.
- **Conclusion:** Plan is not materially stale. The core architecture decisions and extension points are valid against current main. Frontend implementation should use the new shared styled primitives.
