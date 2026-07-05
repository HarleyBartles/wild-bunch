# Validation Policy

Use this reference when running validation, debugging CI failures, or deciding test coverage scope.

## Validation Commands
- Run `dotnet build`.
- Run `dotnet test`.
- Run `dotnet tool restore` before EF validation commands when the repo-local tool manifest is used.
- Run `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api` when persistence may be affected, or as standing validation unless clearly irrelevant.
- Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent tests or validation to reuse the shared local service (idempotent: no-op when already healthy).
- Run `.\scripts\postgres-dev.ps1 validate` for the repo-local PostgreSQL-backed validation lane; it provisions the persistent cluster, exports the repo-local connection string for child `dotnet` commands, restores tools, and runs the EF and test checks together.
- For targeted PostgreSQL-backed tests, use `.\scripts\postgres-dev.ps1 test -- <dotnet test args>` so the script sets `ConnectionStrings__WildBunchPostgresDb` in the same process before invoking `dotnet test`; do not rely on a standalone `$env:` assignment in a separate command.
- Use `.\scripts\postgres-dev.ps1 status` to check whether the lane is already running, `setup` or `validate` to provision it, and `reset` for the destructive local app-database reset path. `stop` and `reset` are manual/destructive; do not stop the shared service during normal worker cleanup.
- If PostgreSQL port `5434` is closed or connection setup fails, report the exact command and output after running the repo-local setup/status lane instead of treating it as a product regression.
- Report warnings separately from failures.

## Index Mesh CI Failures

The "Index mesh + plugin manifest" CI job runs `python scripts/generate_index_mesh.py --check` on a clean Linux checkout. It fails when the committed INDEX.md files don't match what the generator produces from the CI tree. Common causes and fixes:

- **Stale INDEX.md after file rename/add/delete:** Regenerate with `python scripts/generate_index_mesh.py` and commit the updated INDEX.md files. The generator walks the live tree, so any renamed/added/deleted file or directory needs an index refresh.
- **`TestResults` directory (gitignored test output):** `TestResults/` is a gitignored directory created by `dotnet test` runs. It contains dynamic GUID-named subdirectories. The generator must exclude it (it is in `EXCLUDED_DIR_NAMES` in `scripts/generate_index_mesh.py`). If a new gitignored output directory appears, add it to `EXCLUDED_DIR_NAMES` and `EXCLUDED_ROOT_NAMES` in the generator script, then regenerate. Do NOT commit INDEX.md files inside gitignored output directories.
- **PowerShell pipe encoding corrupts `git cat-file` output:** When debugging blob contents on Windows, do NOT pipe `git cat-file -p` through PowerShell `|` or `>` — PowerShell converts stdout to UTF-16LE, adding a `\xff\xfe` BOM and wide characters that look like file corruption. Use `git cat-file -p <sha> | python -c "import sys; ..."` with `sys.stdin.buffer.read()` to inspect raw bytes, or write to a file with `git cat-file -p <sha> -o <file>`.
- **`core.autocrlf=true` on Windows:** The repo uses `autocrlf=true` on Windows. Git stores INDEX.md blobs as LF (the generator writes with `newline="\n"`), and autocrlf normalizes on checkout. This is fine — the generator's `normalize_text` strips CRLF before comparing. The CI check is not a line-ending issue; it is a content/tree-structure mismatch.

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
