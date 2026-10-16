# N-95: Qualify the local 0.1.0 development baseline

## Outcome

Establish whether the retained local product paths, supported persistence/recovery evidence, and source-bound API/public-web/playtest bundle are sufficient to call `develop` a qualified `0.1.0` pre-release candidate. Record the exact qualification boundary in the baseline specification, feature matrix, standards certification, and roadmap. This does not create a release branch, RC, stable tag, GitHub Release, deployment, or `0.1.0` stable claim.

## Source material

- `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 18 and N-80 through N-94 delivery records
- `.agents/contracts/standards-certification.md`
- `.agents/runbooks/release.md` and `tools/README.md`
- `docs/features.md`, PG-001 through PG-008 and PLAT-001 through PLAT-003
- Current production-browser route bodies and the persistence/cache-recovery acceptance tests cited by those entries
- `py -3 tools/run.py release-verify --check` output and its generated manifest

## Decisions and boundaries

- Qualification applies to the current development baseline on `develop`, not a public release. Advance the authored version to `0.1.0-dev.213` in the implementation PR.
- Assess each retained PG-001 through PG-008 lifecycle against the existing behavior evidence. Keep partial assessments and stated evidence limits where routes or lifecycle stages are not proven. Do not inflate the qualification claim to complete gameplay or external-player validation.
- Check supported persisted-history/cache-recovery claims against their owning acceptance tests and spec. Existing static findings and test evidence are not runtime or deployment evidence.
- Build and verify the immutable local release bundle from the clean implementation source commit after the canonical gate. Preserve the manifest and all three artifact trees byte-for-byte after the PR is merged by verifying source/tree equality against the squash merge before transferring the ignored bundle into the primary checkout.
- Hosted release/hotfix promotion, branch protection, publication, cloud deployment, public identity, and public resume remain explicitly unexercised or out of scope. Do not create release refs, tags, or external releases.
- Remove the obsolete instruction to reconcile the blocked release-foundation draft. Its checklist was never adopted; qualification follows this approved spec and roadmap.
- Add no tests unless inspection exposes a concrete behavioral qualification gap that existing tests cannot detect. Do not test document shape, links by inventory, or completed-work receipts.

## Task 1: Commit this JIT plan

Create and commit this plan before implementation. Keep it through its completing PR. Follow the repo completing-plans playbook and preserve the `.dev.213` increment for the single implementation PR.

## Task 2: Inspect the complete qualification evidence

Review every PG-001 through PG-008 assessment and its linked retained-route evidence, then inspect the supported-history/cache-recovery claims in the approved spec and feature matrix. Verify the current test bodies reach and assert the behaviors claimed. Review current ADR-0048 and relevant decision records for any qualification or lifecycle claim this slice would make. Identify concrete gaps; add a behavioral test only when a real gap exists, prove it fails under a behavior-specific mutation, restore the mutation, and run the owning suite.

## Task 3: Produce the source-bound candidate and reconcile live truth

After implementation source is committed and clean, run `py -3 tools/run.py release-verify --check`. Inspect `release-manifest.json`, source commit, authored version, artifact tree counts, and digests. Verify the existing bundle on a second invocation without rebuilding. Update the baseline spec to remove the obsolete draft reference and state the qualification evidence/boundaries. Update only feature-matrix entries whose current assessment or qualification boundary is inaccurate. Update standards certification and row 18 to state what has been qualified and what release/deployment procedures remain unexercised. Set `Directory.Build.props` to `0.1.0-dev.213`; generated web identities must derive from this one authority. In the first substantive implementation commit, retire the completed N-94 plan and update its successor pointer; retain this N-95 plan through the PR.

## Task 4: Verify, review, publish, and merge

Run `py -3 tools/run.py ci --check` from the clean source candidate; the commit hook is the canonical gate and must pass. Open a Draft PR to `develop` with the exact head, qualification boundary, test/gate result, and source-bound bundle manifest identity. Use the standing authorization for an independent fresh-context whole-branch review; repeat review after any correction. Mark ready only after review and local validation. Merge only after hosted exact-head CI passes, then verify the develop-push gate on the exact merge commit.

Fast-forward the primary checkout while preserving unrelated user changes. Confirm the squash merge has the source candidate's exact tree. Copy the ignored bundle byte-for-byte into the primary checkout under its source-commit path and verify every file digest and manifest again. Use the finishing helper in check/apply mode, then remove the clean worktree and its exact local/upstream branches together; verify all are absent.

## Completion evidence

- Every retained PG-001 through PG-008 assessment is reconciled against its current behavior evidence and qualification boundary.
- The stable-baseline specification, feature matrix, certification, and roadmap agree on qualified scope and explicit limits; no blocked draft is presented as live work.
- The `.dev.213` bundle manifest binds API, public web, and playtest web artifact trees to the exact implementation source and authored version; its immutable verification succeeds twice.
- The canonical local gate, independent review, hosted exact-head gate, and develop-push gate pass; primary checkout, bundle custody, worktree cleanup, and paired branch cleanup are verified.
- No stable tag, release publication, hosted promotion, or cloud deployment is claimed or performed.
