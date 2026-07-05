# Code Review Guide

Use this reference when reviewing work in the Wild Bunch repo — PRs, task diffs, or whole-branch reviews. This is a review methodology, not a merge checklist. It defines the lenses, skills, policies, and checks a reviewer must apply.

## 1. Review Lenses

Apply three core lenses to every review. Invoke two conditional lenses when the work touches product points or playability.

### Core Lenses (every review)

**Principal Architect** — architectural alignment, DDD/CQRS/event-sourcing conformance, aggregate boundary discipline, dependency direction, ADR freshness. Does the work respect the established patterns, or does it hand-roll non-DDD, non-CQRS, or non-event-sourced solutions? Are new aggregates, value objects, or domain events modeled correctly? Does the work leak infrastructure concerns into the domain layer?

**Senior QA Engineer** — test coverage adequacy, test quality (real behavior vs mock behavior), edge cases, regression risk. Are the right test kinds used (see `.agents/docs/validation-policy.md` for the repo's five test kinds: unit, integration, game-content, API, brute-force)? Do tests assert on observable behavior, not mock interactions? Are edge cases covered? Does the diff introduce untested branches?

**Senior Software Engineer** — code quality, naming, error handling, DRY without premature abstraction, YAGNI, existing pattern conformance, file organization. Are names accurate (describe what things do, not how they work)? Is error handling at the right boundary? Does each file have one clear responsibility? Is the work following the file structure from the plan?

### Conditional Lenses (invoke when relevant)

**Product Owner** — invoke when the work touches product points, game flow, or feature scope. Does this deliver what the player needs? Is it the right feature? Does it match the Linear issue's goal? Is there scope creep or scope shrinkage? Would a product owner accept this as "done" for the issue it claims to address?

**Player** — invoke when the work touches player-facing UI, game flow, or playability. Would a player understand this? Does the feedback loop feel right? Is the mental model coherent? Does the UI respect the player's attention? This is reviewing code from the player's mental model, not playtesting — that's a separate skill (`/game-playtest`). Check against `.agents/unslop/play-surface-ui.md` for player-surface discipline.

When invoking a conditional lens, say why: "invoking player lens because this diff changes the arrival flow."

## 2. Architecture Review

Reviewers must invoke the architecture skills before reviewing work that touches domain, persistence, or command/query handlers:

- `/wild-bunch-domain-modeling` — GameSession boundaries, player wallet/inventory, travel rules, clue/journal flows, hidden culprit truth
- `/wild-bunch-dotnet-architecture` — GameSession as aggregate root, event-sourced command flows, JSON snapshot cache, persistence boundaries
- `/ddd` — aggregates, value objects, domain events, strongly-typed IDs
- `/cqrs-event-sourcing` — command/query separation, events as source of truth, projections
- `/event-driven-architecture` — domain events and projections
- `/clean-architecture` — Domain/Application/Infrastructure/Api layering, dependency inversion

Reviewers must check the repo's architectural choices in `.agents/docs/architecture-guardrails.md` and `.agents/architecture-hygiene.md` and assess work against alignment with those standards. The skills and ADRs are the authority, not the repo's current code — if code and skills disagree, the skills win.

**ADR freshness check:** If the work changes an architectural decision, the ADR log at `docs/adr/` must be updated. See `.agents/docs/workflow-policy.md` for freshness check requirements.

## 3. Frontend Review

Reviewers must ensure frontend work aligns with this repo's frontend architecture stack:

- **Styling stack:** `styled-components` for component-owned layout, SASS for global concerns (tokens, reset, base). No plain CSS classes in `className`. Design tokens via `var(--token-name)`. Shared primitives from `src/components/ui/sharedStyled.tsx`. Enforced by `src/tests/stylingEnforcement.test.ts`. See `src/WildBunch.Web/AGENTS.md` and `docs/frontend-styling.md`.
- **Play-surface UI:** Player-facing surfaces must be in-world, not cockpit dashboards. See `.agents/unslop/play-surface-ui.md`. Keep player-facing surfaces as player-usable surfaces, not product chrome.
- **Dev overlay:** If work touches dev overlay, apply `.agents/dev-overlay/DOCTRINE.md` and `.agents/unslop/dev-overlay.md`. Dev panels are contextual to the current gameplay surface. Dev mutations go through backend commands — the frontend never fakes player progress.
- **Source truth:** React renders backend/player-known state, never invents canonical game facts or hidden internal interpretations.
- **Routing:** TanStack Router route tree, lazy-loaded components, URL reflects game state via sync hooks (`usePhaseRouteSync`, `useDevSurfaceSync`). Town place routes are flat siblings under rootRoute, not children of townRoute (TownHubSurface has no Outlet).

## 4. Unslop Application

Reviewers must review the repo's unslop profiles and apply the relevant ones to work under review:

- `.agents/unslop/backend-architecture.md` — for .NET backend work
- `.agents/unslop/play-surface-ui.md` — for player-facing UI
- `.agents/unslop/dev-overlay.md` — for dev overlay work
- The portable profiles from `/unslop-plus` — `code-review`, `testing`, `security-review`, `cleanup-custody`, `frontend-react`, `frontend-ui`, `api-design`, `architecture`, etc.

Apply the profile that matches the work's domain. A frontend-only PR does not need the backend-architecture profile, and vice versa.

## 5. Agent Discovery and Durable Guidance

Reviewers must ensure that anything important for future agents to understand is recorded in durable agent guidance:

- If the work introduces a new pattern, convention, or gotcha that future agents would trip over without knowing, it should be documented in AGENTS.md or a doctrine document
- If the work changes the build/test workflow, update the relevant AGENTS.md section
- If the work discovers a tooling issue (like the write-tool phantom files), it must be recorded in durable guidance so future agents don't trip over it
- INDEX.md files must be regenerated if files were added/removed (via `python scripts/generate_index_mesh.py`)

Durable agent guidance is for "agents will trip over this if they don't know." Deferred work is NOT durable agent guidance — it belongs in Linear issues (see section 7).

## 6. Tooling Hygiene

- The `write` tool on Windows creates phantom files in parent directories of paths with hyphenated components. After batch writes, verify no junk files were created. See `.agents/doctrine/write-tool-phantom-files.md`.
- Before claiming work is done, verify the workspace is clean — no stray files, no uncommitted debug artifacts, no phantom files in parent directories.

## 7. Repo Improvement Check

Every PR should leave the repo in a better state than before, not just add functionality on top of existing patterns. The reviewer must evaluate whether the work perpetuates legacy patterns that could have been modernized with minimal additional effort.

The test is not "is the repo better in the abstract" — it's three concrete questions:

1. **Did the work touch code with a legacy pattern that could be modernized in-scope?** If the diff already modifies a file that has an old pattern (e.g. a `useState` chain that should be a focused hook, a plain CSS class that should be styled-components, a string-typed ID that should be strongly-typed), and the modernization would be a small change within that file, the reviewer should flag it as "fix-while-here" — the cost of fixing it now is near-zero because you're already in the file, but the cost of a separate follow-up PR is high (context switch, review overhead, risk of forgetting).

2. **Did the work discover a problem that has a cheap fix?** If during implementation the worker encountered a problem (a confusing API, a missing test, a stale comment, a tooling issue) and deferred it, the reviewer should ask: could this have been fixed in under 10 minutes? If yes, it should have been included. If the fix is genuinely large, it should be tracked as a Linear issue — the worker should flag the deferred work in their report, plan, or PR body, and a Linear issue should be created to track it (when requested). Silent deferral is not acceptable.

3. **Is the work perpetuating a pattern the repo is actively moving away from?** If the repo has an established better pattern (e.g. strongly-typed IDs, event-sourced command flows, styled-components over CSS classes) and the PR adds new code using the old pattern, that's a finding — even if the old pattern still exists elsewhere. New code should always use the better pattern. "The rest of the file does it this way" is not a justification when the repo has decided to move away from that pattern.

**What this is NOT:**
- It is not a license to scope-creep into unrelated refactors. The test is "am I already here, and is the fix small?" — not "should I refactor everything that bothers me."
- It is not a requirement to fix pre-existing tech debt that the PR didn't touch. If you're not in the file, you're not obligated to fix it.
- It is not a blocker for PRs that are scoped correctly but don't happen to touch legacy code. A clean, well-scoped PR that adds a new feature without touching legacy patterns passes this check.

**The deferral trap:** The most common failure mode is "I'll fix this in a follow-up." Follow-ups don't happen unless they're tracked. The reviewer should treat a deferred fix that meets the "already here + small" test as an Important finding, not a Minor one. If the worker wants to defer a larger fix, they must flag it in their return, plan, or PR body so it can be tracked as a Linear issue — silent deferral is not acceptable.

## 8. Test Coverage

Reviewers must verify that the work is adequately tested:

- **New code is covered by tests.** Every new function, component, hook, handler, or domain method must have tests that verify its behavior — not just its existence. If the diff adds production code without corresponding tests, that's an Important finding.
- **Tests verify real behavior, not mock interactions.** Tests should assert on observable outcomes (rendered output, returned values, state changes), not on which mock functions were called in which order. Mock-heavy tests that pass but don't actually test the behavior are a finding.
- **Edge cases are covered.** The reviewer should identify edge cases in the diff (null/undefined inputs, empty collections, error states, boundary conditions) and check that tests exist for them. Missing edge case coverage is an Important finding for critical paths, Minor for non-critical paths.
- **The right test kind is used.** See `.agents/docs/validation-policy.md` for the repo's five test kinds (unit, integration, game-content, API, brute-force). Using a unit test where an integration test is needed (or vice versa) is a finding.
- **No flaky tests.** A test that passes in isolation but fails under full-suite load is a flaky test. Flaky tests are not acceptable — they erode confidence in the suite and waste CI time. Common causes: shared mutable state (router instances, singletons, module-level caches), timing-dependent assertions on lazy-loaded components, missing `waitFor` around async renders, test ordering dependencies. If a test is flaky, the reviewer must flag it as Critical — a flaky test is worse than no test because it trains the team to ignore failures.
- **All tests pass.** The full suite must pass: `npx vitest run` from `src/WildBunch.Web/` for frontend, `dotnet test` for backend. No skipped tests (`it.skip`, `describe.skip`) without a documented reason.
- **Test output is pristine.** No stray warnings, no console noise, no unhandled promise rejections in test output. Warnings in test output are findings — they indicate either a real problem being silenced or test setup that doesn't match production behavior.

## 9. CI Verification

A review is not complete until the reviewer verifies that CI passes on the PR branch:

- **Check CI status before signing off.** Use `gh pr checks <PR-number>` or the GitHub PR UI to verify all CI jobs are green. A review that approves a PR with failing CI is not a complete review.
- **If CI is failing, the review is blocked.** Do not approve a PR with failing CI, even if the failures seem unrelated. Investigate the failures — if they're genuinely pre-existing and unrelated, document that in the review. If they're caused by the PR's changes, they must be fixed before approval.
- **If CI is still running, wait for it.** Do not approve a PR while CI is in progress. The reviewer's job is to verify the final state, not a provisional state.

## 10. Definition of Done Compliance

A review is not complete until the reviewer verifies:

- All tests pass (`npx vitest run` from `src/WildBunch.Web/` for frontend, `dotnet test` for backend)
- CI passes on the PR branch (see section 9)
- Build succeeds (`npm run build` for frontend, `dotnet build` for backend)
- `npx tsc --noEmit` is clean for frontend work (no new errors)
- `dotnet ef migrations list` passes when persistence may be affected
- New code is covered by tests (see section 8)
- No flaky tests (see section 8)
- INDEX.md files are regenerated if files were added/removed
- ADR log is fresh if architectural decisions changed
- Linear issue is updated to reflect the work
- PR description accurately describes the changes
- No secrets or credentials committed
- No junk/phantom files left in the workspace
- The work matches the Linear issue's goal (no scope creep or shrinkage)

See `.agents/docs/workflow-policy.md` for the full GREEN checklist and `.agents/docs/validation-policy.md` for validation commands.

## 11. Review Output Format

Reviews should produce structured output:

- **Spec compliance verdict** — does the diff match what was requested?
- **Strengths** — specific, evidence-based (file:line references)
- **Issues categorized by severity:**
  - Critical (must fix) — incorrect behavior, security issues, missing requirements
  - Important (should fix) — fragile behavior, maintainability damage, deferred fixes that meet the "already here + small" test
  - Minor (nice to have) — polish, coverage broadening, naming improvements
- **Per-lens findings** — architect, QA, engineer, and if invoked, product owner / player
- **Repo improvement check** — any fix-while-here opportunities or deferred-work flags
- **Assessment and verdict** — approved or needs fixes, with reasoning

## 12. Additional Policy Awareness

Reviewers should be aware of these repo policies and apply them when relevant:

- **Validation policy** (`.agents/docs/validation-policy.md`) — the repo's five test kinds (unit, integration, game-content, API, brute-force) and when to use each
- **Workflow policy** (`.agents/docs/workflow-policy.md`) — fresh-main discipline, PR hygiene, publication proof, GREEN checklist
- **Coding discipline** (`.agents/docs/coding-discipline.md`) — scope boundaries, architecture stack discipline, refactoring rules
- **Artifact policy** (`.agents/docs/artifact-policy.md`) — agent artifact management, screenshots, evidence
- **Mesh policy** (`.agents/docs/mesh-policy.md`) — AGENTS.md, INDEX.md, README file management
- **Dev overlay doctrine** (`.agents/dev-overlay/DOCTRINE.md`) — binding doctrine for dev overlay work
