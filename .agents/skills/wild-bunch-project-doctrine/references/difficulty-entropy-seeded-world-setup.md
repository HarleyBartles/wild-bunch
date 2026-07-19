# Difficulty, Entropy, and Seeded Setup Doctrine

This document preserves the product doctrine that [MARK-136](https://linear.app/harleys-workspace/issue/MARK-136/thread-wild-bunch-difficulty-entropy-and-seeded-setup-into-skills-and) must add to the Wild Bunch skill stack.

## Difficulty envelope

Difficulty values:

- `easy`
- `standard`
- `hard`
- `brutal`

Difficulty changes parameters behind stable rules. It does not create different rulebooks.

Examples:

- More or fewer false leads.
- More or fewer requirements to trigger discovery, confrontation, clue, or event flows.
- Luck tables leaning more good, neutral, or bad.
- Harder envelopes lowering confidence from incomplete evidence or increasing false lead pressure.

## Entropy envelope

Entropy/randomness values:

- `boring`
- `classic`
- `adventurous`
- `wild`

`boring` is deterministic by seed and world state. The same action against the same world state should produce the same result. It is for tests, playtests, reproduction, and possibly later power users.

`classic` is normal play. Rolls, shuffles, and outcome selection are normally weighted, then shaped by difficulty and feature-specific weights.

`adventurous` increases surprise while preserving the same rules. Rare or unexpected events appear more often. Difficulty still leans the game, but adventurous entropy can sprinkle rare lucky or unlucky variance into the deck.

`wild` may bend ordinary assumptions while preserving game coherence. Examples: a lawman may move unusually fast between two towns because entropy says so; a random citizen may exist who looks exactly like Elzy Lay.

## Seeded world setup identity

Wild Bunch seeds are evolving world setup identity structures, not one flat random value.

- A world setup may be referenced by many UUIDs.
- One UUID must point to exactly one resolved world setup.
- Difficulty and entropy are part of seed identity.
- A seed under a different difficulty or entropy is a different world setup.
- Seed mapping may use compact selectors, profile bytes, weighted tables, or derived structures.
- Resolved world setup must expose explicit setup values after those mappings are applied.

World-start variables should be easy and boring to add to the seed/world setup mapping. Do not hide initial defaults in unrelated feature code.

## Starting inventory profile exemplars

Use both a boolean exemplar and a numeric exemplar:

```text
startingInventoryProfile:
  sparse:
    hasRope: false
    ammoRounds: 6
  practical:
    hasRope: false
    ammoRounds: 12
  generous:
    hasRope: true
    ammoRounds: 24
  absurd:
    hasRope: true
    ammoRounds: 99
```

Resolved world setup example:

```text
resolved world setup:
  difficulty: standard
  entropy: classic
  startingInventoryProfile: generous
  startingInventory:
    hasRope: true
    ammoRounds: 24
```

## Inventory modeling rule

Profile-derived setup values may be boolean, numeric, enum, or structured depending on gameplay meaning.

Rope is boolean because the meaningful starting-state question is "has rope?" Ammo is numeric because the meaningful starting-state question is "how many rounds?" Do not force everything through one generic stackable inventory abstraction.

## Placement target

Suggested canonical reference path:

`sources/first_party/skills/wild-bunch-project-doctrine/references/difficulty-entropy-seeded-world-setup.md`
