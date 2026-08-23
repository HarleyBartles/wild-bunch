# Inspection

Use these `mcp-playwright` tools to examine page state, JavaScript, console logs, network, and visuals.

| Tool | When to use it | Required inputs | Optional inputs |
| --- | --- | --- | --- |
| `browser_evaluate` | Run JavaScript and get a result | `function` | `element`, `target`, `filename` |
| `browser_console_messages` | Read browser console output | `level` | `all`, `filename` |
| `browser_network_requests` | List network requests/responses | — | `method`, `url`, `filename` |
| `browser_network_request` | Get one specific network request | `id` | `filename` |
| `browser_take_screenshot` | Capture a visual record | — | `filename`, `fullPage` |

## Fast rules

1. **Use `browser_evaluate` for custom assertions.** It is the escape hatch when the accessibility snapshot is not enough.
2. **Use `level: error` for console checks first.** Escalate to `info` only when needed.
3. **Screenshots are for evidence, not primary verification.** Prefer text-based tools for deterministic checks.
