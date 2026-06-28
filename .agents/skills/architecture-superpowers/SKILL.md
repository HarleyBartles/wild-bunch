---
name: architecture-superpowers
description: Use when shaping architecture decisions and review packets that need
  a compositional gate over Superpowers instead of a new doctrine surface.
metadata:
  source-id: architecture-superpowers
  source-path: sources/first_party/skills/architecture-superpowers/SKILL.md
  provenance-name: Architecture Superpowers first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when shaping architecture decisions and review packets that need a compositional
    gate over Superpowers instead of a new doctrine surface.
  use_when:
  - Use when shaping architecture decisions and review packets that need a compositional
    gate over Superpowers instead of a new doctrine surface.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---

# Architecture Superpowers

Use this skill when architecture-related work needs the smallest-applicable
routing gate over Superpowers instead of dumping doctrine into the wrapper
layer.

## Core job

Keep architecture guidance compositional and narrow:

1. start with `/using-superpowers` as the workflow-selection entrypoint;
2. use `/connector-safety` before any Linear or GitHub write or blocked-write
   recovery;
3. route to `architecture-pack:cqrs-event-sourcing` only when the problem has a
   named audit, temporal reconstruction, replay, projection, consistency, or
   complex-domain reason;
4. route to `architecture-pack:event-driven-architecture` when the work is
   about events, integration boundaries, orchestration, or eventual
   consistency;
5. route to `architecture-pack:database-design-patterns` when the work is about
   persistence, schema, indexing, partitioning, or replication;
6. route to the appropriate domain skill, such as `dotnet-kit:ddd` or the
   repo's domain-modeling skill, when the question is about invariants,
   aggregates, or domain boundaries;
7. use `linear-superpowers`, `github-superpowers`, `unslop-superpowers`, and
   `verification-before-completion` for workflow, proof, anti-slop, and final
   checks when those are the smallest fit.

## Guardrails

- Do not absorb DDD, Cortex, or architecture-pack doctrine into Superpowers.
- Do not make CQRS or Event Sourcing the default answer.
- Do not replace a narrower domain skill with architecture guidance when the
  narrower skill is the better fit.
- Keep this skill a router and gate over existing skills, not an expert skill
  that absorbs them.

## Usage rules

- Prefer simpler guidance or a narrower specialist skill when the architecture
  question does not need named replay, audit, projection, or distribution
  reasoning.
- Use `verification-before-completion` for repo-backed work that needs a
  durable final proof checkpoint.
- Treat `architecture-superpowers` as the final allowed pre-fork Superpowers
  wrapper projection.
- The pre-fork wrapper set is `linear-superpowers`, `github-superpowers`,
  `unslop-superpowers`, `verification-before-completion`, and
  `architecture-superpowers`.
