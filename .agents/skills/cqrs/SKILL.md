---
name: cqrs
description: Use when separating read and write models in a distributed or high-scale system, or when event sourcing is under consideration. Do not use when simple CRUD or single-model consistency is sufficient.
metadata:
  source-id: cqrs
  source-path: sources/first_party/skills/cqrs/SKILL.md
  provenance-name: Cqrs first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when separating read and write models in a distributed or high-scale system, or when event sourcing is under consideration.
  use_when:
  - Use when separating read and write models in a distributed or high-scale system, or when event sourcing is under consideration.
  do_not_use_when:
  - Do not use when simple CRUD or single-model consistency is sufficient.
  related_skills:
  - event-sourcing
license: MIT
---

# CQRS

## Overview
CQRS separates read and write models so each side can evolve, scale, and be optimized independently. Commands mutate state; queries return shaped views.

## When to Use
- Use when read and write workloads differ in volume, shape, or scale.
- Use when multiple query models need different projections of the same data.
- Use when event sourcing is being considered as a companion persistence strategy.
- Do not use when a single consistent model and simple CRUD are sufficient.

## Core Pattern
1. Split commands (intent + validation) from queries (read-optimized DTOs/views).
2. Design separate models: a write model for business rules, read models for consumers.
3. Choose a synchronization strategy: same database with separate schemas, materialized views, or event-driven projections.
4. Decide consistency boundaries: eventual consistency is acceptable for most reads; strong consistency only where required.

## Common Mistakes
- Leaking the write model into read APIs or vice versa.
- Using CQRS for a simple domain to justify unnecessary architecture.
- Treating event sourcing as mandatory instead of optional.
