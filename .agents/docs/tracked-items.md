# Tracked Items

Open items that are documented in the codebase (skipped tests, comments) but not yet assigned to a plan or fixed.

## 1. TownStates parity gap (non-first-town starts) — RESOLVED

**Status:** Fixed in BUNCH-134 PR #149. The `GameSession` constructor no longer creates a `TownAggregate` during the setup phase. `_currentTown` is null until `Apply(GameStarted)` creates it with the real starting town. The previously-skipped parity test now passes.
