# Publication proof and PR instructions

## When

Opening, updating, or publishing a Wild Bunch pull request.

## Required skills

- `/publishing-source`
- `/repo-worker-base`
- `/verification-before-completion`

## Composition

`/repo-worker-base` supplies worktree and source-custody boundaries,
`/verification-before-completion` gates the local head, and
`/publishing-source` owns Draft lifecycle and publication.

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

A GitHub pull request from a dedicated linked worktree and task branch, with
remote head, PR body, state, and hosted checks reconciled to the published tree.

## Prohibited combinations

- Do not treat a local branch or push without a PR as publication proof.
