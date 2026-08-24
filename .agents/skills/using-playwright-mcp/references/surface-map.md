# Surface map — Playwright MCP

| Tool | Use when | Required inputs | Optional inputs | Notes |
| --- | --- | --- | --- | --- |
| `browser_navigate` | Open or load a URL | `url` | — | Use as the first step for any new page. |
| `browser_navigate_back` | Go back in history | — | — | — |
| `browser_resize` | Resize the viewport | `width`, `height` | — | — |
| `browser_find` | Search the accessibility snapshot for text/regex | `text` or `regex` | — | Use to locate an element or verify content without a full snapshot. |
| `browser_snapshot` | Capture the full page accessibility tree | — | `filename` | Useful before and after interactions. |
| `browser_click` | Click an element | `target` | `element` | Use `element` for human-readable permission. |
| `browser_hover` | Hover over an element | `target` | `element` | — |
| `browser_type` | Type into an input | `target`, `text` | `element` | — |
| `browser_press_key` | Press a keyboard key | `key` | `target`, `element` | — |
| `browser_select_option` | Select from a combobox/radio group | `target`, `value` | `element` | — |
| `browser_fill_form` | Fill multiple form fields at once | `fields` | — | Use when several fields must be filled together. |
| `browser_file_upload` | Upload file(s) to a file input | `paths` | — | Omit `paths` to cancel the chooser. |
| `browser_drop` | Drop file(s) or MIME data onto an element | `target` | `paths`, `data`, `element` | At least one of `paths` or `data` must be provided. |
| `browser_handle_dialog` | Accept or dismiss a dialog | `accept` | `promptText` | `promptText` is required when accepting a prompt. |
| `browser_evaluate` | Run JavaScript on the page or an element | `function` | `element`, `target`, `filename` | Use for assertions or state that snapshots do not expose. |
| `browser_console_messages` | Read console messages | `level` | `all`, `filename` | `level` may be `error`, `warning`, `info`, `debug`. |
| `browser_network_requests` | Inspect network requests/responses | — | `method`, `url`, `filename` | — |
| `browser_network_request` | Inspect a specific network request | `id` | `filename` | — |
| `browser_take_screenshot` | Capture a screenshot | — | `filename`, `fullPage` | — |
| `browser_tabs` | List or switch browser tabs | — | `action`, `tabId` | — |
| `browser_wait_for` | Wait for an event, text, or timeout | `time` | `selector`, `text`, `textGone` | Provide exactly one of `selector`, `text`, or `textGone` along with `time`. |
| `browser_close` | Close the browser/page | — | — | Destructive; run when done. |

Always check the live `mcp_list_tools` output for the authoritative list and schemas; this map is a routing guide, not a schema dump.
