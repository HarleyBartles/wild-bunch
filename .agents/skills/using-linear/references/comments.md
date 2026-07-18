# Comments and discussion

Use this when you want discussion threads, issue comments, or inline comment readback.

## Tools

| Tool | Use when | Required params | Optional params |
| --- | --- | --- | --- |
| `list_comments` | Read comments on an issue, project, initiative, document, or milestone. | Exactly one of `issueId`, `projectId`, `initiativeId`, `documentId`, `milestoneId` | `cursor`, `limit`, `orderBy` |
| `save_comment` | Create a new thread, reply to an existing thread, or update a comment. | `body`, plus exactly one parent for new threads, or `parentId` for replies, or `id` for updates | `documentId`, `initiativeId`, `issueId`, `milestoneId`, `parentId`, `projectId` |

## Thread shape

- Issues, projects, and initiatives create top-level discussion threads.
- Documents and milestones create description comments.
- Replies inherit the parent thread type; pass `parentId` and `body`.
- Anchored comments may return `quotedText` when the comment is tied to description text.
