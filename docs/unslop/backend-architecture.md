# Wild Bunch Backend Architecture Unslop Profile

Repo-wide drift-prevention profile for Wild Bunch backend work.

Use this profile before designing, implementing, reviewing, or dispatching backend work in Domain, Application, Persistence, API, projections, read models, event-sourced flows, backend tests, or backend architecture plans.

## Purpose

Keep backend work aligned with the architecture stack Wild Bunch has already selected.

This repo is not choosing between generic backend patterns on each issue. The selected posture is Onion/Clean-ish dependency direction, DDD aggregate authority, CQRS command/query separation, typed domain events, event-sourced migrated flows, safe projections, repository/UoW persistence, snapshots as cache/current-state load aid, and PostgreSQL-backed persistence.

Backend slop is architecture drift: using the right pattern names while moving authority, truth, mutation, persistence, or projection responsibilities to the wrong place.

## Quick Scan

Before backend work, answer these first:

- What live game state or player-facing output changes?
- Which layer owns the rule?
- Does mutation flow through `GameSession` or the current aggregate route?
- Is this flow migrated event-sourced, legacy direct, or explicitly transitional?
- Is the data player-known, hidden/internal, or developer-audit only?
- Does persistence preserve the event/snapshot/read-model distinction?
- What behavior, replay, projection, persistence, or safety test proves the change?

## Core Rule

The architecture stack is already selected.

Do not relitigate it inside ordinary feature work. Preserve the selected authority boundaries:

- Domain owns gameplay rules, aggregate behavior, value objects, and typed domain facts.
- Application orchestrates commands/queries, ports, projection contracts, and safe DTO mapping.
- Persistence stores and reconstructs aggregates, appends events through infrastructure envelopes, manages snapshots/cache, and owns EF/schema details.
- API maps transport to application use cases and response shapes. It does not own gameplay truth.
- Player-facing outputs are safe projections/read models, not raw aggregate internals, raw events, or full audit.

## Legacy And Migration Honesty

The repo is mid-migration. Some flows are event-sourced; some legacy/direct surfaces may remain.

Do not pretend transitional code is gone. Do not expand legacy surfaces casually. Do not call a flow event-sourced until command-path and replay-path behavior are materially equivalent.

When touching a legacy/direct flow:

- keep the legacy surface bounded
- do not add new obsolete log/read dependencies
- state whether the slice migrates the flow or leaves it transitional
- preserve behavior with characterization tests before structural changes
- do not route around selected architecture for convenience

When touching an already-migrated flow:

- do not add new direct mutation beside event application
- do not rebuild player-facing output from obsolete logs when typed events/projections are the selected source
- prove replay/projection behavior where state or read output changes

## Golden Shapes

### Aggregate authority

`GameSession` is the live-play aggregate root / aggregate route for current gameplay. It owns live state and command mutation paths.

Good shape:

- command method validates intent
- migrated command creates a typed domain event
- `Apply(...)` mutates aggregate state
- aggregate records uncommitted events
- Application handler loads aggregate, invokes command, stores through repository/UoW, and maps safe output

Bad shape:

- a service mutates player inventory, wallet, case file, or journey directly
- a handler patches domain state before or after aggregate calls
- a repository updates child state independently
- API/frontend DTOs become canonical gameplay state

### Application boundary

Application code coordinates use cases and maps safe output. It does not own domain legality.

Good shape:

- command handler loads aggregate, invokes aggregate behavior, stores through repository/UoW
- query handler reads safe projection/read-model shapes
- mapper translates domain/read-model state without inventing facts
- coherent sub-areas delegate to focused mappers such as travel/journey mapping

Bad shape:

- mapper computes gameplay outcomes
- command handler mutates aggregate children directly
- query handler scrapes obsolete mutable aggregate internals after projection-backed output exists
- DTO fields become the only place a game fact exists

### Persistence boundary

Persistence is an adapter. It stores and reconstructs; it does not own game rules.

Good shape:

- repository loads aggregate state/events
- repository appends typed events through infrastructure envelope
- repository stages snapshot/cache updates
- repository participates in one unit of work
- EF/schema/storage envelopes stay outside Domain

Bad shape:

- EF entities become domain models
- domain events contain persistence envelope fields
- repository commits independently when UoW owns commit
- runtime session state is split into tables without selected-posture or issue justification
- compatibility shims become new domain shape

### Projection boundary

Player-facing reads are safe projections/read models.

Good shape:

- Journal, HUD, diary, and case-file views derive from typed events or player-known aggregate state
- full audit stays developer/replay-only
- each projection names audience and hidden-truth policy

Bad shape:

- raw event stream exposed to normal game API
- raw payload JSON exposed
- hidden culprit truth leaks through helpful DTOs
- player-facing read model scrapes obsolete internals after a projection replacement exists

## Drift Modes To Stop

### Architecture-name compliance theater

Do not use selected architecture vocabulary while violating selected architecture responsibilities.

Stop:

- calling a flow event-sourced when command code mutates state beside event emission
- calling a query CQRS when it scrapes mutable aggregate internals instead of a projection/read model
- calling code Clean/Onion while Domain imports EF, API DTOs, HTTP concerns, or persistence envelopes
- calling code DDD while handlers/services mutate aggregate children directly
- calling something a projection while it leaks hidden culprit truth or raw audit details

Require proof of authority, data flow, and boundary conformance.

### Aggregate bypass

Stop gameplay mutation that routes around `GameSession` or the current aggregate route.

Use pure helpers for calculations. Use domain services only when logic truly crosses aggregate boundaries and does not belong to one root. Do not create broad services that become alternate mutation authorities.

### Event-sourcing drift

For migrated flows, typed events are the mutation path, not a side log.

Stop:

- direct mutation plus event recording
- generic event envelopes in Domain
- `string EventType` / `object Payload` domain events
- events that cannot replay state
- snapshot-only changes for migrated state without replay/event consideration

Require typed domain events, `Apply(...)` mutation, optimistic append, snapshot-as-cache posture, and replay/equivalence proof when behavior changes.

### CQRS drift

Commands express intent and mutate through aggregates. Queries read safe projection/read-model shapes and do not mutate state.

Stop:

- command handlers patching state around aggregate calls
- read models becoming hidden domain truth
- command DTOs becoming canonical fact storage
- full audit/raw events leaking into normal player-facing query routes

### Dependency-direction drift

Stop framework and adapter concerns moving inward.

Domain should not know about EF, HTTP, API DTOs, persistence envelopes, storage JSON, or database rows. Application should depend on ports/contracts, not persistence concretes. API should map transport, not decide gameplay legality.

### Projection/audit confusion

Stop mixing player-facing projection with technical audit.

Use these audience boundaries:

- player diary: curated authored record
- HUD feed: immediate player-facing notices
- case file: neutral evidence-shaped safe read model
- full audit: developer/replay surface only

### Hidden truth leakage

Stop any player-facing output from exposing hidden culprit truth or backend-only inference.

Watch for:

- `trueCulpritId`
- `isTrueCulprit`
- `linkedSuspectIds`
- `killerReleaseState`
- raw case internals
- backend-only ids that imply truth
- `confidence`, `known`, or `solved` states not exposed by the domain as player-known

### Runtime persistence shape drift

The selected posture is strongly typed aggregate state suitable for JSON snapshots plus typed events/projections where migrated.

Stop:

- tables for every live session child object
- EF shape driving domain shape
- compatibility tables for obsolete internal state
- schema changes justified by relational purity

Tables are valid for static content, read models, projections, editor/admin needs, cross-session/player/account data, or explicit query/access-pattern needs.

### Repository proliferation

Stop repositories for aggregate children.

Avoid `IInventoryRepository`, `IHorseRepository`, `IClueRepository`, `IWalletRepository`, and child-entity repositories under `GameSession`. Use the aggregate repository route or read-store/projection loaders as appropriate.

### Generic backend nouns

Stop replacing Wild Bunch domain language with generic game/backend abstractions.

Avoid `Supplies`, `Resources`, `Stats`, `EntityState`, `KnownFacts`, `QuestLog`, and generic reputation when the current game concept is concrete.

Prefer Wallet, Inventory, Food, Canteen charges, Horse feed, Horse and saddle, Healthy/Hungry/Exhausted/Lame/Dead, clues, Journal, Wanted posters, pursuit/lawman pressure, journey/trail-day loop.

### Randomness without a seam

Stop unseeded randomness and hidden setup defaults.

Use explicit setup objects, deterministic seed plumbing, central reusable scenario fixtures, and characterization tests before refactoring deterministic behavior.

### API contract vagueness

For nontrivial API changes, name consumer workflow, request shape, response shape, error behavior, sensitive-field policy, compatibility/default behavior, and idempotency/retry behavior when mutation is involved.

Do not rely on phrases like standard error, RESTful CRUD, easy to consume, or document the fields.

### Test confidence theater

Stop treating test existence as proof.

Prefer tests that prove behavior, negative paths, boundary cases, replay equivalence, projection safety, persistence/schema/serialization boundaries, deterministic seed behavior, and known regression shapes.

## Implementation Checklist

Before implementation:

- Restate the issue goal as observable backend state or behavior.
- Identify touched layers: Domain, Application, Persistence, API, projections, tests.
- Identify authoritative owner and selected architectural path.
- Classify the flow as migrated event-sourced, legacy direct, or migration slice.
- Identify player-known vs hidden/internal vs developer-audit data.
- Identify persistence shape: event stream, snapshot component, read model/projection, static content, or table.
- Identify the behavior/projection/replay/persistence tests needed.

During implementation:

- Keep domain rules in Domain.
- Keep Application orchestration-only.
- Keep EF, storage envelopes, migrations, and serializers in Persistence.
- Keep API as transport mapping.
- Do not add direct mutation beside event application for migrated flows.
- Do not add new obsolete log/read dependencies.
- Do not expose raw events or hidden truth to player-facing API.
- Prefer focused pure helpers over broad services.
- Prefer typed domain events and explicit projection cases over generic envelopes.
- Preserve deterministic seed/randomness seams.

Before review:

- Compare changed source against issue goal and selected architecture path.
- Run falsification checks for drift modes above.
- Search for forbidden recurrence: hidden truth fields, legacy log reads, new direct mutation paths, ad hoc services, generic event envelopes, child repositories.
- Run relevant validation commands.
- Add behavior tests for changed game rules.
- Add persistence/schema/serialization tests for persistence changes.
- Add projection safety tests for player-facing output changes.
- Add replay/equivalence tests for migrated event-sourced behavior changes.
- Report exact commands, outputs, branch, head SHA, changed files, caveats, and cleanup proof.

## Review Questions

Ask these before accepting backend work:

1. What source-of-truth state changed?
2. Which layer owns the rule?
3. Did mutation flow through `GameSession` or the current aggregate route?
4. Is the touched flow migrated event-sourced, legacy direct, or explicitly transitional?
5. If events are involved, are they typed domain facts and replayable?
6. If persistence changed, did it preserve event/snapshot/read-model distinctions?
7. If a table was added, what selected-repo access pattern justifies it?
8. If a mapper changed, did it translate rather than invent domain truth?
9. If an API changed, what is the explicit contract and hidden-truth policy?
10. If a projection changed, who is the audience and what truth is safe?
11. Did any service/repository/provider/manager route around aggregate/application/persistence boundaries?
12. Did tests prove behavior, negative cases, replay/projection equivalence, or persistence boundaries as relevant?
13. Does validation output support the claim, or only show tests ran?
14. Does the PR/update identify remaining legacy compatibility surfaces honestly?

## Acceptance Checks

This profile passes only if it would stop a worker from:

- using selected architecture names while violating selected architecture responsibilities
- moving gameplay mutation into ad hoc services
- routing around `GameSession` / aggregate authority
- recording events beside mutation and calling it event sourcing
- treating snapshots as the conceptual source of migrated history
- using queries/read models as hidden domain truth
- exposing raw events, hidden truth, or backend-only fields to player-facing API
- using DTOs/mappers as canonical game truth
- normalizing runtime session state away from the selected persistence posture
- adding repositories for aggregate children
- adding unseeded randomness or hidden setup defaults
- claiming test coverage without behavior proof
- calling backend work GREEN without issue-goal conformance and falsification checks
