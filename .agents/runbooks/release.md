# Release and hotfix lifecycle

## When

Preparing, qualifying, promoting, or reconciling a Wild Bunch product release
or hotfix.

## Before a release

Read [ADR-0048](../../docs/decisions/ADR-0048-gitflow-and-semver-release-lifecycle.md),
the [Gitflow and release-identity contract](../specs/2026-10-06-cloud-playability-and-releases.md#gitflow-and-release-identity),
and the [decision-record playbook](../playbooks/decision-records.md). Verify the
current remote refs, default branch, allowed merge methods, required checks,
and protection rules. Documentation and branch names do not prove that hosting
enforces a policy. If the product, compatibility policy, or release scope has
changed, consult ADR-0048 and decide whether a successor decision is needed.

## Release procedure

For a local source-bound bundle, first require the canonical CI gate to pass on
the exact clean source commit, then run `py -3 tools/run.py release-verify
--check`. The command builds the API publish output and both public and
playtest web variants under the ignored `artifacts/release/<full-source-commit>`
directory. Its manifest records `Directory.Build.props` version, full source
commit, and per-tree file counts and SHA-256 digests. Re-running verifies the
existing immutable bytes and does not rebuild or overwrite them. This is local
artifact identity evidence only; it is not hosted publication, candidate
qualification, or deployment proof.

1. Start ordinary feature branches from current `develop` and return them by
   PR to `develop`. Reconcile the proposed development checkpoint against the
   latest integration head before merge; one merged PR establishes one unique
   `MAJOR.MINOR.PATCH-dev.N` checkpoint, regardless of its commit count.
2. Start `release/*` from `develop` for release stabilization. Select its core
   version from the current compatibility policy and intended release scope;
   set the first qualified candidate to `MAJOR.MINOR.PATCH-rc.1` in the one
   authored version source, `Directory.Build.props`.
3. Qualify the candidate from its exact source tree and record its version,
   source identity, build variant, and artifact digests. If candidate source
   changes, advance to the next `rc.N` and repeat qualification. Re-running
   checks on unchanged source does not consume a new candidate number.
4. Promote only a reviewed, qualified candidate to `main` with a merge commit.
   The promoted tree must be the qualified tree. A content or version change
   during promotion requires a new candidate and renewed qualification.
5. Publish stable `MAJOR.MINOR.PATCH` from the verified release source. Build
   and verify the stable artifact identity and digest; keep the human tag in
   the form `vMAJOR.MINOR.PATCH`. Never replace contents under a published
   version or tag.
6. Reconcile the release into `develop` with a merge commit. If `develop` is
   left at the released baseline, retain the stable version until the first
   ordinary development merge starts the next line at `dev.1`. If next-release
   work already continues, preserve its core and advance its next unique
   checkpoint for the reconciliation PR.

## Hotfix procedure

Start a hotfix from the released `main` state. Verify and promote the fix to
`main` with a merge commit, identify the immutable released source and matching
artifact, then reconcile the fix into `develop` and every active release line
it affects with merge commits. Preserve continuing integration work and apply
the same joint version-reconciliation rules as a normal release. Requalify
changed candidate source before promotion.

## Evidence and current limits

Bind each release identity to the verified source tree and matching artifact
digests. Read back the PR base, head, merge method, hosted checks, release tag,
and artifact identity from their owning systems. Confirm any preprod and
production deployment only from the deployment workflow's own evidence.
Artifact identity establishes which bytes were built; it does not establish
that the candidate satisfies gameplay or operational qualification. Record
those claims against their owning product and release criteria, with explicit
scope for local, hosted and deployed evidence.
Publication of a GitHub Release is the declared public-deployment trigger once
that automation is in place; this runbook alone does not prove deployment is
implemented or authorize an operator to publish. The current qualification
boundary is recorded in [standards certification](../contracts/standards-certification.md).
