---
name: requesting-code-review
description: Use when completing tasks, implementing major features, or before merging
  to verify work meets requirements
metadata:
  source-id: requesting-code-review
  source-path: codex-marketplace/plugins/superpowers-plus/skills/requesting-code-review/SKILL.md
  provenance-name: Requesting Code Review first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when completing tasks, implementing major features, or before merging
    to verify work meets requirements
  use_when:
  - Use when completing a task or major feature, or before merging.
  - Use after subagent-driven-development per-task review.
  - Use when a fresh reviewer perspective will catch issues before they cascade.
  do_not_use_when:
  - Do not use before tests pass.
  - Do not use when no changes exist to review.
  - Do not use as a substitute for self-review.
  related_skills:
  - receiving-code-review
  - iterative-review
  - finishing-a-development-branch
  - subagent-driven-development
  - executing-plans
license: MIT
---
## Provenance

This skill is a first-party authored derivation of `obra/superpowers` v6.2.0, released under the MIT License. The original upstream snapshot is retained in `codex-marketplace/plugins/superpowers-plus/skills/requesting-code-review/` for reference.

# Requesting Code Review

Dispatch a code reviewer subagent to catch issues before they cascade. The reviewer gets precisely crafted context for evaluation — never your session's history.

**Core principle:** Review early, review often.

**First step:** Read this skill's baseline (`references/code-review-baseline.md`) and the repo's `.agents/runbooks/code-review.md` before executing the stage checklist.

## When to Request Review

**Mandatory:**
- After each task in subagent-driven development
- After completing major feature
- Before merge to main

**Optional but valuable:**
- When stuck (fresh perspective)
- Before refactoring (baseline check)
- After fixing complex bug

## How to Request

**1. Get git SHAs:**
```bash
BASE_SHA=$(git rev-parse HEAD~1)  # or origin/main
HEAD_SHA=$(git rev-parse HEAD)
```

**2. Dispatch code reviewer subagent:**

Dispatch a `general-purpose` subagent, filling the template at [code-reviewer.md](code-reviewer.md)

**Placeholders:**
- `{DESCRIPTION}` - Brief summary of what you built
- `{PLAN_OR_REQUIREMENTS}` - What it should do
- `{BASE_SHA}` - Starting commit
- `{HEAD_SHA}` - Ending commit

**3. Act on feedback:**
- Fix Critical issues immediately
- Fix Important issues before proceeding
- Note Minor issues for later
- Push back if reviewer is wrong (with reasoning)

## Branch or PR diff review

When the code-review request is about a branch or PR diff, the orchestrator (this session)
prepares the review inputs; the reviewer subagent only reads the prepared diff and
description.

1. Determine the base ref (`<base>`) and branch (`<branch>`).
2. Generate the review package as UTF-8 without a BOM:
   - Bash: `.agents/skills/subagent-workspace/scripts/review-package - <base> <branch> <diff_path>` (use `-` for no plan file; `diff_path` is optional and the script prints the path it wrote).
   - PowerShell: `.agents/skills/subagent-workspace/scripts/review-package.ps1 - <base> <branch> <diff_path>`
3. If the review object is a PR, capture the PR title and body into `<pr_description>`
   (e.g. with `gh pr view <number> --json title,body` or `mcp_call_tool`).
4. Dispatch the reviewer subagent with the prepared inputs:
   - `reviewer` for most reviews.
   - `reviewer-strong` for full branch/PR reviews where the whole diff is in scope.
   - `reviewer-fixes` for small, tightly focused re-reviews of a single fix or a small
     coherent diff.

Inputs to pass to the subagent:
- `<diff_path>` — the prepared diff file.
- `<pr_description>` — the PR title/body and any linked issue/spec context (optional).
- `<base>` and `<branch>` — the base and head refs (optional, for extra verification).

The subagent reads the prepared diff, uses `<pr_description>` to understand intent and
scope, cites specific files and line numbers, and does not modify files.

Use the prepared-diff prompt template at [reviewer-prompt.md](reviewer-prompt.md).

## Example

```
[Just completed Task 2: Add verification function]

You: Let me request code review before proceeding.

BASE_SHA=$(git log --oneline | grep "Task 1" | head -1 | awk '{print $1}')
HEAD_SHA=$(git rev-parse HEAD)

[Dispatch code reviewer subagent]
  DESCRIPTION: Added verifyIndex() and repairIndex() with 4 issue types
  PLAN_OR_REQUIREMENTS: Task 2 from .agents/plans/deployment-plan.md
  BASE_SHA: a7981ec
  HEAD_SHA: 3df7661

[Subagent returns]:
  Strengths: Clean architecture, real tests
  Issues:
    Important: Missing progress indicators
    Minor: Magic number (100) for reporting interval
  Assessment: Ready to proceed

You: [Fix progress indicators]
[Continue to Task 3]
```

## Common Rationalizations

| Excuse | Reality |
|--------|---------|
| "I'll just review the diff myself instead of dispatching a reviewer" | You're the coordinator — reviewing the diff inline burns the context window you need to keep driving the work. Dispatch a reviewer subagent: the diff and the evaluation live in its context, and only the findings come back to you. |
| "The reviewer needs my whole session history to understand the change" | Hand it precisely crafted context, never your session's history. That keeps the reviewer on the work product, not your thought process. |

## Red Flags

**Never:**
- Skip review because "it's simple"
- Ignore Critical issues
- Proceed with unfixed Important issues
- Argue with valid technical feedback

**If reviewer wrong:**
- Push back with technical reasoning
- Show code/tests that prove it works
- Request clarification

See templates at [code-reviewer.md](code-reviewer.md) for commit-range review and
[reviewer-prompt.md](reviewer-prompt.md) for prepared branch/PR diff review.

Before requesting review on a PR — or changing a PR's draft state to signal readiness — consult `.agents/runbooks/pr.md` `## Draft PR policy` so the review request aligns with the repo's draft-to-ready transition.
