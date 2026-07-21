---
name: wild-bunch-dotnet-architecture
description: Use when applying Wild Bunch .NET architecture guardrails for C#/.NET
  repo work touching GameSession live-play flows, application orchestration, infrastructure
  persistence, CQRS/read models, event-stream plus snapshot-cache state, database-table
  pressure, or framework leakage.
metadata:
  source-id: wild-bunch-dotnet-architecture
  source-path: sources/first_party/skills/wild-bunch-dotnet-architecture/SKILL.md
  provenance-name: Wild Bunch Dotnet Architecture first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when apply Wild Bunch .NET architecture guardrails for C#/.NET repo work
    involving domain ownership, GameSession Aggregate Root mutation paths, application
    orchestration, infrastructure persistence, CQRS/read models, JSON snapshot state,
    database-table pressure, or framework leakage. Use when designing, reviewing,
    dispatching, or verifying Wild Bunch code changes that could move rules out of
    the domain, confuse Aggregate Root boundaries with coordinators, normalize live
    session state too early, overuse CQRS/event sourcing, or confuse static content/read
    models with runtime aggregate state.
  use_when:
  - Use when apply Wild Bunch .NET architecture guardrails for C#/.NET repo work involving
    domain ownership, GameSession Aggregate Root mutation paths, application orchestration,
    infrastructure persistence, CQRS/read models, JSON snapshot state, database-table
    pressure, or framework leakage. Use when designing, reviewing, dispatching, or
    verifying Wild Bunch code changes that could move rules out of the domain, confuse
    Aggregate Root boundaries with coordinators, normalize live session state too
    early, overuse CQRS/event sourcing, or confuse static content/read models with
    runtime aggregate state.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---

# Wild Bunch .NET Architecture

Use this skill for structure decisions in the Wild Bunch C#/.NET codebase. Protect the domain first, keep application code as orchestration, and persist runtime state in a shape that matches the live session model.

## Core rules

- The domain owns rules and invariants.
- `GameSession` is the live-play Aggregate Root.
- External live-play commands mutate through `GameSession`.
- `GameSession` live-play command flows are event-sourced for migrated flows: commands
  produce typed domain events, apply them through `Apply`, and record uncommitted
  events while the repository appends typed events and keeps JSON component snapshots
  as cache.
- `GameSession` owns `BountyLoop`, `JourneyLoop`, `InvestigationLoop`, `StoreLoop`,
  and `ActionContextTracker`; child components receive narrow context and return
  outcomes or events-to-produce.
- Owned aggregate/component files under the root may own cohesive state, behavior, invariants, and lifecycle transitions when the DDD model calls for them.
- Policy/coordinator/resolver extraction is not aggregate progress unless a DDD aggregate/component owns responsibility.
- The application or use-case layer coordinates commands and queries but does not become the source of domain truth.
- Infrastructure owns persistence envelopes and JSON component snapshot cache; do not
  leak those details into the domain.
- Persist runtime session state as an event stream plus component snapshot cache;
  keep JSON snapshots as cache, not the conceptual source of history, unless the
  issue explicitly says otherwise.
- Do not introduce a separate event-store interface, broker, EventStoreDB, normalized
  live-session table split, or full-system event-sourcing expansion unless the issue
  explicitly scopes it.
- Do not normalize live session runtime state into many database tables too early.
- Static content, read models, projections, editor or admin needs, and cross-session data may justify tables later.
- CQRS is allowed as a read/write separation tool, not a mandate to split everything.
- Onion or clean architecture only matters when it protects domain rules from UI, database, or framework leakage.
- Seeded setup and randomness require explicit seams. Avoid unseeded random calls and hidden world-start defaults in domain or application code.
- Prefer deterministic seed plumbing and explicit setup objects when a task touches initial world state or variability controls.
- Use the installed `wild-bunch-project-doctrine` skill reference for seeded setup, difficulty, entropy, and world-start posture.

## Reference trigger

Read `references/dotnet-architecture.md` when the task needs persistence-shape, CQRS, database-boundary, layering, or verification detail beyond the core rules. Do not reread it after the architecture route is classified unless a concrete unresolved decision remains.
Consult the installed `wild-bunch-project-doctrine` skill reference when a task touches world-start identity, randomness, or setup-seam design.

## Boundary

This skill supplies Wild Bunch .NET architecture posture. It does not verify live repo state, dispatch workers, close issues, or replace source inspection. For current file state, inspect the repo. For worker/PR proof, use the relevant dispatch or GitHub verification route.
