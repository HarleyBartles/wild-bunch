# MCP surface

Use this when the GitHub MCP server is available and you need to pick the right tool.

## Default toolsets

By default the GitHub MCP server exposes `context`, `repos`, `issues`, `pull_requests`, and `users`. Additional toolsets can be enabled with `enable_toolset` or server configuration.

## Tool selection by intent

| Intent | Start with |
| --- | --- |
| Read a file | `get_file_contents` |
| List/search commits | `list_commits` / `get_commit` |
| List branches or tags | `list_branches` / `list_tags` |
| Search repositories | `search_repositories` |
| Search code | `search_code` |
| Read or create a PR | `pull_request_read` / `create_pull_request` |
| List PRs | `list_pull_requests` / `search_pull_requests` |
| Merge or update a PR | `merge_pull_request` / `update_pull_request` / `update_pull_request_branch` |
| Review a PR | `pull_request_review_write` |
| Comment on a PR | `add_comment_to_pending_review` or `add_reply_to_pull_request_comment` |
| Read or create an issue | `issue_read` / `issue_write` |
| Comment on an issue or PR | `add_issue_comment` |
| List/create labels | `list_labels` / `create_label` / `update_label` / `delete_label` |
| Run or list Actions | `actions_list` / `actions_get` / `actions_run_trigger` |
| Read security alerts | `list_code_scanning_alerts` / `list_dependabot_alerts` / `list_secret_scanning_alerts` |
| List notifications | `list_notifications` |
| Project board work | `list_projects` / `get_project` / `create_project_item` / `update_project_item` |

## Capability and route choice

Choose by capability and evidence need, not by a fixed runtime tool name:

- Use MCP tools when the active runtime exposes the GitHub MCP server and the task is a narrow read or authorized write.
- Use `gh` when you need ad-hoc, scriptable CLI access or when the MCP server does not expose the needed tool.
- Use `gh api` or `gh api graphql` when MCP lacks a specific object (e.g., review thread resolution, exact head SHA, custom GraphQL fields).
- Use local `git` when the state is already in the working tree or history and does not need remote GitHub evidence.

## Notes

- The MCP server does not expose full GraphQL or review-thread resolution. Drop to `gh api graphql` for those.
- Before any write, classify the intended tool as `read_only` or `mutation` and confirm the current task authorizes the exact mutation.
