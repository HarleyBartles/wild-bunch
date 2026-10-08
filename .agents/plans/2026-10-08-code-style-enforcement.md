# Code Style Enforcement Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Establish pinned, enforceable C#/.NET, TypeScript/React/SCSS, and Python style tools behind the repository command bus, with local and hosted checks failing fast before behavioral test suites.

**Architecture:** `tools/run.py` remains the sole agent-facing command bus. Formatter and linter checks do not mutate; explicit targeted format apply is a separate bus operation. Pre-commit and hosted CI validate their staged or committed candidate, respectively, and stop on the first failed check. This plan delivers the toolchain and gate foundation; editor format-on-save and synchronous agent post-edit adapters remain within roadmap row 04 for a later JIT plan after the bus path contract is proven.

**Tech Stack:** .NET SDK `10.0.301` with SDK-owned analyzers and `dotnet format`; Node 20 with ESLint `9.39.2`, `@eslint/js` `9.39.2`, `typescript-eslint` `8.71.1`, `eslint-plugin-react-hooks` `7.1.1`, `eslint-config-prettier` `10.1.8`, and Prettier `3.9.9`; Ruff `0.16.10` under the repository's Python 3.12 CI runtime.

**Specification:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially the executable style/lint policy and CI-gate contract; [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 04; [ADR-0043](../../docs/decisions/ADR-0043-repository-command-bus-ownership-and-gate-order.md).

**Execution Strategy:** `executing-plans` with Native inline execution. The language toolchains, path-aware bus targets, initial formatting migration, and commit/hosted gate are coupled through shared configuration and a single staged-candidate contract; one implementer context avoids handoff drift, with one fresh whole-branch review before publication.

## Global Constraints

- Target `develop` from merged PR #191 at `4c9a9f2ab6fc25b6798ae8aea387a4f7bf69eb2a`; refresh `origin/develop` before the worktree is used and do not reuse the row 03 worktree.
- Advance the sole authored application version in `Directory.Build.props` exactly once from `0.1.0-dev.6` to `0.1.0-dev.7` for this PR; preserve generated web version identity and do not add duplicate version fields.
- In the first substantive commit, verify PR #191's merge and gate evidence, mark roadmap row 03 complete with merge commit `4c9a9f2` and its review/CI evidence, mark row 04 executing and link this plan, advance the version, and retire the completed `.agents/plans/2026-10-08-command-bus-and-scripts.md`. Keep the baseline specification, roadmap, and mixed/live investigation evidence because they still govern the epic.
- Preserve the check-only tracked hook, staged-candidate isolation, partial staging, no-repair/no-stage behavior, and manual-only aggregate diagnostics. Never run formatter apply from pre-commit or CI.
- Commit the plan/bootstrap first; keep intentionally red style tests and a partially integrated toolchain uncommitted until the complete formatting migration makes the staged candidate pass the new hook. Commit the integrated implementation as one changeset rather than attempting a knowingly failing check-only commit.
- Configure LF and final newline for supported source formats, four-space C#/Python indentation, and two-space TypeScript/TSX/SCSS indentation. Do not apply a code formatter or code-width rule to Markdown prose; preserve the repository's unwrapped Markdown convention.
- Use the settled tool choices and exact package pins above. Do not introduce a second formatter, analyzer framework, hook manager, build system, or standalone validation script.
- Keep semantic lint fixes explicit and narrowly within the style-policy scope. Do not suppress whole directories or make gameplay, architecture, API, or UI behavior changes to silence diagnostics; if a diagnostic exposes a substantive defect, record it for the owning roadmap slice and use only a justified narrow suppression until that slice.
- `format --apply` requires explicit file paths or an explicit whole-scope option, rejects paths outside the repository, and reports the files it changed. `format --check` and `lint --check` never change maintained files. Exclude generated, dependency, vendor, build, and Markdown files.
- Order the gate from cheap checks to expensive checks, and finish all available formatting, lint, and static checks before starting any behavioral test suite. In particular, a lint failure must stop the gate before pytest, Vitest, or `dotnet test` begins. The gate stops at the first failure and prints the exact focused repair/recheck command.
- Do not implement the editor integration or agent lifecycle hook in this plan. A later JIT plan in row 04 may consume the target path API after this foundation merges; do not write that successor plan ahead of time.
- Before merge, reconcile the version with latest `develop`; if another PR merged first, advance to the next unused dev number and rerun affected review and validation.

## Review Focus

1. The new `format` and `lint` commands provide explicit modes, preserve path arguments with spaces, reject repository escapes, exclude generated/vendor/build/Markdown content, and return truthful child statuses.
2. Format check and lint check remain non-mutating, while format apply changes only the requested supported code paths and never silently fixes semantic lint findings.
3. A real gate behavior test proves that a formatting/lint failure prevents later behavioral test commands from starting; the test does not freeze a private list of CI steps or inspect source text.
4. The pre-commit hook checks the exact staged candidate, rejects unformatted or lint-invalid staged code, and restores unrelated unstaged work without changing the index; hosted validation uses the same style policy against the committed tree.
5. Initial formatting preserves behavior, leaves Markdown prose unwrapped, and does not turn this style slice into feature cleanup or broad domain refactoring.
6. Certification, command documentation, runbooks, ADR-0043 and the roadmap describe the live implementation accurately; the row 03 plan is retired only after its whole scope and delivery are verified.

---

## Task 1: Bootstrap row 04 and retire completed row 03 planning

**Files:** Create `.agents/plans/2026-10-08-code-style-enforcement.md`; modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-08-command-bus-and-scripts.md`.

- [ ] Verify PR #191 is merged to `develop` at `4c9a9f2ab6fc25b6798ae8aea387a4f7bf69eb2a`, its final reviewed head is `f2ac51532469dcdb20b765293ab708bf2dc8a7d1`, and the canonical hosted gate passed for that head.
- [ ] Classify the row 03 plan's full scope against the merged files, ADR-0043, and publication proof; mark row 03 done with PR #191 evidence and retire its completed plan link.
- [ ] Mark roadmap row 04 executing, link this JIT plan, and record that this first slice establishes lint/format tools and fail-fast gates while post-edit lifecycle integration remains uncompleted row 04 scope.
- [ ] Set `Directory.Build.props` to `0.1.0-dev.7` and preserve its single-authority/generated-web identity contract.
- [ ] Commit only this plan, roadmap/version bookkeeping, and predecessor retirement before implementation.

## Task 2: Add behavior-first tests for style targets and fail-fast ordering

**Files:** Modify `tools/tests/test_run.py`; add focused tests under `tools/tests/`; extend `scripts/tests/test_precommit_candidate.py` only for a real uncovered staged-style boundary.

- [ ] Write tests for `format --check` reporting a formatting difference without changing the file and `format --apply` fixing only explicit supported paths, including a path containing spaces.
- [ ] Write negative tests proving path traversal/absolute paths outside the repository are rejected before a formatter starts, generated/build/vendor paths are excluded, Markdown prose is never formatted, and omitted mutating scope is not guessed.
- [ ] Write a lint-check test proving an invalid Python or TypeScript fixture returns failure without applying auto-fixes; provide focused actionable repair and recheck commands.
- [ ] Write a gate behavior test that injects a failing format/lint operation before test commands and proves pytest, Vitest, and `dotnet test` are not invoked after the early failure; do not assert the private `CI_CHECKS` tuple or a source-text ordering.
- [ ] Reuse existing staged-snapshot behavior coverage; add only the missing end-to-end case that stages an unformatted supported source file, leaves a separate unstaged edit, invokes the actual check-only hook path with the real style gate, observes rejection, and verifies both the index and unrelated unstaged edit are preserved.
- [ ] Run each new focused test and confirm the failure is caused by the currently missing style behavior rather than fixture setup.
- [ ] Keep intentionally failing tests uncommitted until their owning implementation tasks pass and the whole repository can satisfy the new check-only gate.

## Task 3: Configure the pinned language toolchains

**Files:** Add `.editorconfig`, `.gitattributes`, and `global.json`; modify `Directory.Build.props`, `.github/workflows/ci.yml`, `tools/requirements.txt`, `src/WildBunch.Web/package.json`, and `src/WildBunch.Web/package-lock.json`; add the ESLint and Prettier configuration files.

- [ ] Configure `.editorconfig` for LF, final newlines, language-specific indentation, and explicit C# naming/brace/namespace preferences that match the existing coherent source conventions; configure `.gitattributes` so supported text files are normalized to LF without treating binary assets as text.
- [ ] Pin .NET SDK `10.0.301` through `global.json` and align `actions/setup-dotnet` with the same SDK; enable the SDK-owned analyzer and code-style diagnostics selected by the specification without adding a third-party C# analyzer package.
- [ ] Add exact web development dependency pins from the plan's Tech Stack and regenerate the npm lockfile; configure flat ESLint with type-aware TypeScript rules and React Hooks rules, plus Prettier for TypeScript/TSX/SCSS only.
- [ ] Add exact `ruff==0.16.10` to the repository-owned Python tooling requirements and configure Ruff formatting plus correctness, unused-import, import-order, and bug-prone rules for the Python 3.12 target.
- [ ] Leave existing source unchanged until the bus targets exist; record supported path classes and exclusions in configuration so the subsequent apply pass has one canonical owner.
- [ ] Run dependency installation and configuration validation, then record any tool-version or runtime incompatibility as a plan ruling before proceeding; do not silently float or replace a selected package.
- [ ] Keep configuration and lockfile changes uncommitted with the red tests until the new gate is implemented and the whole-source migration passes.

## Task 4: Implement targeted bus operations and compose the fail-fast gate

**Files:** Modify `tools/run.py`, `tools/tests/test_run.py`, focused new `tools/` helper modules only when a single helper makes the bus behavior clearer, and `tools/README.md`.

- [ ] Expose `format` with non-mutating `--check` and explicit targeted `--apply`, and `lint` with non-mutating `--check`; require meaningful modes, forward paths without shell-string reconstruction, and preserve subprocess output/status.
- [ ] Give each command accurate top-level and target help for prerequisites, exact tool command, supported file types, path behavior, side effects, and focused repair/recheck instructions.
- [ ] Compose `ci --check` so whitespace/repository-contract and available formatter/linter/static checks run before Python, web, or .NET behavioral tests; prepare required tool dependencies only once per gate; keep the sequence sequential and stop on the first failure.
- [ ] Keep `ci --check --diagnostics` manual-only, and ensure format apply is never called from the commit hook or hosted workflow.
- [ ] Preserve the existing `web --check` target for the focused web pipeline while exposing its style/type/test/build stages through the canonical bus without running a web test before all lint and format checks finish.
- [ ] Update command help and self-certification to state only the checks actually implemented; update durable guidance to direct authors to the bus and keep exact current/future boundaries truthful.
- [ ] Confirm no new durable architectural decision was made beyond ADR-0043's existing command-bus and gate-order decision; update ADR-0043 or its catalogue only if the actual live decision record has become false or incomplete.
- [ ] Run the focused bus tests after each target implementation, but keep these changes uncommitted until Task 5 completes the source-format migration and the full gate is green.

## Task 5: Run the whole-repository style migration and reconcile durable guidance

**Files:** All Task 1-4 changes; modify `.agents/contracts/standards-certification.md`, `.agents/contracts/repo-standards-commands.json` only if the current declaration must name new check commands, `.agents/playbooks/code-style.md`, `.agents/playbooks/testing.md`, `.agents/runbooks/implementing.md`, `.agents/runbooks/pr.md`, and `.github/workflows/ci.yml` where current behavior requires it.

- [ ] Start the format migration from the clean merged source baseline through the bus's explicit whole-scope format apply; format supported maintained code and tests, and review the diff to confirm changes are mechanical and Markdown prose, generated files, package contents and gameplay behavior are untouched.
- [ ] Run formatter checks and focused type/build checks after the migration; fix only in-scope formatting or lint findings, and record any substantive application defect for its owning later row instead of changing behavior here.
- [ ] Update certification and durable guidance to describe exact current commands and order; preserve the requirement that any future editor or agent lifecycle adapter call the same bus rather than introduce another formatter implementation.

## Task 6: Verify and prepare the PR

**Files:** All Task 1-5 changes.

- [ ] Run pinned Ruff format/lint, Prettier check, ESLint, and `dotnet format --verify-no-changes` independently and review every remaining diagnostic; do not use broad auto-fix or suppressions to manufacture a green result.
- [ ] Run focused tool behavior tests and the staged-candidate hook behavior tests; verify check mode never changes tracked files and apply mode modifies only its explicit path set.
- [ ] Run `py -3 tools/run.py ci --check` against the committed candidate prerequisites and inspect the fail-fast ordering evidence; hosted CI must use the same gate.
- [ ] Search the final diff and live documentation for stale claims about absent lint/format targets, direct tool commands presented as the agent-facing route, accidental Markdown reflow, and the row 03 plan after retirement.
- [ ] Independently reconcile the ADR catalogue and decision-record obligations against the diff; include any required dated correction in the same PR and do not create an ADR merely to record package versions or implementation details.
- [ ] Stage the integrated source, tests, configuration, version, roadmap and guidance changes, then make one implementation commit through the normal check-only hook; do not bypass the hook.
- [ ] Complete a fresh whole-branch review, fix all actionable findings and re-review the fixed diff, then publish a Draft PR to `develop`, advance it to Ready only after local review and validation pass, and verify the exact remote head and hosted gate before merge.
- [ ] Keep this plan and the governing specification through the completing PR; the next substantive successor slice will classify them for retirement.
