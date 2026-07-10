# Destructive actions

Use this when the task is specifically to archive or delete a status update.

## Tool

| Tool | Use when | Required params | Optional params |
| --- | --- | --- | --- |
| `delete_status_update` | Archive a project or initiative status update. | `id`, `type` | None |
| `delete_comment` | Remove a comment thread or comment reply. | `id` | None |
| `delete_attachment` | Remove an attachment. | `id` | None |
| `delete_customer` | Remove a customer record. | `id` | None |
| `delete_customer_need` | Remove a customer need. | `id` | None |

## Rule

Do not use this surface unless the user actually asked for the destructive action.
