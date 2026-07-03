## Task 7: Fix GetStartingTownsHandlerTests and GetTownStoreOffersHandlerTests

**Files:**
- Modify: `tests/WildBunch.Application.Tests/GetStartingTownsHandlerTests.cs`
- Modify: `tests/WildBunch.Application.Tests/GetTownStoreOffersHandlerTests.cs`
- Modify: `tests/WildBunch.Application.Tests/PurchaseStoreItemHandlerTests.cs`

These tests fail because they reference old town names (pinecross, redmesa, etc.) or old service flags (Supplies) that no longer exist in the derived town model.

- [ ] **Step 1: Run the failing tests to see current errors**

Run: `dotnet test --filter "FullyQualifiedName~GetStartingTownsHandlerTests|FullyQualifiedName~GetTownStoreOffersHandlerTests|FullyQualifiedName~PurchaseStoreItemHandlerTests"`

- [ ] **Step 2: Fix GetStartingTownsHandlerTests**

Update tests to use the canonical world's actual towns instead of hardcoded names. Use `SeedWorldCatalog.CreateCanonicalWorld().Towns` to get valid town IDs.

- [ ] **Step 3: Fix GetTownStoreOffersHandlerTests**

Update the test that checks for empty catalog when town has no store services â€” every town now has a store (prosperity-driven). Adjust assertions to check prosperity-based stock instead.

- [ ] **Step 4: Fix PurchaseStoreItemHandlerTests**

Update the test that expects purchase to fail for unknown offers â€” adjust to use the new prosperity-based store catalog.

- [ ] **Step 5: Run all Application.Tests**

Run: `dotnet test --filter "FullyQualifiedName~WildBunch.Application.Tests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "test: fix Application tests for derived town model and prosperity-based stores"
```

---

