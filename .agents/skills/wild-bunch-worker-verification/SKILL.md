---
name: wild-bunch-worker-verification
description: Verify Wild Bunch worker returns, PRs, commits, validation notes, and closure claims against issue goals, source changes, publication evidence, browser/UI screenshots when relevant, and verified mainline state before completion is accepted. Use when reviewing or finishing Wild Bunch work, checking Linear/GitHub issue conformance, deciding Green/Amber/Red status, or preventing tests, reports, or worker summaries from being treated as proof.
metadata:
  origin: first_party
  source_author: Harley Bartles
  source_license: MIT
  source_repo: https://github.com/HarleyBartles/agent-asset-marketplace
  source_path: sources/first_party/skills/wild-bunch-worker-verification/SKILL.md
  content_mode: verbatim
---

# Wild Bunch Worker Verification

Use this skill when finishing or reviewing Wild Bunch work. Passing tests are not the same thing as issue-goal conformance.

## Workflow

1. Identify the issue, PR, branch, commit, changed files, validation evidence, and claimed completion state.
2. Compare the changed source against the Linear or GitHub issue goal.
3. Falsify likely misses before accepting the return.
4. Report validation commands run and their results.
5. Include branch, commit SHA, PR URL or number, and a concise touched-files summary.
6. For browser or UI work, require screenshot evidence or state why it is unavailable.
7. Do not claim landed or mainline state unless it is verified after merge.
8. If a PR changes variable gameplay outcomes or initial setup, verify difficulty, entropy, and seeded setup handling or an explicit deferral.
9. Use the installed `wild-bunch-project-doctrine` skill reference when verification needs the seeded setup doctrine.

Read `references/verification-checklist.md` only when building or checking a verification report, closure recommendation, or Green/Amber/Red status. After reading it once for the current verification, do not reread it unless the task changes.
Consult the installed `wild-bunch-project-doctrine` skill reference when the verification needs a falsification pass for setup or entropy handling.

## Rules

- Worker reports, chat summaries, and issue comments are not source proof.
- Passing validation is necessary evidence, not issue-goal conformance by itself.
- A PR existing is not landed state.
- A merge claim is not mainline proof until main is checked after merge.
- Preserve uncertainty when a required source, issue, PR, or validation route is unavailable.
