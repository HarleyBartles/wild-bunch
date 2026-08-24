---
name: using-github-mcp
description: Use when choosing the right GitHub or Git surface for a task, picking
  between the GitHub MCP server, gh CLI, REST API, GraphQL, or plain git commands.
metadata:
  source-id: using-github-mcp
  source-path: codex-marketplace/plugins/mcp-usage-pack/skills/using-github-mcp/SKILL.md
  provenance-name: Using GitHub MCP first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when choosing the right GitHub or Git surface for a task, picking between
    the GitHub MCP server, gh CLI, REST API, GraphQL, or plain git commands.
  use_when:
  - Use when choosing the right GitHub or Git surface for a task, picking between
    the GitHub MCP server, gh CLI, REST API, GraphQL, or plain git commands.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---

# Using GitHub MCP

Use this skill to pick the right GitHub or Git surface from the task intent, then open the matching reference.

## Router

| Intent | Read first |
| --- | --- |
| Search, list, or read repositories, files, commits, branches, tags, or releases | [`references/read-discover.md`](references/read-discover.md) |
| Create, update, merge, or review pull requests | [`references/pull-requests.md`](references/pull-requests.md) |
| Read or write PR reviews, review threads, and inline review comments | [`references/reviews.md`](references/reviews.md) |
| Read or write issues and issue/PR timeline comments | [`references/issues-comments.md`](references/issues-comments.md) |
| Work with commits, branches, tags, or low-level git refs | [`references/commits-branches.md`](references/commits-branches.md) |
| Create, update, or delete files, repositories, labels, or other mutations | [`references/mutations.md`](references/mutations.md) |
| Run a GitHub GraphQL query or mutation | [`references/graphql.md`](references/graphql.md) |
| Use the `gh` command-line interface | [`references/gh-cli.md`](references/gh-cli.md) |
| Pick the right GitHub MCP tool | [`references/mcp-surface.md`](references/mcp-surface.md) |
| Need the complete callable surface | [`references/surface-map.md`](references/surface-map.md) |

## Fast rule

If you need exact current repository state, prefer `gh api` or `gh api graphql`. If the intent is still unclear after the first pass, open `references/surface-map.md` and return to the use-case file that matches the object you are touching.

Before changing a PR's draft state (opening, flipping to ready, or reopening), consult `.agents/runbooks/pr.md` `## Draft PR policy` for the repo-specific and consumer-canonical rules.
