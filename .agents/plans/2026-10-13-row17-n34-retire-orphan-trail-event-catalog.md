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

- [x] Retire the N-33 plan, update row 17 and its delivery evidence, advance the shared version to `0.1.0-dev.152`, and commit this successor plan as `3047ffa7`.

## Task 2: Retire the unreachable producer and close its audit trail

- [x] Confirm the only pre-change source hits were the class and inventory entry; no callers existed.
- [x] Remove the class and its inventory entry, then preserve the dated disposition in test-quality, .NET test, and Domain source audits while leaving earlier dated claims intact.
- [x] Confirm remaining references are limited to historical/planning dispositions, preserve event types and live generator, and pass the Domain suite (539 passed, 1 pre-existing skip).

No behavior test was added for unreachable code. The class had no caller; existing tests continue to exercise reachable generator outcomes.

## Task 3: Review and deliver N-34

- [ ] Compare the final diff with the spec, backend profile, feature matrix, and relevant ADRs; record why product and durable decision truth are unchanged.
- [ ] Run the full fail-fast repository gate and commit through the staged-candidate hook.
- [ ] Build the review package, obtain the already-authorized fresh-context review, and resolve any material findings.
- [ ] Open the PR to `develop`, pass exact-head hosted CI, squash-merge, verify the develop gate, fast-forward primary, and perform paired worktree/ref cleanup.

## Review Focus

- Is the class truly unreachable across source, tests, reflection, and generated code?
- Does the diff preserve the active event producer, event facts, replay, persistence, and all product promises?
- Are the audit statements narrowly evidenced, and do they leave unrelated travel findings open?
- Are roadmap delivery facts, version advancement, successor artifact retirement, hosted gates, and paired worktree/branch cleanup accurate?
