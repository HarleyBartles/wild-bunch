# Verifying DeepWiki answers

DeepWiki answers are AI-generated from public source. They can be stale, incomplete, or overconfident. Verify before acting on critical information.

1. Check the suggested wiki pages and the DeepWiki search URL returned by `ask_question`.
2. For code-level facts, open the actual file with `using-github-mcp`, `webfetch`, or a local file read.
3. For version or dependency facts, read `package.json`, `pyproject.toml`, or the equivalent in the actual repo.
4. For security or deployment decisions, confirm with live source or ask your human partner.

Do not treat DeepWiki output as ground truth for exact current behavior.
