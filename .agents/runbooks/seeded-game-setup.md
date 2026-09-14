# Seeded game setup runbook

Use this runbook when changing the UUID codec or the setup pipeline.

## Composition

1. Use `/seed-ownership` to classify each changed fact.
2. Use `/wild-bunch-domain-modeling` if the resolved fact changes a gameplay
   invariant, and `/wild-bunch-dotnet-architecture` if it crosses application
   or persistence boundaries.
3. Use `/test-driven-development` for the focused change and
   `/verification-before-completion` for the final claim.

## Repository route

- Seed codec and world factory: `src/WildBunch.GameContent/`.
- Setup orchestration and session creation: inspect live symbols named by
  [seed pipeline doctrine](../doctrine/game-content-seed-pipeline.md).
- Tests: `tests/WildBunch.GameContent.Tests/` plus the affected domain,
  application, or integration lane.

Update both codec directions for a seed-owned field. Store `SeedWorld` values
in tests and derive UUIDs with `CreateRepresentativeSeedCode`; do not freeze
encoded UUID fixtures. Run the focused tests, then the canonical gate in
[testing](testing.md).
