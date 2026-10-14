# Row 17 N-53: Prove the World Map Projection Contract

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Replace weak world-map output checks with behavior tests that prove the single player-facing map query returns the exact settled town and trail facts through Application and HTTP/PostgreSQL boundaries.

**Architecture:** Keep the read path unchanged: `GetWorldMapHandler` maps the persisted `GameSession.World` into `WorldMapDto`, and the API exposes one `GET /api/games/{id}/world-map` route. Use a small explicit world with distinctive coordinates, edge IDs, endpoints, and distances so the expected result is independent of generated output. Exercise both the handler mapping and the serialized endpoint using a persisted setup session that contains hidden case truth.

**Tech Stack:** C# 10, xUnit, ASP.NET Core integration testing, EF Core, PostgreSQL, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [PG-003 world-map promise](../../docs/features.md); [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [web test follow-up, one world map](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md).

**Execution Strategy:** `executing-plans` (native inline). The application and HTTP tests protect separate boundaries of one read contract. Use small explicit mutations of the DTO mapping and HTTP route to prove each new test fails for the behavior it owns, then restore production files byte-for-byte.

## Global Constraints

- Preserve one settled `World` and the one existing `GET /api/games/{id}/world-map` endpoint; do not create or restore separate setup/travel map contracts.
- The current response contains town ID, name, X/Y coordinates, and trail ID, endpoints, and ride-day distance. Town services and layout belong to the town surface and are not new map response fields.
- Keep the query read-only and map persisted world facts without deriving or mutating gameplay truth.
- Do not change generation, event history, the start/prologue/first-arrival sequence, persistence schema, or player-facing feature promises.
- Keep the missing-session HTTP negative and the existing setup-to-start map-stability assertion.
- Own the next development identity only in `Directory.Build.props` at `0.1.0-dev.171`; target the fresh `develop` worktree and PR back to `develop`.
- Require independent whole-branch review, green exact-head hosted CI, green develop-push CI, primary-checkout fast-forward, and paired worktree/local/upstream branch cleanup before retiring this worktree.

## Review Focus

- A field-swapped or omitted town coordinate, trail endpoint, or distance must fail against the independently arranged map.
- A successful HTTP response must contain the exact public map facts and the established map JSON shape without case-file truth; a map containing only non-empty arrays is insufficient evidence.
- The endpoint behavior test must fail if the one supported route becomes unreachable.
- Existing map facts must remain identical across setup and completion of game start, as already asserted by `ProjectionEndpointTests`.
- Live source currently has one world-map handler and one endpoint; stale inventory references to `GetStartingTownMapHandlerTests` and a second map URL must not be repeated as current code.

---

### Task 1: Promote N-52 and establish N-53

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n53-world-map-contract.md`
- Delete: `.agents/plans/2026-10-14-row17-n52-route-reachability.md`

**Interfaces:**
- Consumes: merged N-52 PR #355, reviewed source `5d7060a4bc179fcc70019bde32e1cc4db74aed93`, squash merge `d316266ac3d42d8cdd281003b261b6b2e666cfcb`, identical tree `ab4e3155d4936b4b0f4d1f905207f4a853aa7ec0`, successful exact-head run `38587789742`, successful develop-push run `38588781034`, and verified removal of its worktree and paired branch refs.
- Produces: promoted N-52 delivery evidence, active N-53 at `0.1.0-dev.171`, and retirement of the completed N-52 plan.

- [ ] **Step 1: Revalidate N-52 integration and cleanup.** Confirm PR #355 is merged into `develop`, the recorded source and merge SHAs match, both hosted gates passed on the exact source and merge commits respectively, both commits have the same tree, the primary checkout is at the merge, and the N-52 worktree and local/upstream refs are absent.
- [ ] **Step 2: Promote N-52, activate N-53, and advance the version.** Add PR #355 and its reviewer, tree, exact-head CI, develop-push CI, and paired cleanup evidence to row 17. Replace the active plan/version with this N-53 plan and `0.1.0-dev.171`. Change only the authored version in `Directory.Build.props`.
- [ ] **Step 3: Retire the completed predecessor.** Remove the N-52 plan only after its delivery evidence has been promoted. Keep the baseline specification and investigation records.
- [ ] **Step 4: Commit successor planning first.** Run `git diff --check`, review the complete staged diff, and commit the roadmap, version, N-53 plan, and N-52 plan retirement with `docs: plan N-53 world map contract` before editing implementation tests.

### Task 2: Prove Application world-map mapping

**Files:**
- Modify: `tests/WildBunch.Application.Tests/Handlers/GetWorldMapHandlerTests.cs`
- Inspect only: `src/WildBunch.Application/Games/Queries/GetWorldMapHandler.cs`, `src/WildBunch.Application/Games/Models/WorldMapDto.cs`, and `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs`.

**Interfaces:**
- Consumes: `GetWorldMapHandler`, `IGameSessionReadRepository`, `GameSession.StartSetup`, and the current `WorldMapDto` contracts.
- Produces: an explicit three-town/two-trail fixture and literal expected DTO rows proving every current map field is mapped from the loaded world; the missing-session negative remains.

- [ ] **Step 1: Replace generated self-comparison with an explicit world.** Construct a setup session from towns `map-town-red-mesa` / `Red Mesa` at `(17, 39)`, `map-town-dry-creek` / `Dry Creek` at `(83, -11)`, and `map-town-riverbend` / `Riverbend` at `(-26, 104)`. Add `trail-red-mesa-dry-creek` from Red Mesa to Dry Creek at `3.75m` and `trail-riverbend-red-mesa` from Riverbend to Red Mesa at `4.25m`. Use `TrailRisk.High`/`TrailTerrain.Badlands`/`WaterFeature.None` and `TrailRisk.Low`/`TrailTerrain.Hills`/`WaterFeature.Spring` respectively so the source objects carry other distinctive facts that must not be substituted for map facts. Use a minimal case with one `hidden-culprit` suspect, seed `f5505e33-f2dd-4c4e-8fd2-f64ff891a653`, Standard difficulty, Boring entropy, and fixed salt `n53-map-salt`. Compare every output record against literal DTO rows; do not compute expected values through `SeedWorldMapLayout`, the handler, or the generated world resolver.
- [ ] **Step 2: Run the focused handler tests.** Run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter FullyQualifiedName~GetWorldMapHandlerTests` and confirm the explicit mapping case and missing-session negative pass.
- [ ] **Step 3: Prove the mapping test fails on a real projection defect.** Temporarily map one town coordinate to the other coordinate (or one trail endpoint to the other endpoint), run only the new handler behavior test, and confirm the literal expected map fails. Restore `GetWorldMapHandler.cs` byte-for-byte and rerun the focused tests.

### Task 3: Prove the persisted HTTP map contract

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/WorldMapEndpointTests.cs`
- Inspect only: `src/WildBunch.Api/Games/GameSessionEndpoints.cs`, `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`, and `src/WildBunch.Persistence/GameSessions/EfGameSessionUnitOfWork.cs`.

**Interfaces:**
- Consumes: `PostgreSqlApiFactory`, `IGameSessionRepository`, `IGameSessionUnitOfWork`, `GameSession.StartSetup`, and the exact public map contract.
- Produces: a successful API read over a persisted explicit setup world with hidden culprit truth, exact town/trail values and JSON fields, while retaining the missing-session `404` behavior.

- [ ] **Step 1: Persist a deliberately distinctive setup world.** In the integration test, construct the exact three towns and two trails listed in Task 2, plus a minimal `CaseFile` containing `hidden-culprit`. Build it with `GameSession.StartSetup`, store it through the factory's registered `IGameSessionRepository`, then commit with `IGameSessionUnitOfWork`. Do not seed EF rows directly or create truth outside the domain event path.
- [ ] **Step 2: Assert the full serialized response.** Replace `NotEmpty` checks and arbitrary substring scans with the exact root keys `towns`/`trails`, exact town keys `id`/`name`/`x`/`y`, exact trail keys `id`/`fromTownId`/`toTownId`/`rideDayDistance`, and the literal values listed in Task 2. Confirm the response contains no case-file fields by asserting the allowed serialized map shape while the persisted session contains hidden culprit truth. Keep the independent missing-session test.
- [ ] **Step 3: Prove HTTP route reachability.** Temporarily change the registered `world-map` route to an unsupported path, run only `WorldMapEndpointTests.GetWorldMapReturnsThePersistedWorldMap`, and confirm the expected successful response fails. Restore `GameSessionEndpoints.cs` byte-for-byte. Ensure the PostgreSQL service with `.\tools\postgres-dev.ps1 ensure`, then run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter FullyQualifiedName~WorldMapEndpointTests` and confirm both endpoint cases pass.

### Task 4: Reconcile the finding and validate durable truth

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Inspect only: `tests/WildBunch.Integration.Tests/ProjectionEndpointTests.cs`, `docs/features.md` PG-003, ADR-0035, ADR-0039, and ADR-0046.

**Interfaces:**
- Consumes: the final behavior tests, current single world-map handler/route, one-map product promise, existing map-stability assertion, and applicable decision records.
- Produces: a dated disposition that corrects stale map-test references and records why the evidence changed without changing current product, architecture, or input decisions.

- [ ] **Step 1: Record current-source reconciliation.** Add a dated N-53 disposition explaining that the earlier follow-up cited a `GetStartingTownMapHandlerTests` file and a possible second map URL that are absent from current source; today there is one `GetWorldMapHandler`, one `world-map` endpoint, and one DTO that maps town identity/name/coordinates and trail identity/endpoints/distance. Note that services are not map response fields and preserve the historical investigation wording as a dated finding.
- [ ] **Step 2: Verify the map remains settled across start.** Run the existing `ProjectionEndpointTests.CompleteGameStart_ReturnedDto_HudProjectionIncludesGameStartedState` test and keep its complete map equality check unchanged; it proves setup-to-start world stability independently of the map projection oracle.
- [ ] **Step 3: Compare durable truth and run checks.** Through the decision-record and feature-matrix playbooks, verify that the change preserves ADR-0035's React/Phaser/server authority boundaries, ADR-0039's one shared map and free first arrival, ADR-0046's pointer-operated baseline, and PG-003's town navigation promise. Record why none changes. Format/lint the changed C# files, run `git diff --check`, then run `py -3 tools/run.py ci --check` on the final implementation tree.

### Task 5: Review, publish, integrate, and retire N-53

**Files:**
- Review: complete merge-base diff, plan, roadmap, N-52 delivery disposition, map-test disposition, current map DTO/handler/endpoint, ADRs, PG-003, and focused/canonical evidence.
- Modify: the roadmap with N-53 final delivery evidence only in its next successor's first substantive commit.

**Interfaces:**
- Consumes: explicit handler mapping proof, exact persisted HTTP response, missing-session negative, route and field mutation failures with production restored, and unchanged world-map facts across start.
- Produces: N-53 integrated into `develop` at `0.1.0-dev.171` with independent review and passing hosted gates.

- [ ] **Step 1: Check exact worktree and diff.** Verify version `0.1.0-dev.171`, clean staged/committed state, restored route and handler files, focused PostgreSQL and application tests, and full canonical gate evidence all refer to the same final commit.
- [ ] **Step 2: Obtain independent review.** Dispatch a fresh-context reviewer for the full merge-base diff, including the ADR and feature-matrix truth comparison. Resolve any actionable finding with a behavior proof and repeat review on the changed head.
- [ ] **Step 3: Publish and verify.** Create a Draft PR to `develop`, attach it in Codex, verify its body and exact head, mark it ready after review, and require exact-head hosted CI to pass.
- [ ] **Step 4: Integrate and clean up.** Squash-merge to `develop`, require the develop push gate on the merge SHA to pass, fast-forward the primary checkout without discarding its pre-existing uncommitted investigation edit, and remove this clean worktree with the `finishing-a-development-branch` helper. Delete the exact local and upstream refs after worktree removal and verify all three absences.

## Acceptance

- The handler returns every public town and trail field from a deliberately distinct loaded world; field swaps, omissions, or wrong edge facts fail an independently authored expected-value assertion.
- The successful HTTP route serializes exactly the intended public map shape from a PostgreSQL-persisted session whose case file contains hidden culprit truth. The `404` negative for a missing session remains.
- The route and field mutation proofs fail for their intended reasons, and both production files are restored byte-for-byte before final validation.
- The existing before/after-start map comparison remains green. No gameplay, event history, map input, persistence schema, ADR, or feature dependency changed.
- The review, exact-head hosted gate, develop-push gate, fast-forward, and paired worktree/branch cleanup are verified.
