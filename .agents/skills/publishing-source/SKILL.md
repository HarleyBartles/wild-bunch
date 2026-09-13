---
name: publishing-source
description: Use when completed source work needs a decision about whether to commit,
  push, tag, release, or export it.
metadata:
  source-id: publishing-source
  source-path: codex-marketplace/plugins/superpowers-plus/skills/publishing-source/SKILL.md
  provenance-name: Publishing Source first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  use_when:
  - source work is finished and you must decide whether to commit, tag, release, push source, or export a pack.
  - choosing between a direct-main commit, a PR, a tag/release, or a pack export for the current change.
  - publication proof is required and you must pick the right GitHub-visible surface.
  do_not_use_when:
  - the change is not yet validated; finish verification-before-completion first.
  - the task is GitHub mechanics (PR/branch/commit reads or writes) rather than the publication decision; use using-github-mcp.
  - the task is release pipeline or CI/CD operation rather than the source-publication decision; use release-engineering.
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

1. **Validated?** If the committed tree would not pass the pre-commit hook
   (materialize staged snapshot, `ci --apply`, stage owned generated surfaces,
   `ci --check --diagnostics`), stop and finish `verification-before-completion`
   first. Do not run `ci --check` immediately before a normal commit. Publication is not
   a substitute for validation.
2. **Marketplace source edited?** If the consumer's canonical marketplace source,
   inventory, provenance, or bundle manifest changed, regenerate with the
   consumer repository's canonical marketplace-generation command before
   publishing. Do not assume a particular repository layout or command name.
3. **Pick the surface.** Choose the smallest sufficient surface from
   [`references/publishing-decisions.md`](references/publishing-decisions.md).
4. **Publish.** Hand off to the owning skill for the mechanics
   (`using-github-mcp` for GitHub surfaces, `release-engineering` for release
   pipelines, `finishing-a-development-branch` for branch closeout).
5. **Record proof.** Capture the PR URL or direct-main commit SHA as the
   publication proof required by the repo root `AGENTS.md`.

## Canonical sequences

- **Direct-main commit (authorized only):** regenerate -> stage intended tree
  -> commit (pre-commit hook applies and checks) -> push -> record SHA.
- **PR (default):** regenerate -> stage intended tree -> commit (pre-commit hook
  applies and checks) -> branch -> push -> open a **Draft** PR -> record the PR
  URL. Keep it Draft during local review and repair; move it to
  Ready only when the current committed state has the required evidence and
  review. Do not ask a second permission question when the publication route
  was already authorized.
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
