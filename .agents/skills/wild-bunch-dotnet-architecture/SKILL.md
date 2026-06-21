---
name: wild-bunch-dotnet-architecture
description: Apply Wild Bunch .NET architecture guardrails for C#/.NET repo work involving domain ownership, GameSession Aggregate Root mutation paths, application orchestration, infrastructure persistence, CQRS/read models, JSON snapshot state, database-table pressure, or framework leakage. Use when designing, reviewing, dispatching, or verifying Wild Bunch code changes that could move rules out of the domain, confuse Aggregate Root boundaries with coordinators, normalize live session state too early, overuse CQRS/event sourcing, or confuse static content/read models with runtime aggregate state.
metadata:
  origin: first_party
  source_author: Harley Bartles
  source_license: MIT
  source_repo: https://github.com/HarleyBartles/agent-asset-marketplace
  source_path: sources/first_party/skills/wild-bunch-dotnet-architecture/SKILL.md
  content_mode: verbatim
---

# Wild Bunch .NET Architecture

Use this skill for structure decisions in the Wild Bunch C#/.NET codebase. Protect the domain first, keep application code as orchestration, and persist runtime state in a shape that matches the live session model.

## Core rules

- The domain owns rules and invariants.
- `GameSession` is the live-play Aggregate Root.
- External live-play commands mutate through `GameSession`.
- Owned aggregate/component files under the root may own cohesive state, behavior, invariants, and lifecycle transitions when the DDD model calls for them.
- Policy/coordinator/resolver extraction is not aggregate progress unless a DDD aggregate/component owns responsibility.
- The application or use-case layer coordinates commands and queries but does not become the source of domain truth.
- Infrastructure persists snapshots or read models and should not leak framework objects into the domain.
- Persist runtime session state as strongly typed aggregate state suitable for JSON snapshots unless the issue explicitly says otherwise.
- Do not normalize live session runtime state into many database tables too early.
- Static content, read models, projections, editor or admin needs, and cross-session data may justify tables later.
- CQRS is allowed as a read/write separation tool, not a mandate to split everything.
- Event-sourcing concepts may inform audit or replay thinking, but do not convert persistence to full event sourcing unless the issue scopes it.
- Onion or clean architecture only matters when it protects domain rules from UI, database, or framework leakage.
- Seeded setup and randomness require explicit seams. Avoid unseeded random calls and hidden world-start defaults in domain or application code.
- Prefer deterministic seed plumbing and explicit setup objects when a task touches initial world state or variability controls.
- Use the installed `wild-bunch-project-doctrine` skill reference for seeded setup, difficulty, entropy, and world-start posture.

## Reference trigger

Read `references/dotnet-architecture.md` when the task needs persistence-shape, CQRS, database-boundary, layering, or verification detail beyond the core rules. Do not reread it after the architecture route is classified unless a concrete unresolved decision remains.
Consult the installed `wild-bunch-project-doctrine` skill reference when a task touches world-start identity, randomness, or setup-seam design.

## Boundary

This skill supplies Wild Bunch .NET architecture posture. It does not verify live repo state, dispatch workers, close issues, or replace source inspection. For current file state, inspect the repo. For worker/PR proof, use the relevant dispatch or GitHub verification route.
