# Repo Posture

## Source truth

- Wild Bunch is a C#/.NET game in `HarleyBartles/wild-bunch`.
- Fetch and inspect current `origin/main` before starting repository changes.
- Use the live task branch for its unmerged diff and GitHub for published branch, PR, review, check, and merge facts.
- Use Linear for issue facts when a task is Linear-backed.
- Treat briefs, comments, chat summaries, reports, local artifacts, and memory as context rather than proof of current repository or publication state.

## Work route

- Use a dedicated linked worktree under `Z:\_agent-worktrees\wild-bunch` and a task branch.
- Record the starting main SHA, branch, worktree path, and pre-existing status.
- Keep scratch artifacts under `Z:\_agent-scratch\wild-bunch\<branch-name>`.
- Preserve protected and generated surfaces; use their owning scripts.
- Keep the change within the stated scope and validate the exact final tree.

## Return evidence

Return the task identifier, worktree, branch, starting main SHA, final commit, changed scope, validation commands and results, final status, and concerns. Include PR and remote-check evidence only when publication was requested and proved.

Read [Policy references](policy-references.md) before reporting missing tooling. Read [Skill routing](skill-routing.md) before architecture, domain, browser, connector, or workflow work.
