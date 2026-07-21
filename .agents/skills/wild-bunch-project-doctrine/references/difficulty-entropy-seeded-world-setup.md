# Difficulty, Entropy, and Seeded World Setup

Inspect `src/WildBunch.GameContent/NewGame/` and the related domain enums before retaining setup claims. The current pipeline is:

```text
seed code -> SeedWorld -> DifficultyEnvelope -> EntropyPolicy
          -> MysteryTruthResolution -> ResolvedGameSetup
```

## Seed identity

- A UUID-shaped seed code deterministically decodes to a `SeedWorld` candidate map.
- Difficulty and entropy are not encoded in the seed. They are separate player-selected policies applied downstream.
- The same UUID resolves to the same `SeedWorld`; multiple UUIDs may resolve to the same world shape.
- The seed owns candidate world/map fields and seed defaults. It does not own the final starting town, difficulty, entropy, loadout, horse/saddle posture, or final cash.
- `StartingTownPolicy` accepts any town in the generated world and uses the first town as a safe default when the player supplies none. That default is not seed-authored.

## Difficulty

`GameDifficulty` currently defines `Standard`, `Easy`, `Challenging`, and `Brutal`.

`DifficultyEnvelope` applies pressure downstream of the seed. Current differences include starting cash and travel rules; `GameSetupResolver` also varies starting health. The current transitional loadout is `Standard` with horse and saddle for every difficulty. Do not describe future loadout, clue-pressure, or starting-town constraints as implemented.

## Entropy

`GameEntropy` defines `Boring`, `Classic`, `Adventurous`, and `Wild`.

- `Boring` uses a fixed seed-derived salt and a zero cash-bonus cap.
- `Classic`, `Adventurous`, and `Wild` use runtime salt with cash-bonus caps of 2, 5, and 8.
- `MysteryTruthResolver` currently keeps the seed world's culprit and accusation indices for every mode, then applies the entropy cash cap and salt policy.
- World generation receives entropy and salt after seed decoding. This can affect resolved output without changing seed identity.

Do not present planned salted culprit rerolls or wider variance as current behavior.

## Canonical home

This reference is repo-owned at `.agents/skills/wild-bunch-project-doctrine/references/difficulty-entropy-seeded-world-setup.md`.
