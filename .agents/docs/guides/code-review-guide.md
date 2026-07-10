# Code Review Guide

Use this reference when reviewing work in the Wild Bunch repo â€” PRs, task diffs, or whole-branch reviews. This is a review methodology, not a merge checklist. It defines the lenses, skills, policies, and checks a reviewer must apply.

## 1. Review Lenses

Apply three core lenses to every review. Invoke two conditional lenses when the work touches product points or playability.

### Core Lenses (every review)

**Principal Architect** â€” architectural alignment, DDD/CQRS/event-sourcing conformance, aggregate boundary discipline, dependency direction, ADR freshness. Does the work respect the established patterns, or does it hand-roll non-DDD, non-CQRS, or non-event-sourced solutions? Are new aggregates, value objects, or domain events modeled correctly? Does the work leak infrastructure concerns into the domain layer?

**Senior QA Engineer** â€” test coverage adequacy, test quality (real behavior vs mock behavior), edge cases, regression risk. Are the right test kinds used (see `.agents/docs/validation-policy.md` for the repo's five test kinds: unit, integration, game-content, API, brute-force)? Do tests assert on observable behavior, not mock interactions? Are edge cases covered? Does the diff introduce untested branches?

**Senior Software Engineer** â€” code quality, naming, error handling, DRY without premature abstraction, YAGNI, existing pattern conformance, file organization. Are names accurate (describe what things do, not how they work)? Is error handling at the right boundary? Does each file have one clear responsibility? Is the work following the file structure from the plan?

### Conditional Lenses (invoke when relevant)

**Product Owner** â€” invoke when the work touches product points, game flow, or feature scope. Does this deliver what the player needs? Is it the right feature? Does it match the Linear issue's goal? Is there scope creep or scope shrinkage? Would a product owner accept this as "done" for the issue it claims to address?

**Player** â€” invoke when the work touches player-facing UI, game flow, or playability. Would a player understand this? Does the feedback loop feel right? Is the mental model coherent? Does the UI respect the player's attention? This is reviewing code from the player's mental model, not playtesting â€” that's a separate skill (`/game-playtest`). Check against `.agents/unslop/play-surface-ui.md` for player-surface discipline.

When invoking a conditional lens, say why: "invoking player lens because this diff changes the arrival flow."

## 2. Architecture Review

Reviewers must invoke the architecture skills before reviewing work that touches domain, persistence, or command/query handlers:

- `/wild-bunch-domain-modeling` â€” GameSession boundaries, player wallet/inventory, travel rules, clue/journal flows, hidden culprit truth
- `/wild-bunch-dotnet-architecture` â€” GameSession as aggregate root, event-sourced command flows, JSON snapshot cache, persistence boundaries
- `/ddd` â€” aggregates, value objects, domain events, strongly-typed IDs
- `/cqrs-event-sourcing` â€” command/query separation, events as source of truth, projections
- `/event-driven-architecture` â€” domain events and projections
- `/clean-architecture` â€” Domain/Application/Infrastructure/Api layering, dependency inversion

Reviewers must check the repo's architectural choices in `.agents/docs/architecture-guardrails.md` and `.agents/docs/architecture-hygiene.md` and assess work against alignment with those standards. The skills and ADRs are the authority, not the repo's current code â€” if code and skills disagree, the skills win.

**ADR freshness check:** If the work changes an architectural decision, the ADR log at `docs/adr/` must be updated. See `.agents/docs/workflow-policy.md` for freshness check requirements.

## 3. Frontend Review

Reviewers must verify that frontend work aligns with the frontend standards documented in [`.agents/docs/frontend-standards.md`](../frontend-standards.md). That document covers the styling stack, play-surface UI, source truth, dev overlay, and routing conventions. Reviewers should read it before reviewing frontend work and check the diff against each applicable standard.

## 4. Unslop Application

Reviewers must review the repo's unslop profiles and apply the relevant ones to work under review:

- `.agents/unslop/backend-architecture.md` â€” for .NET backend work
- `.agents/unslop/play-surface-ui.md` â€” for player-facing UI
- `.agents/unslop/dev-overlay.md` â€” for dev overlay work
- The portable profiles from `/unslop-plus` â€” `code-review`, `testing`, `security-review`, `cleanup-custody`, `frontend-react`, `frontend-ui`, `api-design`, `architecture`, etc.

Apply the profile that matches the work's domain. A frontend-only PR does not need the backend-architecture profile, and vice versa.

## 5. Agent Discovery and Durable Guidance

Reviewers must ensure that anything important for future agents to understand is recorded in durable agent guidance:

- If the work introduces a new pattern, convention, or gotcha that future agents would trip over without knowing, it should be documented in AGENTS.md or a doctrine document
- If the work changes the build/test workflow, update the relevant AGENTS.md section
- If the work discovers a tooling issue, it must be recorded in durable guidance so future agents don't trip over it
- INDEX.md files must be regenerated if files were added/removed (via `python scripts/generate_index_mesh.py` or `.\scripts\generate_index_mesh.ps1`)

Durable agent guidance is for "agents will trip over this if they don't know." Deferred work is NOT durable agent guidance â€” it belongs in Linear issues (see section 7).

## 6. Tooling Hygiene

Reviewers must verify the workspace is clean â€” no stray files, no uncommitted debug artifacts, no phantom files in parent directories.

## 7. Repo Improvement Check

Every PR should leave the repo in a better state than before, not just add functionality on top of existing patterns. The reviewer must evaluate whether the work perpetuates legacy patterns that could have been modernized with minimal additional effort.

The test is not "is the repo better in the abstract" â€” it's three concrete questions:

1. **Did the work touch code with a legacy pattern that could be modernized in-scope?** If the diff already modifies a file that has an old pattern (e.g. a `useState` chain that should be a focused hook, a plain CSS class that should be styled-components, a string-typed ID that should be strongly-typed), and the modernization would be a small change within that file, the reviewer should flag it as "fix-while-here" â€” the cost of fixing it now is near-zero because you're already in the file, but the cost of a separate follow-up PR is high (context switch, review overhead, risk of forgetting).

2. **Did the work discover a problem that has a cheap fix?** If during implementation the worker encountered a problem (a confusing API, a missing test, a stale comment, a tooling issue) and deferred it, the reviewer should ask: could this have been fixed in under 10 minutes? If yes, it should have been included. If the fix is genuinely large, it should be tracked as a Linear issue â€” the worker should flag the deferred work in their report, plan, or PR body, and a Linear issue should be created to track it (when requested). Silent deferral is not acceptable.

3. **Is the work perpetuating a pattern the repo is actively moving away from?** If the repo has an established better pattern (e.g. strongly-typed IDs, event-sourced command flows, styled-components over CSS classes) and the PR adds new code using the old pattern, that's a finding â€” even if the old pattern still exists elsewhere. New code should always use the better pattern. "The rest of the file does it this way" is not a justification when the repo has decided to move away from that pattern.

**What this is NOT:**
- It is not a license to scope-creep into unrelated refactors. The test is "am I already here, and is the fix small?" â€” not "should I refactor everything that bothers me."
- It is not a requirement to fix pre-existing tech debt that the PR didn't touch. If you're not in the file, you're not obligated to fix it.
- It is not a blocker for PRs that are scoped correctly but don't happen to touch legacy code. A clean, well-scoped PR that adds a new feature without touching legacy patterns passes this check.

**The deferral trap:** The most common failure mode is "I'll fix this in a follow-up." Follow-ups don't happen unless they're tracked. The reviewer should treat a deferred fix that meets the "already here + small" test as a P1 finding, not a P2. If the worker wants to defer a larger fix, they must flag it in their return, plan, or PR body so it can be tracked as a Linear issue â€” silent deferral is not acceptable.

## 8. Test Coverage

Reviewers must verify that the work is adequately tested. The test quality standards are documented in [`.agents/docs/validation-policy.md`](../validation-policy.md) under "Test Quality Standards" â€” reviewers should read that section and check the diff against each standard. Key points the reviewer must verify:

- New code is covered by tests
- Tests verify real behavior, not mock interactions
- Edge cases are covered
- The right test kind is used (see validation-policy.md's Test Kinds section)
- No flaky tests â€” a flaky test is a P0 finding (worse than no test)
- All tests pass with no skipped tests without reason
- Test output is pristine (no warnings, no noise)

## 9. CI Verification

A review is not complete until the reviewer verifies that CI passes on the PR branch:

- **Check CI status before signing off.** Use `gh pr checks <PR-number>` or the GitHub PR UI to verify all CI jobs are green. A review that approves a PR with failing CI is not a complete review.
- **If CI is failing, the review is blocked.** Do not approve a PR with failing CI, even if the failures seem unrelated. Investigate the failures â€” if they're genuinely pre-existing and unrelated, document that in the review. If they're caused by the PR's changes, they must be fixed before approval.
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

Reviews must produce structured output reported back to the session AND post findings to the GitHub PR (see section 14 for the connector posting flow). The structured output is the primary deliverable; PR comments are for provenance and resolution tracking.

Structured output must include:

- **Spec compliance verdict** â€” does the diff match what was requested?
- **Strengths** â€” specific, evidence-based (file:line references)
- **Findings categorized by priority** â€” reviewers must use the P0â€“P3 taxonomy defined in section 13. Each finding gets a stable label (P0.1, P1.1, P1.2, P2.1, P3.1, â€¦) so they can be referenced in discussion and follow-up. Group findings by priority level, not by lens.
- **Per-lens findings** â€” architect, QA, engineer, and if invoked, product owner / player. Lens findings should still reference the P-labels from the grouped findings.
- **Repo improvement check** â€” any fix-while-here opportunities or deferred-work flags
- **Assessment and verdict** â€” approved or needs fixes, with reasoning. A verdict of "needs fixes" must list the P0 and P1 findings that block approval. P2 and P3 findings do not block approval.
- **PR posting confirmation** â€” confirm that findings were posted to the PR (review with inline comments or summary comment), or report the connector error if posting failed.

## 12. Additional Policy Awareness

Reviewers should be aware of these repo policies and apply them when relevant:

- **Validation policy** (`.agents/docs/validation-policy.md`) â€” the repo's five test kinds (unit, integration, game-content, API, brute-force) and when to use each
- **Workflow policy** (`.agents/docs/workflow-policy.md`) â€” fresh-main discipline, PR hygiene, publication proof, GREEN checklist
- **Coding discipline** (`.agents/docs/coding-discipline.md`) â€” scope boundaries, architecture stack discipline, refactoring rules
- **Artifact policy** (`.agents/docs/artifact-policy.md`) â€” agent artifact management, screenshots, evidence
- **Mesh policy** (`.agents/docs/mesh-policy.md`) â€” AGENTS.md, INDEX.md, README file management
- **Dev overlay doctrine** (`.agents/docs/dev-overlay-doctrine.md`) â€” binding doctrine for dev overlay work

## 13. Finding Priority Taxonomy

All review findings must be labeled using the P0â€“P3 priority taxonomy. This replaces the previous Critical/Important/Minor severity scheme. The taxonomy is priority, not severity â€” it tells the author what to do about the finding, not just how bad it is.

### Priority levels

| Label | Meaning | Blocks approval? | Examples |
|-------|---------|-------------------|----------|
| **P0** | Must fix | Yes | Incorrect behavior, security issues, missing requirements, data loss, broken build, flaky tests |
| **P1** | Should fix | Yes | Fragile behavior, maintainability damage, deferred fixes that meet the "already here + small" test, missing test coverage for new behavior |
| **P2** | Could fix | No | Polish, coverage broadening, naming improvements, minor type-safety widening |
| **P3** | Ok to defer | No | Pre-existing tech debt not touched by the diff, style preferences, larger refactors that warrant a separate issue |

### Labeling convention

When a review has multiple findings in the same priority group, number them sequentially within that group:

- `P0.1`, `P0.2` â€” two must-fix findings
- `P1.1`, `P1.2`, `P1.3` â€” three should-fix findings
- `P2.1` â€” one could-fix finding
- `P3.1`, `P3.2` â€” two deferred findings

If a group has only one finding, use the bare label (`P0`, `P1`, `P2`, `P3`) or the numbered form (`P0.1`) â€” both are acceptable, but be consistent within a single review.

### Mapping from the old severity scheme

Reviews written before this taxonomy used Critical/Important/Minor. The mapping is:

- Critical â†’ P0
- Important â†’ P1
- Minor â†’ P2
- (no equivalent) â†’ P3

### Verdict rules

- **Needs fixes:** any P0 or P1 finding blocks approval. The verdict must list the blocking findings by label.
- **Approved with notes:** no P0 or P1 findings, but P2/P3 findings exist. The author may address them at their discretion.
- **Approved:** no findings, or only P3 findings.

## 14. Posting Review Findings to the PR

A review is not complete until findings are posted to the GitHub PR. The structured output in the agent's response is the primary deliverable; PR comments are for tracing, provenance, and for follow-up agents to pick up and resolve. Reviewers must always post review output to the PR â€” there is no "review without posting."

### Same-authored constraint

The PR author and code reviewer share the same GitHub connector auth (Harley's token). This means:

- **Cannot submit `APPROVE` or `REQUEST_CHANGES`** â€” GitHub does not allow an author to review their own PR with a blocking event. Attempting it will fail or be silently ignored.
- **Can submit `COMMENT` reviews** â€” a non-blocking review with inline comments attached to diff lines. This is the correct channel for findings with file:line references.
- **Can post regular PR comments** â€” via `add_issue_comment`. This is the correct channel for summary comments when there are no inline findings.

### When to use which channel

| Situation | Channel | Tool |
|-----------|---------|------|
| One or more findings (P0â€“P3) with file:line references | COMMENT review with inline comments | `pull_request_review_write` + `add_comment_to_pending_review` |
| No findings, or only a summary verdict (approved, no inline items) | Regular PR comment | `add_issue_comment` |
| Both inline findings AND a summary verdict | COMMENT review with inline comments + body summary | `pull_request_review_write` (body = summary) |

### Connector: posting inline review comments

Use the `github-mcp-server` connector. The flow is a three-step sequence: create a pending review, add inline comments, then submit the pending review as a `COMMENT` review.

**Step 1 â€” Create a pending review:**

```
mcp_call_tool:
  server_name: github-mcp-server
  tool_name: pull_request_review_write
  arguments:
    method: create
    owner: HarleyBartles
    repo: wild-bunch
    pullNumber: <PR number>
    commitID: <latest PR head SHA>
    # Do NOT pass event â€” omitting it creates a pending review
```

**Step 2 â€” Add each inline comment** (one call per finding):

```
mcp_call_tool:
  server_name: github-mcp-server
  tool_name: add_comment_to_pending_review
  arguments:
    owner: HarleyBartles
    repo: wild-bunch
    pullNumber: <PR number>
    path: <file path as it appears in the PR diff, relative to repo root>
    line: <line number in the new (RIGHT) side of the diff>
    side: RIGHT
    subjectType: LINE
    body: |
      **P1.2 â€” Missing test coverage for arrival flow**

      The `?arrived=1` param is never set by any code path, making the
      arrival notice UI dead code. Add a test that verifies the param
      is set when transitioning from on-trail to in-town.
```

For multi-line comments, also pass `startLine` and `startSide`:

```
    startLine: <first line of the range>
    startSide: RIGHT
```

For file-level comments (no specific line), use `subjectType: FILE` and omit `line`/`side`/`startLine`/`startSide`.

**Step 3 â€” Submit the pending review as COMMENT:**

```
mcp_call_tool:
  server_name: github-mcp-server
  tool_name: pull_request_review_write
  arguments:
    method: submit_pending
    owner: HarleyBartles
    repo: wild-bunch
    pullNumber: <PR number>
    event: COMMENT
    body: |
      ## Code Review Summary

      **Verdict:** Needs fixes (P0.1, P1.2)

      See inline comments for findings. Structured output with full
      per-lens analysis has been reported back to the session.
```

### Connector: posting a summary comment (no inline findings)

When there are no inline findings (e.g. approved with no P2/P3 items, or only a verdict summary), post a regular PR comment:

```
mcp_call_tool:
  server_name: github-mcp-server
  tool_name: add_issue_comment
  arguments:
    owner: HarleyBartles
    repo: wild-bunch
    issue_number: <PR number>
    body: |
      ## Code Review Summary

      **Verdict:** Approved

      All 227 tests pass, tsc clean, build succeeds. No findings.
      Structured output reported back to the session.
```

### Comment body format

Each inline comment body must include:

1. **The P-label** (P0.1, P1.2, etc.) as a bold header
2. **A one-line summary** of the finding
3. **The detail** â€” what's wrong, why it matters, and what to do about it
4. **A file:line reference** if the comment is file-level but references a specific location

The review body (submitted with the pending review) must include:

1. **Verdict** â€” "Needs fixes", "Approved with notes", or "Approved"
2. **Blocking findings** â€” list P0 and P1 labels by name (if any)
3. **A note** that structured output with full per-lens analysis has been reported back to the session

### Getting the PR head SHA and diff for line mapping

Before posting inline comments, the reviewer must map each finding to a line in the PR diff. Use:

```
mcp_call_tool:
  server_name: github-mcp-server
  tool_name: pull_request_read
  arguments:
    method: get
    owner: HarleyBartles
    repo: wild-bunch
    pullNumber: <PR number>
```

This returns the PR metadata including `headRefOid` (the latest head SHA). Use `method: get_diff` to get the diff for line mapping. The `line` parameter in `add_comment_to_pending_review` refers to the line number in the file (not the diff hunk), on the RIGHT side (new state).

### Error handling

If the connector rejects a review submission or inline comment:

- Do NOT silently fall back to a generic PR comment for inline findings â€” that loses the file:line context.
- Report the connector error in the structured output and note that the PR comments were not posted.
- If a specific line cannot be mapped to the diff, omit that inline comment and note it in the review body with the file:line reference in plain text.

### What PR comments are NOT for

PR comments are not a substitute for the structured output. The structured output is the primary deliverable and must always be reported back to the session. PR comments are for:

- **Provenance** â€” a durable record on the PR that a review happened and what it found
- **Resolution tracking** â€” follow-up agents can read the PR review comments and resolve them
- **Visibility** â€” non-agent humans can see the review happened

PR comments are NOT for:

- Duplicating the full structured output (that goes in the session response)
- Posting findings that the reviewer has already fixed (fixed findings are noted in the structured output, not posted as PR comments)
- Generic progress notes or chat â€” use the structured output for that



