---
name: database-design-patterns
description: Use when designing relational database schemas, normalizing data,
  choosing keys and constraints, or applying transactions, indexing, and
  partitioning patterns. Do not use when the task is engine-specific operations
  or NoSQL design.
metadata:
  source-id: database-design-patterns
  source-path: codex-marketplace/plugins/architecture-pack/skills/database-design-patterns/SKILL.md
  provenance-name: Database Design Patterns first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Engine-agnostic relational database design and optimization patterns
  use_when:
  - Use when designing or reviewing a relational database schema
  - Use when normalizing tables, choosing keys, or defining constraints
  - Use when modeling transactions, concurrency, views, or stored procedures
  - Use when planning indexes, partitions, or query tuning
  do_not_use_when:
  - Do not use for engine-specific operations; prefer database-engines
  - Do not use for NoSQL or document databases
license: MIT
---

# Database Design Patterns

Use this skill for engine-agnostic relational design: data modeling,
normalization, keys and constraints, views and programmability, transactions
and concurrency, indexing, and partitioning. The operational references adapt
the BCcampus *Database Design – 2nd Edition* textbook and supplement it with
citable indexing, query-tuning, and architecture references.

## Core topics

1. Start with `references/data-modeling.md` for conceptual, logical, and physical
   models.
2. Use `references/normalization.md` for normal forms and functional
   dependencies.
3. Read `references/keys-and-constraints.md` for keys, uniqueness, and domains.
4. Use `references/views-and-programmability.md` for views, procedures,
   functions, and triggers.
5. Read `references/transactions-and-concurrency.md` for isolation, locking,
   and deadlocks.
6. Use `references/indexing-and-query-tuning.md` for index strategy and plan
   fundamentals.
7. Read `references/partitioning-and-sharding.md` for scale-out basics.

## Common mistakes

- Designing for today only, ignoring query patterns and growth.
- Skipping normalization or over-normalizing without reason.
- Using natural keys that are not stable or not unique.
- Adding indexes without checking the query plan.
- Treating sharding as a fix for poor indexing or bad queries.

For source-grounded detail, read `assets/authority/CITATIONS.md` and
`assets/authority/source-map.yaml`.
