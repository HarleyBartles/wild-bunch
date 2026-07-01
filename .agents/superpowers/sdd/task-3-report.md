# Task 3 Report: Disambiguate duplicate Horse feed display names by vendor

## What I implemented

Disambiguated the duplicate `"Horse feed"` `StoreOffer.DisplayName` values in the town store catalog by appending the vendor source suffix. The `ItemKind.HorseFeed` shared inventory kind is unchanged — this is display-name disambiguation only.

- General store offers: `"Horse feed"` → `"Horse feed (General store)"` (4 offers: Boomtown, Prosperous, Poor, Destitute)
- Stable offers: `"Horse feed"` → `"Horse feed (Stable)"` (2 offers: Boomtown, Prosperous)

In Boomtown/Prosperous towns both vendors sell horse feed at different prices ($1.00 vs $1.25), so the store panel previously showed two identical-looking cards. The disambiguated names now render distinctly in `StoreOffersPanel.tsx` (which renders `offer.displayName` directly).

## TDD Evidence

### RED (test showing duplicate names)

Added `HorseFeedDisplayNamesAreDisambiguatedByVendor` to `TownStoreCatalogResolverTests.cs`. Before the fix:

```
Failed WildBunch.Domain.Tests.TownStoreCatalogResolverTests.HorseFeedDisplayNamesAreDisambiguatedByVendor [21 ms]
  Error Message:
   Assert.Equal() Failure: Strings differ
                     ↓ (pos 10)
Expected: "Horse feed (General store)"
Actual:   "Horse feed"
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1
```

### GREEN (test passing after fix)

After updating the six `DisplayName` values:

```
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5 - WildBunch.Domain.Tests.dll (TownStoreCatalogResolverTests)
```

## Files changed

1. `src/WildBunch.Domain/Economy/TownStoreCatalogModels.cs` — updated 6 `Horse feed` DisplayName strings (4 General store, 2 Stable) to include vendor suffix.
2. `tests/WildBunch.Domain.Tests/TownStoreCatalogResolverTests.cs` — added `HorseFeedDisplayNamesAreDisambiguatedByVendor` test.

## Build + test results

- `dotnet build`: succeeded (no errors).
- `dotnet test tests/WildBunch.Domain.Tests`: Passed 485, Failed 0.
- `dotnet test tests/WildBunch.Application.Tests`: Passed 188, Failed 0.
- `dotnet test tests/WildBunch.Application.Tests --filter "PurchaseStoreItemHandlerTests|GetTownStoreOffersHandlerTests"`: Passed 10, Failed 0 (no DisplayName assertions on Horse feed — they assert on ItemKind/VendorType).
- `dotnet test` (full suite): Domain + Application + Api.Tests all pass. Integration tests fail (140) due to pre-existing PostgreSQL test lane requirement (`ConnectionStrings__WildBunchPostgresDb` not set) — unrelated to this change. No Horse-feed-related integration failures.

## Frontend check

- `StoreOffersPanel.tsx` renders `offer.displayName` directly (line 75), so the new disambiguated names display correctly with no code change.
- `formatters.ts` `formatItemKind` still returns `"Horse feed"` for `ItemKind.HorseFeed` (case 1) — this is the inventory item-kind label, separate from the store offer DisplayName. Unchanged, correct.
- `TravelPanel.test.tsx` line 332 has a negative assertion (`queryByText(/horse feed/i).not.toBeInTheDocument()`) in a travel-diary context — unrelated to store offers, no update needed.
- No frontend test asserts the old `"Horse feed"` store offer display name.

## Self-review findings

- **Completeness**: All six Horse feed DisplayName values updated (4 General store, 2 Stable). Test covers the Boomtown duplicate-vendor scenario.
- **Quality**: Minimal, surgical change matching existing conventions. No new abstractions.
- **Discipline (YAGNI)**: `ItemKind.HorseFeed` remains a single shared inventory kind — not split. `StoreOffer` record unchanged; only `DisplayName` string values changed. No other item names touched (no other vendor-overlap exists per plan).
- **Testing**: New test added (RED→GREEN). No regressions in Application tests. Integration failures are pre-existing PostgreSQL-lane infrastructure, not caused by this change.

## Concerns

None.
