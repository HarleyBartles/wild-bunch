# Scope developer form state to the active playthrough

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Ensure unsent Saloon and Travel developer form values and asynchronous feedback belong only to the playthrough that was active when the panel rendered.

**Architecture:** Keep the existing Saloon and Travel dev panels and backend commands. Give the stateful form lifetime an explicit `gameId` boundary so switching playthroughs starts with that panel's defaults and work completing for an old playthrough cannot place stale feedback in the new playthrough's panel. Preserve current query keys, captured-game invalidation, API contracts and control behavior.

**Specification:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, row 13's identity-scoped asynchronous developer drafts.

**Investigation:** `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, WB-19 and its test follow-up.

**Decision records:** No durable architecture decision changes; this implements the already settled identity boundary for local developer form state.

**Execution Strategy:** Add behavior tests which change the active `gameId` while retaining the panel tree. First demonstrate that a draft from playthrough A is submitted when playthrough B is active, and that a pending action's failure from A appears in B; then implement the smallest per-panel identity lifetime boundary that makes those behaviors impossible. Do not assert a React `key`, component nesting or other implementation shape.

## Global Constraints

- A Saloon or Travel developer form draft entered for one playthrough must not be submitted for another playthrough after the active identity changes.
- Async completion from a command started for playthrough A must not change the visible error or pending state for playthrough B.
- Keep each panel's current defaults, fields, labels, backend command payload and query cache identity unchanged.
- Do not change `GameSessionProvider`, player-facing state, server authority, dev endpoints, or unrelated provider ownership.
- Do not add a generic state-reset hook unless the two panels prove a real shared behavior that cannot be expressed with a small local boundary.
- Retire `.agents/plans/2026-10-11-pointer-map-0.1.0-scope.md` in this successor PR and record PR #271's implementation evidence in the roadmap; retain its historical content in Git history.
- Advance the single authoritative product version in `Directory.Build.props` from `0.1.0-dev.86` to `0.1.0-dev.87`.
- Deliver through a fresh worktree from refreshed `origin/develop`, with a plan commit preceding implementation, a PR to `develop`, passing exact-head hosted gate, merge, passing develop-push gate, primary-checkout fast-forward and verified worktree/branch cleanup.

## Review Focus

- Both panels discard unsent form choices when the active playthrough changes, including a selected Saloon person/role and Travel encounter inputs.
- An old request's late rejection cannot show an error in the new playthrough's panel; a new playthrough can still submit and display its own result.
- Tests fail against the prior behavior for the behavior each test claims to protect and do not freeze component structure.
- No changes leak into the player-facing flow or server-side dev command semantics.
- Roadmap row 13 records the completed PR #271 scope correction and this slice's final PR evidence; row 15 remains pending without an active plan link to the retired pointer-scope plan.
- The version is advanced exactly once for the implementation PR.

---

### Task 1: Prove developer drafts cannot cross playthrough identity

- [ ] Adapt Saloon and Travel test harnesses to change the current `gameId` while preserving the rendered panel tree.
- [ ] Add a Saloon behavior test which selects a non-default identity/role for playthrough A, changes to playthrough B, submits, and proves B receives only B's default selection.
- [ ] Add a Travel behavior test which enters a non-default category/message or foe value for A, changes to B, submits, and proves B receives only B's default values.
- [ ] Run each new test against the current implementation and record the observed failure before making production changes.

### Task 2: Keep late async feedback with its originating playthrough

- [ ] Add a focused behavior test for at least one panel that holds a dev command started for A, switches to B, rejects A's command, and verifies the error is not shown in B while B remains usable.
- [ ] Prove the test fails against the current implementation; if the existing async state cannot reproduce a visible cross-identity failure, narrow or remove this assertion rather than preserving a theoretical test.

### Task 3: Implement identity-scoped form lifetimes

- [ ] Apply the smallest local composition change to Saloon and Travel panels that scopes their local drafts and async feedback to the captured active `gameId`.
- [ ] Preserve API payloads, form defaults, pending-override reads, query keys, and invalidation behavior.
- [ ] Run the focused tests and the applicable frontend validation lane.

### Task 4: Advance, retire and publish

- [ ] Advance `Directory.Build.props` from `0.1.0-dev.86` to `0.1.0-dev.87`.
- [ ] Update roadmap row 13 with the merged PR, reviewed source and merge/tree identity where available, plus exact-head and develop-push hosted gates; retire this plan in that successor entry.
- [ ] Record PR #271 as merged and retire `.agents/plans/2026-10-11-pointer-map-0.1.0-scope.md`; row 15 should state that the scope correction landed and that row-15 implementation remains pending.
- [ ] Run the non-mutating fail-fast repository gate on the exact final commit candidate, request the required independent review, push and open a PR to `develop`.
- [ ] Confirm exact-head hosted CI passes before merge; merge the PR, confirm the develop-push gate passes, fast-forward the primary checkout, and remove only the verified merged worktree and local branch.
