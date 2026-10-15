# Row 18 N-80 Retained Startup Browser Qualification Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this approved JIT plan task by task.

**Goal:** Qualify one representative retained player path in the built public web application, backed by the real local API and PostgreSQL event store, including restoration after closing and reopening the browser page.

**Architecture:** Extend the Playwright lane to serve the production-mode web build and start the local API. A browser test creates a game through name-only quick start, reopens the page to prove prologue restoration from persisted server state, acknowledges the prologue, selects a server-returned town on the existing Phaser world map, reaches that town, then reopens the page to prove the server-owned location is restored. Keep the browser's current-game pointer as a client locator only; the API and persisted event stream remain authoritative. Do not add test-only application behavior or map input affordances.

**Tech Stack:** Playwright, production-mode Vite build, ASP.NET API, PostgreSQL 16, Python command bus and pytest.

**Implementation Guide:** [Implementing runbook](../runbooks/implementing.md), [testing playbook](../playbooks/testing.md), and [code-review runbook](../runbooks/code-review.md).

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially workstream G and its retained-path evidence; [feature matrix](../../docs/features.md), especially PG-001 and PLAT-004; [ADR-0048](../../docs/decisions/ADR-0048-gitflow-and-semver-release-identity.md).

**Execution Strategy:** `executing-plans` - this is one sequential qualification path across command-bus ordering, production build, API startup, browser interaction, durable database state and feature evidence. Keep implementation inline so the behavior proof and its negative mutation remain directly inspectable.

## Global Constraints

- This PR advances the authored product version exactly once, from `0.1.0-dev.197` to `0.1.0-dev.198`; `Directory.Build.props` remains the sole authored product version.
- Seed identity owns the decoded world shape. Any bit combination that cannot produce a legal world must be normalized at seed decode so every editable UUID can start a game deterministically.
- Qualify only the name-only quick-start, prologue, one world-map town selection, town arrival and page-reopen restoration path. Do not claim complete gameplay, all retained paths, cross-tab synchronization, deployed resume, hosted release, public cloud deployment or OIDC.
- Use the built public web output and the repository's real API/PostgreSQL path. Do not substitute fixture HTML, mock API responses, browser memory, or a test-only server for this path.
- Give each Playwright invocation a uniquely named disposable PostgreSQL database and remove only that database after the API stops. Never use or reset the shared `wildbunch_dev` database for this browser test.
- Preserve the existing pointer-only map input decision under ADR-0046. Do not add keyboard or screen-reader town selection, DOM markers, test hooks, SignalR or new product behavior for the test.
- The test must prove the persisted game locator drives page restoration: first establish a passing test, temporarily break the real `localStorage` write, observe the test fail because the reopened page cannot recover the game, restore the code and observe it pass. Do not make the assertion itself the mutation.
- Keep the pre-existing user edit in the main checkout untouched. This plan, its implementation and its paired local/upstream refs stay in the fresh N-80 worktree until the reviewed PR and exact develop-push gate pass.

## Review Focus

- The test opens a real Chromium page against Vite's production preview and a real API backed by PostgreSQL; all relevant browser requests must reach those services.
- Setup and prologue restoration must be proven across a page close/reopen boundary, not by React rerender or in-page navigation alone.
- The town chosen must be a town returned by the API, selected through the existing Phaser canvas interaction, and reflected by the server-backed Town Hub.
- Final restoration must assert the selected town after another close/reopen, demonstrating that the current-game ID is a locator and the server remains authoritative for phase and location.
- The command bus and CI lane build and validate version identity before starting the browser suite; a direct browser command documents its build and PostgreSQL prerequisites.
- PG-001 and PLAT-004 evidence must describe the exact local proof and retain the limits on broader candidate and hosted/deployed qualification.
- Compare the final changes with ADR-0048, ADR-0046, the feature matrix and release guidance. No durable decision changes are expected; if implementation reveals one, stop to write its successor record before claiming completion.

---

### Task 1: Commit the N-80 plan and retire completed N-79

**Files:**
- Create: `.agents/plans/2026-10-15-row18-n80-retained-startup-browser-qualification.md`
- Delete: `.agents/plans/2026-10-15-row18-n79-local-release-bundle-verification.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`

**Interfaces:**
- Consumes: merged PR #382 source `92a61e1d43670c437207328267198e9e7da44867`, merge `be07b02de4bc4313b831ff57d8eb7c7408b175a7`, passing exact-head and develop-push gates, N-79 release bundle evidence and current row-18 status.
- Produces: this active N-80 plan at `0.1.0-dev.198`; a dated N-79 delivery record; N-79 plan retirement only after its actual merge, gates, bundle verification and paired worktree/ref cleanup are recorded.

- [x] Verify the worktree starts at current `origin/develop` `be07b02de4bc4313b831ff57d8eb7c7408b175a7`; confirm N-79 PR, merge result, both hosted gates and cleanup. Recheck ADR-0048, ADR-0046, PG-001, PLAT-004 and release guidance against current source.
- [x] Delete the completed N-79 plan, retain its delivery evidence in the roadmap, set row 18's active plan and bounded outcome to N-80, advance `Directory.Build.props` once to `0.1.0-dev.198`, and create this plan. Do not mark row 18 complete.
- [x] Run `git diff --check`; stage only the four named paths; commit through the normal check-only hook without bypassing it.

### Task 2: Prove a retained player path through production web and persisted API state

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs`
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldResolverTests.cs`
- Modify: `src/WildBunch.Web/src/api/types.ts`
- Modify: `src/WildBunch.Web/src/api/httpClient.ts`
- Modify: `src/WildBunch.Web/src/components/CaseFileSurface.tsx`
- Modify: `src/WildBunch.Web/src/components/JournalSurface.tsx`
- Modify: `src/WildBunch.Web/src/tests/CaseFileSurface.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/JournalSurface.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/wildBunchApi.test.ts`
- Modify: `src/WildBunch.Web/playwright.config.ts`
- Modify: `src/WildBunch.Web/package.json`
- Modify: `src/WildBunch.Web/package-lock.json`
- Create: `src/WildBunch.Web/browser-tests/player-startup.spec.ts`
- Create: `src/WildBunch.Web/browser-tests/support/postgresApi.ts`
- Modify: `tools/run.py`
- Modify: `tools/tests/test_run.py`
- Modify: `tools/README.md`
- Modify: `.agents/playbooks/testing.md`
- Modify: `.agents/doctrine/validation-policy.md`
- Modify: `.agents/runbooks/release.md`
- Modify: `docs/features.md`

**Interfaces:**
- Consumes: the public web build and generated version identity; local PostgreSQL on `localhost:5435`; Playwright Chromium; quick-start, prologue, world-map and town-hub APIs and existing Phaser pointer interaction.
- Produces: a browser qualification test proving setup and town state restore after page close/reopen against real persisted server state; a fail-fast browser lane that runs only after web build and identity validation; updated feature/release evidence with explicit scope limits.

- [x] Inspect the current browser surface, setup API contract, persisted game pointer, prologue flow, world-map DTO/render coordinate transform, town-hub contract, PostgreSQL ensure script and command-bus orchestration. Resolve town marker screen coordinates from the actual returned DTO and rendered canvas dimensions; do not add an alternate interaction path. The API returns a null current town before first arrival; the journal and case-file surfaces now honor that contract instead of fabricating a location.
- [x] Add the browser test and production web preview plus API setup/teardown. Create a unique database per Playwright invocation, pass its connection string as API command-line configuration so the shared database environment inherited from the .NET task runner cannot override isolation, and drop only that database after the API stops. Use the explicit IPv4 loopback endpoint used by the web client; refuse an already-occupied port there and wait for the child API's own Kestrel listening log before accepting health. Verify the disposable database has no active playthrough before the journey. Give the whole browser journey 120 seconds because the real API and fresh database run under the canonical gate. Keep the existing fixture tests on the dev server and in the same suite.
- [x] Run the browser test with PostgreSQL ensured against current behavior. The first run exposed the null-town rendering failure; behavior tests failed on the actual dereferences before the UI correction. Temporarily removing the persisted current-game ID write made the reopened-page request assertion fail in 5 seconds; restoring the write made the complete browser route pass again.
- [x] The randomized quick-start exposed invalid seed encodings. One behavior test exhausts 1,024 encoded cash/town-count/outlier combinations, validates every resolved world and asserts the two normalization rules; removing either production rule independently makes it fail. `Resolve` clamps cash bonus to its legal envelope and removes an outlier slot when maximum town count leaves no legal slot. The complete GameContent suite passed before the browser path was rerun.
- [x] Reorder `_build_web()` and `ci` steps so style, typecheck, unit tests, web build, build identity and then browser tests run fail-fast in that order. Focused command-bus behavior tests cover both orderings; temporary early-browser mutations made each ordering test fail before restoration.
- [x] Document direct browser-test build and local PostgreSQL prerequisites in the command-bus README and testing guidance. Keep artifact identity distinct from gameplay qualification in the release runbook; local route evidence does not establish complete candidate, hosted release or deployed resume.
- [x] Update PG-001, PG-002 and PLAT-004 evidence to record only the production-mode browser path with local API/PostgreSQL and page-reopen restoration. Preserve broader retained-path and full candidate qualification work in row 18.
- [x] Run `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`, `npm --prefix src/WildBunch.Web ci`, `py -3 tools/run.py web --check`, `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --no-restore`, and `py -3 -m pytest tools/tests -q`. The web target passed formatting, typecheck, 377 Vitest tests, production build and version identity, and all 5 Chromium tests; GameContent passed 159 tests and tools passed 95. The restored browser route passed after the storage mutation proof. A read-only database query confirmed teardown left no `wildbunch_browser_%` databases. `git diff --check` passed.
- [x] Run the repository gate `py -3 tools/run.py ci --check`; all checks passed, including 332 PostgreSQL integration tests, 377 Vitest tests, the production build and identity check, and all 5 Chromium tests.
- [x] Close the independent review's setup-failure gap by moving `spawn()` inside the cleanup guard, so a synchronous process-creation failure drops the generated disposable database. The fail-fast gate passed after this fix; commit through the normal hook.

### Task 3: Review, publish and close N-80

**Files:**
- Review the complete N-80 branch diff, this plan, the stable baseline specification, ADR-0046, ADR-0048, feature matrix, testing guidance and release runbook.

**Interfaces:**
- Consumes: committed browser qualification, passing local validation, accurate feature evidence, retained ADR truth and no unresolved important review findings.
- Produces: a reviewed PR to `develop` at `0.1.0-dev.198`, passing exact-head hosted CI, merged output and paired worktree/branch-ref cleanup.

- [ ] Independently compare the complete diff with ADR-0046 and ADR-0048 and the feature matrix. Record in the PR why no successor ADR is needed and verify PG-001/PLAT-004 make no broader claim than the evidence.
- [ ] Dispatch one fresh-context reviewer for the exact corrected head with the repo's code-review and decision-record routes. Resolve Critical or Important findings with a witnessed behavior proof and rerun the required checks after any correction.
- [ ] Push the dedicated branch and create a Draft PR targeting `develop`. Verify title, body, exact base/head and Draft state; make it ready only after local validation and independent review pass.
- [ ] Require the hosted canonical gate to pass on the exact PR head before merge. Merge to `develop`, then require the develop-push gate to pass on the exact merge result.
- [ ] Fast-forward the main checkout to the verified merge while preserving its pre-existing user edit. Run the finishing helper `--check` then `--apply`; remove the clean N-80 worktree, delete its exact local and upstream branch refs, prune registrations where appropriate, and verify all three are absent.

## Scope Boundaries

This slice qualifies one local retained player route only. It does not certify every feature-matrix entry, every supported event-history class, browser refresh in other phases, multiple tabs, cross-device recovery, developer/playtest composition, hosted release automation, container images, infrastructure, cloud deployment, authentication or public operation.
