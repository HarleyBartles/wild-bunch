# Task 1 Report: Broaden dev CORS to allow any localhost port

## What I implemented

Broadened the dev CORS policy `"ViteDevClient"` in `src/WildBunch.Api/DependencyInjection.cs` to allow any `http://localhost:*` or `http://127.0.0.1:*` origin instead of hard-coding only port 5173. This handles Vite's fallback to 5174+ when 5173 is occupied.

Created a new test project `tests/WildBunch.Api.Tests` matching the conventions of `tests/WildBunch.Domain.Tests` (net10.0, ImplicitUsings, Nullable, IsPackable=false, same xunit/coverlet/Microsoft.NET.Test.Sdk package versions), added it to `WildBunch.sln`, and added a project reference to `src/WildBunch.Api`.

The fix replaces `WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")` with `SetIsOriginAllowed(...)` using a predicate that accepts any localhost/127.0.0.1 origin (case-insensitive) and rejects all non-local origins.

## TDD Evidence

### RED (failing test before fix)

Command: `dotnet test tests/WildBunch.Api.Tests --filter CorsPolicyTests`

To demonstrate RED, I temporarily reverted `DependencyInjection.cs` to the original `WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")` form, then ran the test.

Output:
```
Failed WildBunch.Api.Tests.CorsPolicyTests.ViteDevClientPolicyAllowsAnyLocalhostPort [156 ms]
  Error Message:
   Assert.True() Failure
Expected: True
Actual:   False
  Stack Trace:
     at WildBunch.Api.Tests.CorsPolicyTests.ViteDevClientPolicyAllowsAnyLocalhostPort() in ...CorsPolicyTests.cs:line 23
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 171 ms
```

Why expected: Line 23 asserts `policy.IsOriginAllowed("http://localhost:5174")` is true. The old policy only allows port 5173, so port 5174 is rejected — exactly the bug being fixed.

### GREEN (passing test after fix)

Command: `dotnet test tests/WildBunch.Api.Tests --filter CorsPolicyTests`

After re-applying the `SetIsOriginAllowed` predicate fix:

Output:
```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 213 ms - WildBunch.Api.Tests.dll (net10.0)
```

### Full suite

Command: `.\scripts\postgres-dev.ps1 test -- dotnet test`

Output:
```
Passed!  - Failed:     0, Passed:   139, Skipped:     0, Total:   139  - WildBunch.GameContent.Tests
Passed!  - Failed:     0, Passed:   188, Skipped:     0, Total:   188  - WildBunch.Application.Tests
Passed!  - Failed:     0, Passed:   475, Skipped:     0, Total:   475  - WildBunch.Domain.Tests
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1  - WildBunch.Api.Tests
Passed!  - Failed:     0, Passed:   169, Skipped:     0, Total:   169  - WildBunch.Integration.Tests
```

Total: 972 passed, 0 failed.

### Build + index mesh

- `dotnet build`: 0 errors, 6 warnings (pre-existing NU1903 Microsoft.OpenApi advisory, unrelated).
- `python scripts/generate_index_mesh.py --check`: `OK index mesh: 99 indexes current`.

## Files changed

- `src/WildBunch.Api/DependencyInjection.cs` — replaced `WithOrigins(...)` with `SetIsOriginAllowed(...)` predicate.
- `tests/WildBunch.Api.Tests/CorsPolicyTests.cs` — new test (created).
- `tests/WildBunch.Api.Tests/WildBunch.Api.Tests.csproj` — new test project (created).
- `tests/WildBunch.Api.Tests/INDEX.md` — generated index for new test folder (created).
- `WildBunch.sln` — added `WildBunch.Api.Tests` project entry + build configs.
- `tests/INDEX.md` — added `WildBunch.Api.Tests` directory entry.

## Self-review findings

- **Completeness:** Test project created, added to solution, project reference wired, test written, fix applied, all per the task brief. The test matches the brief verbatim.
- **Conventions:** New test csproj mirrors `WildBunch.Domain.Tests.csproj` exactly (target framework net10.0, ImplicitUsings, Nullable, IsPackable, identical package versions, `Using Include="Xunit"`).
- **YAGNI:** No extra packages or unrelated changes. Only the CORS slice was touched.
- **Test quality:** The test verifies real behavior — it builds the actual DI container, resolves the real `CorsOptions`, and asserts both positive (localhost any port) and negative (non-local origins rejected) cases. This is a real behavior test, not a mock.
- **TDD discipline:** RED demonstrated by reverting the fix and confirming the failure at the expected assertion (line 23, port 5174), then GREEN after re-applying.
- **Index mesh:** Verified current after adding the new test project folder.

## Concerns

- The `Microsoft.OpenApi` NU1903 high-severity advisory warning is pre-existing (unrelated to this task; originates from `WildBunch.Api.csproj`).
- Uncommitted session artifacts remain in `.agents/superpowers/sdd/` (progress.md, task-1-brief.md, task-3-brief.md) from the planning phase; these are SDD session records, not part of the implementation slice, and were intentionally left out of this commit.
