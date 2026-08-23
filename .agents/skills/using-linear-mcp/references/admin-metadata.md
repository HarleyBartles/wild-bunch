# Metadata and admin surfaces

Use this when the task is about Linear-owned taxonomy, workspace structure, or status metadata.

## Tools

| Tool | Use when | Required params | Optional params |
| --- | --- | --- | --- |
| `list_teams` | Find workspace teams. | None | `createdAt`, `cursor`, `includeArchived`, `limit`, `orderBy`, `query`, `updatedAt` |
| `get_team` | Resolve one team by key, UUID, or name. | `query` | None |
| `list_users` | Find users in the workspace. | None | `cursor`, `limit`, `orderBy`, `query`, `team` |
| `get_user` | Resolve one user by ID, name, email, or `me`. | `query` | None |
| `list_issue_labels` | Inspect issue labels, optionally scoped to a team. | None | `cursor`, `limit`, `name`, `orderBy`, `team` |
| `list_project_labels` | Inspect project labels. | None | `cursor`, `limit`, `name`, `orderBy` |
| `create_issue_label` | Create a new issue label. | `name` | `color`, `description`, `isGroup`, `parent`, `teamId` |
| `list_issue_statuses` | Inspect issue statuses for a team. | `team` | None |
| `get_issue_status` | Resolve one issue status by ID or name. | `id` and `name` and `team` | None |
| `list_cycles` | Inspect cycles for a team. | `teamId` | `type` |
| `list_milestones` | Inspect milestones for a project. | `project` | None |
| `get_milestone` | Resolve one milestone by project and name or ID. | `project`, `query` | None |
| `get_status_updates` | Read project or initiative status updates. | `type` | `createdAt`, `cursor`, `id`, `includeArchived`, `initiative`, `limit`, `orderBy`, `project`, `updatedAt`, `user` |
| `save_status_update` | Create or update a project or initiative status update. | `type` | `body`, `health`, `id`, `initiative`, `isDiffHidden`, `project` |

## Notes

- Use the team key or verified team UUID when a team-scoped read is picky.
- Use `list_*` tools for discovery, then switch to the matching `save_*` tool when you need to mutate.
- Project status update work belongs here because it is project/initiative metadata, not issue body content.
