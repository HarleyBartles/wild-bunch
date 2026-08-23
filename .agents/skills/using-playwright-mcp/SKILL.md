---
name: using-playwright-mcp
description: Use when working with the Playwright MCP server, choosing the right browser tool call, or falling back to non-MCP Playwright surfaces when the MCP does not cover the task.
metadata:
  source-id: using-playwright-mcp
  source-path: codex-marketplace/plugins/mcp-usage-pack/skills/using-playwright-mcp/SKILL.md
  provenance-name: Using Playwright MCP first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when working with the Playwright MCP server, choosing the right browser tool call, or falling back to non-MCP Playwright surfaces when the MCP does not cover the task.
  use_when:
  - Use when working with the Playwright MCP server, choosing the right browser tool call, or falling back to non-MCP Playwright surfaces when the MCP does not cover the task.
  do_not_use_when:
  - Do not use when another more specific skill owns the task.
license: MIT
---

# Using Playwright MCP

Use this skill to pick the right `mcp-playwright` tool for browser automation or web inspection, and to fall back safely to other Playwright surfaces when the MCP does not have what you need.

## Router

| Intent | Read first |
| --- | --- |
| Open a page, search for text, or capture a page snapshot | [`references/navigation-and-discovery.md`](references/navigation-and-discovery.md) |
| Click, type, hover, drag/drop, fill forms, select options, upload files, or handle dialogs | [`references/interactions.md`](references/interactions.md) |
| Evaluate JavaScript, read console, inspect network, or take screenshots | [`references/inspection.md`](references/inspection.md) |
| Manage tabs, resize, wait, or close the browser | [`references/tabs-and-lifecycle.md`](references/tabs-and-lifecycle.md) |
| The MCP does not cover the task; use another Playwright surface | [`references/other-playwright-tools.md`](references/other-playwright-tools.md) |
| Need the complete callable surface | [`references/surface-map.md`](references/surface-map.md) |

## Fast rules

1. **MCP first.** Start by routing to the use-case file that matches your intent.
2. **Confirm absence before exit.** Before opening `references/other-playwright-tools.md`, check `references/surface-map.md` or run `mcp_list_tools` for `mcp-playwright` to confirm the needed tool is not there.
3. **Environment check for fallbacks.** Before using a non-MCP Playwright tool, run `/inspecting-the-environment` to confirm it is installed and available.
