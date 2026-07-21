# Commits, branches, and tags

Use this when working with git history, refs, branches, tags, or trees.

## Pick the surface

| Surface | Use when | Required | Optional/notes |
| --- | --- | --- | --- |
| `git log` | Local commit history. | None | `--oneline`, `--graph`, `--all`, `<branch>`. |
| `git show <ref>` | Full commit or object details. | `ref` | `--stat`, `--name-only`. |
| `git branch` | List or manage local branches. | None | `-a`, `-r`, `-vv`. |
| `git tag` | List tags. | None | `-l`, `--sort=-creatordate`. |
| `git remote` | List remotes. | None | `-v`. |
| `git fetch origin` | Fetch remote refs. | remote name | `--prune`. |
| `git push origin <branch>` | Push a branch. | `branch` | `--force-with-lease` instead of `--force` when rewriting. |
| `gh api repos/{owner}/{repo}/commits` | List commits through REST. | `owner`, `repo` | `sha`, `path`, `per_page`. |
| `gh api repos/{owner}/{repo}/branches` | List branches through REST. | `owner`, `repo` | `per_page`. |
| `gh api repos/{owner}/{repo}/git/trees/{ref}` | Read a git tree. | `owner`, `repo`, `ref` | `recursive=1`. |
| `list_commits` (MCP) | List commits through MCP. | `owner`, `repo` | `sha`, `path`, `limit`. |
| `list_branches` (MCP) | List branches through MCP. | `owner`, `repo` | `limit`. |
| `list_tags` (MCP) | List tags through MCP. | `owner`, `repo` | `limit`. |
| `get_commit` (MCP) | Read a single commit. | `owner`, `repo`, `sha` | None. |
| `get_tag` (MCP) | Read a single tag. | `owner`, `repo`, `tag` | None. |
| `get_repository_tree` (MCP) | Read a git tree. | `owner`, `repo`, `tree_sha` | `recursive`. |

## Notes

- Use `git` for local history and `gh api` or MCP for remote/GitHub-hosted state.
- `git push` without force is sufficient for new commits on top of an already-pushed head. Use force only when history was rewritten (rebase, amend).
- `gh api repos/{owner}/{repo}/git/trees/{ref}?recursive=1` is the fastest way to enumerate a full tree without cloning.
