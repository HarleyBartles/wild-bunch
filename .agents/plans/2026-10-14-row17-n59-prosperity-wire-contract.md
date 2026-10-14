# N-59 Remove the DTO echo and prove prosperity wire values

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Remove the `PathSegmentDto` self-assignment test and protect the actual numeric `TownProsperity` contract at its event, API and web-consumer boundaries.

**Architecture:** `TownProsperity` is serialized numerically by `WorldGenerated` event storage and appears in `TownLayoutDto` returned by `GET /api/games/{id}`. The web app maps those numeric values to prosperity-specific sprite assets. Keep behavior proof at those paths; do not test an object literal against its own assignments.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [whole-test quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [frontend standards](../doctrine/frontend-standards.md); [validation policy](../doctrine/validation-policy.md).

**Execution Strategy:** `executing-plans` (Native inline). This is one cross-layer serialized-value contract and one adjacent test deletion. Do not change production code or product behavior.

## Global Constraints

- Start from `origin/develop` in a fresh worktree created with the bundled `using-git-worktrees/scripts/new_worktree.py`; target the PR to `develop`.
- Promote N-58 in this plan's first substantive commit, retire its completed plan, and advance only the authored version in `Directory.Build.props` from `0.1.0-dev.176` to `0.1.0-dev.177`.
- Preserve numeric prosperity codes `Boomtown=0`, `Prosperous=1`, `Poor=2`, `Destitute=3` across persisted `WorldGenerated` payloads, the `GameSessionDto` HTTP representation, and web asset selection.
- Remove only the `PathSegmentDto` coordinate self-test from `types.test.ts`; TypeScript compilation remains the type proof.
- No generated API platform, production mapper, enum representation, CSS asset, ADR, or feature-matrix change is expected.
- Run `py -3 tools/run.py ci --check` with PostgreSQL ensured. Commit hooks and hosted CI remain check-only.

## Review Focus

- The event serializer emits the four named prosperity values as their existing JSON numbers; the test fails if their numeric meanings change or strings replace them.
- The actual game HTTP response exposes numeric layout prosperity, and tests do not infer wire shape from a typed DTO alone.
- Web asset-selection behavior receives literal numeric wire values and maps each to its intended prosperity directory; tests do not source their inputs from the constants being tested.
- The coordinate object-literal echo is removed without replacing it with interface-presence or source-shape coverage.
- ADR-0016, existing prosperity-driven town presentation, and PG-003 remain unchanged; no durable product promise changes.

---

### Task 1: Promote N-58 and establish N-59

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n59-prosperity-wire-contract.md`
- Delete: `.agents/plans/2026-10-14-row17-n58-styling-policy-lint.md`

**Interfaces:**
- Consumes: merged PR #361, reviewed source `6a8ff013b9a47110e5065d5e63c9d97e31e15013`, squash merge `912d0d67f2b980d8b01f9254325d8d8cbce3b86b`, exact-head gate `38619790098`, develop-push gate `38620559460`, clean independent review, primary fast-forward with the user's investigation edit preserved, and verified removal of N-58's worktree and both branch refs.
- Produces: promoted N-58 delivery evidence, active N-59 at `0.1.0-dev.177`, and retirement of the completed N-58 plan.

- [x] **Step 1: Revalidate N-58 integration and cleanup.** Verify PR #361 is merged to `develop`, its recorded head and merge SHAs match, both hosted gates succeeded on those commits, the merge is contained in `origin/develop`, the primary checkout is at that merge with the pre-existing user edit intact, and N-58's worktree and exact local/upstream refs are absent.
- [x] **Step 2: Promote N-58, activate N-59, and advance the version.** Add the PR, source and merge SHAs, independent review, exact-head and develop-push gate runs, and paired worktree/ref cleanup evidence to row 17. Set the active plan/version to N-59 and `0.1.0-dev.177`. Change only the authored version in `Directory.Build.props`.
- [x] **Step 3: Retire the completed predecessor.** Remove the N-58 plan only after its delivery evidence is promoted; keep the baseline specification and investigation records.
- [x] **Step 4: Commit successor planning first.** Check whitespace and inspect the staged planning diff, then commit the roadmap, version, N-59 plan and N-58 plan retirement as `docs: plan N-59 prosperity wire contract` before editing tests.

### Task 2: Replace the tautology and prove the serialized contract

**Files:**
- Delete: `src/WildBunch.Web/src/tests/types.test.ts`
- Modify: `src/WildBunch.Web/src/components/town-hub/sprite-loader.test.ts`
- Modify: `tests/WildBunch.Integration.Tests/GameApiTests.cs`
- Create: `tests/WildBunch.Integration.Tests/TownProsperitySerializerTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`

**Interfaces:**
- Consumes: existing event serializer, game GET response, prosperity-aware asset selection, and the W-01 audit disposition.
- Produces: exact event JSON numbers, observable HTTP numeric encoding, observable web interpretation of literal numeric values, and a dated disposition explaining why the enum encoding checks are retained as behavior proof.

- [x] **Step 1: Establish current behavior.** Run the focused existing web tests. Inspect the `WorldGenerated` serializer and existing `GetGameByIdReturnsCreatedSession` HTTP scenario; confirm the setup fixture returns towns with layouts.
- [x] **Step 2: Add event serialization behavior proof.** Construct a `WorldGenerated` event containing one town per prosperity tier, serialize through `GameSessionJsonSerializer.SerializeEvent`, and assert each payload field is a JSON number with the established value `0`, `1`, `2`, or `3`. Prove the test fails when an expected serialized value or numeric representation changes.
- [x] **Step 3: Assert the real HTTP representation.** Use a deterministic MixedBag seed that includes all four tiers. Extend `GetGameByIdReturnsCreatedSession` to inspect actual response JSON and assert each `layout.prosperity` is numeric and matches the independently generated domain session by town ID; require the endpoint response to exercise all four codes `0` through `3`. Do not infer JSON representation from `ReadFromJsonAsync` or the same returned DTO alone.
- [x] **Step 4: Assert web behavior from wire-shaped inputs.** Delete `types.test.ts`, whose only two cases are the `PathSegmentDto` assignment echo and direct `TownProsperity` constant self-check. In the existing sprite-loader prosperity table, use literal numeric wire values paired with their intended directories so changing a frontend constant or mapping causes a behavioral failure.
- [x] **Step 5: Prove negatives and record disposition.** Confirm event values, the API prosperity projection and web mappings fail under targeted mutations, restore production files, and add dated N-59 dispositions to both audit records. State that coordinate type safety remains compile-time responsibility and the current numeric enum values are a persisted/API/client contract. No production source, ADR or feature promise changes.
- [x] **Step 6: Validate the final slice.** Run focused serializer and API tests, the focused sprite-loader/types Vitest files, the full web lane, `git diff --check`, and the complete canonical `ci --check` after ensuring PostgreSQL. Commit implementation and audit changes as `test: prove prosperity wire contract`.

### Task 3: Review, publish, integrate, and retire N-59

**Files:**
- Review: complete merge-base diff, N-58 promotion, event/API/web prosperity behavior and dated audit dispositions.
- Update: row 17 receives N-59 delivery evidence in the next successor's first substantive commit.

**Interfaces:**
- Consumes: committed behavior proof for serialized prosperity values and the passing check-only canonical gate.
- Produces: N-59 integrated into `develop` at `0.1.0-dev.177`, independently reviewed and verified by hosted CI.

- [ ] **Step 1: Verify committed state.** Confirm the exact head, clean worktree, focused results, full canonical local gate and version before publication.
- [ ] **Step 2: Obtain independent whole-branch review.** Review against unslop, testing, API/event serialization boundaries, ADR catalogue and feature matrix. Resolve actionable findings with behavior proof and obtain a fresh clean re-review.
- [ ] **Step 3: Publish and verify.** Push the branch, create a Draft PR to `develop`, verify body/base/head and exact SHA, attach if supported, mark ready after review, and require the exact-head hosted gate to pass.
- [ ] **Step 4: Integrate and clean up.** Squash-merge to `develop`, require the develop-push gate on the merge SHA, fast-forward the primary checkout while preserving its user edit, remove the clean worktree with the `finishing-a-development-branch` helper, delete the exact local/upstream refs, and verify all are absent.

## Acceptance

- The coordinate literal self-test is gone and is not replaced by another source-shape or type-presence assertion.
- Persisted event serialization uses the established numeric prosperity meanings, and the actual game endpoint sends a numeric value in its layout DTO.
- The frontend prosperity selection behavior maps literal values `0–3` to the correct directories.
- The test-quality and web follow-up records state why these values are a real contract and why no product behavior or decision changed.
- N-58 is promoted and retired only after its exact PR, independent review, both hosted gates, primary fast-forward and paired cleanup facts are verified.
- N-59 is independently reviewed, hosted CI passes on its exact PR head and develop merge SHA, and its worktree plus exact branch refs are removed together.
