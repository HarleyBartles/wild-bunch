# Architecture Hygiene

This document records recurring repository responsibilities for architecture cleanup work in Wild Bunch.

## Purpose

Keep routine worker posture boring, source-backed, and safe. The goal is to prevent architecture drift from becoming accepted background entropy.

## Source Documentation Hygiene

- Source-resident docs are not a second issue tracker.
- Do not copy GitHub issue bodies, dispatch YAML, worker reports, closeout notes, or future planning queues into `docs/` as source truth.
- `docs/` is for durable human-facing reference: current architecture, current operational setup, accepted design decisions, validation instructions, and stable constraints.
- `.agents/` is for durable agent-facing doctrine and routing rules, not issue replication.
- If planning content is promoted into source docs, rewrite it as stable reference material and strip out issue lifecycle language.
- If a worker is unsure whether a planning note belongs in source, leave it in the GitHub issue or comment thread and report the ambiguity instead of creating a source-controlled planning document.

## Recurring Responsibilities

- Preserve CQRS separation.
- Preserve DDD aggregate-root authority.
- Preserve SOLID and DRY without inventing broad frameworks.
- Preserve Onion dependency direction.
- Keep read repositories query-only.
- Keep command mutation flowing through `GameSession` or the established aggregate route.
- Keep repository-side unit-of-work discipline local and boring while only one command aggregate repository is coordinated.
- Introduce a formal application-level `IUnitOfWork` only when multiple command repositories or aggregates make the need concrete.
- Keep overloaded files and classes split by coherent responsibility before they become catch-alls.

## Aggregate Authority

- `GameSession` remains the live-play aggregate root.
- Do not move gameplay mutation out of `GameSession` just to satisfy abstraction goals.
- Session-owned travel topology work belongs to a separate source-backed decision, not this issue. In particular, the `TravelJourney` aggregate-root direction is tracked separately and should not be reintroduced here.
- Travel, Player, and similar session-owned domains may be cohesive internal aggregates under `GameSession`, but they are not separate aggregate roots by default.

## Mapper Ownership

- Travel, journey, and encounter DTO mapping should live in `TravelMapper`.
- `GameSessionMapper` should delegate travel mapping rather than duplicating that shape in parallel helper code.

## CQRS and Repository Boundaries

- Command-side repositories coordinate persistence for mutations.
- Read repositories project state and must not gain mutation behavior.
- Keep repository and read-loader behavior aligned with the current store shape without duplicating ownership logic.

## Persistence and Snapshot Codecs

- Persistence snapshot codecs belong in `WildBunch.Persistence`.
- Split codecs by coherent domain area when the serializer grows large.
- Keep serializer facades stable when they already serve as the public persistence entrypoint.
- Prefer small persistence-internal helpers over generic codec registries, plugin models, or reflection-driven frameworks.
- Repo-local database artifacts belong under repo-root `.local/` or another ignored local area, never under `src/`.

## Local PostgreSQL Safety

- Distinguish the persistent local development app database from temporary test-created databases.
- The persistent local development app database must have an explicit repo-local provisioning convention and should live under `.local/` or another ignored repo-local path.
- Temporary test-created databases may be created, migrated, exercised, and dropped by the harness that created them when they contain no production or user data.
- Destructive cleanup is allowed only for databases created specifically for a validation/test run or by an explicit reset command targeted at the persistent local development app database.
- Do not let test cleanup silently target the persistent app database.
- Do not rely on an unspecified machine-global PostgreSQL cluster or data directory when the repo can define its own local convention.

## Onion Dependency Direction

- Domain and Application should not depend on Persistence/EF implementation details.
- Persistence adapts the domain, not the other way around.
- Keep runtime session persistence JSON snapshot-oriented unless a later source-backed decision says otherwise.

## SOLID, DRY, and Overloaded Surfaces

- Favor one canonical formatter, mapper, or codec owner over duplicated versions that drift.
- Extract pure helpers when a class becomes too broad, but avoid splitting away the aggregate authority that belongs in `GameSession`.
- Leave temporary debug or cockpit surfaces lightweight; do not polish them for architecture credit alone.
- When cleanup touches a surface, leave a clear before/after ownership boundary that tests can prove.

## Hidden-State and Public Boundaries

- Hidden culprit truth remains internal.
- Hidden encounter state, rolls, salts, bribe internals, and generator internals must stay out of public DTO/API/read responses.
- Persist hidden state only where persistence requires it.
- Tests should prove the hidden boundary stays intact when persistence work changes.

## Connector / Tool Safety During Verification

- Read-only verification must stay read-only.
- Do not use mutation routes such as `create_tree`, `create_commit`, or other create/update/delete/add/remove primitives as inspection tools.
- If a read route is missing, use an approved read path instead of switching to a mutation primitive.

## Worker Issue-Closure Boundary

- Workers do not close GitHub issues.
- Workers return source-backed closeout evidence and recommendations only.
- Harley or GPT performs closure only after explicit latest-turn authorization.
- A worker dispatch can assess closeout readiness, but it cannot mutate issue state.

## False-Green Checks

- Confirm `GameSession` still owns live-play mutation.
- Confirm read repositories remain query-only.
- Confirm Domain/Application did not gain Persistence/EF dependencies.
- Confirm hidden internal state stays out of public DTO/API/read responses.
- Confirm a cleanup did not just move overload into another catch-all file or class.
- Confirm the change did not introduce a generic framework where a small helper would do.
- Confirm no unrelated gameplay, API, or schema change slipped in.
