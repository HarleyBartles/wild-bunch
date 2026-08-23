# Pull request runbook

Use this runbook for pull-request workflow and publication proof in this repo.

## Before you begin

- Read root [`AGENTS.md`](../../AGENTS.md) `## Publication proof for repo work`.
- Read [`.devin/rules/tools.md`](../../.devin/rules/tools.md) for validation commands.
- Invoke `/repo-worker-base`.

## When to use

- Preparing a branch for review.
- Creating or updating a PR.
- Providing publication proof for repo work.

## Draft PR policy

- Open pull requests as **draft**.
- Keep a PR in draft while iterating, running local validation, and performing self-review.
- Only flip a PR out of draft when:
  - self-review is complete,
  - the relevant validation commands pass,
  - the branch is ready for review or merge.
- This repo's CI must not run on draft pull requests. For GitHub Actions, gate `pull_request` workflows so they run only when `github.event.pull_request.draft == false` or on `ready_for_review` activity.
- After flipping a PR to ready, monitor CI and address failures before requesting human review.
- The PR body must include publication proof per root `AGENTS.md`.

## Repo-specific guidance

- Work in an isolated worktree on a task branch.
- Run the relevant validation before pushing:
  - <!-- list validation commands here -->
- Commit focused changes. Do not commit generated artifacts unless the generator produced them.
- Push the branch and open a **draft** PR into `main` unless direct-main work is explicitly authorized.
- A valid repo-work return must include one of:
  1. an open PR URL with branch name and full head SHA;
  2. a verified direct-main commit SHA;
  3. a concrete publication blocker.

## Routing to skills

- `/repo-worker-base` for worktree, branch, and publication boundaries.
- `/using-github-mcp` for PR evidence and GitHub proof.
- `/verification-before-completion` before claiming the PR is green.
