### Task 3: Disambiguate duplicate "Horse feed" display names by vendor

**Files:**
- Modify: `src/WildBunch.Domain/Economy/TownStoreCatalogModels.cs` (lines 77, 84, 91, 97, 109, 115)
- Test: `tests/WildBunch.Domain.Tests/TownStoreCatalogResolverTests.cs` (add test)

**Interfaces:**
- Consumes: `Town`, `TownProsperity` from `WildBunch.Domain.World`
- Produces: `StoreOffer.DisplayName` for `ItemKind.HorseFeed` offers includes the vendor source suffix, e.g. `"Horse feed (General store)"` / `"Horse feed (Stable)"`

- [ ] **Step 1: Write the failing test**

Add to `tests/WildBunch.Domain.Tests/TownStoreCatalogResolverTests.cs`:

```csharp
    [Fact]
    public void HorseFeedDisplayNamesAreDisambiguatedByVendor()
    {
        var resolver = new TownStoreCatalogResolver();
        var town = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Telegraph, TownProsperity.Boomtown);

        var catalog = resolver.Resolve(town);

        var horseFeedOffers = catalog.Offers
            .Where(o => o.ItemKind == ItemKind.HorseFeed)
            .ToList();

        // Boomtown has both a general store and a stable selling horse feed
        Assert.Equal(2, horseFeedOffers.Count);

        var generalStoreOffer = horseFeedOffers.Single(o => o.VendorType == StoreVendorType.GeneralStore);
        var stableOffer = horseFeedOffers.Single(o => o.VendorType == StoreVendorType.Stable);

        Assert.Equal("Horse feed (General store)", generalStoreOffer.DisplayName);
        Assert.Equal("Horse feed (Stable)", stableOffer.DisplayName);

        // Display names must be distinct so the store panel doesn't show duplicate-looking cards
        Assert.NotEqual(generalStoreOffer.DisplayName, stableOffer.DisplayName);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter HorseFeedDisplayNamesAreDisambiguatedByVendor`
Expected: FAIL â€” both offers have `DisplayName == "Horse feed"`.

- [ ] **Step 3: Implement the fix**

In `src/WildBunch.Domain/Economy/TownStoreCatalogModels.cs`, update all six `Horse feed` display names:

General store offers (lines 77, 84, 91, 97) â€” change `"Horse feed"` to `"Horse feed (General store)"`:

```csharp
                new StoreOffer(ItemKind.HorseFeed, "Horse feed (General store)", 1m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
```
(Repeat for each prosperity tier's general store horse feed offer, keeping the respective price.)

Stable offers (lines 109, 115) â€” change `"Horse feed"` to `"Horse feed (Stable)"`:

```csharp
                new StoreOffer(ItemKind.HorseFeed, "Horse feed (Stable)", 1.25m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room")
```
(Repeat for Boomtown and Prosperous stable horse feed offers.)

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter TownStoreCatalogResolverTests`
Expected: PASS

- [ ] **Step 5: Run purchase and store-offers tests to verify no regressions**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "PurchaseStoreItemHandlerTests|GetTownStoreOffersHandlerTests"`
Expected: PASS â€” these tests assert on `ItemKind` and `VendorType`, not on the exact `DisplayName` string. If any test asserts the old `"Horse feed"` display name, update it to the new disambiguated name.

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Domain/Economy/TownStoreCatalogModels.cs tests/WildBunch.Domain.Tests/TownStoreCatalogResolverTests.cs
git commit -m "BUNCH-118: disambiguate duplicate Horse feed display names by vendor"
```

---
