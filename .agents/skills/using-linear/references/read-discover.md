# Read and discover

Use this when you want current Linear state without mutating anything.

## Pick the tool

| Tool | Use when | Required params | Optional params |
| --- | --- | --- | --- |
| `search` | Natural-language or keyword search across issues, projects, initiatives, and documents. | `query` | `includeArchived`, `limit`, `type` |
| `fetch` | Exact lookup by entity-prefixed ID or known identifier returned by search. | `id` | None |
| `list_issues` | Structured issue lookup with filters. | None | `assignee`, `createdAt`, `cursor`, `cycle`, `delegate`, `includeArchived`, `label`, `limit`, `orderBy`, `parentId`, `priority`, `project`, `query`, `state`, `team`, `updatedAt` |
| `get_issue` | Read one issue with attachments, relations, or release links. | `id` | `includeCustomerNeeds`, `includeRelations`, `includeReleases` |
| `list_projects` | Structured project lookup with filters. | None | `createdAt`, `cursor`, `includeArchived`, `includeMembers`, `includeMilestones`, `initiative`, `label`, `limit`, `member`, `orderBy`, `query`, `state`, `team`, `updatedAt` |
| `get_project` | Read one project, optionally with members, milestones, or resources. | `query` | `includeMembers`, `includeMilestones`, `includeResources` |
| `list_documents` | Find documents by workspace, team, project, or initiative. | None | `createdAt`, `creatorId`, `cursor`, `includeArchived`, `initiativeId`, `limit`, `orderBy`, `projectId`, `query`, `teamId`, `updatedAt` |
| `get_document` | Read one document by ID or slug. | `id` | None |
| `list_initiatives` | Structured initiative lookup with filters. | None | `createdAt`, `cursor`, `includeArchived`, `includeProjects`, `includeSubInitiatives`, `limit`, `orderBy`, `owner`, `parentInitiative`, `query`, `status`, `updatedAt` |
| `get_initiative` | Read one initiative, optionally with projects or sub-initiatives. | `query` | `includeProjects`, `includeSubInitiatives` |
| `list_comments` | Read comment threads or inline comments on an issue, project, initiative, document, or milestone. | Exactly one of `issueId`, `projectId`, `initiativeId`, `documentId`, `milestoneId` | `cursor`, `limit`, `orderBy` |
| `list_teams` | Find workspace teams. | None | `createdAt`, `cursor`, `includeArchived`, `limit`, `orderBy`, `query`, `updatedAt` |
| `get_team` | Resolve one team by key, UUID, or name. | `query` | None |
| `list_users` | Find users in the workspace. | None | `cursor`, `limit`, `orderBy`, `query`, `team` |
| `get_user` | Resolve one user by ID, name, email, or `me`. | `query` | None |

## When to choose search

Use `search` when the user gives you a phrase, a loose title, or a human description.
Use a list or get tool when you already know the filter shape and need reliable structure.

## Notes

- `list_*` tools are better when you need predictable filters or pagination.
- `fetch` is the bridge from `search` results to a single durable record.
- If you need the exact current object before a write, read it by the smallest stable filter you have and then read back from that durable surface after the mutation.
