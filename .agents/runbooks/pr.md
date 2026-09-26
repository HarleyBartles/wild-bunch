# Publication proof and PR instructions

## When

Opening, updating, or publishing a Wild Bunch pull request.

## Required skills

- `/publishing-source`
- `/repo-worker-base`
- `/verification-before-completion`

## Composition

1. Use `/repo-worker-base` to confirm dedicated-worktree, branch, clean-tree,
   and source-custody state.
2. Use `/verification-before-completion` to bind the publication claim to the
   committed head and canonical validation result.
3. Use `/publishing-source` to push the task branch and create or update the
   Draft PR against `main`.
4. Read back the GitHub PR and reconcile its head SHA, body, Draft state, and
   applicable hosted checks with the published tree.

## Doctrine and contracts

- This runbook is the publication-proof surface routed from root
  [AGENTS.md](../../AGENTS.md).
- [Repository command declaration](../contracts/repo-standards-commands.json)
  defines the canonical apply and check capabilities.

## Local commands and paths

- Base branch: `main`
- Default PR state: Draft
- Apply: `py -3 tools/run.py ci --apply`
- Check: `py -3 tools/run.py ci --check`
- Diagnostics: `py -3 tools/run.py ci --check --diagnostics`
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
