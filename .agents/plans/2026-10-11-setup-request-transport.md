# Repair setup request transport Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Send startup JSON bodies as `application/json` while preserving operation identity headers, so a player can complete setup, read the prologue, choose a starting town, and enter the game.

**Architecture:** Keep `requestJson` as the shared HTTP boundary. Merge its default JSON content type with caller-provided `RequestInit.headers` instead of allowing the later `init` spread to replace defaults. Keep request payloads, persisted operation IDs, backend validation, and server-owned game state unchanged.

**Tech Stack:** React, TypeScript, Vitest, ASP.NET minimal APIs, PostgreSQL, browser playtest.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, Hunt creation PG-001; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 14.

**Current source:** `develop` at `77592f74bf6c632d30917657f469d207eff1cd07`; authored application version `0.1.0-dev.92`.

**Decision records:** ADR-0016 keeps TanStack Query for web server state and ADR-0028 keeps event-backed server state authoritative. This repair preserves the existing HTTP and startup-operation contracts; no durable decision or product promise changes.

**Execution Strategy:** `executing-plans` because the failing API-client contract, shared header merge, and real startup browser flow form one narrow sequential path; one inline executor preserves the exact request and response context, while another implementation task would duplicate that context without an independent slice.

## Global Constraints

- The server remains authoritative for setup, prologue, town selection, and game state.
- Keep the JSON request bodies, `Idempotency-Key` values, operation fingerprints, and endpoint routes unchanged.
- Preserve caller-supplied headers and explicit content types while retaining the shared default `Content-Type: application/json`.
- Advance the sole authored application version from `0.1.0-dev.92` to `0.1.0-dev.93` in this PR; generated web identity remains build-derived.
- Retire the completed shared-read-recovery plan in this successor PR and keep this plan through its completing PR.

## Review Focus

- Startup requests must carry both their JSON content type and their persisted idempotency key; cover setup and first-town requests in the API-client tests and verify the actual browser network requests.
- A caller-provided `Content-Type` must remain the effective value when supplied; cover the shared merge behavior without adding a generic header-framework test matrix.

---

### Task 1: Merge JSON defaults with caller request headers

**Files:**
- Modify: `src/WildBunch.Web/src/tests/wildBunchApi.test.ts`
- Modify: `src/WildBunch.Web/src/tests/httpClient.test.ts`
- Modify: `src/WildBunch.Web/src/api/httpClient.ts`

**Interfaces:**
- Consumes: `setupGame(request, operationId)`, `startGameWithTown(gameId, request, operationId)`, and `requestJson<T>(path, init)`.
- Produces: a `fetch` request whose default JSON header survives call-specific headers and whose body and operation ID remain unchanged.

- [x] Add `Content-Type` assertions to the existing setup and first-arrival API-client tests:

```typescript
expect(new Headers(init.headers).get("Content-Type")).toBe("application/json");
```

- [x] Add one `requestJson` test supplying `Headers` with an explicit `Content-Type` and a custom header, then assert both values reach `fetch` unchanged. Temporarily omit the caller-header merge and confirm this test fails for the missing caller headers; restore the merge before continuing.

```typescript
const fetchMock = vi.fn().mockResolvedValue({
  ok: true,
  headers: new Headers({ "content-type": "application/json" }),
  json: async () => ({}),
});
vi.stubGlobal("fetch", fetchMock);

await requestJson("/api/example", {
  method: "POST",
  headers: new Headers({ "Content-Type": "application/vnd.wild-bunch+json", "X-Trace": "kept" }),
  body: "{}",
});

const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
const headers = new Headers(init.headers);
expect(headers.get("Content-Type")).toBe("application/vnd.wild-bunch+json");
expect(headers.get("X-Trace")).toBe("kept");
```

- [x] Run `npm --prefix src/WildBunch.Web test -- src/tests/wildBunchApi.test.ts -t "setup operation ID|first-arrival operation ID"` before changing production code. Confirm the assertion fails because the call-specific `headers` object replaces the shared default.
- [x] In `requestJson`, construct a `Headers` instance from the default JSON content type, merge every entry from `init?.headers` with `Headers.set`, then pass `{ ...init, headers }` to `fetch`. Keep the caller's explicit header value authoritative and keep the serialized body unchanged.
- [x] Rerun the focused tests and confirm the setup and first-arrival requests each preserve their expected body, idempotency key, and `application/json` content type, while an explicit caller `Content-Type` and other caller headers remain intact.
- [x] Run the HTTP-client tests with `npm --prefix src/WildBunch.Web test -- src/tests/httpClient.test.ts src/tests/wildBunchApi.test.ts`.

### Task 2: Prove setup through first-town selection in a real browser

**Files:**
- No tracked evidence files; use the repository's browser playtest procedure and report evidence in the PR.

**Interfaces:**
- Consumes: the worktree's API and Vite servers, PostgreSQL at `localhost:5435`, the startup flow, and the shared `requestJson` implementation from Task 1.
- Produces: observed successful setup and first-town requests from the matching worktree browser, followed by the active in-town phase.

- [ ] Ensure PostgreSQL with `.\tools\postgres-dev.ps1 ensure`; start this worktree's API and web servers with `.\scripts\dev-servers.ps1 ensure` and verify their actual URLs and worktree identity with `.\scripts\dev-servers.ps1 status`.
- [ ] Open the worktree web URL in a clean browser context. If an existing active local playthrough blocks setup, archive it through the confirmed in-game Start Over flow before continuing; do not delete or rebuild the database.
- [ ] Enter a player name and choose `Ride on`; observe `/api/games/setup` returning success with request `Content-Type: application/json` and a valid `Idempotency-Key`.
- [ ] Continue through the returned prologue with `Ride on`, select one town on the starting map, and observe `/api/games/{id}/start` returning success with the same JSON and operation-ID header contract. Confirm the game enters that town and no 415 response or browser console error occurs.
- [ ] Record the API URL, web URL, selected town, request/response statuses, and console/network result in the PR description; stop only the servers started for this worktree with `.\scripts\dev-servers.ps1 stop`.

### Task 3: Verify the slice and publish it through the epic route

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Delete: `.agents/plans/2026-10-11-shared-read-recovery.md`
- Keep: `.agents/plans/2026-10-11-setup-request-transport.md` through this PR.

- [ ] Confirm row 14 records merged PR #277, its merge commit, and the two successful hosted gates, while this setup transport plan remains the active row-14 slice.
- [ ] Confirm ADR-0016 and ADR-0028 remain accurate and `docs/features.md` does not change; no ADR or feature-matrix update is needed for the HTTP-header repair.
- [ ] Run `py -3 tools/run.py ci --check` on the final candidate, stage the intended files, inspect the staged diff, and commit through the check-only pre-commit hook.
- [ ] Request a fresh whole-branch review, resolve any findings, push the branch, open a non-draft PR to `develop`, and verify the exact PR head and hosted canonical gate.
- [ ] Merge the PR to `develop`, verify its develop-push gate, fast-forward the main checkout, then remove this clean worktree and its matching local and upstream branch refs together and verify each is gone.
