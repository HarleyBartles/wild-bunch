---
name: ddd
description: Use when modeling a complex business domain, defining bounded contexts,
  or choosing tactical DDD patterns. Do not use when the domain is simple CRUD or
  when a more specific skill already owns the abstraction.
metadata:
  source-id: ddd
  source-path: sources/first_party/skills/ddd/SKILL.md
  provenance-name: Ddd first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Domain-driven design for complex business domains
  use_when:
  - Use when modeling a complex business domain
  - Use when defining bounded contexts and ubiquitous language
  - Use when choosing tactical DDD patterns such as aggregates, entities, value objects,
    or domain events
  do_not_use_when:
  - Do not use when the domain is simple CRUD
  - Do not use when a more specific skill already owns the abstraction
license: MIT
---

# DDD

Apply domain-driven design when the problem space justifies modeling a
non-trivial business domain. Start by drawing a bounded context around a
specific business capability and cultivating a ubiquitous language shared by
domain experts and developers. Let the language shape the model, not the other
way around.

## When to Use

- Use when business rules, invariants, and workflows are complex and evolving.
- Use when multiple teams or systems need clearly separated conceptual models.
- Use when choosing strategic patterns such as bounded contexts, context maps,
  and core/support/generic subdomains.
- Use when choosing tactical patterns such as aggregates, entities, value
  objects, domain events, repositories, factories, services, and modules.

Do not use when the domain is a thin data layer or simple CRUD. Do not use when
a more focused skill, such as event-sourcing or clean-architecture, already
owns the abstraction.

## Core Pattern

1. Explore the domain with experts and capture the ubiquitous language.
2. Define bounded contexts where a single coherent model applies.
3. Map relationships between contexts (partnership, customer-supplier,
  conformist, anti-corruption layer, separate ways, shared kernel).
4. Classify subdomains into core, supporting, and generic.
5. Model inside a context with tactical building blocks aligned to the
  language.
6. Keep aggregates small, consistent, and transaction-safe.

## Common Mistakes

- Using DDD tactical patterns in a simple CRUD domain.
- Creating one giant bounded context for an entire enterprise.
- Confusing entities with value objects.
- Allowing aggregates to grow too large or cross transactional boundaries.
- Skipping the ubiquitous language and jumping straight to code patterns.

For source-grounded detail, read `references/operational-guidance.md`.
