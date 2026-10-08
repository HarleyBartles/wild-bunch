# Seeded game setup playbook

## When

Changing the UUID codec, game-setup pipeline, difficulty, entropy, starting
town, or setup-owned player facts.

## Required capabilities

- Develop focused behavior tests and verify implementation evidence.

## Required repository-owned skills

- seed-ownership
- wild-bunch-domain-modeling (when gameplay invariants change)
- wild-bunch-dotnet-architecture (when application or persistence boundaries change)

## Unslop before work
Before work in this scope, follow the [unslop playbook](unslop.md) and its scoped profile selection. Consult the [decision-record playbook](decision-records.md) when work makes, changes, corrects, or materially removes a durable decision.

## Composition

1. Use `/seed-ownership` to classify every changed setup fact as seed, pressure,
   entropy, or player owned.
2. Add `/wild-bunch-domain-modeling` or `/wild-bunch-dotnet-architecture` only
   when the classified fact crosses those boundaries.
3. Use behavior-focused test development to change codec directions, setup flow, and
   the focused round-trip or integration lane together.
4. Use evidence-based result verification to prove the resolved seed world and
   setup behavior through the public pipeline and canonical gate.

## Doctrine and contracts

[Seed pipeline doctrine](../doctrine/game-content-seed-pipeline.md),
[entropy and seed doctrine](../doctrine/entropy-and-seed.md), and applicable
architecture or gameplay doctrine bind the change.

## Local commands and paths

- Codec and world factory: `src/WildBunch.GameContent/`
- Tests: `tests/WildBunch.GameContent.Tests/` plus affected domain, application,
  or integration lanes
- Update both codec directions for seed-owned fields. Store `SeedWorld` values
  in tests and derive UUIDs with `CreateRepresentativeSeedCode`.

## Evidence contract

- [ ] Every changed fact has one declared owner.
- [ ] Seed-owned fields round-trip through both codec directions.
- [ ] Tests derive UUIDs from `SeedWorld` rather than freezing encoded fixtures.
- [ ] Setup and affected domain/application/integration tests pass.

## Prohibited combinations

- Do not place difficulty, entropy, starting town, or player setup inside seed
  identity.
- Do not freeze encoded UUID fixtures.

## Runbook routing

- [Design](../runbooks/design.md)
- [Implementing](../runbooks/implementing.md)
- [Decision records](decision-records.md) - when setup ownership or deterministic setup rules change.
