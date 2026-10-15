# N-66: Starting-Town Map Data Handoff

**Status:** Active

**Goal:** Prove that the session-scoped world-map query supplies the starting-town map host with the towns the player can choose, while retaining the distinct loading, unavailable, and retry behavior already implemented.

**Architecture:** React Query owns loading the world map for the current session. `StartingTownStep` gives the returned DTO to `PhaserMapHost`; the renderer displays supplied map facts, and its existing selection intent returns through React. The renderer's own drawing and legal selection behavior remains owned by `PhaserMapHost` tests. Do not add another map, selection model, or production behavior.

**Tech Stack:** TypeScript, React Testing Library, Vitest, TanStack Query, Phaser test doubles, Prettier, ESLint, and the Wild Bunch validation command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [W-07 whole-test finding](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [web-test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [PG-003 world-map and first-arrival contract](../investigations/stable-0.1.0/2026-10-07-playthrough-lifecycle-contract.md); [decision-record playbook](../playbooks/decision-records.md); [frontend standards](../doctrine/frontend-standards.md); [validation policy](../doctrine/validation-policy.md).

**Execution Strategy:** `executing-plans` (Native inline). The authorized roadmap goal requires inline execution and JIT planning from the refreshed source.

## Global Constraints

- Start from refreshed `origin/develop` in this fresh canonical worktree and target the PR to `develop`.
- Promote N-65's merged delivery, retire its completed plan, and advance the sole authored application version once to `0.1.0-dev.184` in `Directory.Build.props`.
- Preserve the single shared world map and current server-authoritative start sequence. Do not change production code, API shape, copy, accessibility scope, ADRs, or feature promises.
- Keep N-54's settled empty/error/retry behavior. Do not re-add a duplicate test for a renderer role or for selection behavior already owned by `PhaserMapHost` and N-65.
- Use a narrowly scoped `PhaserMapHost` test double only to observe the `StartingTownStep` composition boundary: unique session-returned town names must reach the map host after the request resolves. The renderer's real drawing and selection tests remain the owners of those behaviors.
- Preserve the primary checkout's uncommitted investigation edit by not touching its file. Keep generated artifacts and test-result receipts out of the commit.
- Run focused web tests and format/lint checks, then the canonical fail-fast `py -3 tools/run.py ci --check` on the committed candidate.

## Review Focus

- The test uses a distinct session ID and a pending map response, checks that the loading message appears while no map is available, then resolves a world whose town names cannot come from a default or hard-coded map.
- The rendered test double exposes the names it receives from the map DTO. The test must fail if `StartingTownStep` stops forwarding the session's fetched world map.
- `PhaserMapHost.test.tsx` continues to own actual renderer drawing, selection callback, eligibility, React-controlled selection, and resize layout. N-65 owns the successful start-flow town intent. N-54 owns settled empty/error/retry recovery.
- Reconcile W-07 precisely: loaded-data composition, legal town selection, and terminal recovery are now covered by their distinct behavior owners. Do not claim this component test proves event persistence or server command semantics.

---

### Task 1: Promote N-65 and establish N-66

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-15-row17-n66-start-map-data-handoff.md`
- Delete: `.agents/plans/2026-10-15-row17-n65-startflow-consolidation.md`

**Interfaces:**
- Consumes: PR #368 merged to `develop` from reviewed head `035e52c2fffad0346c773f9e47736ecc09fe0784`, merge `9759025618a6649c391d47c8f4ab09493f0f985d`, exact-head gate `38657638856`, develop-push gate `38658212475`, independent review with no actionable findings, and verified N-65 worktree plus paired ref retirement.
- Produces: promoted N-65 delivery evidence, active N-66 at `0.1.0-dev.184`, and retirement of the completed N-65 plan.

- [x] Verify PR #368 is merged to `develop`; its head and merge identities match the values above; both hosted gates passed; the merge is contained in refreshed `origin/develop`; the primary checkout remains current with the user edit preserved; and the N-65 worktree plus exact local/upstream refs are absent.
- [x] Add PR #368 to row 17's delivery references, make N-66 the active plan, and record N-65's disposition and paired cleanup in a dated roadmap delivery note.
- [x] Advance only `Directory.Build.props` from `0.1.0-dev.183` to `0.1.0-dev.184`; retire N-65 and commit this plan, roadmap, and version update together before changing tests.
- [x] Review the staged planning diff, confirm no generated version copies are tracked or edited, run `git diff --check`, and commit through the normal staged-candidate hook.

### Task 2: Prove the session map reaches the starting-town renderer

**Files:**
- Modify: `src/WildBunch.Web/src/tests/StartingTownStep.test.tsx`

**Interfaces:**
- Consumes: the `StartingTownStep` session identity, the `getWorldMap` query result, and `PhaserMapHost`'s supplied map DTO.
- Produces: one behavior test proving a pending session map request resolves into the map host with its actual returned towns.

- [x] Run the current `StartingTownStep.test.tsx` and `PhaserMapHost.test.tsx` suites before editing; confirm the baseline is green.
- [x] Replace the weak successful map-shell assertion with a deferred query scenario: verify pending copy, exact session-scoped query invocation, then distinct returned town names at the map-host composition boundary after resolution.
- [x] Remove the standalone loading-only test because the new pending-to-loaded scenario proves the same state transition; preserve independent settled-empty, rejected-read/retry, no-session, forward-flow, and player-copy contracts.
- [x] Temporarily change the production `StartingTownStep` map handoff so the returned map is not supplied to the host; verify the new behavior test fails, restore production source byte-for-byte, and rerun both focused suites.

### Task 3: Reconcile W-07 ownership and evidence

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`

**Interfaces:**
- Consumes: N-54 map-read recovery, N-65 map selection in the successful start flow, and N-66 session-query-to-host composition.
- Produces: a current W-07 disposition and test inventory that no longer describes covered map loading, selection, or recovery behavior as absent.

- [x] Update the `StartingTownStep.test.tsx` inventory to name the distinct query-to-render, loading, empty, failed-read/retry, and no-session behaviors it owns.
- [x] Add a dated W-07 disposition that cites N-54, N-65, and N-66 as separate owners; state the mutation proof and the scope it closes.
- [x] Keep renderer drawing/eligibility under `PhaserMapHost` tests and command/persistence guarantees under their existing owners; do not add redundant assertions to those files.
- [x] Confirm PG-003 and ADR-0035, ADR-0039, and ADR-0046 remain accurate; leave the feature matrix unchanged because no player promise or dependency changes.

### Task 4: Validate and commit N-66

**Files:**
- Review: the complete candidate from Tasks 1-3.

- [x] Run the focused `StartingTownStep.test.tsx` and `PhaserMapHost.test.tsx` suites, formatter/linter checks for the changed test, and `git diff --check` for prose.
- [x] Run the canonical fail-fast `py -3 tools/run.py ci --check` from the repository root; resolve its first failure before continuing.
- [x] Inspect the full candidate diff and confirm it contains only N-65 promotion, one version increment, query-to-render test proof, and accurate W-07 reconciliation.
- [x] Commit the test correction and audit disposition through the normal check-only staged-candidate hook.

### Task 5: Review and deliver N-66

- [ ] Prepare a whole-branch review package against this plan, W-07/PG-003, frontend standards, the play-surface and code-review unslop profiles, ADR-0035/0039/0046, and the feature matrix. Obtain an independent fresh-context review with GPT-6 Luna; resolve actionable findings and request fresh review of the corrected head.
- [ ] Push the branch and create a Draft PR to `develop`; verify its body, base, head SHA, and draft state. State that no durable decision or feature promise changed. Mark ready after clean review and require exact-head hosted CI to pass.
- [ ] Squash-merge under the active goal authorization and require the develop-push gate to pass on the merge SHA.
- [ ] Fast-forward the primary checkout without disturbing the user's existing investigation edit. Verify the merged tree, then remove this clean worktree with the finishing helper, delete only its exact local/upstream refs, and verify all three absences before the next plan.

## Acceptance

The starting-town surface proves that the map the current session actually returned reaches its map host after a real pending interval. Empty/error/retry and no-session behavior remain protected, map rendering and selection remain under their existing owners, W-07 has no stale missing-behavior claim, and the merged version is `0.1.0-dev.184` with the worktree and paired refs retired.
