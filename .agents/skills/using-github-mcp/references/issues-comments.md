# Issues and comments

Use this when reading or writing issues and issue or pull-request timeline comments.

## Pick the surface

| Surface | Use when | Required | Optional/notes |
| --- | --- | --- | --- |
| `gh issue view <number>` | Read an issue. | `number` | `--json` for structured fields. |
| `gh issue list` | List issues in a repository. | None | `--repo`, `--label`, `--state`, `--limit`. |
| `gh issue create` | Create an issue. | `--title` | `--body`, `--label`, `--project`. |
| `gh issue comment <number>` | Add a timeline comment to an issue. | `number`, `body` | `--edit-last`. |
| `gh issue close <number>` | Close an issue. | `number` | `--comment` to add closing comment. |
| `gh pr comment <number>` | Add a timeline comment to a PR. | `number`, `body` | `--edit-last`. |
| `issue_read` (MCP) | Read an issue through the connector. | `owner`, `repo`, `issue_number` | None. |
| `list_issues` (MCP) | List issues through the connector. | `owner`, `repo` | `state`, `label`, `assignee`, `limit`. |
| `issue_write` (MCP) | Create or update an issue. | `owner`, `repo`, `title` | `body`, `state`, `labels`, `assignees`. |
| `add_issue_comment` (MCP) | Add a comment to an issue or PR. | `owner`, `repo`, `issue_number`, `body` | `comment_id` for reaction on an existing comment. |
| `sub_issue_write` (MCP) | Create or update sub-issues. | `owner`, `repo`, `issue_number` | `sub_issues` to add/remove. |

## Notes

- The `add_issue_comment` MCP tool works for both issues and pull requests; pass the PR number as `issue_number` only when you are not adding a review comment.
- `gh pr comment` and `gh issue comment` add timeline comments, not review threads. Use the reviews reference for review threads.
- Sub-issues (`sub_issue_write`) are a newer GitHub feature; verify the target repo supports them before using.
