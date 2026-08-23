# Surface map — DeepWiki MCP

| Tool | Use when | Required inputs | Optional inputs | Notes |
| --- | --- | --- | --- | --- |
| `ask_question` | Ask a natural-language question about one or more repos | `question`, `repoName` | — | `repoName` can be a string or an array of up to 10 `owner/repo` strings. |
| `read_wiki_structure` | List available wiki topics for a repo | `repoName` | — | One `owner/repo` only. |
| `read_wiki_contents` | Read the full generated wiki for a repo | `repoName` | — | Output can be very large; prefer `ask_question` or `read_wiki_structure` first. |

Always check the live `mcp_list_tools` output for the authoritative list and schemas; this map is a routing guide, not a schema dump.
