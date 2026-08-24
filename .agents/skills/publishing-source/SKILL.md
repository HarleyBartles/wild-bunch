---
name: publishing-source
description: Use when deciding how to publish source work in this repo - whether to commit, tag, release, push source, or export a pack - and which publication sequence fits the change.
metadata:
  source-id: publishing-source
  source-path: codex-marketplace/plugins/superpowers-plus/skills/publishing-source/SKILL.md
  provenance-name: Publishing Source first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when deciding how to publish source work in this repo - whether to commit, tag, release, push source, or export a pack - and which publication sequence fits the change.
  use_when:
  - Use when source work is finished and you must decide whether to commit, tag, release, push source, or export a pack.
  - Use when choosing between a direct-main commit, a PR, a tag/release, or a pack export for the current change.
  - Use when publication proof is required and you must pick the right GitHub-visible surface.
  do_not_use_when:
  - Do not use when the change is not yet validated; finish verification-before-completion first.
  - Do not use when the task is GitHub mechanics (PR/branch/commit reads or writes) rather than the publication decision; use using-github-mcp.
  - Do not use when the task is release pipeline or CI/CD operation rather than the source-publication decision; use release-engineering.
  use_instead:
  - using-github-mcp
  - release-engineering
  - verification-before-completion
  related_skills:
  - verification-before-completion
  - finishing-a-development-branch
  - using-github-mcp
  - release-engineering
  - repo-worker-base
license: MIT
---

# Publishing Source

Owns the source-publication decision tree for this repo: pick the smallest
sufficient publication surface for the change, then hand off to the skill that
performs the mechanics.

## Decision checklist

Run these in order. Stop at the first row that matches the change.

1. **Validated?** If `tools/run ci --check` is not green on the staged tree,
   stop and finish `verification-before-completion` first. Publication is not
   a substitute for validation.
2. **Marketplace source edited?** If `codex-marketplace/plugins/<plugin>/` skill content,
   `codex-marketplace/plugin-roots.json`, `codex-marketplace/plugins/<plugin>/SOURCE.md`, or `references/bundle-manifest.json` changed,
   regenerate with `py -3 tools/run.py marketplace --apply` before publishing.
3. **Pick the surface.** Choose the smallest sufficient surface from
   [`references/publishing-decisions.md`](references/publishing-decisions.md).
4. **Publish.** Hand off to the owning skill for the mechanics
   (`using-github-mcp` for GitHub surfaces, `release-engineering` for release
   pipelines, `finishing-a-development-branch` for branch closeout).
5. **Record proof.** Capture the PR URL or direct-main commit SHA as the
   publication proof required by the repo root `AGENTS.md`.

## Canonical sequences

- **Direct-main commit (authorized only):** validate -> stage -> `ci --check`
  -> commit -> push -> record SHA.
- **PR (default):** validate -> stage -> `ci --check` -> branch -> push ->
  open PR -> record PR URL and head SHA.
- **Tag/release:** finish the source change and merge -> tag the merged commit
  -> publish release notes -> record tag URL.
- **Pack export:** regenerate marketplace -> validate -> export the pack
  archive -> record the export artifact and the source commit it was built
  from.

## Common mistakes

- Treating a local commit hash as publication proof. Local state is not repo
  completion.
- Publishing before `ci --check` is green on the staged tree.
- Editing generated plugin surfaces by hand instead of regenerating from the
  registry.
- Skipping publication proof in the worker return.
