# N-77: Reconcile the residual test inventory

**Goal:** Close roadmap row 17 only after reconciling every remaining test finding against current behavior owners and dated evidence. Preserve historical audit observations while correcting stale statements that still imply resolved gaps are open. Retain any genuinely uncovered 0.1.0 invariant as a named residual; do not invent tests for unsupported promises or conditional future behavior.

**Architecture:** This slice changes investigation and roadmap truth only unless the final audit identifies a concrete, already-approved 0.1.0 invariant whose behavior lacks an owner. Current tests and source are implementation evidence; the specification and feature matrix define release scope; dated investigation dispositions preserve what was observed and when. Do not alter production behavior or durable decisions.

**Tech Stack:** Markdown, PowerShell, Python command bus, Git, GitHub PR and hosted CI.

**Specification:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; roadmap row 17 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; test inventory `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md` and its linked layer follow-ups.

**Execution Strategy:** Native inline execution. The work is one coupled audit-to-documentation reconciliation where each finding's disposition depends on current source, tests and later dated evidence; handing off separate files would increase context reconstruction without creating independent implementation.

## Global Constraints

- Use the committed N-77 plan and the repository's `.agents/` entry guidance, unslop playbook, testing playbook, validation doctrine and decision-record playbook.
- Preserve audit-time observations and dated history. Add dated corrections or disposition entries instead of rewriting what was true at the time.
- Do not add tests that assert document presence, section names, fixed code shape, or unsupported future behavior. Do not add a test when existing behavior coverage already proves the invariant.
- Do not change gameplay, event history, persistence schemas, ADRs, feature promises or feature assessments unless the evidence demonstrates a real durable change; this plan expects no such change.
- Reconcile the known stale residuals against live evidence: N-15 integration negatives; N-19 map-generator claims; N-20 travel-context and arrival-reset coverage; and the N-33 completion/replay residual superseded by N-39. Verify any other finding still explicitly described as open in the test inventory and linked layer follow-ups before classifying it.
- Close row 17 only when included 0.1.0 behaviors have meaningful current owners and every remaining test observation is either historically resolved, explicitly conditional, outside the approved 0.1.0 contract, or recorded as a concrete uncovered release invariant with its next JIT slice. Do not let stale prose imply an open bug or silently erase a real gap.
- Retire only the completed N-76 plan in this successor. Preserve the active specification, roadmap and investigations that remain useful or govern the epic.
- Advance only the authored application version from `0.1.0-dev.194` to `0.1.0-dev.195` for this PR.
- Use the canonical fail-fast command bus and check-only hook. Never bypass validation or let a hook mutate the staged candidate.
- Publish a Draft PR to `develop`; require a fresh independent whole-branch review and passing exact-head hosted CI before merge, then passing develop-push CI after merge.
- After verified merge, fast-forward the primary checkout while preserving its unrelated user edit, then use `remove_worktree.py --check` and `--apply` before deleting and verifying only this plan's exact local/upstream branch refs.

## Review Focus

- Does each status correction point to the current behavior owner or a later dated disposition, rather than treating old audit prose as current implementation truth?
- Are all genuine unresolved 0.1.0 test gaps retained with an observable invariant and a concrete next owner, while unsupported/conditional cases stay deferred?
- Does the row 17 exit claim match the evidence and release scope in the specification and feature matrix?
- Are historical findings preserved without redundant test additions, structural document checkers or invented intent?
- Do the roadmap, `Directory.Build.props`, PR delivery record and N-77 disposition agree on the merged version and reviewed source?

### Task 1: Commit the JIT plan and retire N-76

**Files:** `.agents/plans/2026-10-15-row17-n77-residual-test-inventory.md`, `.agents/plans/2026-10-15-row17-n76-phaser-hover-browser-proof.md`, `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `Directory.Build.props`.

- [x] Confirm this worktree is clean on `codex/row17-n77-residual-test-inventory`, based on refreshed `origin/develop` at `3961d7896474d63a55f70357606f3a5f45c89a5e`.
- [x] Record the missing N-75 delivery narrative for PR #378, reviewed source `b8060e2d09bba901d0e216ac8049666a7f082547`, squash merge `aed37561cb08fd5c570bf6c3f8e77e300189be4b`, exact-head hosted gate `38703144397` and develop-push gate `38703943112`; then record N-76 PR #379, reviewed source `c89571d89f57abc81e8fcbb710901e48dd4f47d4`, squash merge `3961d7896474d63a55f70357606f3a5f45c89a5e`, independent review, exact-head hosted gate `38708150101`, develop-push gate `38708955863`, local canonical gate and verified worktree/branch cleanup in the roadmap.
- [x] Point row 17 at N-77 and `0.1.0-dev.195`; link merged PR #379 and append N-76 delivery evidence without changing the current row exit before reconciliation.
- [x] Delete the completed N-76 plan in this substantive successor commit and set `Directory.Build.props` to `0.1.0-dev.195`.
- [x] Run the staged-candidate check-only hook and commit the plan, successor retirement, roadmap and version before the reconciliation work. Commit `9929c13f` passed the staged check-only gate; all .NET suites passed (533 Domain, 1 skipped; 1 API; 163 Application; 158 GameContent; 332 Integration), Python passed (70 tests), Vitest passed (375 tests), Chromium passed (4 tests), and the production web build passed.

### Task 2: Verify residual findings against current owners

**Files:** `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`, `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`, `.agents/investigations/stable-0.1.0/2026-10-07-domain-test-followup.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`, `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`, plus only source/test files needed to verify a named residual.

- [x] Read the remaining open-status language in the consolidated test-quality investigation and linked follow-ups. For each claim, locate its latest dated disposition and current test owner; do not rely on plan summaries or test names.
- [x] Verify N-15 against `UpcasterCorrectnessTests.Registry_DuplicateTransition_ThrowsAtConstruction`, the blank/retired-category `DevTravelEndpointTests` cases and the rejected-purchase `GameApiPurchaseTests` scenarios, including read-back of inventory, log/journal and audit history.
- [x] Verify N-19 against current `MapGeneratorBruteForceAnalysisTests`, `MapGeneratorTests`, `TerrainAssignerTests`, and the dated 2026-10-13 map dispositions; close only behaviors whose meaningful assertion and intended failure reason remain present.
- [x] Verify N-20 and N-33 against `GetTravelDevContextHandlerTests`, the Domain arrival/context-refresh test, and `Replay_FullQuietJourneyCycle_ReconstructsCompletedArrival`; reconcile stale statements with the later N-39 completion/replay proof.
- [x] For every other explicit open residual, record the exact invariant and test owner or its reason for conditional/out-of-scope treatment. No included 0.1.0 invariant was found to be uncovered, so row 17 is closed by the final dated reconciliation.
- [x] Do not rerun expensive suites merely to reread existing evidence. The prior staged-candidate check exercised all owners on the same source tree; no focused rerun was needed.

### Task 3: Write the dated reconciliation and set the honest row exit

**Files:** The investigation documents identified in Task 2, `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, and `Directory.Build.props` only as required by Task 1.

- [x] Add a dated N-77 reconciliation to the consolidated test-quality investigation that records which stale open claims are closed by existing behavior owners, which observations remain historical, and any actual residual test slice. Preserve the original findings and previous dated entries.
- [x] Add concise dated corrections to linked follow-ups only where their latest wording still asserts a resolved gap; avoid duplicating the full disposition across multiple documents.
- [x] Mark row 17 complete and row 18 as the next pending JIT outcome because all exit conditions are met; no current invariant remains uncovered.
- [x] State the ADR and feature-matrix comparison in the PR: this slice changes test-evidence status only; ADRs and product promises remain unchanged.

### Task 4: Validate, review, publish and close out N-77

**Files:** The N-77 committed diff and review package.

- [x] No focused test rerun was needed because this slice changes no tests or source. The final staged-candidate check-only gate passed on the tree committed as `e63bbbab`: all .NET suites (533 Domain, 1 skipped; 1 API; 163 Application; 158 GameContent; 332 Integration), 70 Python tests, 375 Vitest tests, 4 Chromium tests and production build.
- [x] Inspect the committed diff and verify the row exit, N-77 dated reconciliation and `Directory.Build.props` consistently use the evidence and version `0.1.0-dev.195`.
- [ ] Dispatch a fresh-context whole-branch reviewer at GPT-6 Luna with exact base/head, diff, this plan and the baseline specification. Require independent ADR and feature-matrix comparison; resolve Critical/Important findings and obtain fresh review after corrections.
- [ ] Publish the Draft PR to `develop`, include focused and canonical local proof plus reviewer outcome, then mark ready and require exact-head hosted CI to pass before merge.
- [ ] Verify develop-push CI for the squash merge, fast-forward the primary checkout without disturbing its unrelated edit, and use `remove_worktree.py --check` then `--apply` before deleting and verifying the exact local and upstream branch refs.
