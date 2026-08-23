# Other Playwright tools (non-MCP)

Use this reference when the Playwright MCP surface does not cover the task.

## When to fall back

1. The MCP `surface-map.md` does not list a tool that fits the intent.
2. The task needs Playwright API patterns that only the Node/Python library supports.
3. The human partner explicitly asked for a standalone Playwright script.

## Procedure

1. Confirm absence with `mcp_list_tools` for `mcp-playwright`.
2. Run `/inspecting-the-environment` to confirm Playwright is installed.
3. Choose the right non-MCP surface:
   - **Browser preview** (`browser_preview`) for live interaction.
   - **`py -3 -m playwright` / `npx playwright`** for CLI-only tasks.
   - **A temporary Python/Node script** for complex flows.
4. Keep the script near the task; clean it up after use.
