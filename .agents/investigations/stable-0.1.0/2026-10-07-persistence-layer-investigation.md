# Persistence layer investigation

**Status:** Static investigation for the stable `0.1.0` baseline, with proposed dispositions. No persistence code, tests, schema or services were changed or exercised. Source contradictions are distinguished from conditional corruption, concurrent-read and historical-upgrade risks. Remediation remains subject to the interactive baseline discussion.

**Scope:** All 55 tracked files in `src/WildBunch.Persistence`, read in full across the investigation: five root files, 15 GameSessions files, 11 Serialization files, five Versioning files and 19 migration/model files. Committed generated migration designers and the model snapshot were included; local `bin/` and `obj/` output was excluded. Supporting Domain, Application and test paths were traced. The [baseline tracker](2026-10-06-stable-0.1.0-investigation.md) owns scope; the [Application findings](2026-10-07-application-layer-investigation.md) and [test follow-up](2026-10-07-application-test-followup.md) supply related concerns.

## Ownership assessment

**Test remediation:** [The persistence source-to-test follow-up](2026-10-07-persistence-test-followup.md) maps every PS finding to existing expectations, misleading proof or a genuine gap, with corrected behavioral scenarios and consolidation at their owners.

**Dated disposition, 2026-10-08:** The prepped-start lifecycle, developer layout-salt event and corresponding persistence codecs described in this static snapshot were retired from the 0.1.0 candidate. These findings and file inventory rows remain historical evidence, not current implementation recommendations; see the dated [Persistence test follow-up](2026-10-07-persistence-test-followup.md) and [developer control feature record](../../../docs/features.md#dev-001-developer-salt-controls). The normal event-backed player setup and generated layout persistence/replay contract remain supported.

| Owner | Assessment |
|---|---|
| Root composition, DbContext, options and design-time factory | Correct infrastructure ownership. The runtime requires an explicit PostgreSQL connection; the design-time factory has a local development default. Neither needs relocation. Migration execution needs a release owner before public deployment. |
| `GameSessions` | Coherent aggregate persistence adapter: envelopes, mappings, command/read repositories, UoW and reconstruction. The journal port is a read adapter, not an independently mutable child repository. Keep this grouping; correct divergent load policy rather than proliferating folders. |
| `Serialization` | Correct owner for storage codecs. Partial classes grouped by payload concern are reasonable. Obsolete private types, a second test-only whole-session route and reflective restoration obscure the actual production contract. |
| `Versioning` | Correct owner for event compatibility and projection rebuild policy. The event chain is useful; the unused projection-upcaster extension contradicts the selected rebuild policy. |
| `Migrations` | Correct owner for schema history, including generated designers. Historical columns and retired entity names are not current-model bugs. Preserve applied migrations and explain material data loss truthfully; use forward changes for current schema correction. |

## Findings

### PS-01: The prepped session cannot be saved through EF

**Confirmed source contradiction, baseline blocker.** `Domain/Game/GameSession.cs:944-976` creates prep with null world/case and no genesis event. `Application/Games/Commands/PrepGameSessionHandler.cs:30-38` passes it to the repository. `GameSessions/EfGameSessionRepository.cs:182-185` unconditionally serializes world/case; `Serialization/GameSessionJsonSerializer.Components.cs:31-47` rejects null. The write fails before the advertised snapshot load exception can help. The three-phase HTTP scenario in `tests/WildBunch.Integration.Tests/Dev/TownLayoutDevIntegrationTests.cs:14-15` is skipped.

Correct the phase-aware persistence contract together with AP-01's replay-complete genesis, or explicitly retire the flow if that is the agreed product direction. Do not invent a world/case merely to satisfy storage, or bless zero-event prep as the permanent architecture. Existing snapshot-owned rows need an explicit transition decision.

### PS-02: An emitted event has no read codec

**Confirmed source contradiction, baseline blocker.** `GameSession.SetDevLayoutSalts` emits `DevLayoutSaltsForced` (`Domain/Game/GameSession.cs:1479-1482`); the repository writes concrete event names without restricting them (`EfGameSessionRepository.cs:153-169`). `Serialization/GameSessionJsonSerializer.Events.cs:34-67` omits that event from its resolver. Both read loaders deserialize the full event history, including on a current-snapshot load. A persisted salt event therefore makes that history unreadable before Domain `Apply` can run.

Add the missing supported event codec and exercise an actual persisted read/replay after the prep contract is corrected. This is a new v1 event, not an event shape migration: do not create an empty upcaster to make it appear registered. Preserve fail-closed behavior for genuinely unknown event types.

### PS-03: Read-side snapshots have a weaker freshness policy

**Confirmed divergence; stale-data impact conditional on stale or damaged snapshots.** `GameSessionReadStoreLoader.cs:133-174` never compares envelope `SnapshotVersion` to `StreamVersion`. Its component reads check component schema version only, whereas `EfGameSessionRepository.LoadAsync:80-107` checks stream freshness and missing required rows. A current-schema stale component can therefore supply old player/case/clock state beside newer event-derived journal output. Ordinary Store/UoW writes update events and snapshots atomically, so this audit does not allege ordinary partial commits.

Use a coherent freshness/reconstruction policy for both aggregate and player/journal read paths. Component schema compatibility does not establish that a snapshot represents the latest stream position.

### PS-04: Current-version corruption is either accepted or throws without replay recovery

**Confirmed recovery gap; specific damage is conditional on malformed stored shape.** `Versioning/PersistedPayloadLoader.cs:71-86` returns current-version component JSON unchanged. The command loader's corruption check counts five row names, not valid content. Deserialization then happens without an event-backed recovery boundary. PostgreSQL `jsonb` checks JSON syntax, not domain completeness. Current-version diary rows are likewise deserialized directly (`PersistedPayloadLoader.cs:99-105`). This contradicts the live doctrine's recoverable corrupted-cache posture.

Some codecs silently supply plausible state instead: missing inventory becomes empty (`Components.cs:197-200`), missing entropy becomes Classic (`Setup.cs:17-23`), missing case fields become empty collections/generic text (`Components.cs:251-267`), and nonpositive journey sequence becomes one (`Travel.cs:123-125`). Those may represent historical compatibility, but they are not proof that a damaged current snapshot is valid. Validate the current contract and recover caches from valid events; invalid or future event history must still fail closed. Preserve real legacy contracts until their stored shapes and upgrade policy are established.

### PS-05: Missing optional components can erase authoritative facts or invent a salt

**Confirmed load behavior; corruption/history impact conditional on which row is absent.** `PersistedPayloadLoader.cs:71-77` returns null for any missing component. The required-row guard excludes journey, completed history, presence ledger, action context, setup, salt source and developer overrides. `EfGameSessionRepository.cs:375-403,468-503` treats absence as defaults/empty/no override, even when events established the missing state. In particular `:378-379` calls `SaltSource.CreateRuntime`, which generates fresh cryptographic randomness (`Domain/Game/SaltSource.cs:13-17`). Repeated loads can acquire different salts without an event.

Distinguish legitimate optional absence from loss of a component that should exist at this stream position. Restore event-established state from history. Historical missing-salt rows need an explicit deterministic transition or declared unrecoverable outcome, not new hidden state on every read. The whole-session fallback test in `GameSessionDifficultyPersistenceTests.cs:271-281` protects the current fallback but does not establish its correctness for production saves.

### PS-06: Diary cache completeness is not checked

**Confirmed conditional cache-loss gap.** `PersistedPayloadLoader.LoadDiaryDays:99-105` trusts any nonempty set whose versions match. Removing one of several rows leaves a nonempty current-version set, so no projector rebuild occurs and diary history is truncated. Schema version alone does not prove sequence completeness or stream freshness. `SyncDiaryDaysAsync` also updates rows by ordered array position rather than repairing their sequence keys (`EfGameSessionRepository.cs:554-585`), so it is not a demonstrated repair for a gap.

Define a valid cache boundary at the event stream position, or derive the diary on read. Prove recovery of a partially missing cache and preserve the distinction between missing cache and corrupt authoritative events.

### PS-07: Multi-query reads do not establish one consistent stream position

**Static concurrent-read risk, not dynamically reproduced.** The read loader fetches envelope, components, events and diary in separate queries; the command loader also reads the envelope twice and loads each payload group separately. Neither creates a coherent read transaction or checks that the stream position stayed unchanged. A concurrent atomic writer can commit between those reads, producing an old envelope with newer component/event state. Write atomicity alone does not make a sequence of reads atomic. Command concurrency may reject a later save, but read-only output has no such protection.

Select a bounded consistent-read strategy, such as a coherent database snapshot or version verification/retry. The accepted stale-browser-tab policy still requires each server result and command to use a legal coherent server state. Validate the actual interleaving rather than introducing timing-dependent sleeps.

### PS-08: Partial-snapshot replay is dormant on the supported load route

**Dormant logic and misleading comments, with a concurrent/external-state caveat.** `LoadAsync:80-85` routes a mismatched snapshot directly to full replay. Its fast-path `LoadStoreAsync` still builds post-snapshot events (`:315-322`), and `ToAggregate` carries version arithmetic and conditional replay (`:421-435,505-508`). With an unchanged envelope this branch cannot execute. Because a second envelope is queried, external mutation or a concurrent stale-envelope writer could change that premise; do not call it unconditionally unreachable under every possible database change.

Choose full replay for stale snapshots or a deliberately supported incremental snapshot route, then remove the competing dormant path. Update the canonical doctrine diagram and comments accordingly. A branch-presence/source-string test is not proof of reachability.

### PS-09: Start-phase derivation has already drifted between loaders

**Confirmed source divergence, currently latent for the public combined start command.** `GameSessionReadStoreLoader.cs:66-82` omits `StartingTownSelected`; `EfGameSessionRepository.cs:589-608` recognizes it. The command repository has an intermediate-phase test at `EfGameSessionRepositoryTests.cs:963-997`, but that does not exercise the read model. Persisting this legitimate intermediate phase yields different phase reports by adapter.

Use one event-derived phase owner and verify both read contracts against the same independently constructed history. Do not add a test merely preserving each implementation's different answer. This is another AP-15-style duplicated derivation problem.

### PS-10: Unique errors are classified by message as event-sequence races

**Confirmed overbroad error classification.** `EfGameSessionUnitOfWork.cs:24-31,39-63` recognizes SQLSTATE and generic duplicate text in messages, then labels every match as duplicate event sequence. Other constraints include event ID, session ID and component keys. Retrying an unrelated integrity error obscures its cause. Inspect typed PostgreSQL SQLSTATE/constraint identity and translate the actual append conflict only. The UoW does roll back and clear tracked entities on its translated failure; no dirty-tracker allegation is made.

### PS-11: Projection evolution repeats whole replay and cannot cover prep genesis

**Confirmed structure; outage/cost conditional on a version change.** `ProjectionVersions.ForComponent:44` ignores the name and uses a single global version. Each stale component invokes a separate whole-session rebuild (`PersistedPayloadLoader.cs:79-86`) and reserializes/reparses it. `SessionRebuilder.cs:25` requires one `WorldGenerated`; AP-01's prepped/injected histories cannot satisfy that, even after PS-01's write blocker is fixed.

A shared version is not inherently wrong, and a per-component registry is not automatically required. At minimum rebuild once per coherent load and ensure every supported phase has authoritative reconstructable history before relying on projection-version changes. Settle version granularity when a real independent shape change requires it. Do not use database deletion as compatibility handling.

**Dated disposition, 2026-10-10, row 07 repeated component rebuild:** Component-cache access is now scoped to one loaded `GameSessionStore`. The first stale component lazily reconstructs one aggregate from the already-upcasted ordered event list; every other stale payload requested by that same command or read load serializes from that aggregate, while current-version payloads still use stored JSON. PostgreSQL behavior tests observed one reconstruction across seven stale required components on command load, one per player/journal read, command-save convergence, and read no-writeback for component rows, envelope watermarks and events. Missing rows, malformed-cache recovery, future authoritative-history failure and single-component loader behavior remain separately covered. Prep/injected genesis and migration deployment policy are not settled by this change; projection version granularity remains shared until an independent version is required. No schema, event, ADR or player-facing contract changed.

### PS-12: The projection-upcaster extension is inert

**Confirmed unused extension contract.** `IPayloadUpcaster.cs` and `PayloadUpcasterRegistry.cs:112-113` admit `PayloadKind.Projection`, but current version lookup, upcast and chain validation operate on event chains only. Components and diary rebuild through `ProjectionVersions`. No production projection upcaster consumer exists. Reduce the registry to the real event capability; do not implement a second projection migration strategy just to justify scaffolding.

The actual event chain rejects duplicates, gaps and future event versions. `WorldGeneratedV1ToV2Upcaster` preserves existing `caseFile` and adds null only when absent. Keep this real compatibility machinery. Small fragment tests alone do not establish that a complete historical saved stream can load and replay.

**Dated disposition, 2026-10-09, PS-12:** The registry now accepts only `IEventUpcaster` and keys chains by event payload type; `PayloadKind` and the unused projection-upcaster extension are removed. Constructor validation still rejects duplicate transitions and gaps, and persisted event version lookup, ordered transforms, future-version failure and production `WorldGenerated` v1-to-v2 compatibility remain. Projection components and diary rows continue through `ProjectionVersions` rebuild. No event, schema, migration or ADR contract changed.

### PS-13: Dead codecs and a test-only whole-session restore path obscure production persistence

**Confirmed repository call graph, external consumers not established.** `GameSessionJsonSerializer.Log.cs` has only a private unused log snapshot. `Travel.cs:293-375` contains unused `JourneyTrailEventSnapshot` and `TravelDiaryEncounterResolutionSnapshot`; the live diary snapshot stores the domain records directly. `GameSessionRehydrator.RestoreDevLayoutSalts:93-102` has no callers. The whole-session `Serialize`/`Deserialize` and `SessionSnapshot` have callers in tests only; production writes component rows and uses `Rehydration.cs`. The whole-session restoration omits the repository's event-derived start phase/version handling.

Remove confirmed private dead helpers in cleanup. Determine whether the whole-session API has a genuine retained compatibility consumer; retire or clearly bound it and transfer meaningful tests to production repository behavior. Keep shared nested codecs used by component serialization. This continues AP-16/AP-18's reference-only/compatibility scaffolding pattern.

### PS-14: Reflection and implicit nested domain JSON are fragile storage contracts

**Confirmed structural debt; no present constructor failure claimed.** `GameSessionRehydrator.cs:12-55,63-79` binds an exact private constructor and named fields; component codecs restore clock/pursuit private fields (`Components.cs:628-653`), and diary serialization locates internal properties by string (`Travel.cs:514-516,561-582`). Refactors fail at runtime rather than compilation, and restoration bypasses normal state validation. The diary and developer override codecs serialize several domain records directly, so a Domain refactor can also alter persisted shape without editing the codec.

Prefer explicit, bounded Domain restoration/snapshot seams with validation and materially meaningful repository round trips. Do not expose setters or public arbitrary mutation to solve this. Explicit nested storage contracts are justified where they improve actual evolution safety, not as a blanket DTO duplication exercise.

### PS-15: Schema redundancy, an unused version field and unbounded load work

**Confirmed model/configuration debt.** Stored events have both a composite primary key and a separate unique index on exactly `(StreamId, Sequence)` (`StoredEventEntityConfiguration.cs:11,21`; EventStore migration). The second index duplicates uniqueness/lookup support. Retain separate event-ID uniqueness and remove the redundant index through a new migration after confirming database definition.

`GameSessionEntity.SchemaVersion` is written as one but never consulted during load; component/event/diary versions do have consumers. Either define a real envelope evolution policy when needed or stop implying this field validates the snapshot. Do not create a constants test to justify it. `SessionRebuilder` also accepts an unused serializer and generates an extraction-only placeholder ID; simplify the parameter, while retaining the safe extraction boundary rather than treating the documented placeholder as a game identity bug.

`GetByStatusAsync:41-58` selects every matching session ID and loads each sequentially; even current snapshots deserialize the full event history (`EfGameSessionRepository.cs:304-314`). That history has actual projection consumers, so it is not dead work, but AP-09's global active-session archival multiplies it and couples one setup operation to every active stream. Player ownership requires a bounded indexed lookup in its feature slice. Consider separately loaded/materialized read projections where justified; no measured performance regression or infrastructure sizing claim is made by this audit.

**Dated disposition, 2026-10-10, row 07 PS-15 schema artifacts:** The unused `GameSessionEntity.SchemaVersion` property, its repository writes and EF mapping, and the unique `(StreamId, Sequence)` index were removed from the current model. Forward migration `20261009225627_RetireUnusedSessionSchemaArtifacts` drops that index and column; its Down restores both. Historical migrations are unchanged. `MigrationTests.RemovingUnusedSchemaArtifactsPreservesStoredSession` migrates to the prior schema, stores a real session through `EfGameSessionRepository`, applies the forward migration, then proves a fresh repository load retains player identity, location, stream version and event count. The greenfield migration/save/load behavior remains covered. The existing append-race test continues to exercise sequence uniqueness through the retained primary key, and the EventId collision test retains independent unique-event behavior. The stale assertion that the unused envelope column exists was removed without an absence detector. This closes only the unused envelope version and duplicate sequence index; `SessionRebuilder` cleanup and unbounded status loads remain open. Component, event and diary-day schema versions remain because consumers use them.

**Dated disposition, 2026-10-10, row 07 PS-15/17 session rebuilder:** `SessionRebuilder` no longer accepts an unused serializer dependency; the event-only callback has the explicit `RebuildForComponentCache` name and forwarding lambdas use its method group. The stale “Plan C” comment is removed. `SessionRebuilder_ComponentJson_IsIndependentOfSessionId` is retired because it exercised the known-ID overload and only compared five component encodings, so it did not verify the component-cache callback and froze an unsupported helper detail. `LoadComponentPayload_StaleVersion_TriggersRebuildFromEvents` remains the behavior proof for the callback's recovered player payload. Event application, event upcasting, cache formats, replay and read behavior are unchanged. This closes the PS-15 unused-argument and PS-17 stale-comment subfindings; unbounded status loads remain open.

### PS-16: Historical migrations do not preserve all historical playthrough data

**Confirmed migration operations; past intent and affected deployed data unknown.** `ComposedSessionPersistence.Up:15-17` drops `StateJson` before creating empty component tables. Its Down recreates empty snapshot strings, not lost data. `EventStore.Up` creates an empty event table with existing envelopes at stream version zero and no backfill. `DropGameSessionLogEntries.Up` removes prior journal rows; its Down restores only the table. Together these cannot establish lossless upgrades from populated pre-composition/pre-event databases. The rename migration and nullable seed addition are materially different: they preserve data/schema rather than pretending to restore discarded history. The diary version addition correctly tags existing rows v1.

Preserve these historical files and record the material removals and unknown provenance in the ADR cleanup. Decide the supported baseline/version boundary and explicit disposition for unrecoverable saves. Do not rewrite migration history to pretend a backfill happened. Future releases require populated upgrade fixtures and explicit destructive-transition decisions, not only greenfield schema creation. `MigrationTests.MigrationsCreateGameSessionsTableAndRoundTripSession` migrates an empty database and saves a new session afterward; it cannot prove retention across these historical upgrades. Its manually empty upcaster registries also differ from production registration.

### PS-17: Comments and startup migration ownership overstate the supported system

**Confirmed stale comments; release policy is a planning gap.** `DependencyInjection.cs:20` says the first event shape change is future work, while its registry already contains `WorldGeneratedV1ToV2Upcaster`. Completeness tests are called build-time enforcement although ordinary compilation does not execute them. Rebuilder comments retain “Plan C”; fast-path comments promise dormant partial replay; the loader claims a single domain-object funnel although it returns component JSON for codecs to interpret. Describe the actual guarantees and boundary instead of architectural slogans. This continues AP-10/U-005.

`ApplyWildBunchMigrations:57-62` runs schema migration synchronously at API startup. This is a real owner today, not automatically a bug for local development. The release plan needs an explicit migration step/privilege/failure and rollback posture before relying on every production application startup to perform schema changes. Decide it in the deployment slice, not by silently changing runtime composition during this audit.

## Behavioral proof and cleanup boundaries

Prioritize a real phase-aware prep save/load/replay scenario; supported typed-event round trips through the registered production loader; aggregate/read-model equivalence at a coherent stream position; missing/partial/malformed cache recovery with intact history; deterministic salt preservation; and rejection of unknown/future/corrupt authoritative history. Use populated historical upgrades for the declared support boundary. Existing meaningful snapshot-removal, optimistic append, event chain, legacy-shape and replay parity tests should be strengthened at their actual owners, not replaced with filename or constants checks.

Keep schema history even where it records a destructive decision. Compatibility defaults need historical evidence and a defined transition, not automatic deletion. Test-only serializers are not interchangeable with EF persistence tests. Pure comment/dead-helper cleanup needs compile and caller checks rather than a new regression test per removed symbol. No performance measurement, live-database inspection, migration execution or runtime reproduction is claimed here.

## Full-read file dispositions

Paths below are relative to `src/WildBunch.Persistence`. Each row represents a full-file read, including committed generated migration files.

| File | Disposition |
|---|---|
| `DependencyInjection.cs` | Keep composition; PS-11/17 callback and stale comments/release ownership. |
| `PersistenceDbContextOptions.cs` | Keep explicit required runtime configuration; no additional defect found. |
| `WildBunch.Persistence.csproj` | Keep inward Application/Domain references and provider/design dependencies. |
| `WildBunchDbContext.cs` | Keep four DbSets and assembly configuration ownership. |
| `WildBunchDbContextFactory.cs` | Keep design-time local default; document explicit target for deployment migration commands. |
| `GameSessions/EfGameJournalReadRepository.cs` | Keep focused read adapter; inherits loader findings. |
| `GameSessions/EfGameSessionReadRepository.cs` | Keep focused read adapter; inherits loader findings. |
| `GameSessions/EfGameSessionRepository.cs` | Correct PS-01/03-11; retain aggregate persistence owner. |
| `GameSessions/EfGameSessionUnitOfWork.cs` | Keep commit ownership; correct PS-10 classification. |
| `GameSessions/GameSessionComponentEntity.cs` | Keep infrastructure row. |
| `GameSessions/GameSessionComponentEntityConfiguration.cs` | Keep composite component key and JSONB shape. |
| `GameSessions/GameSessionComponentNames.cs` | Keep component identity/helpers; PS-05 presence semantics. |
| `GameSessions/GameSessionDiaryDayEntity.cs` | Keep derived cache row. |
| `GameSessions/GameSessionDiaryDayEntityConfiguration.cs` | Keep mapping; cache completeness belongs to load policy. |
| `GameSessions/GameSessionEntity.cs` | Keep the remaining session envelope; PS-15 unused schema version removed. |
| `GameSessions/GameSessionEntityConfiguration.cs` | Keep current remaining envelope mapping; ownership feature remains separate. |
| `GameSessions/GameSessionReadStoreLoader.cs` | Correct PS-03-07/09 divergent read semantics. |
| `GameSessions/SessionRebuilder.cs` | PS-15 unused argument and PS-17 stale comment removed; AP-01 phase coverage remains. |
| `GameSessions/StoredEventEntity.cs` | Keep infrastructure event envelope outside Domain. |
| `GameSessions/StoredEventEntityConfiguration.cs` | Keep append primary key and event-ID uniqueness; PS-15 duplicate index removed. |
| `Serialization/GameSessionJsonSerializer.Components.cs` | Keep live codecs; PS-01/04/14 validation, defaults and restoration. |
| `Serialization/GameSessionJsonSerializer.Events.cs` | Correct PS-02 missing supported event; keep fail-closed resolver. |
| `Serialization/GameSessionJsonSerializer.Log.cs` | Remove unused private codec, PS-13. |
| `Serialization/GameSessionJsonSerializer.Rehydration.cs` | Keep live component restoration adapter; PS-14 seam. |
| `Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` | Retire or bound test-only whole-session path after consumer assessment, PS-13. |
| `Serialization/GameSessionJsonSerializer.Setup.cs` | Keep codec; PS-04 scope missing-entropy compatibility. |
| `Serialization/GameSessionJsonSerializer.Travel.cs` | Keep live travel/diary codecs; PS-04/13/14 clamps, dead types and reflection. |
| `Serialization/GameSessionJsonSerializer.UnrelatedCriminalLedger.cs` | Keep live ledger component codec. |
| `Serialization/GameSessionJsonSerializer.WantedSuspectPresence.cs` | Keep live codec; PS-05 distinguish absence from lost history. |
| `Serialization/GameSessionJsonSerializer.cs` | Keep shared options/converter; assess whole-session API under PS-13. |
| `Serialization/GameSessionRehydrator.cs` | Replace brittle restoration seam and remove dead wrapper, PS-13/14. |
| `Versioning/IPayloadUpcaster.cs` | Keep real event capability; remove inert projection extension, PS-12. |
| `Versioning/PayloadUpcasterRegistry.cs` | Keep useful event chain validation; narrow PS-12 unused kind. |
| `Versioning/PersistedPayloadLoader.cs` | Correct cache validity/rebuild boundary, PS-04-06/11. |
| `Versioning/ProjectionVersions.cs` | Keep explicit rebuild versions; assess granularity/replay once per load, PS-11. |
| `Versioning/WorldGeneratedV1ToV2Upcaster.cs` | Keep actual historical compatibility transform. |
| `Migrations/20260529130641_InitialCreate.cs` | Preserve original monolithic schema history. |
| `Migrations/20260529130641_InitialCreate.Designer.cs` | Preserve corresponding historical target model. |
| `Migrations/20260531081955_ComposedSessionPersistence.cs` | Preserve destructive snapshot transition; record PS-16. |
| `Migrations/20260531081955_ComposedSessionPersistence.Designer.cs` | Preserve composed target model, including retired log entity. |
| `Migrations/20260531154230_PostgresCutoverSync.cs` | Preserve provider/type normalization transition. |
| `Migrations/20260531154230_PostgresCutoverSync.Designer.cs` | Preserve PostgreSQL target model. |
| `Migrations/20260531161409_JsonbPayloadStorage.cs` | Preserve explicit JSONB cast; historical populated invalid JSON would fail conversion rather than silently repair. |
| `Migrations/20260531161409_JsonbPayloadStorage.Designer.cs` | Preserve JSONB target model. |
| `Migrations/20260622154258_EventStore.cs` | Preserve event-store introduction; no backfill under PS-16; duplicate index PS-15. |
| `Migrations/20260622154258_EventStore.Designer.cs` | Preserve event-store target model. |
| `Migrations/20260624104903_DropGameSessionLogEntries.cs` | Preserve log-table removal; document irreversible row loss, PS-16. |
| `Migrations/20260624104903_DropGameSessionLogEntries.Designer.cs` | Preserve post-log target model. |
| `Migrations/20260628062535_RenameTravelDifficultyToGameDifficulty.cs` | Keep reversible data-preserving rename. |
| `Migrations/20260628062535_RenameTravelDifficultyToGameDifficulty.Designer.cs` | Preserve renamed target model. |
| `Migrations/20260628153853_AddSeedCodeToGameSession.cs` | Keep nullable addition; legacy recovery is event/load responsibility. |
| `Migrations/20260628153853_AddSeedCodeToGameSession.Designer.cs` | Preserve seed-column target model. |
| `Migrations/20260719061600_AddDiaryDaySchemaVersion.cs` | Keep existing-row v1 tagging. |
| `Migrations/20260719061600_AddDiaryDaySchemaVersion.Designer.cs` | Preserve diary-version target model. |
| `Migrations/WildBunchDbContextModelSnapshot.cs` | Current fields/types/keys align with inspected configuration; preserve generated migration owner, then regenerate for real forward schema changes. |

**Dated disposition, 2026-10-09, PS-08 command snapshot-tail replay:** The command loader now encloses the initial envelope read, snapshot/stream comparison, component-presence check and subsequent `LoadStoreAsync` reads in one PostgreSQL `RepeatableRead` transaction. A stale initial envelope takes full replay; a fast-path envelope equals the stream position visible to the store loader, so `PostSnapshotEvents` is empty. External commits cannot change that transaction snapshot. Retire the command-side tail calculation and replay branch while preserving query full replay. This disposition applies to the current private command loader route; it does not generalize to future loaders that do not enforce the same coherent-read boundary.

**Dated disposition, 2026-10-09, PS-13:** Repository call-site review found no production consumer for whole-session `Serialize(GameSession)`/`Deserialize(string)` or `GameSessionSnapshot`. The supported path persists versioned component snapshots and typed events, then rehydrates through the repository event-aware route. Retire the test-only whole-session API, snapshot type and tests that asserted that unsupported shape. Preserve generic component decoding, production event/component codecs and upcasters, `RehydrateGameSession`, `RestoreFromSnapshot`, and event-based cache recovery. Remove the unused private log/travel snapshot helpers and Domain-test Persistence reference after confirming no callers. The PostgreSQL confrontation recovery test now also proves two surrendered suspects recover as `SecuredAlive` from the event path. No schema, migration, event, component-version, replay or ADR contract changes.

**Dated disposition, 2026-10-10, row 07 persisted travel values:** Journey and diary snapshots now map `JourneyTrailEventState`, `TravelDiaryEncounterResolutionState`, and `HorseTravelState` through explicit Persistence-owned value snapshots; nested `JourneyEncounterState` reuses the existing explicit encounter snapshot. Repository round trips cover generated trail-event facts, interrupted and resolved encounters, and horse travel state. A literal v1 diary payload protects the existing camel-case fields and values, while a journey codec boundary covers populated current-day nested values. The persisted property shape, enum representation, null behavior, projection version, events, database schema, and Domain behavior are unchanged. Direct developer-override serialization remains outside this slice, so PS-14 is only partially addressed.

**Dated disposition, 2026-10-10, row 07 pending developer overrides:** `DevTravelOverride` and `DevSaloonOverride` now serialize through Persistence-owned component snapshots, including the nested `JourneyFoeProfile` and `SuspectId` values. Literal v1 fixtures protect numeric enum values, camel-case fields, nullable fields, and the nested suspect `{ "value": ... }` shape; PostgreSQL save/load tests prove each override remains available to the same next-action behavior after repository rehydration. Focused tests passed before the explicit mapping, and independent mutations to the foe-profile and suspect-ID mappings each caused their intended fixture assertion to fail. The remaining `JsonSerializer.Serialize` calls in the serializer either map through explicit Persistence snapshot types, serialize typed domain events through the event codec, or serialize the explicit Domain `WorldSnapshot` that is the established `WorldGenerated` fact shape. No direct component serialization of the pending developer-override Domain records remains. The typed-event and world-generation snapshot shapes remain separate PS-14 review subjects; this slice makes no claim that all reflection-based rehydration concerns are closed.

**Dated disposition, 2026-10-10, row 07 PS-04/PS-06 malformed diary cache:** A diary row with matching projection watermark, expected day count, current schema version and contiguous sequence is no longer trusted when its JSON cannot materialize. `DeserializeTravelDiaryDay` wraps the targeted JSON/materialization failures as `InvalidComponentCacheShapeException`; `LoadDiaryDays` rebuilds the complete diary from `TravelDiaryDayProjector` when a cached row has that shape failure. Player and journal reads leave rows, envelope watermarks and events unchanged, and a later ordinary command save repairs the ordered diary cache. A current-version payload that successfully materializes to an incorrect but structurally valid value is not detected by this boundary. Event upcasting and projection failures remain fail-closed. No schema, event, upcaster, ADR or player-facing contract changed.

**Dated disposition, 2026-10-10, row 07 PS-04 World generated-fact cache:** A current-version World component is now compared with the sole `WorldGenerated` event's World snapshot using the established serializer shape. A missing or changed generated town, ordered trail, or persisted layout fact routes command and player/journal reads through full event replay; reads preserve the damaged component and event history, while a later legal save repairs the component. A PostgreSQL scenario with four towns, three trails and populated layouts failed before the comparison for the missing trail, then proved command/player/journal recovery, read no-writeback and ordinary-save repair. Malformed authoritative WorldGenerated JSON still throws during event decoding and is not hidden by cache recovery. This closes equality for the generated World snapshot only; semantic contradictions in other mutable World-related state and other component caches remain open. No schema, event, upcaster, migration, ADR or player-facing contract changed.
