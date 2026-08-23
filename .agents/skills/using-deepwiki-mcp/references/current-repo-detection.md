# Detecting the current repo for DeepWiki

When the user does not name a repo, derive `owner/repo` from the current git checkout.

1. Run `git remote -v`.
2. Pick `origin` unless the question is about upstream policy, then use `upstream`.
3. Convert the URL to `owner/repo`:
   - HTTPS: `https://github.com/owner/repo.git` → `owner/repo`
   - SSH: `git@github.com:owner/repo.git` → `owner/repo`
4. If the remote cannot be parsed, or the checkout is not a git repo, ask the user for `owner/repo` explicitly.

Use this derived value as the default `repoName` for `ask_question`, `read_wiki_structure`, and `read_wiki_contents`.
