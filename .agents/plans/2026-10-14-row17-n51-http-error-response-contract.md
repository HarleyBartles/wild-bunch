# Row 17 N-51: Prove HTTP Error Response Contracts

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the manual browser API client's supported HTTP failure messages and status propagation resistant to regressions with behavior-focused tests.

**Architecture:** Keep `requestJson` as the sole fetch boundary and test it through real `Response` bodies at the fetch seam. Extend the exported travel-preview API test once to prove the typed client preserves structured HTTP failures; do not test private helpers or change production parsing behavior.

**Tech Stack:** TypeScript, Vitest, Fetch API, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially [test design and acceptance](../specs/2026-10-07-stable-0.1.0-baseline.md#test-design-and-acceptance); [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md) W-10; [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md) transport and route contracts.

**Execution Strategy:** `executing-plans` (native inline). The single response-extraction contract is owned by `requestJson`; its table, exported API propagation case, evidence correction, and review all share one narrow seam. Splitting tests across multiple implementers would add handoffs without isolating an independently shippable behavior. Use one temporary validation-error extraction mutation to prove the new observable test detects the intended regression, then restore the client byte-for-byte.

## Global Constraints

- Preserve the manual typed API client decision in ADR-0019; do not generate clients or distribute fetch behavior into components.
- Preserve the existing distinction between HTTP response errors and fetch transport rejection.
- Do not change successful malformed-JSON handling, successful empty-body handling, cancellation, or unrelated API behavior in this slice.
- Do not modify production behavior, event history, product promises, or feature dependencies.
- Assign exactly one unique next `0.1.0-dev.N` version for the merged PR and retain `develop` as the PR base.
- The exact-head hosted gate and the develop push gate on the merge SHA must pass before worktree and paired branch cleanup.
- Read relevant ADRs and compare the diff with the feature matrix through their playbooks; preserve both unchanged when no durable decision or PG-008 promise changes.

## Review Focus

- Problem title takes precedence over error/detail and retains the HTTP status; `error`, validation `errors`, `detail`, plain text, and generic status fallback each return the expected visible message.
- Invalid JSON in a non-success response falls back to the HTTP status instead of escaping a parse error; fetch rejection remains a transport error without an HTTP status.
- The exported `previewTravel` call propagates a structured non-success response through the real `requestJson` boundary.
- Successful malformed JSON and successful empty-body semantics are explicitly outside this plan and remain unclaimed.

---

### Task 1: Promote N-50 and establish N-51

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n51-http-error-response-contract.md`
- Delete: `.agents/plans/2026-10-14-row17-n50-retire-default-travel-tuning-constants.md`

**Interfaces:**
- Consumes: N-50 PR #353, reviewed head `d3ce9764bdf991c6ae781797e1dca8e37024d489`, merge SHA `57400b2f684a81dd246a7bdd95849e8b895b9197`, exact-head hosted gate `38575047818`, develop-push gate `38576460633`, and verified cleanup.
- Produces: promoted N-50 delivery evidence, active N-51 plan at `0.1.0-dev.169`, and retirement of the completed N-50 plan.

- [x] **Step 1: Revalidate N-50 delivery and cleanup.** Confirm PR #353 is merged into `develop`, its recorded head and merge SHA match the values above, both hosted runs passed on their respective SHAs, the primary checkout is current, and the N-50 worktree plus exact local/upstream refs are absent.
- [x] **Step 2: Promote N-50, activate N-51, and advance the version.** Update row 17 to record PRs #352 and #353, N-50 review/merge/gate identities, paired cleanup, and this active N-51 plan at `0.1.0-dev.169`. Change only the authored version in `Directory.Build.props` to `0.1.0-dev.169`.
- [x] **Step 3: Retire the completed predecessor.** Delete the N-50 plan only after confirming its full scope and evidence are promoted. Preserve the baseline specification, roadmap, and investigation documents.
- [x] **Step 4: Commit successor planning and predecessor retirement first.** Run `git diff --check`, inspect the full staged diff, and commit the plan, roadmap, version, and N-50 retirement as the first substantive commit with message `docs: plan N-51 HTTP error response contract`.

### Task 2: Prove HTTP error response behavior

**Files:**
- Modify: `src/WildBunch.Web/src/tests/httpClient.test.ts`
- Modify: `src/WildBunch.Web/src/tests/wildBunchApi.test.ts`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Inspect only: `src/WildBunch.Web/src/api/httpClient.ts`, `src/WildBunch.Web/src/api/wildBunchApi.ts`, ADR-0019, PG-007/PG-008, and the current travel-surface tests

**Interfaces:**
- Consumes: public `requestJson<T>(path, init?)`, `HttpRequestError(message, status)`, and `previewTravel(gameId, destinationTownId)`.
- Produces: observable message/status cases through the fetch boundary, one exported API propagation case, and current dated dispositions for W-10.

- [x] **Step 1: Write the failure-response cases.** Add public-call tests for JSON `title` precedence, `error`, the first validation message in `errors`, `detail`, plain-text body, and generic status fallback for an unrecognized or malformed JSON error body. Each case must assert both exact message and status.
- [x] **Step 2: Prove the behavior test is sensitive.** Temporarily remove only the production `errors`-collection extraction branch, run the new validation-error case and confirm it fails on the expected message, then restore `httpClient.ts` byte-for-byte before continuing.
- [x] **Step 3: Prove the public API wrapper propagates the failure.** Stub only global `fetch` with an actual non-success `Response`, call exported `previewTravel`, and require the rejection to retain the server message and status. Keep the existing fetch-rejection test proving transport errors remain distinct.
- [x] **Step 4: Reconcile the audit records.** Add `httpClient.test.ts` to the frontend inventory and record the tested error contract. Correct W-10 to state that current `TravelPrepSurface` and `TravelPanel` tests already cover preview/departure or command failures, retry, stale result identity, and unchanged session state. Keep the unmounted `TravelRoutesPanel` component/test disposition conditional and outside this API slice. State that successful malformed-JSON and empty-body semantics remain untested and unchanged.
- [x] **Step 5: Recheck durable truth and run focused tests.** Read ADR-0019 and the relevant feature entries; state why no durable decision or product promise changes. Run `npm test -- src/tests/httpClient.test.ts src/tests/wildBunchApi.test.ts` from `src/WildBunch.Web` and compare exact output/messages against the plan.
- [x] **Step 6: Commit the test and evidence changes.** Run focused web formatting and lint checks through `py -3 tools/run.py format --check <paths>` and `py -3 tools/run.py lint --check <paths>`, stage only the two tests and investigation edits, inspect the complete staged diff, and commit with message `test: prove HTTP error response messages`.

**Ruling:** The command-bus format and lint targets reject Markdown investigation paths as unsupported. Applied focused format/lint checks to both changed TypeScript tests, reviewed the Markdown edits and ran `git diff --check`; the canonical staged-candidate gate will validate the complete documentation change.

### Task 3: Review, publish, integrate, and retire N-51

**Files:**
- Review: the complete branch diff, this plan, roadmap, both investigation entries, ADR-0019, relevant feature entries, and validation evidence.
- Modify: `.agents/plans/2026-10-14-row17-n51-http-error-response-contract.md` and the roadmap with final delivery evidence.

**Interfaces:**
- Consumes: Task 2's exact message/status cases and one mutation-proven extraction branch.
- Produces: N-51 merged into `develop` at `0.1.0-dev.169`, with reviewed exact-head evidence and paired worktree/ref retirement.

- [x] **Step 1: Verify the committed tree.** Confirm the version is `0.1.0-dev.169`, no generated version field is authored separately, the staged-candidate canonical hook passed, and the complete `py -3 tools/run.py web --check` lane passes on the implementation commit.
- [x] **Step 2: Complete author truth checks.** Compare the actual diff against ADR-0019 and PG-007/PG-008 through the decision-record and feature-matrix playbooks; record why no ADR or feature-matrix change is warranted.
- [ ] **Step 3: Obtain independent review and publish.** Build the exact merge-base-to-HEAD review package, dispatch a fresh-context whole-branch reviewer, resolve any findings and re-review corrections, then publish a Draft PR to `develop`, read back its exact head/body, mark it ready, and require exact-head hosted CI to pass.
- [ ] **Step 4: Verify integration and clean up.** Squash-merge to `develop`, require the develop push gate to pass on the exact merge SHA, fast-forward the primary checkout, remove this clean worktree with the `finishing-a-development-branch` skill script, delete the matching local and upstream branches, and verify all three refs/paths are absent.

**Ruling:** The roadmap's dated delivery ledger is updated by the next successor's first substantive commit, when this PR's merge SHA and hosted run identities are known. N-51 remains the active plan through this PR; N-52 will promote N-51's completed delivery evidence and retire this plan together, avoiding a post-merge evidence-only commit.

**Author truth check:** ADR-0019 remains satisfied because `requestJson` stays the sole manual typed HTTP boundary. PG-007 and PG-008 promises, dependencies, dispositions and evidence assessments do not change; these tests clarify the existing transport contract without changing player-facing capability.

## Acceptance Criteria

- The HTTP failure matrix observes each supported error source and fallback through `requestJson` and asserts exact public messages plus status.
- The validation-message test fails when only its matching extraction branch is removed; production is restored byte-for-byte before final tests.
- An exported API call propagates a structured HTTP failure with status and message; transport rejection remains distinguishable.
- No production code or successful malformed-body contract changes in this slice.
- W-10 and the frontend file inventory accurately distinguish current live travel failure coverage, the unmounted `TravelRoutesPanel` disposition, and the remaining successful-body parsing question.
- N-50 evidence is promoted before its plan is retired in the first substantive successor commit; this plan remains through its completing PR.
- Focused tests, web lane, canonical staged-candidate gate, independent review, exact-head hosted CI, exact-merge develop gate, primary fast-forward, and paired worktree/ref cleanup are verified.
