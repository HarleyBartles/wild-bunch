# N-34 Retire the Unreachable Trail Event Catalog Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Remove the unused `TrailEventCatalog` producer and its stale inventory/audit references while preserving the live journey generator, event model, replay behavior, and product promises.

**Architecture:** This is a dead-code retirement in Domain, not a gameplay change. Repository-wide reference search shows no caller; the active `TravelDayPlanGenerator` produces the route outcomes and existing behavioral tests cover reachable outcomes. Keep event identifiers, event facts, application, persistence, and replay untouched.

**Specification and evidence:** The Stable 0.1.0 baseline specification and row 17 roadmap govern scope. The N-18 test-quality audit identifies `TrailEventCatalog` as an open route-specific producer finding; the current source search establishes it is unreachable. Read the backend architecture unslop profile and decision-record playbook. No ADR or feature-matrix change is expected because reachable behavior and product truth do not change.

**Execution Strategy:** Native inline execution. One unreachable class, its reserved-source inventory line, and directly owning audit dispositions form one bounded retirement; deleting a class with no call sites has no behavior to red-test, so validate unchanged live behavior with the owning Domain suite and full repository gate.

## Global Constraints

- Preserve strict CQRS, event sourcing, event replay, event identifiers, `JourneyTrailEventState`, and all reachable `TravelDayPlanGenerator` outcomes.
- Do not add a test for a code shape that has no caller. Existing reachable generator and travel behavior remains the behavioral authority.
- The merged N-33 predecessor is `0.1.0-dev.151`; this slice advances the single authored application version to `0.1.0-dev.152` exactly once.
- In the first substantive commit, retire the completed N-33 plan, record its delivery in the roadmap, activate this successor plan in row 17, and advance `Directory.Build.props`.
- Preserve the AWS learning worktree and all unrelated branches/worktrees.

## Task 1: Record N-33 succession and commit this plan

Update `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` to link this N-34 plan in row 17, set its version to `0.1.0-dev.152`, include PRs #335 and #336 in the delivered PR list, and append the verified N-33 delivery details: PR #336, source `c0d5858e38ae82fad31ed5fb131a93cc5087dd21`, squash merge `361c9fb2625cb1b9ce2e89eb0e359dc610216028`, identical tree `6e75d36c5e8ac85bf49387c5031572522e8e0e08`, exact-head gate 38467645145, develop gate 38468387552, clean independent review, and paired worktree/ref cleanup. Do not rewrite older delivery history.

Delete `.agents/plans/2026-10-13-row17-n33-event-backed-journey-completion.md`, update `Directory.Build.props` to `0.1.0-dev.152`, inspect for stale N-33 plan links, verify the roadmap points to this plan, verify one-source version identity, and commit through the staged-snapshot hook before implementation.

## Task 2: Retire the unreachable producer and close its audit trail

Delete `src/WildBunch.Domain/Travel/TrailEventCatalog.cs` and remove its obsolete entry from `LICENSE-ASSETS.md`. Update `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`, and `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md` with a dated N-34 disposition: no production or test call sites existed; this unreachable duplicate producer is removed; the live generator remains the only route outcome producer; its behavioral tests remain; no replacement test was added for dead code; other travel findings remain open. Keep the language scoped to this class and do not imply all generator concerns are closed.

Before removal, record the repository-wide `TrailEventCatalog` reference search. After removal, prove there are no remaining references to the class, inspect that `JourneyTrailEventId`, `JourneyTrailEventState`, and live generator branches are unchanged, and run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore`. No production behavior mutation test is applicable because the deleted class is unreachable; the existing reachable behavior tests remain the relevant proof.

## Task 3: Review and deliver N-34

Review the whole diff against the baseline specification, backend unslop profile, feature matrix, and applicable ADRs. State that ADR and feature truth do not change and why. Run `py -3 tools/run.py ci --check`, check the staged version/build identity, and require its staged-candidate hook to pass. Build the review package and dispatch the already-authorized fresh-context reviewer; resolve any material findings before publication.

Push and open a PR to `develop`; ensure hosted canonical CI passes on the exact PR head before squash merge. Verify the merge commit and tree against the PR source and the develop-push gate. Fast-forward the primary checkout, then remove this clean worktree before deleting its exact local and upstream refs; verify all three are absent and preserve the AWS learning worktree.

## Review Focus

- Is the class truly unreachable across source, tests, reflection, and generated code?
- Does the diff preserve the active event producer, event facts, replay, persistence, and all product promises?
- Are the audit statements narrowly evidenced, and do they leave unrelated travel findings open?
- Are roadmap delivery facts, version advancement, successor artifact retirement, hosted gates, and paired worktree/branch cleanup accurate?
