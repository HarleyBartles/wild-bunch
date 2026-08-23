---
name: using-deepwiki-mcp
description: Use when you need high-level orientation, conventions, architecture, or cross-repo context for a GitHub repo and need to choose the right DeepWiki MCP tool and question phrasing.
metadata:
  source-id: using-deepwiki-mcp
  source-path: codex-marketplace/plugins/mcp-usage-pack/skills/using-deepwiki-mcp/SKILL.md
  provenance-name: Using DeepWiki MCP first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when you need high-level orientation, conventions, architecture, or cross-repo context for a GitHub repo and need to choose the right DeepWiki MCP tool and question phrasing.
  use_when:
  - Use when you need high-level orientation, conventions, architecture, or cross-repo context for a GitHub repo.
  - Use when you need to decide whether to ask a targeted question, list wiki topics, or read the full generated wiki.
  - Use when you want to compare or contrast up to 10 public repos.
  do_not_use_when:
  - Do not use when you need exact, current source or version-specific behavior.
  - Do not use when the repo is private, not indexed by DeepWiki, or the answer has safety/security implications without verification.
  - Do not use when another more specific skill owns the task.
license: MIT
---

# Using DeepWiki MCP

Use this skill to decide when and how to call the `deepwiki` MCP server for a GitHub repo, with good question phrasing, current-repo detection, multi-repo support, and safe verification.

## When to use

- You need a map of a repo's architecture, conventions, or release process.
- You want a high-level answer to "how do I...?" in the repo you are working in.
- You want to compare or contrast a few public repos.
- You are picking a repo to investigate.

## When not to use

- You need exact, current source or a guarantee of version-specific behavior.
- The repo is private or not indexed by DeepWiki.
- The answer has safety, security, or deployment implications — verify first.

## Current-repo flow

1. If the user does not name a repo, derive `owner/repo` from the current git remote. See [`references/current-repo-detection.md`](references/current-repo-detection.md).
2. Call `read_wiki_structure` to orient yourself on the available topics.
3. Call `ask_question` for targeted specifics, or `read_wiki_contents` if you genuinely need the full generated wiki.

## Tool selection

| Situation | Tool | Read first |
|---|---|---|
| "How do I...?" / "What is...?" / compare | `ask_question` | [`references/golden-questions.md`](references/golden-questions.md) |
| "What docs exist for this repo?" | `read_wiki_structure` | [`references/surface-map.md`](references/surface-map.md) |
| "I want the whole generated wiki" | `read_wiki_contents` | [`references/surface-map.md`](references/surface-map.md) |
| Need the complete callable surface | — | [`references/surface-map.md`](references/surface-map.md) |

## Multi-repo questions

For up to 10 repos, pass `repoName` as an array of `owner/repo` strings to `ask_question`. See [`references/multi-repo.md`](references/multi-repo.md).

## Trust and verification

DeepWiki content is AI-generated from public source. `ask_question` returns a `result`, suggested wiki pages, and a DeepWiki search URL. Use them as starting points, then verify critical facts with the live repo. See [`references/verifying-against-source.md`](references/verifying-against-source.md).
