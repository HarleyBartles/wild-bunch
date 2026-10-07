# Check-Only Commit Gate and Development Build Identity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make local and hosted validation check-only, then publish one generated web build identity from the application's sole authored version.

**Architecture:** Preserve the tracked hook's staged-candidate isolation, but remove its apply and auto-staging path; the hook and hosted workflow validate the exact candidate and leave the index, HEAD, and authored files unchanged. `Directory.Build.props` remains the single authored application version, and the production Vite build emits an ignored `version.json` from effective MSBuild evaluation; CI verifies that artifact after the web build.

**Tech Stack:** Python 3 and pytest, Git Bash, Git, .NET 10/MSBuild, Vite/TypeScript, npm lockfile v3, and GitHub Actions.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; delivery policy in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`.

**Execution Strategy:** `executing-plans` because hook behavior, build identity, runner ordering, and candidate validation are sequential and share one repository gate; inline execution keeps that state together and is the user's approved lane.

## Global Constraints

- The merged preparation reserves `0.1.0-dev.1`; this PR sets `Directory.Build.props` to `0.1.0-dev.2` once, regardless of its task commit count.
- `Directory.Build.props` is the only authored application version; the private npm package and its lockfile root carry no application version.
- A production build generates `src/WildBunch.Web/dist/version.json`; generated outputs remain ignored and uncommitted.
- Pre-commit and hosted CI never apply formatting, refresh generated tracked files, stage corrections, or move HEAD. The local hook may temporarily materialize the staged candidate and restore the user's worktree to preserve isolation.
- Check mode may create ignored build/test outputs, but it must leave the candidate tree, staged index, HEAD, and unrelated tracked/untracked work unchanged.
- Explicit maintenance remains available through its existing apply command and must be run and reviewed by the author before staging; do not make this plan a broad command-bus rewrite or formatter adoption.
- Preserve gameplay, API behavior, persistence schema, dependency versions, and the currently selected React architecture.
- Keep the active plan, specification, and roadmap through this PR. Retire eligible predecessor plans in the first substantive commit as defined by `.agents/doctrine/completed-artifacts.md`.

## Review Focus

- A staged invalid candidate fails even if an unstaged repair exists, while failure and success both preserve HEAD, the index, staged contents, unstaged edits, and untracked files.
- Stale generated decision metadata is rejected without being rewritten or staged; diagnostics identify the existing explicit correction command.
- Hosted validation checks the checked-out commit without moving HEAD or rebuilding a different staged tree.
- `git diff --check` observes the intended staged candidate locally and the checked-out commit in hosted mode.
- Missing, duplicate, malformed, non-development, or mismatched version identity fails with a path-specific explanation.
- The production web artifact matches effective MSBuild `Version`; Vitest and Vite development do not invoke MSBuild or emit a production identity.
- Removing npm root version fields changes no dependency declarations or resolved package versions.

---

### Task 1: Make the tracked gate check-only and retire superseded planning artifacts

**Files:**
- Modify: `githooks/pre-commit`
- Modify: `tools/run.py`
- Modify: `.agents/contracts/repo-standards-commands.json`
- Modify: `.agents/contracts/standards-certification.md`
- Modify: `.agents/runbooks/implementing.md`
- Modify: `.agents/runbooks/pr.md`
- Modify: `.agents/playbooks/testing.md`
- Modify: `.agents/playbooks/completing-plans.md`
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `scripts/tests/test_precommit_candidate.py`
- Create: `tools/tests/test_run.py`
- Delete: `.agents/plans/2026-09-26-bunch-151-worldgenerated-legacy-events.md`
- Delete: `.agents/plans/2026-09-29-ambient-opt-in-standards-and-decisions-home.md`
- Delete: `.agents/plans/2026-10-02-aom-self-cert-migration.md`
- Delete: `.agents/plans/2026-10-06-versioned-release-foundation.md`

**Interfaces:** Local hooks invoke the declared `check` command only. Hosted mode requires a clean detached checkout at the declared commit and validates it in place. The runner's whitespace check compares `HEAD` to the materialized local candidate, or the hosted commit to its parent, without resetting refs or staging changes.

- [ ] **Step 1: Replace behavior tests that protect apply-and-stage behavior.** Keep the existing temporary-Git-repository fixture and fake gate, but make the check path reject stale generated content with a distinct status and make apply visibly refresh it. Add cases proving the hook rejects stale content without invoking apply, does not stage or rewrite that output, preserves staged-versus-unstaged candidate truth on failure, preserves unrelated tracked/untracked work on success and failure, and leaves hosted HEAD/index unchanged. Assert observed Git state and files, not hook source text or command-string inventories. Run `py -3 -m pytest scripts/tests/test_precommit_candidate.py -q` and witness the intended failures before changing the hook.

- [ ] **Step 2: Change the hook to validate only.** Preserve fail-closed candidate materialization and restoration for local partial staging, but remove `run_declared apply`, generated-path parsing/staging, and any index update. In hosted mode require the supplied commit to equal detached `HEAD`, require a clean checkout, and run the declared check without `git reset`. After validation, fail if the staged tree, HEAD, or candidate worktree changed. Remove `generated_paths` from the command declaration because the hook no longer owns generated-output staging.

- [ ] **Step 3: Make the whitespace gate inspect the candidate.** Update the runner to use `git diff --check HEAD` for the local materialized candidate and `git diff --check <commit>^ <commit>` when `REPO_STANDARDS_HOSTED_COMMIT` is set. Add temporary-repository behavior cases in `tools/tests/test_run.py` for whitespace errors in both states and for clean candidates. Keep command construction argument-safe and preserve subprocess diagnostics/status.

- [ ] **Step 4: Run the hook and runner behavior suites.** Run `py -3 -m pytest scripts/tests/test_precommit_candidate.py tools/tests/test_run.py -q`; compare staged tree, HEAD, staged file contents, restored unstaged bytes, and untracked bytes in the test outcomes.

- [ ] **Step 5: Update live workflow instructions and certification.** Change the implementing, PR, testing, and completing-plans guides so they say to run explicit apply/refresh operations before staging when needed and then rely on the check-only hook. Update the tracked-hook assessment to describe the new behavior and state plainly that strict certification remains pending if the pinned standard's hook-side normalization clause is not met; do not claim Windows/Linux or hosted evidence that has not run. Keep `ci --apply` for explicit existing maintenance and `ci --check` for validation.

- [ ] **Step 6: Retire completed and superseded predecessor plans.** Verify the already confirmed merged PRs #181, #182, and #184 remain the delivery evidence for the BUNCH-151, ambient standards, and AOM self-certification plans; their durable event, repository-standard, and certification outcomes now live in current source. Record in the roadmap that the unapproved versioned-release-foundation draft is superseded by the accepted specification and roadmap and that release implementation will be planned JIT from row 18. Confirm no active links remain, then remove these four stale plans in this first substantive commit. Preserve the current plan, current spec/roadmap, Cloud roadmap/spec, and all stable-0.1.0 investigation records.

- [ ] **Step 7: Commit the first substantive task.** Run `git diff --check`, inspect the exact staged candidate, and commit normally with `fix: make repository validation check-only`. The updated tracked hook must validate this commit without applying or staging changes.

### Task 2: Generate and validate one development build identity

**Files:**
- Create: `tools/versioning.py`
- Create: `tools/tests/test_versioning.py`
- Modify: `tools/run.py`
- Modify: `Directory.Build.props`
- Modify: `src/WildBunch.Web/vite.config.ts`
- Modify: `src/WildBunch.Web/package.json`
- Modify: `src/WildBunch.Web/package-lock.json`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`

**Interfaces:** `read_authored_version(path: Path) -> str` validates the sole direct `Project/PropertyGroup/Version`; `check_version_identity(root: Path) -> None` rejects duplicate npm root metadata and missing, malformed, or stale generated identity. The production Vite plugin emits exactly `{"version":"0.1.0-dev.2"}` from `dotnet msbuild src/WildBunch.Api/WildBunch.Api.csproj -getProperty:Version`.

- [ ] **Step 1: Write version behavior tests.** Use temporary repository fixtures to cover one valid `0.1.0-dev.2`, missing/duplicate/malformed/non-development values, npm package or lockfile root version fields, absent/malformed artifact JSON, missing artifact version, and stale artifact version. Include dependency data in the fixture to make accidental dependency changes observable. Run `py -3 -m pytest tools/tests/test_versioning.py -q` and confirm failures are the intended absent behavior.

- [ ] **Step 2: Implement the pure identity validator.** Parse XML and JSON structurally, validate `0.1.0-dev.N` with a positive integer suffix, resolve every path from the supplied root, and report the offending file/property. Do not inspect source text or invoke a shell.

- [ ] **Step 3: Add a production-only Vite build plugin.** Resolve repository paths from `import.meta.url`; invoke `dotnet msbuild` with an argument array and repository `cwd`; reject command failure or invalid output; emit `version.json` through Vite's build output API. Do not call MSBuild in dev-server or Vitest execution and do not write tracked files.

- [ ] **Step 4: Remove duplicate npm version metadata and advance the shared version.** Set `Directory.Build.props` to `0.1.0-dev.2`, remove the private package's `version`, and regenerate its lockfile with the installed npm package-lock-only command without scripts or dependency updates. Verify the package root and `packages[""]` omit version and the dependency graph is unchanged.

- [ ] **Step 5: Wire the identity checks into the canonical runner.** Add `tools/tests` to the tooling behavior test lane. After `_build_web` in `CI_CHECKS`, call `check_version_identity(ROOT)` and report the precise correction command. The version check must run after the production bundle emits its artifact; no separate release API or UI display is in scope.

- [ ] **Step 6: Prove build and test behavior.** Run `py -3 -m pytest tools/tests -q`, `npm --prefix src/WildBunch.Web ci`, `npm --prefix src/WildBunch.Web run test`, `npm --prefix src/WildBunch.Web run build`, and `dotnet msbuild src/WildBunch.Api/WildBunch.Api.csproj -getProperty:Version`. Confirm the generated artifact equals the evaluated property, and confirm the focused Vitest suite runs without MSBuild. Inspect the lockfile diff for dependency changes.

- [ ] **Step 7: Commit the identity task.** Commit with `build: generate web development identity`; the check-only hook validates the staged candidate and may create only ignored build/test outputs.

### Task 3: Verify the complete first slice and prepare its develop PR

- [ ] **Step 1: Verify required local prerequisites.** Use `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure` for the PostgreSQL lane and install the declared Python test requirements if they are absent. Do not substitute a skipped integration lane for evidence.

- [ ] **Step 2: Commit only after focused checks are green.** Inspect `git diff --check` and stage the exact final candidate. A normal hooked commit runs the canonical gate once against that staged state; do not run the full gate immediately before or after a successful hooked commit, and do not invoke `ci --apply` through the commit hook.

- [ ] **Step 3: Review the committed branch against the plan.** Inspect the full diff from the refreshed `origin/develop` base, verify no generated or dependency files changed unexpectedly, confirm version `0.1.0-dev.2` is the only authored application value, and confirm the hook does not stage or apply output. Resolve every material review finding with focused proof and a normal hooked commit; reuse canonical proof only while its committed state remains unchanged.

- [ ] **Step 4: Update the roadmap and prepare a Draft PR to `develop`.** Set row 01 to the actual plan/implementation status and preserve a separate JIT successor for the feature/history matrix. Publish only after the committed tree and required local proof are ready; verify the PR head equals local `HEAD`, the base is `develop`, and hosted CI status is read back from GitHub. Keep the PR Draft until whole-branch review and local gate pass, then allow hosted validation.

## Completion Evidence

Report the starting develop SHA, final commit and PR head, changed files, focused and canonical commands/results, Windows hook result, hosted Linux result, and any unresolved certification limitation. Do not claim PR merge or hosted success until the relevant GitHub state proves it. The goal's publication process continues only after the PR is actually merged into `develop`.
