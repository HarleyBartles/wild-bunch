---
name: github-superpowers
description: Use when shaping GitHub-facing work so it starts with @using-superpowers, selects the smallest applicable specialist workflow, and keeps GitHub proof, review routing, publication proof, and final main-state verification bound to github-operations.
metadata:
  source-id: github-superpowers
  source-path: sources/first_party/skills/github-superpowers/SKILL.md
  provenance-name: MARK-143 GitHub Superpowers compositional skill
license: "MIT"
---
# GitHub Superpowers

Use this skill when GitHub-facing work needs workflow selection and proof boundaries instead of a generic catch-all packet.

## Core job

Shape boring, worker-send-ready GitHub packets so they say:

1. which specialist workflow is the smallest applicable fit;
2. why that skill applies;
3. what evidence will prove the workflow was followed.

## Composition

Start with `@using-superpowers` as the workflow-selection entrypoint.

Use `@github-operations` for GitHub evidence, PR/branch/commit/status/review/merge/main-state/publication proof, and native review-write boundary handling.

When the packet is plan-shaped and meant for a worker, use `@writing-plans` for route review and `@executing-plans` as the outer execution workflow.

Use `@requesting-code-review` when the task is asking for another review pass.

Use `@receiving-code-review` when interpreting review feedback, verifier feedback, or worker replies before action.

Use `@verification-before-completion` before claiming fixed, passing, merged, published, or complete.

Use `@finishing-a-development-branch` when implementation is complete and branch closeout is the actual task.

Use `@connector-safety` before any GitHub mutation or blocked-write recovery,
including PR comments, PR reviews and inline comments, issue mutations,
labels, milestones, file or profile updates, merges, closes, deletes,
publishes, and other high-risk mutations.

Use `@unslop-superpowers` when the GitHub packet needs repo-specific anti-slop controls, profile-aware review findings, or evidence requirements.

Nesting rule:

- pick the smallest specialist workflow that actually fits;
- use TDD, debugging, verification, or closeout skills only when they are the smallest applicable specialist workflow;
- do not stack skills just because they are available.

## GitHub shaping rules

- Shape one boring GitHub proof packet or branch-closeout packet at a time.
- Keep PR, branch, commit, review, status, merge, publication, and final main-state questions separate from implementation work.
- Prefer read-before-write on the smallest relevant GitHub surface when practical.
- Prefer one GitHub side effect per call.
- Keep issue, PR, review, status, comment, merge, and closeout payloads narrow.
- Treat blocked connector writes as a signal to narrow, verify, or stop, not as completion.
- Do not claim a GitHub mutation succeeded unless the connector result or readback proves it.

## Authority split

This skill shapes GitHub-facing workflow selection and proof boundaries.
It does not dispatch Codex workers, own Linear planning, implement code, or claim publication, merge, or closeout by itself.
