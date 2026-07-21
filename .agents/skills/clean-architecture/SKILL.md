---
name: clean-architecture
description: Use when designing testable, framework-independent applications with
  clear dependency rules. Do not use when the team is committed to a framework-centric
  stack and the cost of ports/adapters is unjustified.
metadata:
  source-id: clean-architecture
  source-path: sources/first_party/skills/clean-architecture/SKILL.md
  provenance-name: Clean Architecture first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when designing testable, framework-independent applications with clear
    dependency rules.
  use_when:
  - Use when designing testable, framework-independent applications with clear dependency
    rules
  do_not_use_when:
  - the team is committed to a framework-centric stack and the cost of ports/adapters
    is unjustified
license: MIT
---

# Clean Architecture

## Overview

Structure an application so that domain logic sits at the center and every dependency points inward; frameworks, UI, and databases are replaceable details.

## When to Use

- Domain rules are independent of delivery mechanisms and you need to delay technology choices.
- You want unit tests that run without databases, web servers, or frameworks.
- Do not use when the team is committed to a framework-centric stack and the cost of ports/adapters is unjustified.

## Core Pattern

Organize code into concentric layers:

1. **Entities** — enterprise-wide business rules and domain objects.
2. **Use cases** — application-specific workflows that orchestrate entities.
3. **Interface adapters** — controllers, presenters, and gateways that translate data for use cases.
4. **Frameworks and drivers** — databases, web frameworks, and external services.

The Dependency Rule: source code dependencies may only point inward. Inner layers define interfaces; outer layers implement them. Keep framework code in the outermost ring so a change to a database or UI library does not ripple into the domain.

## Common Mistakes

- Letting SQL, ORM annotations, or UI framework types leak into entities or use cases. Fix by moving those concerns to adapters and depending on abstractions.
- Calling frameworks directly from use cases. Fix by declaring interfaces in the inner layers and injecting implementations from the outer layers.
- Treating every small app as a fully layered system. A trivial CRUD wrapper rarely justifies the ceremony.
