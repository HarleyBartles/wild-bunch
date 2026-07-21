---
name: event-sourcing
description: Use when the system needs an audit log, temporal queries, or event-driven state reconstruction. Do not use when a simple relational model is enough or when strong immediate consistency is required.
metadata:
  source-id: event-sourcing
  source-path: sources/first_party/skills/event-sourcing/SKILL.md
  provenance-name: Event Sourcing first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when the system needs an audit log, temporal queries, or event-driven state reconstruction.
  use_when:
  - Use when the system needs an audit log, temporal queries, or event-driven state reconstruction.
  do_not_use_when:
  - Do not use when a simple relational model is enough or when strong immediate consistency is required.
  related_skills:
  - cqrs
license: MIT
---

# Event Sourcing

## Overview
Event sourcing persists the state of a system as a sequence of immutable events, making the event store the source of truth. Current state is reconstructed by replaying events.

## When to Use
- Use when an immutable audit trail and temporal querying are required.
- Use when reconstructing past states or diagnosing why a state changed is valuable.
- Use when event-driven integrations need a reliable history.
- Do not use when simple relational queries and strong immediate consistency are sufficient.

## Core Pattern
1. Record every state change as an append-only event with identity, type, payload, and timestamp.
2. Organize events into streams keyed by aggregate or entity.
3. Build projections (read models) by folding events into views; use snapshots to speed replay.
4. Version events carefully and evolve schemas without breaking historical streams.

## Common Mistakes
- Modifying or deleting events instead of appending compensating events.
- Reconstructing state on every read without projections or snapshots.
- Ignoring concurrency conflicts and idempotency.
