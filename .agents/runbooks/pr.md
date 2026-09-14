# PR instructions

Use this local overlay after `using-superpowers-plus` hands publication to
`publishing-source`.

## Wild Bunch publication settings

- Base branch: `main`
- Default PR state: Draft
- Draft-aware CI: pull-request jobs run only when
  `github.event.pull_request.draft == false`
- Direct pushes to `main` require explicit authorization

## Publication proof

A GitHub pull request from a dedicated linked worktree and task branch is the
repository's publication proof.

## Local commands

- Apply mechanical outputs: `py -3 tools/run.py ci --apply`
- Full fail-fast check: `py -3 tools/run.py ci --check`
- Aggregate diagnostics: `py -3 tools/run.py ci --check --diagnostics`
