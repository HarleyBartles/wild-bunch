# Mutations

Use this when creating, updating, or deleting GitHub objects.

## Pick the surface

| Surface | Use when | Required | Optional/notes |
| --- | --- | --- | --- |
| `gh repo create` | Create a new repository. | `name` | `--public`, `--private`, `--clone`. |
| `gh repo fork` | Fork a repository. | `repo` | `--clone`, `--remote`. |
| `gh label create` | Create a label. | `name` | `--color`, `--description`. |
| `gh label edit` | Update a label. | `name` | `--color`, `--description`, `--name`. |
| `gh label delete` | Delete a label. | `name` | `--yes`. |
| `gh release create` | Create a release. | `tag` | `--title`, `--notes`, `--draft`, `--prerelease`. |
| `create_repository` (MCP) | Create a repo through the connector. | `name` | `owner`, `description`, `private`. |
| `fork_repository` (MCP) | Fork a repo through the connector. | `owner`, `repo` | `organization`, `default_branch_only`. |
| `create_branch` (MCP) | Create a branch. | `owner`, `repo`, `branch` | `from_branch` (defaults to default). |
| `create_or_update_file` (MCP) | Create or update a file in a repo. | `owner`, `repo`, `path`, `content`, `message` | `branch`, `sha` required for updates. |
| `push_files` (MCP) | Push multiple files in one commit. | `owner`, `repo`, `branch`, `files[]`, `message` | None. |
| `delete_file` (MCP) | Delete a file from a repo. | `owner`, `repo`, `path`, `message`, `sha` | `branch`. |
| `create_label` (MCP) | Create a label. | `owner`, `repo`, `name` | `color`, `description`. |
| `update_label` (MCP) | Update a label. | `owner`, `repo`, `name` | `new_name`, `color`, `description`. |
| `delete_label` (MCP) | Delete a label. | `owner`, `repo`, `name` | None. |
| `star_repository` / `unstar_repository` (MCP) | Star or unstar a repo. | `owner`, `repo` | None. |

## Notes

- Connector/file mutations should be narrow and authorized. Do not call `create_tree`, `create_commit`, `create_file`, `update_file`, `delete_file`, `update_ref`, or other write tools unless the current task explicitly authorizes the exact mutation.
- For single-file edits, `gh api repos/{owner}/{repo}/contents/{path}` with `PUT` or `DELETE` is often clearer than the low-level `create_tree`/`create_commit` primitives.
- When updating a file through the MCP `create_or_update_file`, the `sha` of the existing blob is required.
