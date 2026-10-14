# N-43 Remove Unsupported Moderate-Route BadLuck Exclusion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Remove the test that presents selected BadLuck outcomes as forbidden on moderate routes when neither product truth nor implementation defines that exclusion.

**Architecture:** Keep the existing travel generator and command behavior. `TrailRisk` changes encounter-category weights, while the current Unlucky candidate path has no moderate-route event-ID prohibition; N-42 also proves a BadLuckWashout can occur on a low-risk route. Remove the misleading command-level scenario and its now-unused fixture, and record why no replacement assertion is warranted.

**Tech Stack:** .NET 10, xUnit, Wild Bunch Domain travel tests and test audit documents.

**Spec:** The [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), row 17 of the [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), the [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), the [test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), validation doctrine and ADR-0029.

**Execution Strategy:** `executing-plans` (Native inline execution; one sequential test-audit correction slice).

## Global Constraints

- Preserve route-risk weighting, difficulty, entropy, event generation and all gameplay behavior.
- Remove only the moderate-route test and `CreateBadLuckSession` fixture if repository search confirms it has no other caller.
- Do not replace unsupported hard-route-only expectations with a new assertion that merely freezes a chosen event distribution or seed.
- Existing generator tests retain actual risk/difficulty behavior; N-42 retains reached low-risk BadLuckWashout command, effect, snapshot and replay proof.
- Advance the single authored development version to `0.1.0-dev.161` in this slice.
- Use the fresh canonical worktree and PR to `develop`; require independent review, exact-head hosted CI and develop-push CI; retire the worktree before its paired local and remote branch refs.

## Review Focus

- The deleted scenario claims a route-risk exclusion that is not in the product spec, ADR-0029 or current generator: route risk affects category weights, and `CreateUnluckyEncounter` filters only horse eligibility, recent delay events and duplicate event IDs.
- N-42's fixed low-risk test reaches `BadLuckWashout`, directly disproving the premise that this BadLuck event is hard-route-only.
- The remaining tests still assert reachable risk/difficulty behavior and event-specific cooldown behavior; removal does not widen this plan into a generator redesign.
- No replacement test is justified unless inspection finds an independently specified invariant not already covered by actual behavior tests.

---

## Task 1: Record N-42 delivery and commit the N-43 plan

**Files:**
- Create: `.agents/plans/2026-10-14-row17-n43-remove-unsupported-badluck-exclusion.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-14-row17-n42-badluck-delay-application.md`

**Interfaces:**
- Consumes: merged N-42 source `9e94b50e2c5516b0d949991e3499ff2fe8abae46`, PR #345, squash merge `ff2f1fc4179f77076bc47ef8c50c49a272d1b632`, identical source/merge tree `4eea46a161083b4d5f8d09c27aa2fcdbeb2eb3fc`, exact-head gate run `38535005489`, develop-push gate run `38535522072`, clean N-42 worktree/ref retirement, and current `develop`.
- Produces: an active N-43 plan, successor retirement and dated delivery record for N-42, and application version `0.1.0-dev.161`.

- [x] Verify PR #345 merged the exact N-42 branch head to `develop`, both hosted gates passed, the primary checkout is at merge `ff2f1fc4179f77076bc47ef8c50c49a272d1b632`, and the N-42 worktree plus local/upstream refs are absent.
- [x] Record N-42's independent review, test and hosted-gate outcomes in row 17; select N-43 and advance `Directory.Build.props` to `0.1.0-dev.161`.
- [x] Commit the N-42 retirement, N-43 plan, roadmap update and version transition before changing the test inventory.

## Task 2: Remove the unsupported moderate-route BadLuck exclusion test

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`

**Interfaces:**
- Consumes: `AdvanceJourneyDayDoesNotApplyHardRouteOnlyBadLuckOnModerateRoutes`, its `CreateBadLuckSession` fixture, `TravelDayPlanGenerator` risk weighting and Unlucky candidate filtering, N-42's reached low-risk BadLuckWashout proof, and existing generator behavior coverage.
- Produces: removal of the unsupported test and its sole-use fixture, with an audit disposition explaining the absent invariant and retained behavior coverage.

- [ ] Confirm `CreateBadLuckSession` is used only by the misleading moderate-route test and that the remaining tests already cover event reachability, risk weighting, event-ID suppression and lawful candidate fallback.
- [ ] Remove `AdvanceJourneyDayDoesNotApplyHardRouteOnlyBadLuckOnModerateRoutes` and its now-unused fixture. Do not add a replacement for a route/event restriction absent from the spec, ADRs and implementation.
- [ ] Update the dispositions and investigation with a dated correction: route risk affects category weights rather than forbidding BadLuck event IDs; N-42 reaches BadLuckWashout on a low-risk route; this scenario's exact sampled absences and broad completion loop did not establish a durable rule.
- [ ] Compare ADR-0029 and the feature matrix; record why neither changes and preserve all distinct remaining travel/generator findings.
- [ ] Run the focused remaining generator and N-42 behavior tests, then the complete Domain test project. Commit through the check-only hook and require its fail-fast `py -3 tools/run.py ci --check` staged-candidate gate to pass.

## Task 3: Review and deliver N-43

- [ ] Build the whole-branch review package against the recorded `develop` base and have a fresh independent reviewer inspect the committed diff, row-17 scope, ADR-0029, N-18, dispositions and feature matrix. The user has given standing authorization for an independent reviewer on every plan in this epic.
- [ ] Resolve any Critical or Important finding with a scoped correction and re-review the exact fix; preserve documented disposition for nonblocking observations.
- [ ] Open a Draft PR to `develop`, verify its body and exact head, mark ready after clean review, and require exact-head canonical hosted CI.
- [ ] Squash-merge the reviewed PR, verify the merge commit and passing develop-push canonical CI, then fast-forward the primary checkout to `origin/develop`.
- [ ] Remove the clean N-43 worktree before deleting its exact local/upstream refs, and verify the worktree registration and both refs are absent.
