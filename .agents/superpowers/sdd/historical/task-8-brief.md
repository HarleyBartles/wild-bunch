## Task 8: Fix integration tests

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/**/*.cs` (134 failures)

Integration tests likely fail for the same reasons: hardcoded town names, old trail counts, old warrant/clue counts, old service flags.

- [ ] **Step 1: Run integration tests to categorize failures**

Run: `dotnet test --filter "FullyQualifiedName~WildBunch.Integration.Tests" 2>&1 | Select-String "Failed "`
Categorize failures by type (town name, count, service, warrant/clue).

- [ ] **Step 2: Fix town-name-dependent tests**

Replace hardcoded town IDs with canonical world's actual town IDs.

- [ ] **Step 3: Fix count-dependent tests**

Update trail counts (12 for 8 towns), warrant counts (28 public, 0 known at start), clue counts (6 public, 1 known at start).

- [ ] **Step 4: Fix service/store-dependent tests**

Update for prosperity-based stores and always-present sheriff/saloon/noticeboard.

- [ ] **Step 5: Run all integration tests**

Run: `dotnet test --filter "FullyQualifiedName~WildBunch.Integration.Tests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "test: fix integration tests for derived town model and restructured case pools"
```

---

