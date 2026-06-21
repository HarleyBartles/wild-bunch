# Upstream OpenAI Linear skill note

This skill is adapted from the OpenAI curated Linear skill concept in `openai/skills`, `skills/.curated/linear`.

The upstream skill is intentionally small. Its core ideas are:

- use Linear MCP access for Linear issues, projects, and workflows;
- read first with search/get operations before creating or updating;
- identify project, team, assignee, and label IDs before writes;
- create or update Linear work items with exact titles, descriptions, labels, assignees, statuses, priorities, and due dates;
- summarize created/updated item identifiers and blockers;
- support workflows such as sprint planning, bug triage, documentation audit, workload balancing, release planning, dependency mapping, status updates, smart labeling, and retrospectives.

The upstream setup instructions are Codex-specific and should not be treated as ChatGPT setup instructions. In ChatGPT, use the available Linear connector namespace instead.

The upstream skill is Apache-2.0 licensed. Keep `LICENSE.txt` in this package.
