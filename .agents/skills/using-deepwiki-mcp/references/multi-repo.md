# Multi-repo questions with DeepWiki

`ask_question` accepts `repoName` as a single string or an array of up to 10 `owner/repo` strings.

Use an array when:
- Comparing two or more public repos.
- Asking how a pattern in one repo maps to another.
- "How do repo A and repo B handle side effects?"

Keep the question focused on a single theme. Do not use more than 10 repos.

The response will synthesize across the repos and may cite specific source files or wiki topics for each.
