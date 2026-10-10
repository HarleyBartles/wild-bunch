# Apply horse death to player inventory

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** When the event stream establishes that the player's horse died during travel, the horse is no longer in player inventory after the command or a fresh replay. The saddle and other items remain, and a replacement horse can be purchased after arrival. Arrival refills an owned canteen and never creates one.

**Architecture:** `GameSession` remains the gameplay authority. The existing travel events record absolute `JourneySnapshot.HorseState`; when that recorded state is dead under the session's selected `TravelRules`, applying the event removes the owned Horse item. This applies identically on the command and replay paths that already share `SyncPlayerFromJourneySnapshot`. Do not add a second horse lifecycle store, event, snapshot shape, migration, or UI-derived fact. The dead condition remains in the journey event history and `HorseLostMessage`; current inventory represents ownership. Keep the travel snapshot's historical fact intact.

**Spec and row:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, PG-004; roadmap row 10. Row 09 is complete at `develop` merge `b4c4019152ac2ee77ca5da3bf3e214f7532345d9`.

**Execution Strategy:** Native inline execution. The command mutation, event-application/replay path, inventory semantics and persisted proof share the same domain boundary; one continuous TDD context is lower risk than repeated implementer handoffs. Use one fresh whole-branch review.

## Constraints

- A dead horse leaves inventory; the saddle remains. A living but lame horse remains owned, and its existing mounted-travel rule is unchanged.
- Do not change horse death thresholds, horse upkeep, travel distance, pacing, route delays, health loss, food starvation, store prices, catalogs or item availability.
- Keep the event stream authoritative. Do not add an event or migration when the current travel snapshot already records the horse state that determines death.
- Arrival refills only an existing canteen. Do not create a canteen for a player who has none.
- Inventory quantities never become negative. Preserve the existing rule that over-removal fails without changing quantity.
- Keep PG-004's assessment partial while Rifle acquisition and any other unimplemented product promises remain unresolved by the settled spec. Passing this slice does not certify all of PG-004.
- No user accounts, ownership model, multiple active playthroughs, browser synchronization, cloud, deployment, new vendors, weapon variants, medicine, or balance work.

## Review Focus

Check command and replay parity for each existing call to `SyncPlayerFromJourneySnapshot`; ensure only a dead Horse is removed, and Saddle, other inventory, wallet, journey progress and history remain intact. Verify a lame non-dead horse stays owned. Ensure owned-canteen refill and no-canteen arrival behavior are tested at the behavior owner. Confirm no event schema or migration was introduced, and that existing purchase and prosperity behavior remains unchanged.

---

### Task 1: Commit this successor plan and retire row 09

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Delete: `.agents/plans/2026-10-10-startup-operation-identity.md`
- Create: `.agents/plans/2026-10-10-horse-death-inventory.md`
- Modify: `Directory.Build.props`

**Interfaces:**
- Consumes: refreshed `origin/develop` at `b4c4019152ac2ee77ca5da3bf3e214f7532345d9`, merged PR #254 source `2041c6c39526477468ad893c07691e1f295333ac`, exact-head hosted gate `38078617129`, and develop push gate `38079003680`.
- Produces: row 09 marked done with exact PR/source/merge/gate evidence, retirement of the completed `.68` plan, and this committed `.69` row 10 plan.

- [ ] Reconfirm PR #254's merge commit, source head and both hosted gate conclusions against GitHub; preserve the unknown develop-push result for PR #252.
- [ ] Classify the complete `.68` plan against merged source and evidence, confirm ADR-0034/0039 and PG-001/002 remain at their authority owners, then retire the plan and stale roadmap link.
- [ ] Update row 09 to done and row 10 to executing; include PR #254, source and merge SHA, exact-head PR gate and develop push gate.
- [ ] Advance the single authored application version from `0.1.0-dev.68` to `0.1.0-dev.69` in `Directory.Build.props`; verify generated web identity through the owning build/check command.
- [ ] Commit the plan, successor retirement, roadmap evidence and version update before implementation. The normal hook validates the staged candidate without mutation.

### Task 2: Prove horse-death ownership and quantity boundaries

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/InventoryTests.cs`
- Modify: the owning PostgreSQL travel/replay test in `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs` or `tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs`, after checking which current scenario can express the behavior without a test-only production path.

**Interfaces:**
- Consumes: normal GameSession travel commands and the current typed event snapshots.
- Produces: behavior proof that death removes Horse ownership, preserves Saddle and other inventory through fresh event reconstruction, and over-removal cannot drive a stackable below zero.

- [ ] Add a falsifiable Domain behavior assertion to the real upkeep-death scenario: after its event is applied, inventory has no Horse and still has the Saddle; retain independent route-progress assertions without changing their expected behavior.
- [ ] Run that focused test against unchanged source and observe failure specifically because the dead horse remains owned.
- [ ] Add a negative Inventory behavior assertion: removing more units than owned fails and leaves the actual quantity unchanged; lawful removal of the final unit yields quantity zero and no negative item.
- [ ] Add PostgreSQL proof that a real travel death event saved through the repository reloads in a fresh scope with no owned Horse and the original Saddle and other items intact. Assert the event/history fact still records the travel outcome; do not compare two copies of the same mapper output.
- [ ] If another existing horse-death transition path can occur through the normal trail-event or encounter command, cover it with a deterministic reachable behavior scenario. Do not add a seed sweep, widen a developer override, or assert method/file wiring as a substitute.

### Task 3: Apply dead-horse ownership consistently and verify arrival resources

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: the focused Domain lifecycle tests identified in Task 2.
- Modify: the owning PostgreSQL tests identified in Task 2.
- Modify: `docs/features.md` PG-004 assessment/evidence only where the new behavior was actually demonstrated.

**Interfaces:**
- Consumes: absolute `TravelJourneySnapshot` state applied by `TravelDayAdvanced`, `TrailEventApplied` and `JourneyEncounterResolved` through `SyncPlayerFromJourneySnapshot`.
- Produces: one event-derived inventory transition that removes the Horse exactly when its recorded state meets the current profile's death rule, preserves the Saddle, and remains identical under replay. Arrival tests prove existing owned canteens refill and absent canteens stay absent.

- [ ] Implement the smallest aggregate change in the shared event-application path: if a recorded horse state is dead for this session's `TravelRules`, remove the Horse item if present; otherwise preserve existing live and lame state behavior.
- [ ] Keep horse-death narration sourced from the already recorded event data. Do not parse user-facing copy to decide death and do not erase or fabricate event history.
- [ ] Run the focused Domain tests and PostgreSQL fresh-context/replay test. Include one owned-empty-canteen arrival and one no-canteen arrival scenario if existing behavior coverage does not already prove both through the current event path.
- [ ] Verify the resulting player's inventory allows a legal horse replacement after the journey is completed and arrival acknowledged, using the existing store catalog and purchase command; do not permit purchase during an active or unacknowledged journey.
- [ ] Confirm current store price/stock matrix, duplicate equipment rejection, stackable purchase and other goods remain unchanged through existing behavior tests; do not duplicate those suites.
- [ ] Update PG-004 evidence without claiming Rifle acquisition or complete capability certification.

### Task 4: Review and deliver the bounded slice

**Files:**
- Review: the complete diff against ADR-0013, ADR-0028, PG-004, the backend architecture profile and scoped unslop guidance.
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` only to record actual delivery evidence when this slice's implementation PR is merged by its successor.

**Interfaces:**
- Consumes: committed `.69` plan, event-backed implementation, focused PostgreSQL tests, canonical gate and independent whole-branch review.
- Produces: reviewed `.69` PR to `develop`; row 10 remains open until all remaining store/inventory obligations are actually covered.

- [ ] Compare the actual diff with ADRs and PG-004; state that the existing store promise and lifecycle rules were corrected, not expanded, and update any durable decision only if implementation reveals a real architectural change.
- [ ] Run PostgreSQL ensure, focused Domain and integration tests, `dotnet tool restore`, the EF migration-list check (confirm there is no newly authored migration), the version identity check and canonical `py -3 tools/run.py ci --check`.
- [ ] Inspect the full branch and test mutations for strict CQRS, replay parity, no negative inventory, no dead horse ownership, lame-horse retention, no event-history rewrite, and no broadened vendor/weapon scope.
- [ ] Complete a fresh whole-branch review, correct any consequential findings with witnessed RED/GREEN tests, and commit with the check-only hook.
- [ ] Publish a PR against `develop`, verify the exact head and migration/data prerequisites, require the exact-head hosted canonical gate to pass, then merge under the active goal.
- [ ] In the next fresh successor worktree, record the actual source/merge and both gate results, then retire this completed `.69` plan.

## Acceptance

- A horse that dies during an event-backed travel transition is no longer in player inventory after command application or fresh PostgreSQL reconstruction.
- The Saddle and unrelated inventory remain; a lame but living horse remains owned.
- The player can buy a replacement Horse only after the journey reaches town and arrival is acknowledged.
- An owned empty canteen refills on town arrival; a player without a canteen still has none afterward.
- Inventory rejection prevents below-zero quantities without mutating the existing stack.
- The original typed event stream remains sufficient for exact reconstruction; this slice needs no new event or database migration.
- Existing price, stock, item-only purchase, unique-equipment, consumable-stack and travel-pacing behaviors remain intact.
- Local and hosted canonical gates pass on the exact PR source head before merge; the successor records the develop push gate.

## Explicit Exclusions

No new horse death thresholds, injury or recovery mechanics, item types, store prices, vendor identities, rifle acquisition, weapon variants, consumable classes, food-starvation rules, travel distance/delay changes, user ownership, deployment or infrastructure work.
