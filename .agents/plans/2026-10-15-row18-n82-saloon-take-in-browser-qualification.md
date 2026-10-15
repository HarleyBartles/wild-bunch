# Row 18 N-82 Saloon Named Take-In Browser Qualification Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task by task.

**Goal:** Qualify one complete public player route from a learned wanted poster through observing, naming, and successfully taking in the corresponding saloon person using the production-mode browser, real API, and PostgreSQL event store.

**Architecture:** Extend the existing connected production-browser route without adding a test-only endpoint, dev override, or new game rule. Store one explicit `SeedWorld` fixture and derive its setup UUID at test runtime through `SeedWorldResolver.CreateRepresentativeSeedCode`; use the player-selected Boring entropy mode so the ordinary saloon encounter repeats without storing an opaque UUID or sampling at runtime. Existing Domain and PostgreSQL acceptance tests remain the owners of fine/gun/wrong-name/replay rule coverage.

**Tech Stack:** Playwright Chromium, production-mode Vite build, ASP.NET API, PostgreSQL 16, Python command bus, and pytest.

**Implementation Guide:** [Implementing runbook](../runbooks/implementing.md), [testing playbook](../playbooks/testing.md), [frontend standards](../doctrine/frontend-standards.md), [architecture guardrails](../doctrine/architecture-guardrails.md), [code-review runbook](../runbooks/code-review.md), and [pull-request runbook](../runbooks/pr.md).

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially PG-005, PG-006 and workstream G; [feature matrix](../../docs/features.md), especially PG-005, PG-006 and PG-003; [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), [ADR-0046](../../docs/decisions/ADR-0046-pointer-operated-town-selection-for-0.1.0.md), and [ADR-0048](../../docs/decisions/ADR-0048-gitflow-and-semver-release-lifecycle.md).

**Execution Strategy:** `executing-plans` - setup, public poster discovery, ordinary saloon observation, named command, and matrix evidence form one sequential player journey with a deterministic fixture; inline execution preserves the real HTTP/PostgreSQL state and one whole-branch review. The nearest alternative, subagent-driven-development, would add per-task handoffs without creating independent implementation seams.

## Global Constraints

- Advance the sole authored product version in `Directory.Build.props` exactly once, from `0.1.0-dev.199` to `0.1.0-dev.200`.
- Preserve the established flow of setup, prologue, one shared-map town choice, free arrival, public knowledge, and named saloon take-in.
- Use the existing public web production build, actual player API endpoints, and uniquely named disposable PostgreSQL database established by browser support; never use `wildbunch_dev`, a dev endpoint/override, hidden game truth, or a test-only application route.
- Store the browser fixture as one explicit `SeedWorld` record, derive its UUID through `SeedWorldResolver.CreateRepresentativeSeedCode`, and choose Boring through the player-facing setup control; do not store an encoded UUID or retry/sample seeds at runtime.
- The saloon person must be named from a poster the player actually read, and the rendered distinguishing feature must match a feature on that public poster. Do not identify the target through suspect IDs, culprit flags, internal DTO fields, or server-only test setup.
- Keep PG-006 behavior and event semantics unchanged unless the test exposes a concrete defect against the settled contract; fix only a reproduced defect in its owning production seam and preserve CQRS, event authority, and replay.
- Preserve the existing negatives and PostgreSQL replay coverage for citizens, no-gun refusal, wrong identity, duplicate settlement and the four-to-five culprit-release gate; do not duplicate these contracts in the browser route.
- Prove the browser test detects a wrong declaration by temporarily changing the actual SaloonPlace command to submit an invalid wanted-identity handle, confirm the new browser assertion fails on the unsuccessful API result, restore the correct player-selected handle, and rerun the focused test. Do not commit the mutation.
- Do not create or change an ADR: this route qualifies existing player behavior and changes no durable architecture decision. The author and reviewer compare the actual diff with ADR-0028, ADR-0046 and ADR-0048.
- Preserve the user's pre-existing `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md` edit in the main checkout; all N-82 work stays in the canonical skill-created worktree.

## Review Focus

- The browser reads a public wanted poster and identifies the same target from the saloon's visible descriptor; a successful result must not come from a dev force-override or hidden identity lookup.
- The player-selected poster identity must be the value sent to the real confrontation endpoint; the response must confirm a secured, living target whose public name matches the poster.
- The UI must display the returned outcome while the server remains the authority; the test must not infer successful take-in from button clicks or mocked query invalidation.
- The dated matrix claim remains one local deterministic route and does not claim all PG-006 negatives, the five-of-six release boundary, full gameplay, hosted behavior, or deployment.

---

### Task 1: Commit the successor plan checkpoint and retire N-81

**Files:**
- Create: `.agents/plans/2026-10-15-row18-n82-saloon-take-in-browser-qualification.md`
- Delete: `.agents/plans/2026-10-15-row18-n81-sheriff-poster-browser-qualification.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`

**Interfaces:**
- Consumes: PR #384 reviewed source `cff853766be2af95ea55b873d7a4e61a58687a45`, squash merge `da6c469cadad18bdc95e79508259acc452a7f5eb`, passing exact-head hosted gate [38757251716](https://github.com/HarleyBartles/wild-bunch/actions/runs/38757251716), passing develop-push gate [38760093934](https://github.com/HarleyBartles/wild-bunch/actions/runs/38760093934), fresh whole-branch review with no actionable findings, and verified paired worktree/local/upstream branch retirement.
- Produces: N-81's dated roadmap delivery record; N-81 plan retirement; N-82 as the active row-18 plan; this committed plan and version `0.1.0-dev.200`.

- [x] Verify the worktree starts at current `origin/develop` `da6c469cadad18bdc95e79508259acc452a7f5eb`; verify PR #384 exact-head and develop-push gates, independent review, and paired worktree/branch cleanup from live GitHub and local Git evidence.
- [x] Record N-81's exact reviewed source, squash merge, hosted gates and cleanup in the roadmap; set row 18's active plan and remaining scope to N-82 plus the later retained-path and candidate qualification; advance the version once to `0.1.0-dev.200`; create this plan.
- [x] Run `git diff --check`, stage only the checkpoint paths, and commit the successor checkpoint through the normal check-only commit hook.

### Task 2: Qualify a public named take-in through the production browser

**Files:**
- Modify: `src/WildBunch.Web/browser-tests/player-startup.spec.ts`
- Create: `src/WildBunch.Web/browser-tests/support/seed-code/SeedWorldSeedCode.csproj`
- Create: `src/WildBunch.Web/browser-tests/support/seed-code/Program.cs`
- Create: `src/WildBunch.Web/browser-tests/support/seedWorldSeedCode.ts`
- Modify only if the browser proof reproduces a contract defect: the owning player surface or API/application seam identified from the failing behavior.

**Interfaces:**
- Consumes: the production public web build, `startIsolatedApi()` disposable PostgreSQL path, the ordinary setup/prologue/map/arrival route, `POST /api/games/{id}/wanted-posters/read`, `POST /api/games/{id}/investigations/saloon/look-around`, `POST /api/games/{id}/investigations/saloon/confront`, the public wanted-poster DTO, and the public saloon descriptor in the current session DTO.
- Produces: a test-support function that derives its UUID from the stored `SeedWorld` record through the production resolver, plus one deterministic real-browser proof that the player reads a wanted poster, sees the matching public feature on a saloon person, selects that poster identity in the player surface, receives a successful secured-alive outcome for the same public target, and sees the returned outcome in the rendered game.

- [ ] Create a `SeedWorld` fixture in the C# seed-code provider and derive its UUID with `SeedWorldResolver.CreateRepresentativeSeedCode`; use the helper output as the visible seed-form input and Boring as the player-selected entropy option. Choose the record by observing the normal route's public poster and rendered saloon descriptor, then keep that `SeedWorld` record fixed so the browser test never samples or retries seeds.
- [ ] Extend the existing connected production-browser route to enter the Sheriff Office through the visible town-building control, read exactly one poster through the real player command, then return to town and enter the Saloon through its visible building control.
- [ ] Click `Look around`, wait for the actual saloon observation response, and assert that the rendered person descriptor contains a feature from the public poster read in this game; the fixture must make the interaction correct through player-known evidence rather than a hidden target lookup.
- [ ] Select the returned poster by its visible target name in `Declare wanted identity`, click the exact `Take <descriptor> to sheriff as <name>` action, and wait for the real `/investigations/saloon/confront` POST.
- [ ] Assert the API result is successful, `targetName` is the public poster target, `isAlive === true`, `isSecured === true`, and the rendered Saloon surface presents the returned result message. Do not assert backend-only suspect or culprit fields.
- [ ] Run the browser test on current behavior, then temporarily replace the selected identity handle with an invalid handle in the actual `SaloonPlace` request, rerun the test and observe its success assertion fail, restore the player-selected handle, and rerun to GREEN.
- [ ] If the ordinary flow fails before or during the real confrontation, follow the failure to its owning seam, implement only the smallest fix that restores the settled PG-005/PG-006 behavior, and add no new product rule or dev override. If the defect crosses an unsettled contract, stop that dependent change and record the evidence for a user design decision.
- [ ] Build with `npm --prefix src/WildBunch.Web run build`, ensure the shared PostgreSQL service with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`, then run the focused route with `npm --prefix src/WildBunch.Web run test:browser -- --grep "a player can start, restore a public poster, and take in the named person through the saloon"`.

### Task 3: Record the bounded PG-006 browser evidence

**Files:**
- Modify: `docs/features.md`

**Interfaces:**
- Consumes: the passing production-browser route, the public poster/descriptor relationship it observes, the real API response, and existing Domain/PostgreSQL tests that own the independent rule, negative, event and replay contracts.
- Produces: a dated PG-006 assessment that distinguishes one local browser-qualified named take-in from the broader partial feature and its separately owned rule evidence.

- [ ] Update PG-006's assessment to state the real public poster-to-saloon named take-in route, secured living outcome, and visible returned result; link the actual browser test and preserve the existing PostgreSQL/replay test links and the feature's partial status.
- [ ] State the evidence limit: one deterministic local route does not qualify every citizen/wrong-name/no-gun case in the browser, the five-of-six gate as a browser journey, the full product lifecycle, hosted behavior or deployment.
- [ ] Compare the final browser and matrix changes against PG-005/PG-006, the stable baseline, ADR-0028, ADR-0046 and ADR-0048; state why no current promise or durable decision changed beyond the evidence assessment.

### Task 4: Verify, review, publish and close N-82

**Files:**
- Review the committed N-82 branch, plan, roadmap, PG-006 matrix entry, browser test, stable baseline, ADR-0028, ADR-0046, ADR-0048 and applicable testing/release guidance.

**Interfaces:**
- Consumes: a committed plan checkpoint, a passing ordinary player browser route, truthful matrix evidence, local canonical validation, and independent review of the exact candidate.
- Produces: a reviewed PR to `develop` at `0.1.0-dev.200`, passing exact-head hosted CI, verified merge and develop-push gate, and paired worktree/branch cleanup.

- [ ] Run the focused production browser route and required real PostgreSQL/browser lane; run the complete fail-fast `py -3 tools/run.py ci --check` on the final candidate and use the normal check-only commit hook for every commit.
- [ ] Compare the actual diff against the selected code-review and play-surface profiles, feature-matrix playbook, relevant ADRs and PR/review runbooks; create the review package and dispatch a fresh-context whole-branch reviewer on the committed exact range, resolve Critical and Important findings with witnessed behavior proof, and rerun required checks after corrections.
- [ ] Push the dedicated branch and open a Draft PR targeting `develop`; verify the exact head, base, version and required hosted gate; mark Ready and merge only after review and exact-head hosted CI pass; verify the develop-push gate on the exact squash-merge commit.
- [ ] Fast-forward the main checkout to the verified merge without disturbing the pre-existing user edit; use `finishing-a-development-branch` and the skill's remove-worktree check/apply script; remove only N-82's exact local and upstream refs, then verify the worktree and both refs are absent.

## Scope Boundaries

This slice qualifies one publicly supported wanted-person take-in route from a real wanted poster through saloon observation and identity selection to successful capture. It does not add or broaden the game feature, qualify citizens or failure cases in the browser, change the culprit-release rule, add gunfight/resistance, implement clue deduction, qualify casebook retention, shopping, travel, start over, terminal death, full candidate scope, hosted release, deployed behavior or user ownership.
