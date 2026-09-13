# PR instructions

Use this local overlay after `using-superpowers-plus` hands publication to
`publishing-source`.

## Wild Bunch publication settings

- Base branch: `main`
- Publication proof: a GitHub pull request from a dedicated linked worktree and
  task branch
- Default PR state: Draft
- Draft-aware CI: pull-request jobs run only when
  `github.event.pull_request.draft == false`
- Direct pushes to `main` require explicit authorization

## Local commands

- Apply mechanical outputs: `py -3 tools/run.py ci --apply`
- Full fail-fast check: `py -3 tools/run.py ci --check`
- Aggregate diagnostics: `py -3 tools/run.py ci --check --diagnostics`

For a normal commit, stage the intended tree and use the pre-commit hook as the
single complete local gate. Keep the PR Draft while local review and repair are
in progress; promote it only after current committed proof and review are green.
