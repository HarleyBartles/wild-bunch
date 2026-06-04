# BUNCH-24 Cloud Codex GitHub Publication Probe

This docs-only note exists to prove that a Linear-delegated Cloud Codex session can produce a reviewable publication artifact for `HarleyBartles/wild-bunch` without mutating game, domain, runtime, API, persistence, or UI behavior.

## Probe scope

- Issue: `BUNCH-24`.
- Branch: `codex/bunch-24-github-publication`.
- Change type: documentation-only publication proof.
- Product behavior impact: none.

## Observations from the shell lane

The shell checkout started on branch `work` with no configured remote. Adding a read-only `origin` URL for `https://github.com/HarleyBartles/wild-bunch.git` was possible, but unauthenticated shell GitHub access could not fetch private repository refs from the environment.

GitHub publication for this proof therefore relies on the Codex-native pull-request finalization path rather than printing or injecting GitHub credentials into the shell.
