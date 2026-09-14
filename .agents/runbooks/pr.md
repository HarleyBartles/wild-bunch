# Publication proof and PR instructions

## When

Opening, updating, or publishing a Wild Bunch pull request.

## Required skills

- `/publishing-source`
- `/repo-worker-base`
- `/requesting-code-review` and `/receiving-code-review` when review occurs.
- `/verification-before-completion`

## Composition

Enter through `/using-superpowers-plus` and follow its publication handoff.
Portable skills own Draft lifecycle, commit discipline, review sequencing, and
publication; this runbook supplies Wild Bunch bindings.

## Doctrine and contracts

- Root [AGENTS.md](../../AGENTS.md) owns publication proof.
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
