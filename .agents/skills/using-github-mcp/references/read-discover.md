# Read and discover

Use this when you want current GitHub repository state without mutating anything.

## Pick the surface

| Surface | Use when | Required | Optional/notes |
| --- | --- | --- | --- |
| `gh repo view <repo>` | Read repository metadata, description, and default branch. | `repo` | `--json` for structured fields. |
| `gh search repos <query>` | Search for repositories by phrase. | `query` | `--owner`, `--language`, `--limit`, `--sort`. |
| `gh search code <query>` | Search code across repositories. | `query` | `--repo`, `--language`, `--limit`. |
| `gh pr view <number>` | Read a pull request. | `number` | `--json` for `headRefOid`, `url`, `state`, `mergeable`. |
| `gh pr view <number> --comments` | Read PR timeline comments. | `number` | Not review threads. |
| `gh issue view <number>` | Read an issue. | `number` | `--json` for structured fields. |
| `gh api repos/{owner}/{repo}/pulls/comments` | Read all review comments on a PR. | `owner`, `repo`, `pull_number` | Returns `body`, `id`, `path`, `line`, `start_line`, `side`. |
| `gh api repos/{owner}/{repo}/contents/{path}` | Read file contents from a ref. | `owner`, `repo`, `path` | `ref` defaults to default branch. |
| `gh api repos/{owner}/{repo}/git/trees/{ref}` | Read the git tree of a ref. | `owner`, `repo`, `ref` | `recursive=1` for nested trees. |
| `get_file_contents` (MCP) | Read a file from the MCP server. | `owner`, `repo`, `path` | `ref` optional. |
| `list_commits` (MCP) | List commits through the MCP server. | `owner`, `repo` | `sha`, `path`, `limit`. |
| `search_repositories` (MCP) | Search repos through the MCP server. | `query` | `sort`, `order`, `limit`. |
| `search_code` (MCP) | Search code through the MCP server. | `query` | `owner`, `repo`, `language`, `limit`. |

## Notes

- `gh pr view --comments` returns timeline comments, not review threads. Use `gh api repos/{owner}/{repo}/pulls/comments` or GraphQL for review threads.
- For exact current state of a file or ref, `gh api` is usually faster and less ambiguous than MCP tool discovery.
- Use `list_*` MCP tools when you need predictable filters or pagination; use `get_*` tools when you already have an identifier.
