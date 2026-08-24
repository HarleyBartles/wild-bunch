# Diff reviews

Use this when the task is about Linear diffs, review threads, or GitHub PR-linked diff state.

## Tools

| Tool | Use when | Required params | Optional params |
| --- | --- | --- | --- |
| `list_diffs` | Find diffs by PR number, slug, repo, owner, title, or status. | None | `cursor`, `limit`, `orderBy`, `owner`, `query`, `repo`, `status` |
| `get_diff` | Read one diff by GitHub PR URL, Linear review URL, PR ID, slug, or known identifier. | `urlOrId` | None |
| `get_diff_threads` | Read the review threads for one diff. | `urlOrId` | `orderBy`, `resolved`, `threadId` |

## Notes

- Use `get_diff` when you already have the PR or review identifier and want the exact record.
- Use `get_diff_threads` when you need resolved state or per-thread review context.
- Keep diff review work separate from issue shaping unless the task explicitly ties them together.
