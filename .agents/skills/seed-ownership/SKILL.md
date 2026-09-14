---
name: seed-ownership
description: Use when a Wild Bunch setup fact must be classified as seed-owned, pressure-owned, entropy-owned, or player-owned.
metadata:
  status: active
  scope: Wild Bunch seeded setup ownership decisions.
  use_when:
    - Use when adding or changing seed identity, difficulty, entropy, starting town, or starting loadout behavior.
  do_not_use_when:
    - Do not use for deterministic mechanics that do not cross a setup ownership boundary.
---

# Seed Ownership

## Owned decision

Classify one setup fact into exactly one owner: seed identity, difficulty
pressure, entropy policy, or player setup.

## Method

1. Read the live codec/setup source and [seed pipeline doctrine](../../doctrine/game-content-seed-pipeline.md).
2. Ask whether the fact identifies the generated world, modifies challenge,
   governs hidden variation, or records a player choice.
3. Return the owner, representation, forbidden owners, determinism invariant,
   and round-trip or setup proof required.

## Boundary

This skill decides ownership only. The seeded-game setup runbook owns file
order, implementation steps, and repository commands.
