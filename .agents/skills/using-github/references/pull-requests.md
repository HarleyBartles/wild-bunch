# Pull requests

Use this when creating, updating, merging, or checking pull requests.

## Pick the surface

| Surface | Use when | Required | Optional/notes |
| --- | --- | --- | --- |
| `gh pr create` | Create a PR from the current branch. | local branch pushed to remote | `--title`, `--body`, `--base`, `--draft`. |
| `gh pr view <number>` | Check PR state, head SHA, and mergeability. | `number` or current branch | `--json headRefOid,url,state,mergeable`. |
| `gh pr merge <number>` | Merge an approved PR. | `number` | `--squash`, `--rebase`, `--merge`, `--auto`. |
| `gh pr checkout <number>` | Check out a PR branch locally. | `number` | `--branch` to rename. |
| `gh pr comment <number>` | Add a timeline comment to a PR. | `number`, `body` | `--edit-last` to edit previous comment. |
| `create_pull_request` (MCP) | Create a PR through the connector. | `owner`, `repo`, `title`, `head`, `base` | `body`, `draft`. |
| `update_pull_request` (MCP) | Update PR title/body, toggle draft, request reviewers. | `owner`, `repo`, `pull_number` | `title`, `body`, `draft`, `reviewers`. |
| `merge_pull_request` (MCP) | Merge a PR through the connector. | `owner`, `repo`, `pull_number` | `commit_title`, `commit_message`, `sha`. |
| `update_pull_request_branch` (MCP) | Update a PR branch with base changes. | `owner`, `repo`, `pull_number` | None. |
| `gh api repos/{owner}/{repo}/pulls/{number}` | Read raw PR JSON. | `owner`, `repo`, `number` | Any PR fields. |

## Notes

- Always verify the remote PR head SHA with `gh pr view <number> --json headRefOid` after a push.
- Force push is only needed when rewriting history; ordinary new commits on top of an already-pushed head can use `git push origin <branch>`.
- The MCP `create_pull_request` and `update_pull_request` tools are writes; only use them when the current task explicitly authorizes a PR mutation.
