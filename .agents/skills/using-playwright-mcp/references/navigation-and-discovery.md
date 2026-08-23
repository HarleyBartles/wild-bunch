# Navigation and discovery

Use these `mcp-playwright` tools to load, move, and search the current page.

| Tool | When to use it | Required inputs | Optional inputs |
| --- | --- | --- | --- |
| `browser_navigate` | Load a new page | `url` | — |
| `browser_navigate_back` | Return to the previous page | — | — |
| `browser_find` | Locate text or a pattern without a full snapshot | `text` or `regex` | — |
| `browser_snapshot` | Get the accessibility tree to understand structure | — | `filename` |

## Fast rules

1. **Always `browser_navigate` first.** Most tools assume a loaded page.
2. **Use `browser_find` before `browser_snapshot`.** It is cheaper when you only need a ref.
3. **Ref values come from `browser_snapshot` or `browser_find`.** Pass the exact `ref` as `target` for click/type actions.
