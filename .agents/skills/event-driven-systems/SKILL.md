---
name: event-driven-systems
description: Use when designing, reviewing, or operating event-driven systems with asynchronous communication.
metadata:
  source-id: event-driven-systems
  source-path: codex-marketplace/plugins/architecture-pack/skills/event-driven-systems/SKILL.md
  provenance-name: Event Driven Systems first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when designing, reviewing, or operating event-driven systems with asynchronous communication.
  use_when:
  - Use when designing asynchronous communication between services.
  - Use when choosing between choreography and orchestration.
  - Use when implementing sagas, idempotent handlers, or event schema versioning.
  do_not_use_when:
  - Do not use when the problem is better solved by synchronous RPC or a single monolith.
  related_skills:
  - event-sourcing
  - cqrs
  - ddd
license: MIT
---

# Event-Driven Systems

## Overview

Design systems where services react to immutable events through a broker, favoring loose coupling and eventual consistency.

## When to Use

- Designing asynchronous communication between services.
- Choosing between choreography and orchestration.
- Implementing sagas, idempotent handlers, or event schema versioning.
- Do not use when the problem is better solved by synchronous RPC or a single monolith.

## Core Pattern

1. Model events as immutable facts in past tense (for example, `OrderCreated`, `PaymentProcessed`) and include correlation IDs.
2. Distinguish events (facts for many subscribers) from commands (requests to a single target) and messages (transport envelope).
3. Choose a broker for the workload:
   - Kafka for high-throughput, replayable event streams.
   - RabbitMQ for flexible routing and request/reply patterns.
   - Managed queues when operational overhead should be minimized.
4. Coordinate workflows with choreography for simple, autonomous steps, and orchestration for complex, sequential processes.
5. Use sagas with compensating transactions when a long-lived workflow spans multiple services.
6. Make consumers idempotent, monitor lag, and version event schemas with a registry.

## Common Mistakes

- Using commands instead of events → name events in past tense and keep them small.
- Ignoring duplicate delivery → design idempotent handlers and track processed IDs.
- Assuming immediate consistency across services → accept eventual consistency and plan for failure.
- Coupling event schemas to internal service types → publish stable, versioned contracts.

Load `references/operational-guidance.md` for deeper coverage of brokers, patterns, and operations.
