# gh CLI

Use this when you need a fast, authenticated command-line path to GitHub.

## Authentication

- `gh auth status` — Check whether `GH_TOKEN`/`GITHUB_TOKEN` or interactive login is active.
- `gh auth token` — Print the token for the active account (do not log it).
- `gh auth login` / `gh auth logout` — Manage sessions.

## Common patterns

| Intent | Command |
| --- | --- |
| Current repo metadata | `gh repo view --json name,owner,defaultBranchRef,url` |
| PR head SHA | `gh pr view <number> --json headRefOid,url,state,mergeable` |
| PR diff | `gh pr diff <number>` |
| PR checks | `gh pr checks <number>` |
| List open PRs | `gh pr list --state open --limit 50` |
| API call | `gh api <endpoint>` |
| GraphQL call | `gh api graphql --input query.json` |
| Workflow runs | `gh run list --limit 20` |
| Workflow logs | `gh run view <run-id> --log` |
| Release list | `gh release list --limit 20` |

## Notes

- `gh` reads `GH_TOKEN` and `GITHUB_TOKEN` for non-interactive use.
- `gh api` accepts `repos/{owner}/{repo}/...` endpoints relative to `https://api.github.com`.
- For complex queries with nested objects, prefer `gh api graphql --input file.json` over `-f` flags.
