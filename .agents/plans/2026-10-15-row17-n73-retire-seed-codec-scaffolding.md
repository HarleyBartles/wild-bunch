# N-73: Retire Dead Browser Seed Codec Scaffolding

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove unused browser seed-codec helpers and their self-referential unit tests while retaining and strengthening the real setup behavior that validates, normalizes, randomizes, and submits a player's seed.

**Architecture:** The browser accepts a UUID-shaped seed string and normalizes it before sending the setup command; the server owns deterministic world resolution. `decodeGameSetupSeed`, `createCanonicalSeedState`, and `withRandomSeed` have no production callers, and the current codec test file is their only direct consumer; the live setup flow uses the same normalization path and has behavior tests for edited, invalid, and fresh seeds. Rename the live client helper to describe normalization, remove dead exports and their test-only suite, add one routed test for the Randomize action, and reconcile the stale WB-05/test-inventory findings without changing the domain seed codec or event replay.

**Tech Stack:** React, TypeScript, Vitest, Python command bus, PostgreSQL-backed canonical repository gate.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [web layer investigation](../investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [PG-001 feature contract](../../docs/features.md#pg-001--start-a-hunt-and-choose-the-first-town).

**Execution Strategy:** `executing-plans` (native inline execution). Promotion, behavior proof, cleanup, and audit reconciliation depend on one another and share the same seed setup path; fresh per-task agents would add context handoffs without an independent implementation boundary.

## Global Constraints

- Start from the latest `origin/develop` in the fresh `using-git-worktrees` helper worktree; commit this plan before implementation.
- Promote N-72 and retire its completed plan in N-73's first substantive commit; advance `Directory.Build.props` from `0.1.0-dev.190` to `0.1.0-dev.191` in that commit.
- Preserve PG-001: each setup visit starts with a fresh editable UUID, Randomize replaces the draft, valid edited UUIDs are normalized and submitted, and invalid drafts do not issue setup.
- Keep deterministic world resolution and fact-only event replay in the server/domain boundary; this slice changes only browser input normalization and test ownership.
- Do not add tests for file/export absence or preserve unit tests that only exercise dead helpers; prove the live setup contract through the routed owner.
- Compare the final diff with ADR-0021, ADR-0039, and PG-001; update PG-001's evidence assessment if the new routed randomization proof is added, and leave ADRs unchanged if their decisions remain true.
- Keep Markdown paragraphs and list items on one physical source line, with regular hyphens only.

## Review Focus

- A player who selects Randomize sees a new UUID and the setup request submits that exact value; `StartFlow.test.tsx` must cover this routed behavior and the test must fail when the randomization state update is removed.
- An edited uppercase UUID is normalized in the submitted setup request, while malformed input shows an error and sends no setup command; retain the existing routed `StartFlow.test.tsx` behavior tests.
- A name-only quick start still receives a fresh UUID and the Standard/Classic defaults; retain the existing routed `StartFlow.test.tsx` behavior test.

---

### Task 1: Promote N-72 and establish N-73 version identity

**Files:** `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/plans/2026-10-15-row17-n72-retire-unmounted-web-shells.md`, `Directory.Build.props`, `.agents/plans/2026-10-15-row17-n73-retire-seed-codec-scaffolding.md`.

- [x] Verify PR #375 merged to `develop` from reviewed source `8b17a6b64741ecaccfc79ac4fa28bb21e08b5f72` as squash merge `6ce8595e439c021679af9e7b6042e6332e8f8f0c`; verify fresh review found no actionable findings, exact-head gate `38686437597` and develop-push gate `38687038920` passed, and the N-72 worktree plus exact local/upstream refs are absent.
- [x] Update row 17 to name N-73 as active at `0.1.0-dev.191`; append dated N-72 delivery evidence with the PR, source and merge SHAs, review outcome, both hosted gates, primary fast-forward, and paired worktree/local/upstream branch cleanup.
- [x] Retire `.agents/plans/2026-10-15-row17-n72-retire-unmounted-web-shells.md` in this successor's first substantive commit while retaining the roadmap, stable baseline specification, and web investigation inventories.
- [x] Change only `Directory.Build.props` to `0.1.0-dev.191` for application version advancement; commit this plan and promotion before editing product source.

### Task 2: Replace helper-level seed tests with routed setup behavior proof

**Files:** `src/WildBunch.Web/src/tests/StartFlow.test.tsx`, `src/WildBunch.Web/src/ui/gameSetupSeedCodec.ts` (rename to `src/WildBunch.Web/src/ui/gameSetupSeed.ts`), `src/WildBunch.Web/src/hooks/useStartGameSeed.ts`, `src/WildBunch.Web/src/tests/gameSetupSeedCodec.test.ts`.

- [x] Add a routed `StartFlow.test.tsx` case that makes `crypto.randomUUID()` return distinct valid UUIDs for initial setup and the Randomize action, clicks Randomize, observes the second UUID in the seed input, continues with a player name, and asserts `setupGame` receives that exact second UUID.
- [x] Run `npm --prefix src/WildBunch.Web run test -- src/tests/StartFlow.test.tsx`; confirm the new case passes on the current implementation, then temporarily remove the live randomization state update, rerun the focused case and confirm it fails because the displayed/submitted UUID stays stale, restore the source exactly, and rerun the focused suite green.
- [x] Rename `gameSetupSeedCodec.ts` to `gameSetupSeed.ts` because the remaining module does not encode or decode; rename `encodeGameSetupSeed` to `normalizeGameSetupSeed` and make its string input/output contract explicit; update `useStartGameSeed.validateSeedDraft` without changing error handling or setup payload behavior.
- [x] Remove the unused `GameSetupSeedState`, `DecodedGameSetupSeed`, `decodeGameSetupSeed`, `createCanonicalSeedState`, and `withRandomSeed` exports after repository-wide consumer search confirms no production callers remain.
- [x] Delete `src/tests/gameSetupSeedCodec.test.ts` rather than replacing it with tests for absence or direct normalization echoes; its setup-visible normalization and malformed-input behavior remain exercised through `StartFlow.test.tsx`.

### Task 3: Reconcile current web and feature evidence

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`, `docs/features.md`, `PR description`.

- [x] Correct WB-05's current assessment to state that the 2026-10-07 issue was real when observed but routed setup now validates and submits the edited normalized draft; retain the dated historical observation and add a dated N-73 disposition citing the live behavior tests.
- [x] Remove the resolved client seed-decoder scaffold from WB-23's remaining findings and update the `gameSetupSeed.ts` file disposition to describe the single live normalization responsibility, without disturbing unrelated WB-23 findings.
- [x] Reconcile the test-follow-up rows that call for retaining isolated codec negatives and adding routed proof; record that the decoder/factory-only unit suite is retired, the routed edited/invalid/fresh seed cases remain, and the new Randomize case proves a changed seed reaches the command. Correct the current test inventory to 45 Vitest files, one Playwright spec, and four support/guidance files.
- [x] Update PG-001's evidence assessment to include the routed Randomize interaction; preserve all existing feature promises and dependencies, and compare the actual diff with ADR-0021 and ADR-0039, leaving them unchanged because UUID input, server resolution, and event replay decisions do not change.
- [x] State the author ADR and feature-matrix comparisons in the PR body; the independent reviewer repeated both checks against the committed diff at `c3514de8d2c140a9f7c4c29ea94d39a434e20cfc` and found no actionable issues.

### Task 4: Validate, review, publish, and retire N-73

**Files:** all N-73 changes and the prepared PR review package.

- [x] Run the focused `StartFlow.test.tsx` behavior suite and the canonical `py -3 tools/run.py ci --check` gate on the intended committed tree; capture the exact committed HEAD and local gate result through the normal check-only pre-commit hook.
- [x] Inspect the committed diff, verify only the planned files changed, search for removed helper names across the repository, and confirm `StartFlow.test.tsx` remains the behavior owner for all retained setup contracts.
- [x] Prepare and dispatch a fresh-context whole-branch reviewer with exact base/head and actual diff; require independent ADR and feature-matrix comparison, resolve findings, and obtain a new review after any correction. The corrected full review approved `c3514de8d2c140a9f7c4c29ea94d39a434e20cfc` with no actionable findings; review evidence is in the off-repo package.
- [ ] Publish a Draft PR to `develop` with scope, author comparisons, exact local evidence, and reviewer outcome; mark it ready after these gates are resolved and wait for the exact-head hosted gate to pass before merging.
- [ ] Verify the develop-push gate on the squash merge, fast-forward the primary checkout without disturbing its existing investigation edit, and use `remove_worktree.py --check` then `--apply` for this merged worktree before deleting and verifying the exact local/upstream branch refs.
