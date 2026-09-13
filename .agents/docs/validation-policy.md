# Validation Policy

Use this reference when running validation, debugging CI failures, or deciding test coverage scope.

## Validation Commands
- Run `dotnet build`.
- Run `dotnet test`.
- Run `dotnet tool restore` before EF validation commands when the repo-local tool manifest is used.
- Run `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api` when persistence may be affected, or as standing validation unless clearly irrelevant.
- Run `.\tools\postgres-dev.ps1 ensure` before PostgreSQL-dependent tests or validation to reuse the shared service on `localhost:5435`.
- Use `py -3 tools\run.py ci --check` as the canonical fail-fast validation lane; the runner supplies the PostgreSQL connection string to child .NET tests.
- For a targeted direct test, set `ConnectionStrings__WildBunchPostgresDb` to `Host=localhost;Port=5435;Database=wildbunch_dev;Username=postgres` in the same PowerShell process before invoking `dotnet test`.
- Use `.\tools\postgres-dev.ps1 status` for a read-only service check. `stop` and `reset` change shared state and require explicit lifecycle intent.
- If PostgreSQL port `5435` is closed, report the exact `status` and `ensure` output instead of treating it as a product regression.
- Report warnings separately from failures.

## CI Preflight (run locally before marking a PR ready)

Before moving a PR out of draft, use the canonical runner:

```powershell
py -3 tools\run.py ci --check
```

This checks repo standards, installed skills, the generated mesh, .NET build and
tests, frontend install/typecheck/tests/build, and diff hygiene. Use
`--diagnostics` when diagnosing multiple independent failures.

If the script fails, fix the issue and re-run before marking the PR ready. Iterate on individual lanes directly if you need a narrower loop.

For persistence work, also run the relevant EF migration command after
`.\tools\postgres-dev.ps1 ensure` with the documented connection string set.

## Index Mesh CI Failures

The "Index mesh" CI job runs the bundled `generating-agent-mesh` skill in check mode. It fails when the committed `INDEX.md` files don't match what the skill produces from the CI tree. Common causes and fixes:

- **Stale INDEX.md after file rename/add/delete:** Regenerate with `py -3 .agents/skills/generating-agent-mesh/scripts/generate-index-mesh.py` (or `.agents/skills/generating-agent-mesh/scripts/generate-index-mesh.ps1`) and commit the updated `INDEX.md` files. The skill walks the live tree, so any renamed/added/deleted file or directory needs an index refresh.
- **`TestResults` directory (gitignored test output):** `TestResults/` is a gitignored directory created by `dotnet test` runs. It contains dynamic GUID-named subdirectories. The skill uses `git check-ignore` and its own `EXCLUDED_DIR_NAMES` set; if a new gitignored output directory appears, add it to `.gitignore`.
- **PowerShell pipe encoding corrupts `git cat-file` output:** When debugging blob contents on Windows, do NOT pipe `git cat-file -p` through PowerShell `|` or `>` — PowerShell converts stdout to UTF-16LE, adding a `\xff\xfe` BOM and wide characters that look like file corruption. Use `git cat-file -p <sha> | python -c "import sys; ..."` with `sys.stdin.buffer.read()` to inspect raw bytes, or write to a file with `git cat-file -p <sha> -o <file>`.
- **Line endings:** The bundled generators write LF (`newline="\n"`). The check normalizes CRLF before comparing. A mismatch is a content or tree-structure issue, not a line-ending issue.

## Testing Posture
- New or updated real application behavior should normally include test coverage in the same slice.
- If coverage is skipped, state the reason explicitly and keep the gap narrow and deliberate.
- Debug-only or temporary prototype surfaces may use lighter-weight coverage while they remain debug-only.

## Test Quality Standards

These standards apply to all test kinds and to both implementers and reviewers.

- **New code must be covered by tests.** Every new function, component, hook, handler, or domain method must have tests that verify its behavior — not just its existence. Production code without corresponding tests is a gap that must be flagged.
- **Tests must verify real behavior, not mock interactions.** Tests should assert on observable outcomes (rendered output, returned values, state changes), not on which mock functions were called in which order. Mock-heavy tests that pass but don't actually test the behavior are a finding — they give false confidence.
- **Edge cases must be covered.** Identify edge cases in the code under test (null/undefined inputs, empty collections, error states, boundary conditions) and ensure tests exist for them. Missing edge case coverage is critical for critical paths, minor for non-critical paths.
- **The right test kind must be used.** See the Test Kinds section above. Using a unit test where an integration test is needed (or vice versa) is a finding.
- **No flaky tests.** A test that passes in isolation but fails under full-suite load is a flaky test. Flaky tests are not acceptable — they erode confidence in the suite and waste CI time. Common causes and fixes:
  - **Shared mutable state** (router instances, singletons, module-level caches) — create fresh instances per test instead of sharing module-level singletons. For TanStack Router, use a `createAppRouter()` factory rather than importing the shared `router` singleton.
  - **Timing-dependent assertions on lazy-loaded components** — use `findByRole`/`findByText` with an extended timeout (e.g. `{ timeout: 5000 }`) instead of the default 1000ms, since lazy imports take longer under full-suite memory pressure.
  - **Missing `waitFor` around async renders** — wrap assertions that depend on async state resolution in `waitFor`.
  - **Test ordering dependencies** — tests must not depend on execution order. Each test must set up and tear down its own state.
  - A flaky test is worse than no test because it trains the team to ignore failures. If a test is flaky, it must be fixed immediately or removed.
- **All tests must pass.** The full suite must pass: `npx vitest run` from `src/WildBunch.Web/` for frontend, `dotnet test` for backend. No skipped tests (`it.skip`, `describe.skip`) without a documented reason.
- **Test output must be pristine.** No stray warnings, no console noise, no unhandled promise rejections in test output. Warnings in test output are findings — they indicate either a real problem being silenced or test setup that doesn't match production behavior.

## Test Kinds

This repo uses five test kinds. Each has a distinct purpose and structure:

- **Unit tests** (`WildBunch.Domain.Tests`, `WildBunch.Application.Tests`) —
  isolated single-code-path tests with known inputs and expected outputs. Fast,
  no external dependencies.
- **Integration tests** (`WildBunch.Integration.Tests`) — full HTTP pipeline
  tests via `WebApplicationFactory` with a real PostgreSQL database. Highest
  value per test — covers routing, binding, validation, business logic, and
  persistence in one shot.
- **GameContent tests** (`WildBunch.GameContent.Tests`) — seed codec and
  game-setup pipeline tests that verify deterministic world generation,
  seed round-tripping, and the full `SeededNewGameFactory` pipeline.
- **API tests** (`WildBunch.Api.Tests`) — API-specific contract tests.
- **Brute-force tests** (within `WildBunch.GameContent.Tests`) — iterate over
  thousands of seed/salt/parameter combinations in a single test method,
  asserting per-combination invariants, statistical distribution fairness,
  and anti-pattern absence. See the `testing` skill (`/testing`) for full
  guidance on when and how to write brute-force tests.

### When to add a brute-force test
- When the system produces deterministic-but-varied output from seed/salt
  combinations and you want to catch silent bias or rare anti-patterns
- When you change a generator (map, encounter, item distribution, mystery
  truth) and want to verify the output distribution remains healthy across
  all valid parameter combinations
- When you add a new parameter axis (entropy level, difficulty, variant) and
  want to verify it actually produces measurably different output

### When NOT to add a brute-force test
- When testing a single code path with known inputs — use a unit test
- When testing the HTTP pipeline — use an integration test
- When the system is not deterministic — brute-force tests require
  determinism (same seed + same salt = same output) to be meaningful
