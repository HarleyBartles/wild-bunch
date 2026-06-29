# Task 7 Report: Fix GetStartingTownsHandlerTests and GetTownStoreOffersHandlerTests

## What I Implemented

Fixed 3 failing test files in `WildBunch.Application.Tests` that referenced the removed `TownServices.Supplies` flag and hardcoded old town names (pinecross, redmesa, dryfork, etc.) that no longer match the seed-derived town model.

### GetStartingTownsHandlerTests.cs
- Rewrote all 3 tests to use `StartingTownCatalog.GetStartingTownCandidates()` (the same public API the handler uses) instead of hardcoded town names.
- `ReturnsStartingTownCandidatesWithSuppliesOrNoticeBoard` → `ReturnsAllCanonicalTownsAsStartingCandidates`: verifies all canonical towns are returned (count match). The old test checked for `Supplies`/`NoticeBoard` service flags that no longer exist; every town is now a valid starting candidate.
- `ReturnsKnownCanonicalTowns`: verifies each canonical town ID is present in the result, using the catalog rather than hardcoded names.
- `ExcludesTownsWithoutSuppliesOrNoticeBoard` → `ReturnsExactlyTheCanonicalWorldTowns`: verifies the returned set exactly matches the canonical world's towns (set equality). The old exclusion premise is gone — every town has a store now.
- Note: `SeedWorldCatalog` is `internal` (only visible to `WildBunch.GameContent.Tests`), so used the public `StartingTownCatalog` instead, which is what the handler itself calls.

### GetTownStoreOffersHandlerTests.cs
- Updated `CreateSession()` to make `dryfork` a `TownProsperity.Destitute` town (was default Prosperous).
- `GetTownStoreOffersReturnsEmptyCatalogWhenTownHasNoStoreServices` → `GetTownStoreOffersReturnsProsperityBasedCatalogForDestituteTown`: every town now has a store (prosperity-driven), so the catalog is never empty. The test now verifies that a Destitute town has only GeneralStore offers — no Stable or Gunsmith (prosperity-based stock profile).
- The existing `GetTownStoreOffersLoadsSessionAndReturnsExpectedCatalog` test (redmesa, Prosperous) already passed and was left unchanged — Prosperous towns have Gunsmith offers including "Revolver ammo".

### PurchaseStoreItemHandlerTests.cs
- Updated `CreateSession()` to make `pinecross` a `TownProsperity.Destitute` town (was default Prosperous).
- `PurchaseUnknownOfferFailsWithoutSaveOrMutation`: now genuinely tests prosperity-based unavailability — pinecross is Destitute, so Gunsmith/RifleAmmo is not offered (gunsmith only in Boomtown/Prosperous). Added a clarifying comment.
- Updated price assertions in `PurchaseCurrentTownOfferSucceedsSavesOnceAndReturnsUpdatedState` and `PurchaseReturnsDtoWithHudAndDiaryProjections`: Destitute Food costs $3.00 (vs $2.00 Prosperous), so 2 Food = $6.00, cash after = $19.00 (was $21.00).

## What I Tested and Test Results

Ran: `dotnet test --filter "FullyQualifiedName~WildBunch.Application.Tests"`

Result: **Passed: 181, Failed: 0, Skipped: 0** — all Application.Tests pass.

Before fix: 4 failures (2 in GetStartingTownsHandlerTests, 1 in GetTownStoreOffersHandlerTests, 1 in PurchaseStoreItemHandlerTests).

## Files Changed

- `tests/WildBunch.Application.Tests/GetStartingTownsHandlerTests.cs` (rewritten)
- `tests/WildBunch.Application.Tests/GetTownStoreOffersHandlerTests.cs` (1 test rewritten, CreateSession updated)
- `tests/WildBunch.Application.Tests/PurchaseStoreItemHandlerTests.cs` (CreateSession updated, 2 price assertion blocks updated, 1 comment added)

No production code was modified.

## Self-Review Findings

- **Completeness**: All 4 originally failing tests now pass. The 3 test files cover the three areas described in the task brief.
- **Quality**: Tests verify real prosperity-based behavior (Destitute = no gunsmith/stable) rather than mocking or hardcoding. The GetStartingTowns tests use the same public catalog the handler uses, avoiding duplication of internal seed logic.
- **Discipline (YAGNI)**: No unnecessary changes. Only the 3 test files were modified. No production code touched. Other test files in the working tree (from prior tasks) were left unstaged.
- **Testing real behavior**: The Destitute prosperity tier genuinely exercises the `TownStoreCatalogResolver` code path that produces fewer offers. The purchase test verifies the real "offer not found" failure path when gunsmith items aren't in a Destitute town's catalog.
- **SeedWorldCatalog accessibility**: The task brief suggested `SeedWorldCatalog.CreateCanonicalWorld()`, but that class is `internal` and only visible to `WildBunch.GameContent.Tests` via `InternalsVisibleTo`. Used `StartingTownCatalog.GetStartingTownCandidates()` (public, same API the handler uses) instead — this is the correct public surface for Application.Tests.

## Issues or Concerns

- None. All Application.Tests pass. The approach of making pinecross Destitute in PurchaseStoreItemHandlerTests required updating 2 price assertions (Food $2→$3), which is a faithful reflection of the prosperity-based pricing in `TownStoreCatalogResolver`.
