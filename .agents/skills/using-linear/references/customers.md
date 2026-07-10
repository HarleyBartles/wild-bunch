# Customers

Use this when the task is about Linear customer records or customer needs.

## Tools

| Tool | Use when | Required params | Optional params |
| --- | --- | --- | --- |
| `list_customers` | Find customers in the workspace. | None | `createdAt`, `cursor`, `includeArchived`, `includeNeeds`, `limit`, `orderBy`, `owner`, `query`, `status`, `tier`, `updatedAt` |
| `save_customer` | Create or update a customer record. | Create: `name`. Update: `id`. | `domains`, `externalIds`, `owner`, `revenue`, `size`, `status`, `tier` |
| `save_customer_need` | Create or update a customer need. | Create: `body`. Update: `id`. | `customer`, `issue`, `priority`, `project` |
| `delete_customer` | Remove a customer record. | `id` | None |
| `delete_customer_need` | Remove a customer need. | `id` | None |

## Notes

- Use `save_customer` when you need the customer entity itself.
- Use `save_customer_need` when you need the need/request tied to a customer, issue, or project.
- Delete tools are archival/destructive surfaces and should only be used when the user explicitly asked for that outcome.
