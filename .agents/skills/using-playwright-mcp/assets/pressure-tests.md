# Pressure tests — `using-playwright-mcp`

These scenarios demonstrate the value of the skill when the raw `mcp_list_tools` output is truncated or when an agent must resolve the right tool quickly.

## Scenario 1: Wait for text to disappear after an action

Task context:

> The page shows a "Saving..." label. After clicking a save button, you need to wait until the label disappears before proceeding.

Raw `mcp_list_tools` output for `mcp-playwright` is large and may truncate before the `browser_wait_for` tool. An agent that only sees the truncated list must infer the right tool.

### RED — without the skill

- The agent reads the truncated `mcp_list_tools` output and does not see `browser_wait_for`.
- It concludes that no explicit wait tool exists and either:
  - uses `browser_evaluate` with a `setTimeout`/`setInterval` loop, adding unnecessary JavaScript and failure modes, or
  - repeatedly calls `browser_find` to poll, wasting context and increasing token cost and latency.

### GREEN — with the skill

- The agent reads `using-playwright-mcp` and is routed to `references/tabs-and-lifecycle.md` for waiting.
- It finds `browser_wait_for` in `references/surface-map.md`.
- It calls `browser_wait_for` with `textGone: "Saving..."` and a `time` value.

Proof value: `browser_wait_for` is discovered mechanically, accurately, and safely, without workaround code or expensive polling.
