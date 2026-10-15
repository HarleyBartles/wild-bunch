# Row 18 Gitflow and SemVer Adoption Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Adopt the pinned AOM Gitflow and SemVer standards, route their durable release obligations, and make the existing single-source build identity accept development, qualified candidate, and stable 0.1.0 identities.

**Architecture:** Keep `Directory.Build.props` as the sole authored Wild Bunch product version. Extend the existing Vite-generated web identity and command-bus validator to support SemVer 2.0.0 identities, including `0.1.0-dev.N`, `0.1.0-rc.N`, and `0.1.0`; record release workflow and observed hosting limits in repository-owned decision, certification, and lifecycle guidance.

**Tech Stack:** .NET 10/MSBuild, Python 3 command bus and tests, Vite/TypeScript production build, Markdown ADR and agent guidance, GitHub PR workflow.

**Spec:** [.agents/specs/2026-10-07-stable-0.1.0-baseline.md](../specs/2026-10-07-stable-0.1.0-baseline.md) and the [Gitflow and release-identity contract](../specs/2026-10-06-cloud-playability-and-releases.md#gitflow-and-release-identity).

**Execution Strategy:** `executing-plans` — the tests, version source, generated identity, standards subscription, and release guidance form one ordered adoption boundary; inline execution preserves the evidence chain and avoids repeated context handoffs. The closest alternative, `subagent-driven-development`, would add per-task reviews across tightly coupled code and documentation before one required whole-branch review.

## Global Constraints

- This PR proposes `0.1.0-dev.196`; the next ordinary PR merged to develop advances the development checkpoint once.
- `Directory.Build.props` remains the only authored application version; web package and lockfile metadata stay unversioned for the product.
- The versioned subject is the Wild Bunch browser game product; during `0.y.z`, breaking API and gameplay changes are allowed and no public compatibility promise is made.
- Do not define a future `1.0.0` compatibility contract.
- Keep ordinary work on fresh `codex/*` branches from current `develop` and merge by PR to `develop`; do not create a release/hotfix branch, tag, GitHub Release, or deployment in this plan.
- Do not change GitHub default-branch, protection, or merge-method settings; guidance must not imply that currently absent protections are enforced.

## Review Focus

- SemVer-valid `dev.N`, `rc.N`, and stable identities must pass from the one authored version through generated `version.json`; malformed versions, stale output, and duplicate npm product versions must still fail in `tools/tests/test_versioning.py`.
- The `web --check` build path must reject a stale generated release identity after build; prove this with a temporary-repository command-bus behavior test in `tools/tests/test_run.py`.
- Gitflow guidance must distinguish the verified `develop` default and available merge methods from enforcement; the release runbook and certification record the current absence of `develop` branch protection and require publication-time verification.

---

### Task 1: Commit the JIT plan and successor checkpoint

**Files:**
- Create: `.agents/plans/2026-10-15-row18-n78-gitflow-semver-adoption.md`
- Delete: `.agents/plans/2026-10-15-row17-n77-residual-test-inventory.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`

**Interfaces:**
- Consumes: N-77 PR #380, merged to `develop` as `583c9b64fd67703454f5b0fcbd5b62695eb20132`; N-77 is complete and eligible for successor retirement.
- Produces: N-78 is the active row-18 plan on `develop` checkpoint `0.1.0-dev.196`; row 18 remains incomplete until local release qualification is delivered by a later JIT slice.

- [x] Verify the worktree branch starts at current `origin/develop` and run `py -3 tools/run.py setup-hooks --apply` in this dedicated worktree.
- [x] Retire the merged N-77 plan, create this committed plan, advance `Directory.Build.props` from `0.1.0-dev.195` to `0.1.0-dev.196`, and update roadmap row 18 to identify N-78 as in progress. Record PR #380's merge and the boundary between adoption and later release qualification; do not mark row 18 complete.
- [x] Run `git diff --check`, stage only the four named changes, and commit. The installed check-only hook runs `py -3 tools/run.py ci --check` against the staged candidate; do not bypass it.

### Task 2: Extend and prove the generated SemVer build identity

**Files:**
- Modify: `tools/versioning.py`
- Modify: `tools/tests/test_versioning.py`
- Modify: `tools/run.py`
- Modify: `tools/tests/test_run.py`
- Modify: `src/WildBunch.Web/vite.config.ts`

**Interfaces:**
- Consumes: Task 1's single authored development version and the existing `read_authored_version`, `check_version_identity`, Vite build-identity plugin, and `web --check` command.
- Produces: valid SemVer product identities can be built and checked; `web --check` fails after build when the generated identity is missing, malformed, or stale.

- [x] In `tools/tests/test_versioning.py`, parameterize `check_version_identity` for `0.1.0-dev.196`, `0.1.0-rc.1`, and `0.1.0`; add malformed SemVer cases for leading-zero core components, leading-zero numeric prerelease identifiers, missing core components, and unsupported characters. Run `py -3 -m pytest tools/tests/test_versioning.py -q` and confirm the new release cases fail for the existing dev-only validator.
- [x] Implement SemVer 2.0.0 parsing for the authored version, allowing optional valid prerelease and build metadata while rejecting leading zeroes in numeric core and numeric prerelease identifiers. Keep dependency versions and event/schema versions outside this product identity check.
- [x] Update the Vite build-identity plugin to accept valid SemVer 2.0.0 values from effective MSBuild evaluation and emit the exact value in `dist/version.json`.
- [x] Add a temporary-repository test in `tools/tests/test_run.py` that invokes the real `run._build_web` path with a stale generated `version.json`; mock only the external npm/style work, retain the real `_version_identity_check`, and assert the command raises `VersionIdentityError` after the build command. Confirm the test fails before wiring validation into `_build_web`.
- [x] Wire `_version_identity_check` to run after the web production build in `_build_web`. Keep CI fail-fast ordering and check-only behavior; no validator or build step may mutate maintained version files.
- [x] Run `py -3 -m pytest tools/tests/test_versioning.py tools/tests/test_run.py -q` and build the web artifact with temporary authored versions `0.1.0-rc.1` and `0.1.0`, checking each emitted `dist/version.json`; restore `Directory.Build.props` to `0.1.0-dev.196` and rebuild before staging.

### Task 3: Adopt the standards and route the release lifecycle

**Files:**
- Modify: `.agents/contracts/operating-standards.json`
- Modify: `.agents/contracts/standards-certification.md`
- Create: `.agents/runbooks/release.md`
- Modify: `.agents/specs/2026-10-06-cloud-playability-and-releases.md`
- Modify: `.agents/doctrine/repo-runbook-policy.md`
- Modify: `.agents/runbooks/pr.md`
- Modify: `.agents/runbooks/code-review.md`
- Modify: `AGENTS.md`
- Modify: `CONTRIBUTING.md`
- Create: `docs/decisions/ADR-0048-gitflow-and-semver-release-lifecycle.md`
- Modify: `docs/decisions/README.md`

**Interfaces:**
- Consumes: the immutable `gitflow` definition at Marketplace commit `7a20ef191bb6be3b67fbbb9e86bd7b0050f96fc5` and `semver` definition at `12c86626f80cad1a2ac05b2f6690c882a3d5959a`; existing root subscription/certification routes and repository lifecycle runbooks.
- Produces: one durable release runbook, a decision record preserving the user-approved branch/version policy, exact v2 standard pins, and factual partial certification routed from entry points used by implementers, reviewers, and release operators.

- [x] Add both exact immutable definitions to `operating-standards.json`, preserving every existing pin and certification reference.
- [x] Add ADR-0048 with the original Gitflow/SemVer decision and dated SemVer clarification. Record the product scope, current `0.y.z` compatibility policy, `develop` and `main` roles, ordinary PR flow, release/hotfix merge-commit routes, one version authority, `dev.N`/`rc.N` cadence, immutable verified release identity, and explicit deferral of any future `1.0.0` compatibility contract. Link the existing cloud release specification; do not duplicate implementation inventories.
- [x] Add a concise release runbook: ordinary `codex/*` work starts at current `develop` and PRs back there (squash is allowed); `release/*` starts at `develop`, uses `0.1.0-rc.1` for the first qualified candidate, advances the RC only when source changes and is requalified, and promotes to `main` with a merge commit; final promotion uses stable `0.1.0` built from the verified source, with human tag label `v0.1.0`; merge the release back into `develop` with a merge commit. A hotfix starts from released `main`, promotes to `main` and merges back to `develop` and affected active release lines with merge commits. If merge-back leaves `develop` at the released baseline, retain the stable identity until the first ordinary development merge starts `dev.1`; if intended development already continues, preserve it and allocate the next unique checkpoint for the reconciliation PR. Require publication-time verification of source, PR base/head, merge method, exact hosted checks, generated identity, and matching artifact digests; do not claim local naming or documentation enforces GitHub policy. Clearly state that this plan does not create a public release or deployment.
- [x] Route the release runbook from the root router, contributor guidance, lifecycle-stage table, PR runbook, and code-review runbook. Keep runbook content durable, use the unslop and decision-record playbooks, and omit empty sections.
- [x] Certify only what current code and observed GitHub state establish: one application version in `Directory.Build.props`, generated .NET/web identity behavior and validation, `develop` as GitHub default, currently allowed merge methods, no `develop` branch protection, and no claimed enforcement from those facts. Mark any not-yet-proven end-to-end candidate/artifact qualification as partial rather than self-certified; document that work remains in row 18.
- [x] Run `py -3 tools/check_operating_standards.py --check`, `py -3 tools/check_agent_routers.py --check`, and `git diff --check`.

### Task 4: Verify and publish N-78

**Files:**
- Review the complete diff across all files above and the active row-18 plan.

**Interfaces:**
- Consumes: completed N-78 implementation, its current certification and ADR, and the row-18 spec/roadmap.
- Produces: reviewed PR to `develop` at `0.1.0-dev.196`, with exact-head hosted CI evidence and no false completion claim for row 18.

- [ ] Independently compare the actual diff with the decision catalogue and `docs/features.md`; update the ADR catalogue when required and record why product feature truth is unchanged.
- [ ] Run `py -3 tools/run.py ci --check`; stage the reviewed tree and commit through the normal check-only hook.
- [ ] Request a fresh whole-branch review against the exact `develop` base and head, resolve Critical/Important findings with proof, and re-run the canonical gate after any correction.
- [ ] Open and read back the Draft PR body, target, head SHA, and hosted checks; mark ready only after the independent review and local gate are green. Merge the PR into `develop` only after the hosted gate passes.
- [ ] Verify the develop-push gate, fast-forward the main checkout while preserving the user's existing web-test follow-up edit, and use the worktree-skill cleanup script to remove only this clean worktree and its exact local/upstream refs.
