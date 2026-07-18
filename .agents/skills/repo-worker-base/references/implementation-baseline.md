# Implementation baseline

## Read when

Read for repo-backed implementation, source-custody changes, evidence,
closeout, or publication before invoking the selected execution lane.

## Baseline

Work from the dedicated checkout described by the worktree policy, then read
the local implementation guide. Edit canonical authored source, not generated
projections, installed trees, or caches. Regenerate and verify downstream
surfaces only through their owners. Keep temporary artifacts in external
scratch custody and keep runtime behavior separate from authored guidance.

Run the planned validation, review the final scope and working tree, then
publish a focused branch through a PR unless direct-main work is explicitly
authorized. Evidence distinguishes commands run, results, skipped checks, and
their reason. Local success is not completion: closeout requires GitHub
publication proof and any required issue-control update.

## Validation and publication gate

Run the repository- and issue-specific validation. Record each command, result,
and skipped check with its reason. Generated output is valid only when its
owner's write path and check path agree. Before closeout, review the changed
files, final diff, and working-tree state.

If repository files changed, publish the exact final commit to the task branch
and expose it through a PR into the approved base unless direct-main work was
explicitly authorized. Verify the remote head SHA, PR state, mergeability, and
required checks. Local files, local validation, a worker report, or an
unpublished commit are not publication proof.

## GREEN gate

Return GREEN only when all relevant facts are true:

- source work is complete and generated surfaces are current;
- the branch was created from or updated onto current required main;
- the final full head SHA is recorded;
- the branch is pushed, or an exact publication blocker is recorded;
- the PR URL is verified when publication is available;
- required validation ran, with any skipped checks justified;
- the working tree is clean, or exact remaining dirty state is reported;
- no known mergeability or required-check blocker remains.

If any required fact is unresolved, return AMBER, RED, or BLOCKED as the
evidence warrants rather than laundering local success into GREEN.

## Required return evidence

Every repo-backed return includes:

- repository and issue or task identifier;
- worktree path and task branch;
- starting main SHA and final full head SHA;
- whether the branch was created from, rebased onto, or merged with current
  required main;
- PR URL, or the exact reason no PR exists;
- changed source and generated files;
- validation commands, results, and skipped-check reasons;
- remote CI and PR state when a PR exists;
- final working-tree state;
- GREEN, AMBER, RED, or BLOCKED judgment;
- remaining blockers, concerns, or follow-up.

## Stop signs

Stop and report instead of continuing when:

- current main cannot be fetched or the branch cannot be updated safely;
- the repository, issue, base, source owner, or mutation target is ambiguous;
- required validation cannot run and no accepted substitute exists;
- merge conflicts require human or product judgment;
- direct-main mutation lacks explicit current authorization;
- publication credentials, required secrets, or local-only resources are
  unavailable;
- remote state contradicts the local completion claim.
