---
name: wild-bunch-dotnet-architecture
description: Use when Wild Bunch C# work changes GameSession command flows, application boundaries, typed event persistence, component snapshots, projections, or framework dependencies.
metadata:
  status: active
  scope: Wild Bunch .NET application and persistence architecture.
  use_when:
    - Use when a task crosses domain, application, persistence, API, or read-model boundaries.
  do_not_use_when:
    - Do not use for isolated gameplay-rule changes that stay inside the domain model.
---

# Wild Bunch .NET Architecture

## Owned decision

Decide which Wild Bunch .NET layer owns a change that crosses domain,
application, persistence, API, or projection boundaries.

## Method

1. Read the live paths and [architecture guardrails](../../doctrine/architecture-guardrails.md),
   plus [event-sourcing integrity](../../doctrine/event-sourcing-integrity.md)
   when events, snapshots, or projections are involved.
2. Separate domain invariants, use-case sequencing, persistence mechanics, API
   translation, and query derivation.
3. Return an ownership map, dependency direction, event/snapshot implications,
   and the replay or projection proof required.
4. Escalate the generic technique only to the focused portable skill that owns
   it: `ddd`, `cqrs`, `event-sourcing`, `event-driven-systems`,
   `clean-architecture`, or `dotnet`.

## Boundary

This skill does not restate the current architecture and does not own feature
implementation order. Use `wild-bunch-domain-modeling` for a domain-only
ownership decision; the composing runbook owns implementation and validation.
