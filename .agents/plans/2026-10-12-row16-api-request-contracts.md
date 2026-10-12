# Row 16 API Request Contract Ownership Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Keep HTTP request contracts in the API adapter and make the shipped `.http` sample call the current setup endpoint.

**Architecture:** ASP.NET Minimal API endpoints own transport binding and validation, then map requests to Application commands. Application owns handlers, commands, queries and output/read-model DTOs; it does not own HTTP request bodies. Developer requests remain outside player commands under ADR-0036. This slice moves only four currently mapped developer request records and does not change their JSON shapes or endpoint behavior.

**Tech Stack:** C#/.NET 10, ASP.NET Core Minimal APIs, PostgreSQL integration tests, Visual Studio/REST Client `.http` request format.

**Spec and evidence:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, row 16 residual source dispositions; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, rows 15 and 16; `.agents/investigations/stable-0.1.0/2026-10-06-stable-0.1.0-investigation.md`, API folder ownership; `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`, developer HTTP request model dispositions; `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, WB-23 API sample/route context.

**Execution Strategy:** Native inline execution. The transport move and current sample form one API-boundary change with unchanged behavior, and one focused whole-branch review is the appropriate cost.

## Global Constraints

- Keep Minimal API routes, JSON property names, required fields, validation, status codes, idempotency behavior, handler commands and application results unchanged.
- Move only the four live developer HTTP request records: `ForceTravelOverrideRequestDto`, `ForceSaloonOverrideRequestDto`, `ForceDevDifficultyRequestDto` and `SetDevEntropyRequestDto`. Keep developer output DTOs and all handlers/commands in Application.
- Do not restore the retired `LockRngRequestDto`, its route, or any retired town-layout preparation surface.
- Keep `WildBunch.Application` independent of ASP.NET/API types. No project reference, endpoint logic, gameplay rule or authorization behavior changes.
- Repair `WildBunch.Api.http` to demonstrate the mapped `POST /api/games/setup` contract with its required `Idempotency-Key`, player name and valid seed code. Preserve the current default difficulty and entropy by omitting those optional JSON fields. The sample demonstrates setup only; it must not imply the game is started before the prologue and starting-town choice.
- Review ADR-0015 and ADR-0036 against the final diff. This implements their existing HTTP-boundary and developer/player separation decisions; no ADR or feature-matrix change is expected.
- The first substantive commit retires the completed `.99` plan, records PR #284's verified merge and both hosted gates plus worktree/local/upstream-ref cleanup in row 15, starts this plan in row 16, and advances `Directory.Build.props` once to `0.1.0-dev.100`.
- Deliver one PR to `develop`. After exact-head hosted CI and develop-push CI pass, fast-forward the primary checkout, remove the clean worktree first, then remove and verify the matching local and upstream branch refs.

## Review Focus

- The four request records are transport-only types owned by API; Application keeps its commands, handlers and result/read DTOs.
- Existing endpoint behavior tests still prove valid and invalid developer requests through HTTP, and startup integration tests prove setup's current route and required idempotency header.
- The `.http` sample matches the route and required inputs actually mapped by `GameSessionEndpoints`; it does not call a deleted route or claim to complete game start.
- The diff does not create structural tests that freeze namespaces, folder inventories, filenames or `.http` text.

---

## Tasks

### Task 1: Correct API request ownership and the local HTTP sample

**Files:**
- Create: `src/WildBunch.Api/Dev/Requests/ForceTravelOverrideRequestDto.cs`
- Create: `src/WildBunch.Api/Dev/Requests/ForceSaloonOverrideRequestDto.cs`
- Create: `src/WildBunch.Api/Dev/Requests/ForceDevDifficultyRequestDto.cs`
- Create: `src/WildBunch.Api/Dev/Requests/SetDevEntropyRequestDto.cs`
- Delete: the four matching request records under `src/WildBunch.Application/Dev/Models/`
- Modify: `src/WildBunch.Api/Dev/DevEndpoints.cs`
- Modify: integration-test imports at the existing developer-endpoint consumers
- Modify: `src/WildBunch.Api/WildBunch.Api.http`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row15-town-rendering-primitives.md`
- Keep through this PR: `.agents/plans/2026-10-12-row16-api-request-contracts.md`

**Interfaces:**
- Consumes: current API request records, `/api/dev` minimal endpoints, the current `/api/games/setup` contract and existing endpoint integration tests.
- Produces: API-owned request records with identical serialized fields and nullability; Application command/result contracts remain in place; a current, manually runnable setup example.

- Run existing `DevTravelEndpointTests`, `DevSaloonEndpointTests`, `DevSessionEndpointTests` and `StartupOperationEndpointTests` before editing to establish current behavior. These tests are the behavioral proof; do not add tests that only assert file placement or sample text.
- Move the four request records to `WildBunch.Api.Dev` under `Dev/Requests`, preserving names, members, required annotations and JSON binding behavior. Update the endpoint and integration-test namespaces that consume them.
- Leave the `.http` sample's host variable; change the request to `/api/games/setup`, include a valid example `Idempotency-Key`, and supply required `playerName` and `seedCode` fields. Keep optional difficulty/entropy omitted so the documented defaults apply.
- In the first substantive commit, classify `.99` as wholly complete from PR #284 and its exact-head/develop gate evidence, update row 15 with final source/head/merge/run identities and paired cleanup verification, link this plan as row 16's active JIT plan, advance the authored version to `0.1.0-dev.100`, and retire the `.99` plan. The stable specification and investigation records remain live.
- Verify no source consumer remains for these request types in Application and no removed DTO was a handler or output contract; do not restore historical disabled request types.
- Run the four focused integration-test classes and `dotnet build src/WildBunch.Application/WildBunch.Application.csproj`; inspect the `.http` request directly against `SetupGameRequest`, `RequestValidation`, and `GameSessionEndpoints` rather than adding a change-detector test.
- Run `py -3 tools/run.py ci --check` once for the final candidate, plus `git diff --check` and the generated version identity check for `0.1.0-dev.100`. Let the normal staged-snapshot pre-commit hook run the complete fail-fast gate on the intended commit; do not repeat the full gate immediately before or after a successful hooked commit.
- Compare the final diff with ADR-0015, ADR-0036 and `docs/features.md`; record in the PR why no durable decision or product promise changes.
- Obtain one fresh whole-branch review, publish the PR to `develop`, and verify the exact PR head and both hosted gates before merge.
- After merge, verify PR head and merge identities and hosted results, fast-forward `Z:\wild-bunch`, remove this worktree before deleting its paired local/upstream refs, and verify all three are absent.

**Validation:** Existing developer endpoint and startup HTTP integration behaviors; Application project build; no remaining Application consumer of the moved request records; manually reviewed `.http` request contract; full `ci --check`; generated identity `0.1.0-dev.100`; whole-branch review; exact-head PR and post-merge develop CI.
