# Consolidate Town Purchasing into One Store Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Make the one Store in each town sell the existing prosperity-dependent union of goods without vendor identity in domain, API, or browser contracts.

**Architecture:** Keep the catalog and purchase rule in Domain, have Application select an offer by item kind, and keep API and React as transport/rendering adapters. The event remains the fact of an item purchase at a town for a quantity and price; it has no vendor field, so this change does not alter event or persistence schemas.

**Tech Stack:** C#/.NET Domain, Application and API; React/TypeScript; xUnit, Vitest/Testing Library, PostgreSQL-backed API integration tests; repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), [PG-004 feature matrix](../../docs/features.md#pg-004-buy-supplies-and-manage-player-resources), and [store/inventory boundaries](../investigations/stable-0.1.0/2026-10-07-store-and-inventory-boundaries.md).

**Execution Strategy:** `executing-plans` (Native inline) because the Domain catalog, Application/API contract, React request and their characterization fixtures are one coupled contract; inline execution preserves one integration context, while independent per-task reviews would add handoffs without separating releasable behavior.

## Global Constraints

- Start from merged `develop` commit `cfea9fb2f064f76257e86c8c1ebf19607ab54b8e` and deliver by PR to `develop`.
- Advance `Directory.Build.props` once from `0.1.0-dev.12` to `0.1.0-dev.13` in this slice.
- One town has one Store; the purchase request and offer response identify goods by `ItemKind`, never by vendor.
- Preserve the current offer union and prosperity-tier availability/prices; use the general-store HorseFeed price where the current catalog duplicates it.
- Do not add goods, tune prices, change inventory lifecycle rules, change game time, or reintroduce differentiated vendor services.
- Current offer contract is the list itself: each listed item is available for purchase. Remove the redundant `StoreOfferAvailability`, always-derived catalog `Available`, and vendor-provenance `SourceNote` fields instead of preserving states the current game never emits or consumes.
- Purchase legality remains in `GameSession`; commands still require an active playthrough, the current town, a listed item, a positive quantity, adequate funds, and any existing equipment-ownership rule.
- Preserve the event-backed `StoreItemPurchased` fact, replay, projections, migrations and schema history; no event or database migration is required because purchase events contain no vendor identity.
- The current `ItemKind.Rifle` has no catalog offer or starting-grant path. This slice preserves the current offered-item union and does not invent rifle stock or pricing; PG-004 remains partial until roadmap row 10 resolves that separate acquisition gap.
- Use test-first behavior changes, meaningful negative assertions, the non-mutating hook, and the canonical fail-fast command bus.

## Review Focus

- Each prosperity tier returns at most one offer per item, and duplicate HorseFeed resolves to the general-store price.
- Consolidation retains the exact existing offered-item union and per-tier stock/prices without exposing vendor provenance.
- A browser purchase uses item kind and quantity, and the server charges and grants only the selected listed item.
- A purchase for a town other than the player's current town or an item absent from that town's catalog fails without changing cash, inventory or event history.
- Purchase event replay and existing equipment/consumable rules remain unchanged.

---

### Task 1: Bootstrap this JIT slice and retire the completed predecessor

**Files:** Create this plan; update `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; update `Directory.Build.props`; delete `.agents/plans/2026-10-08-retire-trail-npc-encounters.md`.

- [x] Record PR #197 as merged to `develop` at `cfea9fb2f064f76257e86c8c1ebf19607ab54b8e`, source head `673a746ee36ef6f466e1499ce518da3a55651025`, hosted canonical gate run 37832754428, and the documented self-review fallback.
- [x] Classify the completed generated friendly trail-NPC retirement as shipped and retire its plan in this successor slice.
- [x] Update roadmap row 05 to link this plan, record `0.1.0-dev.13` as the checkpoint, and keep row 05 executing for later exclusions.
- [x] Advance `Directory.Build.props` exactly once from `0.1.0-dev.12` to `0.1.0-dev.13`.
- [x] Stage only the new plan, roadmap, version and predecessor retirement; inspect the staged diff and commit through the check-only hook before implementation (`5567e19`).
- [x] After the bootstrap commit creates its SHA, record that exact plan-commit SHA in roadmap row 05 and commit the one-row correction before touching implementation code; do not bump the development version again.

### Task 2: Prove and implement the one-catalog prosperity contract

**Files:** `src/WildBunch.Domain/Economy/TownStoreCatalogModels.cs`; `tests/WildBunch.Domain.Tests/TownStoreCatalogResolverTests.cs`; `tests/WildBunch.Domain.Tests/BeatModelEconomyTests.cs`; `tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs`; `tests/WildBunch.Domain.Tests/Game/GameSessionEventReplayTests.cs`; `tests/WildBunch.Domain.Tests/GameSessionArchiveTests.cs`; `tests/WildBunch.Domain.Tests/GameSessionPurchaseTests.cs`; `tests/WildBunch.Domain.Tests/Projections/JournalLogProjectorEquivalenceTests.cs`; `tests/WildBunch.Domain.Tests/PurchaseBeatCostTests.cs`.

- [x] Replace vendor-distinction characterization with one behavior test over all four prosperity tiers. It asserts the exact item/price catalog below, asserts one offer per item, and failed on today's duplicate Boomtown HorseFeed before production code changed.
- [x] Keep deterministic display ordering by item display name; use the single display name `Horse feed`.
- [x] Consolidate offer factories into one prosperity-driven catalog. Remove `StoreVendorType`, `StoreOfferAvailability`, and source notes from Domain catalog records because the offer list contains only current goods and every town has its one Store.
- [x] Update Domain purchase and event replay construction to identify a returned offer by item kind only; preserve wallet/inventory/event assertions and do not add tests for enum or source-file absence.
- [x] Run the Domain test project and focused purchase/replay tests; keep the event schema and generator/version code untouched.

| Town prosperity | Offered item and price |
| --- | --- |
| Boomtown | Food $2.00; Horse feed $1.00; Canteen $5.00; Knife $8.00; Horse $60.00; Saddle $20.00; Revolver $32.00; Revolver ammo $4.00; Rifle ammo $6.00 |
| Prosperous | Food $2.00; Horse feed $1.00; Canteen $5.00; Knife $8.00; Horse $60.00; Saddle $20.00; Revolver $35.00; Revolver ammo $4.00; Rifle ammo $6.00 |
| Poor | Food $2.50; Horse feed $1.25; Canteen $6.00; Horse $75.00; Saddle $25.00 |
| Destitute | Food $3.00; Horse feed $1.50 |

### Task 3: Remove vendor identity from Application, HTTP and browser purchase contracts

**Files:** `src/WildBunch.Application/Games/Commands/PurchaseStoreItemCommand.cs`; `src/WildBunch.Application/Games/Commands/PurchaseStoreItemHandler.cs`; `src/WildBunch.Application/Games/Models/StoreDtos.cs`; `src/WildBunch.Application/Games/Mapping/StoreCatalogMapper.cs`; `src/WildBunch.Api/Games/Requests/BuyStoreItemRequest.cs`; `src/WildBunch.Api/Games/Validation/RequestValidation.cs`; `src/WildBunch.Api/Games/TownStoreEndpoints.cs`; `src/WildBunch.Web/src/api/types.ts`; `src/WildBunch.Web/src/state/GameSessionProvider.tsx`; `src/WildBunch.Web/src/components/StoreOffersPanel.tsx`; `tests/WildBunch.Application.Tests/Execution/GameSessionCommandHandlerTests.cs`; `tests/WildBunch.Application.Tests/Handlers/GetTownStoreOffersHandlerTests.cs`; `tests/WildBunch.Application.Tests/Handlers/PurchaseStoreItemHandlerTests.cs`; `tests/WildBunch.Integration.Tests/Acceptance/StorePurchaseAcceptanceTests.cs`; `tests/WildBunch.Integration.Tests/EventSourcingEndToEndTests.cs`; `tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs`; `tests/WildBunch.Integration.Tests/FullReplayEqualityTests.cs`; `tests/WildBunch.Integration.Tests/GameApiJournalTests.cs`; `tests/WildBunch.Integration.Tests/GameApiPurchaseTests.cs`; `tests/WildBunch.Integration.Tests/GameApiStoreOffersTests.cs`; `tests/WildBunch.Integration.Tests/GameApiTests.cs`; `tests/WildBunch.Integration.Tests/GameApiValidationTests.cs`; `tests/WildBunch.Integration.Tests/SetupPhaseGuardTests.cs`; `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedCatalog.cs`; `src/WildBunch.Web/src/tests/StorePlaceFeedback.test.tsx`; `src/WildBunch.Web/src/tests/AppShell.test.tsx`; `src/WildBunch.Web/src/tests/GameSettingsOverlay.test.tsx`; `src/WildBunch.Web/src/tests/beatNarrationHook.test.tsx`; `src/WildBunch.Web/src/tests/SheriffPlace.test.tsx`; `src/WildBunch.Web/src/tests/StartOverConfirmation.test.tsx`; `src/WildBunch.Web/src/tests/StartOverRegression.test.tsx`; `src/WildBunch.Web/src/tests/TownHubSurface.test.tsx`; `src/WildBunch.Web/src/tests/test-utils/factories.ts`.

- [x] The HTTP purchase behavior test sends `{ "itemKind": 0, "quantity": 2 }` with no `vendorType` through the request record; it succeeds with the selected item charge and inventory result.
- [x] Make `BuyStoreItemRequest` and `PurchaseStoreItemCommand` carry nullable `ItemKind` plus quantity only, with the C# request shape `BuyStoreItemRequest(ItemKind? ItemKind, int Quantity = 1)`; keep missing-item and nonpositive-quantity validation at the API boundary.
- [x] Make `StoreOffer` and `StoreOfferDto` contain `ItemKind`, display name and price only, and make `TownStoreCatalog` and `TownStoreOffersDto` contain town identity and offers only. Remove the catalog `Available` and both vendor-provenance `SourceNote` fields because no public flow consumes them and the Store is present in every town.
- [x] Make the handler select solely by `ItemKind`. Missing catalog items retain the failed-result/no-save behavior; current-town enforcement and `GameSession.Purchase` continue to own legality and event creation.
- [x] Update React request/response types and provider to send item kind and quantity; key/reset offer rows by item kind and remove the impossible per-offer availability disabled state.
- [x] Update the empty-catalog copy to `No goods are currently offered at this store.` Remove vendor text from offer names, source notes, fixtures and tests; preserve unrelated store pending/error behavior.
- [x] Preserve integration proof for successful charge and inventory result, wrong-town no mutation, absent-item no charge/event, invalid request, replay and purchase event. Replace the old vendor-mismatch test with `ItemKind.Rifle`, which is a valid item kind but absent from the current town's catalog.
- [x] Run focused Application, API/integration and Web tests; the store-surface browser behavior test asserts the item-only request contract. A rendered browser pass bought one Food item through this worktree's API and captured before/after screenshots in the plan scratch workspace.

### Task 4: Reconcile feature evidence, validate and deliver

**Files:** `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-store-and-inventory-boundaries.md`; all source and tests from Tasks 2-3.

- [x] Keep PG-004 partial. Update evidence to state that the unified store catalog, prosperity prices, and item-only purchase contract are implemented; do not claim horse/canteen lifecycle or all acquisition gaps are repaired. Record that `Rifle` remains without a current offer or start grant for row 10 to assess.
- [x] Add a dated outcome to the store/inventory investigation preserving its original finding and recording the chosen HorseFeed price and vendor-contract retirement; keep PG-004-A as a future design record for real differentiated services.
- [x] Compare the change with ADRs selected through the decision catalogue. No economy/vendor ADR exists; the current aggregate, CQRS, event-authority and persistence decisions remain unchanged, so no ADR changed for this slice.
- [x] Run focused tests and `py -3 tools/run.py ci --check`; verify store purchase and browser behavior, no persisted event/schema change, and no active vendor identity in the store catalog/request/response path.
- [x] Review the prepared whole-branch diff against this plan, `docs/features.md`, the baseline specification and repository review guidance. Independent reviewer dispatch is unavailable under the current runtime policy; disclose the repository's documented self-review fallback in the PR.
- [ ] Open and attach a PR targeting `develop`; verify its source head, successful hosted canonical gate and clean merged state before ending this slice. Keep this plan until the next successor slice records its delivery and retires it.

**Ruling:** The existing catalog has one duplicated item, HorseFeed. The spec selects the general-store value in each prosperity tier and requires preservation of the offered-item union; therefore Boomtown/Prosperous HorseFeed remains `$1.00`, Poor remains `$1.25`, Destitute remains `$1.50`, and no new offer is inferred from an `ItemKind` that has no current sale or start-grant path.
