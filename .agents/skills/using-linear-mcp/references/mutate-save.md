# Create and update

This is the main save taxonomy. If you are searching for a write tool, search `save_*` first.

## Core save tools

| Tool | Use when | Required params | Optional params |
| --- | --- | --- | --- |
| `save_issue` | Create or update an issue. | Create: `title`, `team`. Update: `id`. | `assignee`, `blockedBy`, `blocks`, `cycle`, `delegate`, `description`, `dueDate`, `duplicateOf`, `estimate`, `labels`, `links`, `milestone`, `parentId`, `priority`, `project`, `relatedTo`, `removeBlockedBy`, `removeBlocks`, `removeRelatedTo`, `state` |
| `save_project` | Create or update a project. | Create: `name` and at least one team via `addTeams` or `setTeams`. Update: `id`. | `addInitiatives`, `addTeams`, `color`, `description`, `icon`, `labels`, `lead`, `priority`, `removeInitiatives`, `removeTeams`, `setInitiatives`, `setTeams`, `startDate`, `startDateResolution`, `state`, `summary`, `targetDate`, `targetDateResolution` |
| `save_document` | Create or update a document. | Create: `title` and exactly one parent from `project`, `issue`, `initiative`, `cycle`, or `team`. Update: `id`. | `color`, `content`, `cycle`, `icon`, `initiative`, `issue`, `project`, `team` |
| `save_initiative` | Create or update an initiative. | Create: `name`. Update: `id`. | `color`, `description`, `icon`, `owner`, `parentInitiatives`, `status`, `summary`, `targetDate` |
| `save_milestone` | Create or update a project milestone. | Create: `name` and `project`. Update: `id`. | `description`, `targetDate` |

## Rules that matter

- Use `assignee`, not `assigneeId`, on issue writes.
- `save_issue` is append-only for issue relations: blocker and related-link removals use the explicit removal fields.
- `save_document` reparents when you pass a parent on update.
- `save_project` can replace team or initiative membership when you use the `set*` fields.
- `save_status_update` belongs in the metadata lane because it is project or initiative status work, not issue content.

## Label creation exception

`create_issue_label` is create-only, not `save_*`.
Use it when you need a new issue label.
