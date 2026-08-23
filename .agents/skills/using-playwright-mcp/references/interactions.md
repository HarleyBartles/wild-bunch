# Interactions

Use these `mcp-playwright` tools to click, type, fill, upload, drag/drop, and handle dialogs.

| Tool | When to use it | Required inputs | Optional inputs |
| --- | --- | --- | --- |
| `browser_click` | Click a control | `target` | `element` |
| `browser_hover` | Hover to reveal hover state | `target` | `element` |
| `browser_type` | Type text into a field | `target`, `text` | `element` |
| `browser_press_key` | Press a single key | `key` | `target`, `element` |
| `browser_select_option` | Select an option in a combobox/radio | `target`, `value` | `element` |
| `browser_fill_form` | Fill many fields together | `fields` | — |
| `browser_file_upload` | Upload file(s) to a file input | `paths` | — |
| `browser_drop` | Drop files or data onto an element | `target` | `paths`, `data`, `element` |
| `browser_handle_dialog` | Accept/dismiss a dialog | `accept` | `promptText` |

## Fast rules

1. **Get a ref or selector first.** Use `browser_find` or `browser_snapshot` to obtain `target`.
2. **Prefer `browser_fill_form` for multi-field forms.** It is more stable than many separate `browser_type` calls.
3. **Ask before destructive dialogs.** If `accept: true` and the dialog is a confirmation, treat it as destructive; confirm with the human partner when in doubt.
