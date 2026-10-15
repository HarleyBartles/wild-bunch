# N-74: Prove Prologue Session Authority Under Nondefault Setup

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove that a refreshed saved game's prologue describes its persisted case even when nondefault setup options and conflicting legacy query values would describe another case, and correct the stale WB-06 audit recommendation.

**Architecture:** Setup persists the resolved case in the session event history. The prologue query receives a session ID and reads that case; the browser reload path fetches the saved session and calls the prologue API with its ID. Strengthen the existing PostgreSQL API integration test at the session boundary and retain the existing routed browser test for session-ID handoff; no production behavior changes.

**Tech Stack:** .NET 10, xUnit, PostgreSQL integration test fixture, React, Vitest, Python command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [web layer investigation](../investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [PG-001 feature contract](../../docs/features.md#pg-001--start-a-hunt-and-choose-the-first-town).

**Execution Strategy:** `executing-plans` (native inline execution). The PostgreSQL proof and its evidence correction share one bounded session-authority claim; a single executor can make the scenario fail for the intended mutation, update its owning inventories and review their consistency without extra implementation handoffs.

## Global Constraints

- Start from this fresh worktree at the verified `origin/develop` commit and commit this plan before implementation.
- In the first substantive implementation commit, promote N-73 delivery into the roadmap, retire its completed plan, and advance `Directory.Build.props` from `0.1.0-dev.191` to `0.1.0-dev.192`.
- Preserve the current one-game flow: setup settles the case before prologue, prologue reads only by saved session identity, and its lead comes from that session's persisted `CaseFile`.
- Keep `StartFlow.test.tsx` as the browser-facing refresh/session-ID handoff proof; it is a mocked route test, not end-to-end evidence. Keep the PostgreSQL integration test as proof that the API returns the persisted session's case lead.
- Use nondefault `Challenging` and `Wild` setup options. Derive the setup and conflicting replacement UUIDs from explicit `SeedWorld` values through `CreateRepresentativeSeedCode`; the replacement world has a different default culprit index and must yield a different descriptor. The clue difference comes from the changed seed, not the difficulty or entropy values, which ADR-0040 says do not change settled mystery facts.
- Mutation-prove the test by temporarily making `GetPrologueHandler` resolve a descriptor from a different seed and default options; the focused integration test must fail on the expected public descriptor assertion. Restore production code exactly and rerun green.
- Correct current WB-06 and follow-up recommendations while preserving dated historical observations. Update PG-001 evidence, but do not change its promise or add a browser-backed/deployed resume claim.
- Compare the actual diff with ADR-0021, ADR-0039, and ADR-0040 and state why their decisions remain true; do not amend ADRs unless source evidence shows a decision changed.
- Keep authored Markdown paragraphs and list items on one physical source line, use regular hyphens, and add no empty sections.

## Files and Responsibilities

- `tests/WildBunch.Integration.Tests/PrologueHiddenTruthTests.cs` owns the HTTP boundary assertion that the public prologue descriptor matches the persisted session case and leaks no hidden culprit internals.
- `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md` owns WB-06's current assessment and dated evidence dispositions.
- `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md` owns the proposed/current browser test disposition and the scoped resume evidence boundary.
- `docs/features.md` owns PG-001's current evidence description and explicit limits.
- `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` promotes N-73 and records N-74's delivery after merge.
- `.agents/plans/2026-10-15-row17-n73-retire-seed-codec-scaffolding.md` is a completed predecessor eligible for retirement in N-74's first substantive implementation commit.
- `Directory.Build.props` owns the single development version identity, advanced once in the same substantive commit.

## Review Focus

- Setup stores `Challenging`, `Wild`, and a fixed seed before the prologue request.
- The test fixture proves its deliberately conflicting replacement seed resolves to a different public descriptor, so an implementation that regenerates from request values cannot pass accidentally; the UUIDs are derived from explicit seed-world fixtures without brute-force search or raw UUID literals.
- The endpoint response still contains the persisted case descriptor and does not reveal hidden culprit internals.
- WB-06 no longer claims local seed/difficulty/entropy reach prologue generation, and no documentation overstates mocked refresh coverage as browser-backed persistence proof.
- ADR-0021, ADR-0039, and ADR-0040 remain accurate because the UUID seed still establishes world truth, difficulty and entropy remain separate from settled culprit identity, setup settles mystery facts before prologue, and event replay restores those facts.

---

### Task 1: Commit the JIT plan

**Files:** `.agents/plans/2026-10-15-row17-n74-prologue-session-authority.md`.

- [x] Verify this worktree is clean, based on `c1634a446a7786b325f9d327e82691c2e8108321`, and has the dedicated `codex/row17-n74-next-web-test-slice` branch.
- [x] Commit this plan before changing implementation, evidence inventories, version, or prior planning artifacts; let the normal staged-candidate hook run.

### Task 2: Strengthen saved-session prologue behavior proof

**Consumes:** The committed plan and the N-73 delivery evidence in the current roadmap.

**Files:** `tests/WildBunch.Integration.Tests/PrologueHiddenTruthTests.cs`.

- [x] Start the shared PostgreSQL service with `./tools/postgres-dev.ps1 ensure` and run the existing `PrologueEndpoint_UsesSettledSessionCaseAndDoesNotLeakHiddenCulpritInternals` test to establish its current baseline.
- [x] Derive the setup UUID from `SeedWorldResolver.CreateCanonicalSeedWorld()` and derive the replacement UUID from that seed world with a distinct valid `DefaultCulpritIndex`; call `SeedWorldResolver.CreateRepresentativeSeedCode` for both rather than hard-coding UUIDs or searching across seeds.
- [x] Update setup to use `GameDifficulty.Challenging` and `GameEntropy.Wild`; assert the stored session retains those nondefault options without asserting that they change the settled mystery facts.
- [x] Assert that the replacement seed's Standard/Classic descriptor differs from the persisted case descriptor, then issue the existing session-scoped prologue request with the conflicting legacy query values.
- [x] Keep the public response assertions for the established descriptor and hidden-truth secrecy; do not expose the culprit identifier or test only the presence of a descriptor-shaped string.
- [x] Temporarily change `GetPrologueHandler` to resolve from a different setup seed with Standard/Classic defaults, run the focused integration test, and confirm it fails because the returned clue does not match the persisted case; restore production source exactly and rerun the focused test green.

### Task 3: Reconcile WB-06, test follow-up, and PG-001 evidence

**Consumes:** Task 2's passing focused test and the verified existing saved-session flow.

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`, `docs/features.md`, `docs/decisions/ADR-0021-uuid-shaped-setup-seeds-resolve-to-legal-starting-world-descriptors.md`, `docs/decisions/ADR-0039-seeded-game-setup-and-free-first-arrival.md`, `docs/decisions/ADR-0040-difficulty-and-randomness-vocabulary.md`.

- [x] Replace WB-06's stale current assertion that `StorySoFarStep` regenerates a descriptor from local setup inputs with a dated historical observation and the current session-bound implementation/evidence; retain broader unresolved resume and legal-phase findings.
- [x] Update the StorySoFar test disposition and resume scenario wording to distinguish the mocked StartFlow proof (saved session ID is used for the prologue request) from the PostgreSQL API proof (returned clue matches the persisted case under nondefault options and conflicting values); preserve GET error/retry and pending negatives.
- [x] Update PG-001's prologue evidence sentence with nondefault persisted options and deliberately conflicting values, retaining its explicit limit that these tests do not establish browser-backed end-to-end or deployed resume.
- [x] Read the relevant catalogue entries and all three ADRs against the diff; leave ADR files unchanged and state the decision comparisons in the PR body if neither durable decision changed.
- [x] Add dated N-74 evidence to the web investigation and web test follow-up so the original observation remains historically legible while the proposed stale test work is marked resolved.

### Task 4: Promote N-73 and establish N-74 version identity

**Consumes:** The committed N-74 plan, Task 2's verified test result, and Task 3's corrected evidence.

**Files:** `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/plans/2026-10-15-row17-n73-retire-seed-codec-scaffolding.md`, `Directory.Build.props`.

- [x] Verify PR #376 merged to `develop` from source `515db382b8e53fbd95c0bc5ad111ec314d222b9e` as squash merge `c1634a446a7786b325f9d327e82691c2e8108321`, the fresh final review found no actionable findings, exact-head gate `38691636873` and develop-push gate `38692278098` passed, and N-73's clean worktree plus exact local/upstream refs are absent.
- [x] Update row 17 to name N-74 as active at `0.1.0-dev.192`; append dated N-73 delivery evidence with the PR, source and merge SHAs, review outcome, both hosted gates, primary fast-forward, and paired worktree/local/upstream branch cleanup.
- [x] Retire the completed N-73 plan in this first substantive successor commit while retaining the roadmap, baseline specification, and live investigation inventories.
- [x] Change only `Directory.Build.props` for product version advancement from `0.1.0-dev.191` to `0.1.0-dev.192`; include Task 2 and Task 3 changes in this same first substantive implementation commit.

### Task 5: Validate, review, publish, and close out N-74

**Consumes:** Tasks 2 through 4.

**Files:** The N-74 implementation diff and its prepared review package.

- [x] Run the focused PostgreSQL integration test after the mutation is restored; run relevant prologue application and web behavior suites only if source/test changes make them applicable.
- [x] Run `py -3 tools/run.py ci --check` on the intended committed tree; use the check-only pre-commit hook for commits and never bypass it.
- [x] Inspect the committed diff, confirm only planned files changed, check references to the stale WB-06 wording, and compare the actual diff with ADR-0021, ADR-0039, ADR-0040 and PG-001.
- [ ] Prepare and dispatch a fresh-context whole-branch reviewer with exact base/head and diff; require independent ADR and feature-matrix comparison, resolve Critical/Important findings with a new review, and record deferred Minor findings in the handoff.
- [ ] Publish a Draft PR to `develop` with the behavior contract, author ADR/feature comparisons, exact local evidence, and reviewer outcome; mark ready after review and local gates are resolved, then wait for exact-head hosted CI to pass before merging.
- [ ] Verify the develop-push gate on the squash merge, fast-forward the primary checkout without disturbing its existing investigation edit, and use `remove_worktree.py --check` then `--apply` for this merged worktree before deleting and verifying the exact local/upstream branch refs.
