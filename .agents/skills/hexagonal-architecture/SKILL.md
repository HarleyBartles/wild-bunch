---
name: hexagonal-architecture
description: Use when isolating domain logic from frameworks, UI, and databases through
  ports and adapters. Do not use when the domain is trivial or the project is a thin
  framework wrapper.
metadata:
  source-id: hexagonal-architecture
  source-path: sources/first_party/skills/hexagonal-architecture/SKILL.md
  provenance-name: Hexagonal Architecture first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when isolating domain logic from frameworks, UI, and databases through
    ports and adapters.
  use_when:
  - Use when isolating domain logic from frameworks, UI, and databases through ports
    and adapters
  do_not_use_when:
  - the domain is trivial or the project is a thin framework wrapper
license: MIT
---

# Hexagonal Architecture

## Overview

Place domain logic at the center of the application and connect it to the outside world through explicit ports and adapters, so the core remains framework-agnostic.

## When to Use

- The application must stay independent of databases, web frameworks, messaging systems, and external APIs.
- You need to test business rules with fake or in-memory adapters instead of real infrastructure.
- You want to swap a primary actor such as a UI, CLI, or test harness, or a secondary actor such as a database or service, without touching domain code.
- Do not use when the domain is trivial or the project is a thin framework wrapper.

## Core Pattern

Define **ports** as interfaces the domain needs to interact with the outside world. Implement each port with one or more **adapters**:

- **Primary (driving) adapters** call the domain: REST controllers, CLI handlers, event consumers, and test fixtures.
- **Secondary (driven) adapters** are called by the domain: repositories, notification services, and external APIs.

The domain declares what it needs; adapters satisfy those needs. Dependencies cross the port boundary only in the direction that leaves the domain untouched.

## Common Mistakes

- Building adapters that bypass the ports and call domain objects directly. Fix by routing all outside interaction through the declared port interfaces.
- Allowing frameworks to leak into application or domain code. Keep framework-specific annotations, SDK types, and configuration in adapter implementations.
- Confusing hexagonal architecture with clean or onion architecture. All three isolate the domain; hexagonal emphasizes ports and adapters as the concrete integration mechanism, while clean adds a layered dependency rule and onion uses concentric layers with dependency inversion.
