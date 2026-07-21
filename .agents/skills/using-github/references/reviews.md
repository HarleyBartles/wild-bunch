# Reviews

Use this when reading or writing pull request reviews and review comments.

## Pick the surface

| Surface | Use when | Required | Optional/notes |
| --- | --- | --- | --- |
| `gh pr review <number>` | Submit a review event from the CLI. | `number` | `--approve`, `--request-changes`, `--comment`, `--body`. |
| `gh pr view <number> --json reviews` | Read existing reviews on a PR. | `number` | `--json` fields include `reviews`. |
| `gh pr diff <number>` | View the diff before reviewing. | `number` | `--name-only`, `--stat`. |
| `gh api repos/{owner}/{repo}/pulls/{number}/reviews` | List submitted reviews. | `owner`, `repo`, `number` | `per_page`. |
| `gh api repos/{owner}/{repo}/pulls/{number}/comments` | List all review comments with file/line context. | `owner`, `repo`, `number` | Returns `id`, `path`, `line`, `start_line`, `side`, `body`. |
| `pull_request_review_write` (MCP) | Submit a full PR review with inline comments. | `owner`, `repo`, `pull_number`, `commit_id`, `event` | `body`, `comments[]`. |
| `add_comment_to_pending_review` (MCP) | Add a comment to the requester's latest pending review. | `owner`, `repo`, `pullNumber`, `path`, `body` | `line`, `side`, `startLine`, `startSide`, `subjectType`. |
| `add_reply_to_pull_request_comment` (MCP) | Reply to an existing review comment. | `owner`, `repo`, `commentId`, `body` | Use numeric `commentId` from `#discussion_r<id>`. |

## Important distinctions

- Timeline comments on a PR are not review comments. Use `gh pr comment` or `add_issue_comment` for those.
- Native PR reviews carry an event (`COMMENT`, `APPROVE`, `REQUEST_CHANGES`). Prefer `COMMENT` when the authoring and reviewing agent share the same GitHub identity.
- Review comment IDs from REST are numeric (used in `#discussion_r<id>` anchors). GraphQL review thread IDs are `PRRT_...` and are used only for thread-level resolution or reply mutations.
- Review-thread resolution and multi-reply mutation currently require GraphQL; the MCP server does not expose a direct thread resolver.

## When to choose CLI vs MCP

- Use `gh pr review` for quick approve/request-changes/comment events from a terminal.
- Use `pull_request_review_write` (MCP) when you need to submit structured inline comments with file and line positions.
- Use `gh api repos/{owner}/{repo}/pulls/{number}/comments` when verifying existing review comments without writing.
