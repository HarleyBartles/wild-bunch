# Publication proof and PR instructions

## When

Opening, updating, or publishing a Wild Bunch pull request.

## Required capabilities

- Verify repository state, validation evidence, and publication prerequisites.
- Publish a branch and open or update a Draft PR, then verify its head and checks.

## Optional capabilities

- None.

## Required repository-owned skills

- None.

## Optional repository-owned skills

- None.

## Unslop before work

Before authoring the PR, read [writing](../unslop/writing.md) and [code-review guards](../unslop/code-review.md) in full; follow their direct links for changed concerns. Confirm the current diff has been reviewed against those guards and update [distinct observations](../unslop/README.md#record-and-improve) when there is new evidence, without adding read or test receipts.

## Composition

For the stable 0.1.0 epic, read the [roadmap's develop integration and per-plan delivery contract](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md#develop-integration-and-per-plan-delivery) before publication. Its explicit user-selected develop base overrides the main defaults below; carry fresh per-plan worktrees, successor-artifact retirement and development-version advancement into the PR. Use the base specified by the active approved work rather than assuming main from this runbook.

1. Use repository branch, source, validation, and publication guidance to confirm dedicated-worktree, branch, clean-tree,
   and source-custody state.
2. Use evidence-based result verification to bind the publication claim to the
   committed head and canonical validation result.
3. Use GitHub branch and Draft PR publication to push the task branch and create or update the
   Draft PR against `main`.
4. Read back the GitHub PR and reconcile its head SHA, body, Draft state, and
   applicable hosted checks with the published tree.

## Doctrine and contracts

- This runbook is the publication-proof surface routed from root
  [AGENTS.md](../../AGENTS.md).
- [Repository command declaration](../contracts/repo-standards-commands.json)
  defines the canonical apply and check capabilities; pre-commit and hosted
  validation invoke only check.

## Local commands and paths

- Base branch: `main`
- Default PR state: Draft
- Apply: `py -3 tools/run.py ci --apply`
- Check: `py -3 tools/run.py ci --check`
- Diagnostics: `py -3 tools/run.py ci --check --diagnostics`
- Run apply explicitly before staging when repository-owned metadata needs
  refreshing; review and stage those changes deliberately.
- The commit hook validates the staged candidate without mutating or staging it.
- Pull-request jobs run when the PR is not Draft.
- Direct pushes to `main` require explicit authorization.

## Evidence contract

- [ ] The local tree is clean and the branch is published from a dedicated worktree.
- [ ] The GitHub PR targets `main` and its remote head equals local `HEAD`.
- [ ] The PR body describes current scope and validation evidence.
- [ ] Draft state and hosted-check expectations match the repo policy.

## Prohibited combinations

- Do not treat a local branch or push without a PR as publication proof.

## Playbook routing

- [Completing plans](../playbooks/completing-plans.md) - before handoff when this slice's plans, specifications, roadmaps, checkpoints, audits, or trackers have completed.
- [Testing](../playbooks/testing.md) - when establishing the committed validation evidence used by the publication claim.
