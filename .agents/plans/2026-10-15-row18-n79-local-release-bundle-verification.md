# Row 18 N-79 Local Release Bundle Verification Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build and verify one local Wild Bunch release bundle whose API publish, public frontend and playtest frontend are bound to one clean source commit and the one authored SemVer version.

**Architecture:** Add a check-only `release-verify` target to `tools/run.py`. It builds into an ignored staging directory, verifies each generated frontend `version.json`, computes a content digest for each artifact tree, writes a manifest containing the source commit and product version, then atomically publishes the completed local bundle under `artifacts/release/<source-commit>`. A repeat invocation verifies the existing immutable bundle without rebuilding or overwriting it. The target does not publish artifacts, create images, tag releases or deploy.

**Tech Stack:** Python 3 command bus and pytest behavior tests, .NET 10 `dotnet publish`, locked Vite builds for `production` and `playtest`, Markdown release guidance.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), [Gitflow and release identity](../specs/2026-10-06-cloud-playability-and-releases.md#gitflow-and-release-identity), and row 18 of the [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md#18-baseline-qualification-and-release-foundation).

**Execution Strategy:** `executing-plans` - the source check, artifact builder, manifest verifier, tests and release documentation form one sequential command contract; inline execution preserves the behavior and evidence chain, while a per-task subagent lane would add handoffs without creating independent deliverables.

## Global Constraints

- This PR proposes `0.1.0-dev.197`, advancing once from N-78's merged `0.1.0-dev.196`; `Directory.Build.props` remains the only authored product version.
- Require a clean tracked and untracked source tree before building and bind the output to the full current Git commit ID.
- Produce only a .NET API publish directory plus public and playtest web builds. Cloud container images, infrastructure, hosted release automation, public deployment, tags, GitHub Releases and gameplay-path qualification are outside this slice.
- Keep generated bundles under ignored `artifacts/release/`; never stage or commit their files. Existing outputs are immutable: reverify matching bundles and fail on mismatched or tampered contents instead of overwriting them.
- A digest identifies the exact built bytes; do not claim separate clean builds are byte-for-byte reproducible unless that behavior is independently established.
- Keep the developer user's existing edit in the main checkout untouched. This worktree and its matching local/upstream branches are removed together only after the reviewed PR and develop-push gate pass.

## Review Focus

- A dirty source tree must fail before any build command runs, because the resulting manifest could not truthfully name a commit containing the built source.
- Every web variant must carry the exact authored version; a missing, malformed or stale generated `version.json` must reject the bundle.
- The manifest must bind all files and relative paths in the API, public web and playtest web trees. A changed, missing or added file must invalidate verification.
- A repeat invocation for the same source must verify existing bytes without rebuilding or replacing them; an invalid existing output must fail without mutation.
- `release-verify` must require `--check`, reject apply/unknown arguments before work, and make no claim of publication or deployment.

---

### Task 1: Commit the N-79 plan and retire completed N-78

**Files:**
- Create: `.agents/plans/2026-10-15-row18-n79-local-release-bundle-verification.md`
- Delete: `.agents/plans/2026-10-15-row18-n78-gitflow-semver-adoption.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`

**Interfaces:**
- Consumes: PR #381's reviewed source `39fc3dbd1483befc6ed2d8209ae02d186bc1216e`, squash merge `7dda5283fbad3b2dfe4548c29399de8ab94963e9`, successful exact-head PR and develop-push gates, N-78's complete source/docs scope, and the existing PLAT-004 assessment.
- Produces: N-79 as the active row-18 plan at `0.1.0-dev.197`; a roadmap record of N-78's actual delivery; N-78's plan retired only after its full implementation, review, hosted validation, merge and cleanup were verified.

- [x] Verify this clean worktree starts at current `origin/develop` commit `7dda5283fbad3b2dfe4548c29399de8ab94963e9`; confirm N-78's PR head, merge result, hosted gates, and current ownership/docs that retain its durable decision and procedure.
- [x] Remove the completed N-78 plan, advance `Directory.Build.props` from `0.1.0-dev.196` to `0.1.0-dev.197`, create this plan, and update roadmap row 18 with N-78 delivery evidence and N-79's bounded local artifact-verification scope. Do not mark row 18 complete.
- [x] Run `git diff --check`, verify only the four named paths are staged, and commit through the normal check-only hook. Do not bypass the hook.

### Task 2: Build and verify source-bound local release bundles

**Files:**
- Create: `tools/release_artifacts.py`
- Create: `tools/tests/test_release_artifacts.py`
- Modify: `tools/versioning.py`
- Modify: `tools/tests/test_versioning.py`
- Modify: `tools/run.py`
- Modify: `tools/tests/test_run.py`
- Modify: `tools/README.md`
- Modify: `.agents/runbooks/release.md`
- Modify: `.agents/contracts/standards-certification.md`
- Modify: `docs/features.md`

**Interfaces:**
- Consumes: `read_authored_version`, `VersionIdentityError`, the Vite `version.json` build identity, the command bus's explicit `--check` dispatch and the release runbook's current source/artifact obligations.
- Produces: `py -3 tools/run.py release-verify --check`, which rejects a dirty source before build, generates or verifies an ignored API/public-web/playtest-web bundle for one source SHA and product version, and records or rechecks each artifact tree digest.

- [ ] In `tools/tests/test_release_artifacts.py`, first prove that the manifest digest changes when any artifact filename or bytes change, and that verification rejects changed or missing files. Calculate expected SHA-256 values independently in the tests; do not call the implementation helper to produce both actual and expected results.
- [ ] In `tools/tests/test_versioning.py`, add behavior cases for checking `version.json` at an explicit build output path. Prove a matching version passes and a stale version at the supplied path raises `VersionIdentityError` before using this helper for release outputs.
- [ ] In `tools/tests/test_run.py`, create a temporary committed Git repository with a minimal API project, one authored version and web metadata. With `_run` replaced only at the external build-process boundary, prove the target runs the API publish and both web variants, writes an accurate manifest bound to the commit/version, and verifies it on a second invocation without running builds again. Also prove dirty source is rejected before build work and a tampered existing bundle fails without being rebuilt or replaced.
- [ ] Run the focused RED tests: `py -3 -m pytest tools/tests/test_release_artifacts.py tools/tests/test_versioning.py tools/tests/test_run.py -q`. Confirm the new cases fail because the manifest/check target behavior is absent or incomplete, not because the fixtures or assertions are broken.
- [ ] Implement a small artifact helper that validates the clean source commit, computes a deterministic tree digest from sorted relative file paths and file-content SHA-256 values, writes a manifest with format version, product version, source commit, artifact names, file counts and tree digests, and verifies an existing bundle's identity and contents. Reject missing or empty artifact trees and never overwrite an existing bundle.
- [ ] Extend `check_version_identity` with an optional explicit generated-artifact path while preserving its existing default `src/WildBunch.Web/dist/version.json` behavior and duplicate npm-version checks. Use this same validator for both separately built frontend variants.
- [ ] Add the `release-verify` `--check` target. Build `WildBunch.Api` in Release configuration into a temporary staging directory with `dotnet publish`; build the public frontend using `npm run build -- --outDir <staging-path>` and the playtest frontend using `npm run build:playtest -- --outDir <staging-path>`. Validate both staged version files against `Directory.Build.props`, calculate the three artifact digests, write `release-manifest.json`, and atomically install the complete bundle under `artifacts/release/<full-source-commit>`.
- [ ] If the source-commit output already exists, verify its manifest, version, all three artifact trees and stored digests without building or changing it. Fail with an actionable message on malformed manifest, identity mismatch, tampered content or incomplete prior output. Clean up only the temporary staging directory owned by this invocation when a build fails.
- [ ] Register `release-verify` in `tools/run.py` and `tools/README.md`; its `--help` must state purpose, prerequisites, ignored output path, immutability behavior and that it does not publish. The target accepts only `--check`; unknown arguments or `--apply` fail before any build command runs.
- [ ] Update the release runbook to require canonical CI on the exact clean source before running `release-verify --check`, explain how the manifest binds the API/public/playtest artifacts, and state that local bundle evidence is not hosted release or deployment proof. Update command-bus and SemVer certification plus PLAT-004's evidence assessment to describe the new local proof and retain the hosted-release and complete-candidate gaps.
- [ ] Run `py -3 -m pytest tools/tests/test_release_artifacts.py tools/tests/test_versioning.py tools/tests/test_run.py -q` and inspect the behavior output. Run `py -3 tools/run.py release-verify --check` from the clean committed source; independently check its manifest's source SHA, authored version and all artifact digests. Run `py -3 tools/check_operating_standards.py --check`, `py -3 tools/check_agent_routers.py --check`, and `git diff --check`. Stage only the named source/docs/plan changes and commit through the normal hook.

### Task 3: Review, publish and close N-79

**Files:**
- Review the complete N-79 branch diff, this plan, the release identity specification, ADR-0048, the release runbook, command-bus certification and PLAT-004.

**Interfaces:**
- Consumes: committed N-79 bundle-verification behavior, its exact local manifest, passing canonical hook, active roadmap and clean independent review.
- Produces: an ordinary reviewed PR to `develop` at `0.1.0-dev.197`, passing exact-head hosted CI, merged output and paired worktree/branch cleanup.

- [ ] Independently compare the final diff with ADR-0048 and the decision catalogue; record why no decision changes or include the required successor. Compare PLAT-004 with the exact local artifact proof and preserve its incomplete hosted-release/retained-route status.
- [ ] Dispatch a fresh-context whole-branch reviewer with the exact base/head package; resolve Critical or Important findings with a witnessed RED/GREEN proof and rerun the complete local gate after any correction.
- [ ] Push the dedicated branch and create a Draft PR targeting `develop`. Read back the exact title/body/base/head, matrix/ADR author checks, Draft state and initial hosted status; mark ready only after local validation and independent review pass.
- [ ] Wait for the hosted canonical commit gate on the exact PR head. Merge to `develop` only after it passes; then wait for the develop-push gate on the exact merge result.
- [ ] Fast-forward the main checkout to the verified merge while preserving the pre-existing user edit. Confirm PR head equals the branch tip and merge commit is in current `develop`. From outside the worktree, run the finishing-a-development-branch helper with `--check` then `--apply`; remove the clean worktree, delete its exact local branch, delete its exact upstream branch and verify all three are absent.

## Scope Boundaries

This slice does not produce or publish a container image, create release/hotfix branches, create a version tag or GitHub Release, deploy to a cloud host, or qualify the connected player gameplay route. Those claims require their own authorized roadmap work and evidence.
