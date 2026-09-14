# Seeded game setup runbook

## When

Changing the UUID codec, game-setup pipeline, difficulty, entropy, starting
town, or setup-owned player facts.

## Required skills

- `/seed-ownership`
- `/wild-bunch-domain-modeling` when gameplay invariants change.
- `/wild-bunch-dotnet-architecture` when application or persistence boundaries change.
- `/test-driven-development`
- `/verification-before-completion`

## Composition

Classify every changed fact with `/seed-ownership`, add only the boundary skills
the change crosses, construct it test-first, then verify the resolved pipeline.

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

Ownership is explicit; round-trip and setup tests prove deterministic behavior;
the canonical gate in [testing](testing.md) passes.

## Prohibited combinations

- Do not place difficulty, entropy, starting town, or player setup inside seed
  identity.
- Do not freeze encoded UUID fixtures.
