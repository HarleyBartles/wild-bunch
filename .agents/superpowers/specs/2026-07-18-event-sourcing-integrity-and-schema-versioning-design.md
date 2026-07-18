# Event Sourcing Integrity and Schema Versioning Design

## Date

2026-07-18

## Status

ready for planning

## Goals

1. Make event sourcing materially true: every persisted state must be
   reconstructable from the event stream alone, and the production load path
   must be able to load a session without the snapshot.
2. Add schema versioning for persisted payloads so contract evolution doesn't
   brick existing playthroughs: event upcasters for immutable history, projection
   rebuild-on-mismatch for derived state.
3. Establish durable agent policy that prevents future agents from re-introducing
   non-true-event-sourcing patterns.

## Non-Goals

- Removing the snapshot write path. Snapshots remain as shortcut caches; they're
  still written on every save. The change is that they're no longer *required*
  to load.
- Removing the command path's diary-day creation. `JourneyLoop` still creates
  diary days as a side effect during live play. The projector is the
  rebuild-path equivalent; both must converge (parity test). Collapsing to
  project-on-demand is a future cleanup, not this design.
- A global migration sweep to bring all existing playthroughs to current schema.
  Active playthroughs converge on next save; abandoned ones stay at their old
  version on disk.
- Building projectors for every existing projection. Only `TravelDiaryDayProjector`
  is in scope (it's the known violation). Other projections are audited for
  replayability (Part 1c) but new projectors for them are only built if the
  audit finds violations.
- Splitting `GameSession` into sub-aggregates (BUNCH-67, closed).
- Introducing a broker (RabbitMQ/Kafka/EventStoreDB).

## Scope

This spec is large and covers three implementation plans' worth of work:

- **Plan A (Part 0 + Part 1c):** Event sourcing integrity policy + replayability
  audit. Establishes the policy surface (policy doc, mermaid chart, negative
  constraints, skill routing, guardrails/review-guide updates) and audits all
  persisted state for replayability. Low-risk documentation/verification work
  that lands the policy before any code changes. If the audit finds violations
  beyond `TravelDiaryDays`, those are added to Plan B's scope.
- **Plan B (Part 1b + Part 1a):** Make event sourcing real. Builds
  `TravelDiaryDayProjector` (the hardest piece — new projector tracking running
  resource state across all events), wires `RehydrateFromEvents` as a production
  load path, and adds the full replay equality test as the completion gate. This
  is where the high-risk code work lives. Depends on Plan A.
- **Plan C (Part 2):** Schema versioning on top of real event sourcing. Upcaster
  registry, projection version columns + EF migration, `PersistedPayloadLoader`
  load funnel, write-side version stamping, 7 test categories. Depends on Plan B.
- **Part 3** is a manual user action (branch protection), not an implementation
  plan item. It can be done at any point but is a hard prerequisite for the
  versioning enforcement guarantee.

The planner should produce three plans, not one. Plans must land in order
(A → B → C). The split points are explicit:
- Plan A ends when the policy doc is committed and the audit is complete.
- Plan B ends when `RehydrateFromEvents` + projectors reconstruct the complete
  session (verified by the full replay equality test).
- Plan C starts with the upcaster registry.

## Context

Wild Bunch is an event-sourced C#/.NET game (ADR-0028). The event stream is the
source of truth; JSONB component snapshots and diary-day rows are documented as
cache/projection (ADR-0028 §8, §9). However, two gaps prevent the versioning
system from being meaningful:

1. **Not all state is reconstructable from events today.** `RehydrateFromEvents`
   exists in the domain (`GameSessionEventReplay.cs:29`) and is tested for
   aggregate state parity (`TravelReplayEqualityTests`), but the production load
   path (`EfGameSessionRepository.LoadStoreAsync`) does not use it — it loads the
   snapshot and replays only post-snapshot events. More critically,
   `TravelDiaryDayState` rows are NOT reconstructed by event replay at all:
   `JourneyLoop.Apply(JourneyStarted)` clears `_travelDiaryDays`, and no `Apply`
   method creates diary days. They are a command-path side effect persisted to a
   separate table. `RehydrateFromEvents` produces correct aggregate state but
   empty `TravelDiaryDays`.

2. **No schema versioning for persisted payloads.** `StoredEventEntity.SchemaVersion`
   and `GameSessionComponentEntity.ComponentVersion` columns exist but are
   stamped with a hardcoded `const int SchemaVersion = 1`
   (`EfGameSessionRepository.cs:14`) and never read on load
   (`GameSessionReadStoreLoader.cs:155` ignores the version column). There is no
   upcaster pipeline, no per-type version resolution, and no mechanism to evolve
   payload contracts without bricking existing playthroughs.

This design addresses both gaps in order: Part 1 makes event sourcing materially
true (the prerequisite), Part 2 adds versioning on top of real event sourcing.

## Design Principles

1. **Events are the source of truth.** Every piece of persisted state must be
   reconstructable from the event stream alone. Snapshots and projections are
   shortcut caches — they must never be operationally required to load a session.

2. **Snapshots are shortcut caches, not part of the replay contract.** A snapshot
   is a performance optimization. The system must function correctly without it.
   If a snapshot is missing or its shape is wrong, the session loads from events.

3. **Projections are derived state.** Projections (components, diary days) are
   rebuildable from the event stream. When a projection's stored version does not
   match the current code version, the projection is dropped and rebuilt from
   events — not upcasted. Upcasters are for events only (immutable history that
   cannot be rebuilt).

4. **Upcasters are the version declarations.** There is no hand-edited version
   registry. The current version for each event type is derived from the count of
   registered upcasters. To bump a version, you write and register an upcaster.
   The act of bumping IS the act of writing the upcaster.

5. **The load path is a funnel.** There is no code path from persisted rows to
   domain objects that bypasses version checking and upcasting. The serializer's
   deserialize methods are internal; the only public load surface is
   `PersistedPayloadLoader`, which always runs the version check.

6. **Fail closed.** If a version transition is missing an upcaster, the load
   fails rather than returning stale-shape data. If a row is at a future version
   the code doesn't understand, the load fails rather than silently treating it
   as current.

7. **Writeback on next save.** When a session is loaded with an old-version
   projection and then saved (the normal play cycle), the projection is written
   back at current version. Active playthroughs converge to current schema
   naturally. Abandoned playthroughs stay at their old version on disk — no
   global migration sweep.

## Part 0: Event Sourcing Integrity Policy

### New policy document

A new policy document at `.agents/docs/event-sourcing-integrity-policy.md`
establishes the rules that prevent future agents from re-introducing
non-true-event-sourcing patterns. The architecture guardrails
(`.agents/docs/architecture-guardrails.md:40`) already say "snapshots are cache"
in principle; this policy makes the specific invariants enforceable.

The policy doc is the **primary operational surface** for agents — it's what
doctrine and guardrails route to. ADR-0028 is the decision record (why the
architecture was chosen); the policy doc is the operational guidance (what an
agent must do and must not do when working in this architecture). ADR-0028
references the policy doc for the live canonical flow rather than duplicating it,
so the policy doc is the single source of truth for the current flow and ADR-0028
remains the historical decision record.

### Policy rules

1. **All persisted state must be reconstructable from the event stream alone.**
   If a piece of state cannot be rebuilt by replaying events through `Apply` or
   through a projector, it is a violation. New state that needs persistence must
   either (a) be set by an `Apply` method from event fields, or (b) be derivable
   by a projector from the event stream.

2. **Snapshots are shortcut caches.** They must never be the only path to load a
   session. The system must function correctly with an empty snapshot table. A
   missing or corrupted snapshot must not prevent session load.

3. **Projections are derived state.** Projection tables (components, diary days)
   must have a projector that rebuilds them from the event stream. If a
   projection table exists but no projector can rebuild it, that is a violation.

4. **`Apply` methods must not create projections.** `Apply` sets aggregate state
   from event fields. Projection creation (diary days, log entries, etc.) is a
   read-path concern handled by projectors, not a write-path side effect of
   `Apply`. The command path may create projections as a side effect for
   performance, but the projector must be able to produce the same result from
   events alone.

5. **Command-path state and replay-path state must converge.** This is already in
   the guardrails (line 42). The policy extends it: projection state must also
   converge. A projector's output must match what the command path produced.

6. **No new persisted state without a replay path.** When adding a new field to a
   projection or a new projection table, the projector that rebuilds it from
   events must be written in the same change. No "we'll add the projector later."

### Canonical flow diagram (mermaid)

The policy doc includes a mermaid chart showing the canonical CQRS + event
sourcing data flow. This is the **target flow** that the system must conform to
after this design lands — not the current (pre-design) state. The chart is the
single visual reference for how commands, events, snapshots, and projections
relate.

**What the chart must show:**

- **Command path:** command → `GameSession` method → `ProduceEvent` → `Apply`
  (sets aggregate state) → uncommitted events → `StoreAsync` (append events to
  stream + write snapshot cache + write projections at current version) →
  `CommitAsync` (single save + transaction).
- **Load fast path (snapshot current):** `LoadStoreAsync` → read snapshot at
  current version → replay post-snapshot events through `Apply` → return
  aggregate. This is the shortcut cache path.
- **Load full replay path (snapshot stale/missing/corrupted):**
  `LoadFromEventsAsync` → upcast events via `PersistedPayloadLoader` →
  `RehydrateFromEvents` (reconstruct aggregate from full event stream) →
  rebuild projections via projectors → return aggregate. This is the
  event-sourcing-true path that proves snapshots are not required.
- **Projection rebuild path:** stale projection version detected on load →
  discard stored projection rows → run projector over event stream → return
  rebuilt projection. Writeback on next save converges the on-disk version.
- **Version check funnel:** all loads pass through `PersistedPayloadLoader` →
  event upcasting (events) + version check with rebuild (projections). No
  bypass path exists.
- **Writeback-on-next-save:** when a session loaded with stale projections is
  saved, projections are written at current version. Active playthroughs
  converge; abandoned ones stay at their old version on disk.

**What the chart must NOT show (negative constraints, rendered as struck-through
or red/dashed paths):**

- ~~Snapshot required to load~~ — the snapshot is a shortcut cache, not a
  requirement. A path that fails when the snapshot is missing is a violation.
- ~~Direct mutation outside `Apply`~~ — state changes that don't flow through
  `ProduceEvent` → `Apply` are violations.
- ~~Projection created by `Apply`~~ — `Apply` sets aggregate state; projections
  are built by projectors on the read/rebuild path.
- ~~Load bypassing `PersistedPayloadLoader`~~ — any path that deserializes
  persisted payloads without version checking is a violation.
- ~~Event shape changed without version bump~~ — events are immutable history;
  shape changes require an upcaster and a version bump.

The negative-constraint rendering makes the contrast explicit for agents who
might be tempted to reintroduce these patterns. A clean chart showing only the
canonical flow assumes the reader infers violations from what's not shown; the
explicit struck-through paths remove that inference burden.

**Chart staleness discipline:**

- The chart is canonical. If the flow changes, the chart must be updated in the
  same PR. A stale chart is worse than no chart because it misleads.
- The code review guide (`.agents/docs/guides/code-review-guide.md`) is updated
  to call out chart-staleness as a review check for any PR touching persistence,
  event sourcing, or load paths.
- There is no automated guardrail test that verifies the chart against code
  (building a chart-from-code generator would be disproportionate). The
  discipline is human/agent review, enforced by the policy doc and review guide.

### Negative constraints and common mistakes

The policy doc includes a section of negative constraints — examples of bad
patterns and common mistakes that agents must not introduce. These are concrete
and anti-pattern-shaped, not abstract principles. Each entry names the pattern,
why it's wrong, and what to do instead.

**Anti-patterns to reject:**

1. **Persisting state that isn't reconstructable from events.** Example: adding a
   new field to a component that's set by a command method but not by any `Apply`
   method and not derivable by a projector. Why wrong: the snapshot becomes the
   only source of truth for that field, breaking event sourcing. Do instead: set
   the field in an `Apply` method from event fields, or add a projector that
   derives it from the event stream.

2. **Making the snapshot required to load.** Example: a load path that throws if
   a component row is missing, rather than rebuilding it from events. Why wrong:
   the snapshot is a shortcut cache; the system must work without it. Do instead:
   fall back to `RehydrateFromEvents` + projector rebuild when the snapshot is
   missing or stale.

3. **Creating projections inside `Apply`.** Example: `Apply(TravelDayAdvanced)`
   appending a `TravelDiaryDayState` to a list. Why wrong: `Apply` is the
   event-sourced mutation path for aggregate state; projections are a read-path
   concern. Mixing them couples write and read concerns and breaks replay parity
   (the projector and `Apply` can drift). Do instead: `Apply` sets aggregate
   state only; a projector derives diary days from the event stream.

4. **Direct mutation outside the event-sourced route.** Example: a command method
   that sets `Player.Health` directly without producing an event. Why wrong:
   replay can't reproduce the state. Do instead: produce an event and let
   `Apply` do the work.

5. **Loading persisted payloads without version checking.** Example: calling
   `serializer.DeserializeEvent` directly on a stored event's `PayloadJson`
   without going through `PersistedPayloadLoader`. Why wrong: bypasses upcasting
   and version checking, can serve stale-shape data. Do instead: always load
   through `PersistedPayloadLoader`, which is the funnel.

6. **Changing an event's shape without bumping its version.** Example: renaming a
   field on `GameStarted` without writing an upcaster. Why wrong: old events in
   the log fail to deserialize or deserialize to wrong shape; the event stream is
   immutable history and can't be rewritten. Do instead: write an upcaster
   (`IEventUpcaster` with `FromVersion = current`), register it, and the version
   bump is implicit.

7. **Hand-editing a version registry.** Example: adding a
   `PayloadVersions.Events["GameStarted"] = 2` line. Why wrong: there is no
   hand-edited version registry; versions are derived from upcaster count. A
   hand-edited registry decouples version declaration from upcaster existence,
   reintroducing the "version bumped without upcaster" failure mode. Do instead:
   write and register the upcaster; the version is derived.

8. **Adding a new projection table without a projector.** Example: adding a
   `GameSessionEncounters` table that's written by the command path but not
   rebuildable from events. Why wrong: the table becomes operationally required
   (can't be rebuilt if lost), breaking the "projections are derived state"
   invariant. Do instead: write the projector in the same change as the table.

9. **Deferring the projector to a follow-up.** Example: "we'll add the
   `TravelDiaryDayProjector` in a later PR; for now the command path writes diary
   days directly." Why wrong: the violation exists in production until the
   follow-up lands, and follow-ups get deprioritized. Do instead: the projector
   ships with the projection table or the projection isn't added.

10. **Treating ADRs as operational guidance.** Example: an agent reading ADR-0028
    to understand how to load a session, instead of reading the policy doc. Why
    wrong: ADRs are decision records (why), not operational guidance (how); they
    may be stale relative to current code. Do instead: read the policy doc for
    operational guidance; ADRs are for understanding the historical decision.

### Skill routing

The policy doc includes a skill-routing section pointing agents to the relevant
repo-resident skills for this architecture. Agents invoke these skills before
working in the corresponding areas:

- **`/cqrs-event-sourcing`** — invoke before any work touching command/query
  separation, event sourcing, projections, or replay. Carries the canonical
  CQRS/ES patterns and the versioning-strategy guidance.
- **`/wild-bunch-dotnet-architecture`** — invoke before any .NET architecture
  work in this repo: `GameSession` boundaries, persistence shape, CQRS/read
  models, event-stream-plus-snapshot-cache state, database-boundary decisions.
- **`/wild-bunch-domain-modeling`** — invoke before DDD tactical modeling work:
  aggregates, `GameSession` boundaries, child components, domain events, `Apply`
  method purity.
- **`/ef-core`** — invoke before EF Core work: `DbContext` configuration,
  migrations, interceptors, value converters, query optimization. Relevant for
  the migration that adds the diary-day `SchemaVersion` column and for any
  future relational schema changes.
- **`/ddd`** — invoke before DDD tactical modeling: aggregates, aggregate roots,
  value objects, domain events, domain services, strongly-typed IDs.
- **`/wild-bunch-project-doctrine`** — invoke before any repo-sensitive work.
  Routes to this policy doc for event-sourcing-integrity concerns.

The policy doc names these skills explicitly so an agent reading the policy is
directed to the right specialist skill rather than having to discover it.

### Policy enforcement

- The policy is referenced in `.agents/docs/architecture-guardrails.md` under the
  Event Sourcing section, pointing to the full policy document.
- The `wild-bunch-project-doctrine` skill's reference docs are updated to route
  persistence/event-sourcing work through this policy.
- A guardrail test asserts that `RehydrateFromEvents` + projectors produce state
  equivalent to the command path for all persisted state (see Part 2e).
- The code review guide (`.agents/docs/guides/code-review-guide.md`) is updated
  to include event-sourcing-integrity checks: replayability of new persisted
  state, projector existence for new projections, version bumps for event shape
  changes, chart-staleness for flow changes.

## Part 1: Make Event Sourcing Real

### 1a: `RehydrateFromEvents` as production load path

**Current state:** `EfGameSessionRepository.LoadStoreAsync` loads the snapshot
(components) and replays only post-snapshot events via `ApplyCommittedEvents`.
`RehydrateFromEvents` is test-only.

**Change:** The repository gains a load mode that uses `RehydrateFromEvents` to
reconstruct the aggregate from the full event stream, then rebuilds projections
from events via projectors. The snapshot load path remains as a fast path — when
the snapshot is present and its version is current, the snapshot load is used;
when the snapshot is missing, corrupted, or version-stale, the full replay path
is used. This makes the snapshot a true shortcut cache: the system works without
it, and it's used only when valid.

**Implementation:**
- `EfGameSessionRepository` gains a `LoadFromEventsAsync` method that fetches all
  stored events, upcasts them (Part 2a), deserializes them, and calls
  `GameSession.RehydrateFromEvents(id, world, events)`.
- **World reconstruction:** The world is needed by `RehydrateFromEvents` but is
  not stored in events (it's an external reference per the method's doc comment).
  On the full-replay path, the world is reconstructed from the `WorldGenerated`
  event's `WorldSnapshot` via `WorldSnapshot.ToDomain()`. This round-trip must
  be verified (Open Question 1). If the snapshot is present and its
  `ComponentVersion` is current, the world MAY be read from the `World` component
  as a cache — but the full-replay path must not *require* the snapshot, so the
  `WorldGenerated`-based reconstruction is the canonical path. The snapshot read
  is an optimization, not a dependency.
- **Path selection (fast vs. full replay):** The decision is per-component, not
  per-snapshot. For each component, if the stored `ComponentVersion` matches
  `ProjectionVersions.ForComponent(componentName)`, use the stored JSON (fast
  path). If any component's version is stale, or if any component row is missing,
  use full replay for the whole session. This is a single check: if any
  component is stale, the snapshot is not used as a fast path. The granularity is
  "all components current → fast path; any component stale → full replay." There
  is no per-component mixed path (that would complicate the load logic for
  marginal benefit).
- The existing snapshot load path (`LoadStoreAsync`) remains as the fast path.

**What this does NOT change:** The write path (`StoreAsync`) is unchanged — it
still writes both events and snapshot. The snapshot is still written on every
save. The difference is that the snapshot is no longer *required* to load.

### 1b: `TravelDiaryDayProjector`

**Current state:** Diary days (`TravelDiaryDayState`) are created inside
`JourneyLoop` as a command-path side effect (`AppendTravelDiaryDay` /
`PersistLatestTravelDiaryDay`). No `Apply` method creates them. No projector
rebuilds them. `RehydrateFromEvents` produces empty `TravelDiaryDays`.

**Change:** A new `TravelDiaryDayProjector` in `WildBunch.Application.Projections`
derives `IReadOnlyList<TravelDiaryDayState>` from the event stream. It is a pure
function over events — no aggregate mutation, no runtime context.

**What the projector needs to track:**
- **Running resource state** (health, wallet, ammo, heat) across the full event
  stream, because the "starting resources" for each journey day depend on the
  player's state at `JourneyStarted`, which depends on all pre-journey events
  (`GameStarted`, `StoreItemPurchased`, etc.).
  - Health: initial from `GameStarted.StartingHealth`, additive from
    `TravelDayAdvanced.HealthDelta`, absolute from
    `JourneyEncounterResolved.PlayerHealth`.
  - Wallet: absolute from `TrailEventApplied.WalletCash` and
    `JourneyEncounterResolved.WalletCash`; also affected by `StoreItemPurchased`
    and `SheriffTurnInSettled` (pre-journey).
  - Heat: absolute `PursuitHeat` on `JourneyStarted`, `TravelDayAdvanced`,
    `TrailEventApplied`, `JourneyEncounterResolved`.
  - Ammo: additive `AmmoSpent` on `JourneyEncounterResolved`; initial and
    store-affected ammo tracked from pre-journey events.
- **Journey-internal state** from `JourneySnapshot` (absolute on every journey
  event): `FoodRemaining`, `HorseFeedRemaining`, `AvailableCanteenCharges`,
  `HorseState`, `TravelMode`, `RemainingRideDayDistance`, `RemainingDays`,
  `DelayDays`, `RouteProfile`, `OpeningNarration`, `Warnings`,
  `PendingEncounter`.
- **Day boundaries:** `TravelDayAdvanced` signals a day completed; the projector
  creates a `TravelDiaryDayState` for that day using the tracked starting state
  (captured at the beginning of the day) and current resources (at the
  `TravelDayAdvanced` event).
- **Encounter updates:** `TrailEventApplied` and `JourneyEncounterResolved` update
  the latest diary day's trail event / encounter resolution fields.
- **Journey completion:** `JourneyCompleted` creates the final diary day if the
  last `TravelDayAdvanced` didn't already cover it.
- **Narrative entries:** built from `DiaryMessage`, `HorseLostMessage`, and
  `AdditionalDiaryMessages` on each event.

**Events carry enough information.** Verified by reading every journey event
type (`JourneyStarted`, `TravelDayAdvanced`, `TrailEventApplied`,
`JourneyEncounterResolved`, `JourneyCompleted`, `JourneyArrivalAcknowledged`)
and the `TravelDiaryDayFactory.Create` inputs. The factory needs
`journeySnapshot`, `startingState`, `currentResources`, `trailEvent`,
`pendingEncounter`, `encounterResolution`, and `entries` — all derivable from
the events listed above.

**Implementation:**
- New `TravelDiaryDayProjector : IDomainEventProjector<IReadOnlyList<TravelDiaryDayState>>`
  in `src/WildBunch.Application/Projections/`.
- The projector processes ALL events (not just journey events) to track running
  resource state. Non-journey events update the running state; journey events
  both update the running state and create/update diary days.
- The projector uses `TravelDiaryDayFactory.Create` (made accessible or
  duplicated) to build each `TravelDiaryDayState` from the tracked state.
- A parity test proves the projector's output matches the command path's
  `TravelDiaryDays` for a full journey cycle (start → advance days → resolve
  encounters → complete → acknowledge arrival).

**What this does NOT change:** The command path (`JourneyLoop`) still creates
diary days as a side effect during live play. The projector is the read-path /
rebuild-path equivalent. Both must produce the same output (parity test). In a
future cleanup, the command path's diary-day creation could be removed in favor
of projecting on demand, but that is not required for this design — the
projector's existence is what makes diary days rebuildable.

### 1c: Verify all other state is replayable

The `TravelReplayEqualityTests` already prove aggregate state parity for journey
events. `EventSourcingEndToEndTests` proves snapshot load == event replay for
purchase flow. The remaining risk is any state that's persisted but not set by
`Apply` and not rebuildable by a projector.

**Audit:** Review every component in `GameSessionComponentNames` and every field
on `GameSessionEntity` to confirm each is either (a) set by an `Apply` method
from event fields, or (b) derivable by a projector from the event stream. Any
that fail this check are violations to fix as part of Part 1.

**Known non-violations (verified):**
- `Player`, `World`, `CaseFile`, `Clock`, `PursuitState`, `GameDifficulty`,
  `SaltSource`, `GameEntropy` — all set by `Apply` methods from event fields.
- `CurrentActionContext` — set by `Apply(TownActionContextEntered)`.
- `PendingDevTravelOverride` — set by `Apply(DevTravelOverrideForced/Cleared/Consumed)`.
- `PendingDevSaloonOverride` — set by `Apply(DevSaloonOverrideForced/Cleared/Consumed)`.
- `DevLayoutSalts` — set by `Apply(DevLayoutSaltsForced)`.
- `UnrelatedCriminalLedger` — rebuilt from `CaseFileGenerated` + gang roster;
  `RestoreBountyLoopState` handles snapshot restore, and `Apply(SheriffTurnInSettled)`
  / `Apply(UnrelatedCriminalTurnInSettled)` handle event replay.
- `CompletedJourneyHistory` — set by `Apply(JourneyArrivalAcknowledged)`.
- `WantedSuspectPresenceLedger` — set by `Apply` methods for bounty/saloon events.
- `Journey` — set by `Apply(JourneyStarted/TravelDayAdvanced/TrailEventApplied/...)`.
- `SeedCode` — set by `Apply(GameStarted)` / `Apply(PlayerSetupCompleted)`.
- `Status` — set by `Apply(GameStarted)` / `Apply(PlaythroughArchived)`.
- `StartFlowPhase` — derived from event stream (already handled in
  `DeriveStartFlowPhase` and `Apply` methods).

**Known violation:**
- `TravelDiaryDays` — NOT set by any `Apply` method. Fixed by Part 1b.

## Part 2: Schema Versioning

### 2a: Event upcaster registry

**Shape:** One registry (`PayloadUpcasterRegistry`) keyed by
`(PayloadKind, payloadType)` where `PayloadKind` is an enum: `Event`,
`Projection`. (See section 2b for why projections don't have upcasters — they
have rebuild instead. The `Projection` kind exists in the enum for version-check
uniformity but has no upcaster chain.)

**Marker interfaces:**
```csharp
internal interface IPayloadUpcaster
{
    string PayloadType { get; }
    int FromVersion { get; }      // transforms FromVersion -> FromVersion + 1
    string Upcast(string payloadJson);
}

internal interface IEventUpcaster : IPayloadUpcaster { }
```

`IEventUpcaster` is the marker for DI filtering. Upcasters produce `JsonNode`
internally (per the agreed `JsonNode` choice) but the interface takes/returns
`string` for the pipeline boundary — the `JsonNode` parse/serialize happens
inside the upcaster.

**Version derivation:**
```csharp
public int CurrentVersion(string payloadType)
    => _upcasters.TryGetValue(payloadType, out var chain)
        ? chain.Keys.Max() + 1   // highest FromVersion + 1
        : 1;                      // no upcasters -> still at v1
```

To ship `GameStarted` at v3, register two upcasters (`FromVersion=1`,
`FromVersion=2`). There is no other API to declare a version. Version bumping IS
upcaster writing.

**Chain validation (startup, belt-and-braces):** The registry constructor
validates that each type's upcasters form a contiguous chain from v1 to
currentVersion. Non-contiguous chains throw at startup. This catches the edge
case where a future refactor breaks the derivation — unreachable under normal
operation (the derivation itself makes "version bump without upcaster"
impossible), but fails closed if the invariant is violated.

**Upcast method:**
```csharp
public string Upcast(string payloadType, int storedVersion, string payloadJson)
{
    var current = CurrentVersion(payloadType);
    if (storedVersion > current)
        throw new InvalidOperationException(
            $"{payloadType} stored at v{storedVersion} but current code " +
            $"supports up to v{current}. Code is older than the data.");
    if (storedVersion == current)
        return payloadJson;  // no upcast needed
    // ... run chain from storedVersion to current ...
    var version = storedVersion;
    var json = payloadJson;
    while (version < current)
    {
        if (!chain.TryGetValue(version, out var upcaster))
            throw new InvalidOperationException(
                $"No {payloadType} upcaster for v{version} -> v{version + 1}.");
        json = upcaster.Upcast(json);
        version++;
    }
    return json;
}
```

**Unknown type throws:** If `payloadType` is not in the registry and
`storedVersion != 1`, the load fails — "payload type not registered." There is
no passthrough branch for unknown types. Every payload type is known to the
loader; an unknown type is a bug.

**Explicit registration:** Upcasters are registered via an explicit
`AddEventUpcasters` call in DI, not by assembly scanning. A build-time test
asserts every `IEventUpcaster` in the assembly is referenced by the registration
call. No silent missed upcasters.

### 2b: Projection version columns + rebuild-on-mismatch

**Projections don't get upcasters.** Projections are derived state — when the
stored version doesn't match current, the projection is dropped and rebuilt from
the event stream via the appropriate projector. The version column on projections
is a "is this current?" check, not an "upcast from here" marker.

**Version columns:**
- `StoredEventEntity.SchemaVersion` — already exists, stamped v1 today. Used by
  the event upcaster registry.
- `GameSessionComponentEntity.ComponentVersion` — already exists, stamped v1
  today. Used as the projection version check for components.
- `GameSessionDiaryDayEntity.SchemaVersion` — **new column** (EF Core migration,
  existing rows defaulted to v1). Used as the projection version check for diary
  days.

**Rebuild-on-mismatch logic (in `PersistedPayloadLoader`):**
- When loading a component: read `ComponentVersion`. If it matches
  `CurrentProjectionVersion(componentName)`, use the stored JSON. If it doesn't
  match, discard the stored JSON and rebuild the component from the event stream
  via the appropriate projector (or by replaying events through
  `RehydrateFromEvents` and extracting the component from the aggregate).
- When loading diary days: read `SchemaVersion` on each row. If any row's version
  doesn't match current, discard all diary-day rows for that session and rebuild
  via `TravelDiaryDayProjector`.
- The rebuild uses the full event stream (upcasted per Part 2a).

**Current projection version derivation:** For projections, the current version
is a per-projection constant declared in a `ProjectionVersions` static class
(hand-edited, since projections don't have upcasters to derive from). Bumping a
projection version is a code change: update the constant, and the rebuild logic
triggers on next load. A build-time test asserts that every projection type has
a version declared in `ProjectionVersions`.

**Why projections use a hand-edited version while events don't:** This is not a
contradiction of the "no hand-edited version registry" principle (Design
Principle 4, anti-pattern #7). That principle applies to *events*, where the
risk is "version bumped without an upcaster" — deriving event versions from
upcaster count makes that failure impossible. Projections don't have upcasters
(they're rebuilt, not upcasted), so there's no equivalent failure mode to
prevent by derivation. A hand-edited `ProjectionVersions` constant that doesn't
match reality causes a rebuild on every load (wasteful but correct) or no
rebuild when one was needed (caught by the projection rebuild parity test in
Part 2e). The failure modes are different, so the enforcement mechanisms differ.

### 2c: Load funnel (`PersistedPayloadLoader`)

**Shape:**
```csharp
internal sealed class PersistedPayloadLoader
{
    private readonly PayloadUpcasterRegistry _eventUpcasters;
    private readonly GameSessionJsonSerializer _serializer;
    // ... projection rebuild dependencies ...

    // Events: upcast, deserialize.
    public IDomainEvent LoadEvent(StoredEventEntity stored)
    {
        var json = _eventUpcasters.Upcast(
            stored.EventType, stored.SchemaVersion, stored.PayloadJson);
        return _serializer.DeserializeEvent(stored.EventType, json);
    }

    // Components: version-check, rebuild if stale, deserialize.
    public string? LoadComponentPayload(
        IReadOnlyDictionary<string, GameSessionComponentEntity> components,
        string componentName,
        IReadOnlyList<IDomainEvent> events)
    {
        if (!components.TryGetValue(componentName, out var entity))
            return null;
        if (entity.ComponentVersion == ProjectionVersions.ForComponent(componentName))
            return entity.PayloadJson;  // current, use as-is
        // Stale: rebuild from events
        return RebuildComponent(componentName, events);
    }

    // Diary days: version-check, rebuild if stale, deserialize.
    public IReadOnlyList<TravelDiaryDayState> LoadDiaryDays(
        IReadOnlyList<GameSessionDiaryDayEntity> stored,
        IReadOnlyList<IDomainEvent> events)
    {
        if (stored.Count > 0 && stored.All(d => d.SchemaVersion == ProjectionVersions.DiaryDay))
            return stored.Select(d => _serializer.DeserializeTravelDiaryDay(d.PayloadJson)).ToArray();
        // Stale or empty: rebuild from events
        return _diaryDayProjector.Project(events);
    }
}
```

**Funnel enforcement:**
- `GameSessionJsonSerializer`'s deserialize methods (`DeserializeEvent`,
  `DeserializePlayer`, `DeserializeWorld`, etc.) become `internal`. No code
  outside `WildBunch.Persistence` can call them directly.
- `PersistedPayloadLoader` is the only surface that turns persisted rows into
  domain objects. It always runs the version check (events: upcast; projections:
  rebuild-or-use).
- `GameSessionComponentPayloads.GetRequiredPayload` / `GetOptionalPayload` route
  through `PersistedPayloadLoader` — no raw-payload accessor remains.
- The three load paths (`EfGameSessionRepository.LoadStoreAsync`,
  `EfGameSessionRepository.GetEventStreamAsync`,
  `GameSessionReadStoreLoader.LoadStoreAsync`) all call `PersistedPayloadLoader`
  instead of the serializer directly.

**Lifetime:** Singleton (registries and serializer are singletons; no per-request
state).

### 2d: Write side

**Per-type version stamping:** Replace `private const int SchemaVersion = 1` in
`EfGameSessionRepository` with calls to `PayloadUpcasterRegistry.CurrentVersion`
for events and `ProjectionVersions.ForComponent` / `ProjectionVersions.DiaryDay`
for projections.

**Event writes:**
```csharp
var eventType = e.GetType().Name;
_dbContext.StoredEvents.Add(new StoredEventEntity
{
    // ...
    EventType = eventType,
    PayloadJson = _serializer.SerializeEvent(e),
    SchemaVersion = _eventUpcasters.CurrentVersion(eventType)
});
```

**Component writes:**
```csharp
UpsertComponent(entity.Id, GameSessionComponentNames.Player,
    _serializer.SerializePlayer(session.Player), now,
    componentVersion: ProjectionVersions.ForComponent(GameSessionComponentNames.Player));
```

**Diary-day writes:** `SyncDiaryDaysAsync` stamps each row with
`ProjectionVersions.DiaryDay`.

**Writeback-on-next-save:** When a session is loaded with stale projections and
then saved, the write path writes the current-version projections (because it
always stamps current version). The stale rows are overwritten with current
shape. This is the convergence mechanism — active playthroughs migrate on their
next save cycle. No global migration sweep.

### 2e: Tests

**1. Event upcaster chain completeness (build-time):**
- Asserts every `IEventUpcaster` in the assembly is registered in the DI
  registration call.
- Asserts every event type that has persisted rows has a contiguous upcaster
  chain from v1 to currentVersion.
- Runs on every build via `dotnet test`.

**2. Event upcaster correctness:**
- For each upcaster: seed a v1 (or vN) payload, run the upcaster chain, assert
  the output matches the expected v(N+1) shape.
- The upcaster is the oracle — any future SQL backfill (if ever needed) would be
  tested against the upcaster's output.

**3. JSON shape snapshot tests (the residual gap closure):**
- For each event type: serialize a representative instance, assert the JSON shape
  (field names, structure) hasn't changed since the version was last bumped.
- Catches "shape changed without version bump" — the one failure mode the
  upcaster pipeline can't structurally prevent.
- If a shape change is intentional, the developer bumps the version (writes an
  upcaster) and updates the snapshot.

**4. Projection rebuild parity:**
- For `TravelDiaryDayProjector`: run a full journey cycle on the command path,
  collect events, project diary days from events, assert the projector's output
  matches the command path's `TravelDiaryDays` exactly.
- Same pattern for any other projection that gains a projector.

**5. Full replay equality (event sourcing integrity):**
- Extends `TravelReplayEqualityTests`: load a session via snapshot path, load via
  full replay (`RehydrateFromEvents` + projectors), assert ALL state matches
  including `TravelDiaryDays` (the current gap).
- This is the guardrail test referenced in Part 0 — it proves the event stream
  reconstructs the complete session, not just aggregate state.

**6. Migration chain test (relational schema):**
- Seeds a database at an old migration version, applies the full migration chain,
  asserts data survives. Catches relational migration regressions.
- Uses the PostgreSQL provider/storage test lane.

**7. Version mismatch behavior:**
- Asserts that loading a row with a future version throws (fail-closed).
- Asserts that loading a projection with a stale version triggers rebuild.
- Asserts that loading an event with a stale version triggers upcasting.

## Part 3: Branch Protection Prerequisite

### The enforcement guarantee depends on CI being a merge gate

The upcaster-pipeline enforcement is build-time + test-time: a developer who
tries to ship a breaking payload change without an upcaster gets a red build/test,
locally and in CI. But `main` currently has **no branch protection** — a red-CI
PR can still be merged. Without branch protection, the build-time enforcement is
advisory, not blocking.

### Required action (manual, by Harley)

Enable branch protection on `main` requiring the `ci` status checks before merge.
The exact command:

```bash
gh api repos/HarleyBartles/wild-bunch/rulesets \
  -X POST \
  -f name="Require CI on main" \
  -f target=branch \
  -f enforcement=active \
  -F conditions[ref_name][include][]="refs/heads/main" \
  -F rules[0][type]=required_status_checks \
  -F rules[0][parameters][required_status_checks][]="backend" \
  -F rules[0][parameters][required_status_checks][]="frontend" \
  -F rules[0][parameters][required_status_checks][]="index-mesh"
```

(Adjust check names to match the actual job names in `.github/workflows/ci.yml`:
`Backend (.NET build + tests)`, `Frontend (Vite tests + typecheck + build)`,
`Index mesh + plugin manifest`.)

This is a repo-settings change, not a code change. It is a hard prerequisite for
the versioning enforcement guarantee. The GitHub MCP connector does not expose
branch-protection tools, so this must be done manually.

## Implementation Order

1. **Part 0:** Write `event-sourcing-integrity-policy.md` (policy rules, mermaid
   canonical-flow diagram with negative-constraint paths, negative constraints /
   common mistakes section, skill-routing section). Update
   `architecture-guardrails.md` to reference it. Update doctrine skill references
   to route persistence/event-sourcing work through the policy. Update
   `.agents/docs/guides/code-review-guide.md` with event-sourcing-integrity
   review checks (replayability, projector existence, version bumps,
   chart-staleness).
2. **Part 1b:** Build `TravelDiaryDayProjector` + parity test. (This is the
   hardest piece — the projector must track running resource state and create
   diary days at the right event boundaries.)
3. **Part 1c:** Audit all persisted state for replayability. Fix any violations
   found.
4. **Part 1a:** Wire `RehydrateFromEvents` as a production load path in
   `EfGameSessionRepository`. Add full replay equality test (Part 2e test 5).
5. **Part 2a:** Build `PayloadUpcasterRegistry` + `IEventUpcaster` interface +
   explicit DI registration + chain validation + build-time completeness test.
6. **Part 2b:** Add `SchemaVersion` column to `GameSessionDiaryDayEntity` (EF
   migration). Add `ProjectionVersions` static class. Wire rebuild-on-mismatch
   logic.
7. **Part 2c:** Build `PersistedPayloadLoader`. Make serializer deserialize
   methods internal. Refactor three load paths to use the loader. Replace
   `GameSessionComponentPayloads` accessors to route through the loader.
8. **Part 2d:** Replace `const int SchemaVersion = 1` with per-type version
   stamping. Update `UpsertComponent` / `SyncDiaryDaysAsync` / event append to
   stamp current versions.
9. **Part 2e:** Write all tests (upcaster correctness, JSON shape snapshots,
   projection rebuild parity, version mismatch behavior, migration chain).
10. **Part 3:** Harley enables branch protection on `main`.

## Open Questions Carried Forward

1. **World reconstruction on full-replay path.** `RehydrateFromEvents` needs the
   `DomainWorld` as an external input. On the snapshot fast-path, the world comes
   from the `World` component. On the full-replay path (no snapshot), the world
   must be reconstructed from `WorldGenerated` event's `WorldSnapshot`. This
   needs verification that `WorldSnapshot.FromDomain` / `ToDomain` round-trips
   correctly. If it doesn't, Part 1a includes fixing the world snapshot
   round-trip.

2. **`TravelDiaryDayFactory` accessibility.** The factory is `internal` in
   `WildBunch.Domain.Travel`. The projector in `WildBunch.Application` may need
   it to be `internal` at the assembly level (InternalsVisibleTo) or the factory
   logic is duplicated in the projector. Prefer making the factory accessible
   rather than duplicating.

3. **Running resource state tracking in the projector.** The projector needs to
   track health, wallet, ammo, and heat across non-journey events
   (`StoreItemPurchased`, `SheriffTurnInSettled`, etc.). The exact set of
   events that affect these resources needs enumeration during implementation.
   The `Apply` methods are the reference — every event that has an `Apply` method
   affecting `Player.Health`, `Player.Wallet`, `PursuitState.Heat`, or ammo
   needs to be handled by the projector's resource tracker.

4. **Projection version declaration shape.** `ProjectionVersions` is hand-edited
   (projections don't have upcasters to derive from). The build-time test
   asserts every projection type has a version declared. The exact list of
   projection types (component names + diary days) needs to be enumerated from
   `GameSessionComponentNames` + the diary-day entity.

## Handoff Confidence

**8/10.** The spec is ready for planning. The contract is concrete: file names,
type names, interface shapes, version-derivation logic, load-funnel enforcement,
test categories, and implementation order are all specified. Source facts have
been verified against the live repo.

The two-point deduction is for the open questions, all of which are
implementation-time verifications rather than design gaps:
- Open Question 1 (world round-trip) could expand Plan A scope if the round-trip
  is broken — but the fix is bounded (fix the round-trip, not redesign the load
  path).
- Open Question 3 (resource-tracking event set) is enumeration work, not design
  work — the `Apply` methods are the reference.

## Handoff Contract Points

**Plan A (Part 0 + Part 1c) — Policy + audit:**
- New file: `.agents/docs/event-sourcing-integrity-policy.md` (policy rules,
  mermaid chart, negative constraints, skill routing)
- Modified: `.agents/docs/architecture-guardrails.md` (reference new policy)
- Modified: `.agents/docs/guides/code-review-guide.md` (ES-integrity review checks)
- Modified: `wild-bunch-project-doctrine` skill references (route to new policy)
- Audit: review every component in `GameSessionComponentNames` and every field on
  `GameSessionEntity` for replayability. Known violation: `TravelDiaryDays`. Any
  additional violations found are added to Plan B scope.
- Cardinality: 1 new policy doc, 3 modified docs/skills, 1 audit (no code changes)

**Plan B (Part 1b + Part 1a) — Make event sourcing real:**
- New file: `src/WildBunch.Application/Projections/TravelDiaryDayProjector.cs`
  implementing `IDomainEventProjector<IReadOnlyList<TravelDiaryDayState>>`
- Modified: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`
  (add `LoadFromEventsAsync`, wire fast-path vs. full-replay selection)
- Test: parity test proving projector output == command-path `TravelDiaryDays`
- Test: full replay equality test (snapshot load == `RehydrateFromEvents` +
  projectors for ALL state including `TravelDiaryDays`) — this is the completion
  gate for Plan B
- Cardinality: 1 new projector, 1 new load method, 2 tests (+ any violations
  found in Plan A's audit)

**Plan C (Part 2) — Schema versioning:**
- New file: `PayloadUpcasterRegistry` + `IEventUpcaster` / `IPayloadUpcaster`
  interfaces in `WildBunch.Persistence`
- New file: `ProjectionVersions` static class in `WildBunch.Persistence`
- New file: `PersistedPayloadLoader` in `WildBunch.Persistence`
- New EF Core migration: add `SchemaVersion` column to `GameSessionDiaryDayEntity`
  (existing rows defaulted to v1)
- Modified: `EfGameSessionRepository.cs` (replace `const int SchemaVersion = 1`
  with per-type version stamping; route loads through `PersistedPayloadLoader`)
- Modified: `GameSessionReadStoreLoader.cs` (route loads through
  `PersistedPayloadLoader`)
- Modified: `GameSessionComponentPayloads` (route through `PersistedPayloadLoader`)
- Modified: `GameSessionJsonSerializer` (deserialize methods become `internal`)
- 7 test categories (see Part 2e)
- Cardinality: 4 new types, 1 migration, 3 modified load paths, 7 test categories

**Part 3 (manual, user-owned):**
- `gh api` command to enable branch protection on `main` (exact command in spec)
- CI job names: `backend`, `frontend`, `index-mesh` (verified against
  `.github/workflows/ci.yml`)
