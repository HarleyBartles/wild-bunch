# Command Bus Adoption and Script Rationalization Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Adopt and certify the repository-owned command bus against the current AOM contract, make supported repository validation discoverable through it, and retire redundant wrappers and tests that preserve them.

**Architecture:** `tools/` owns the command bus and its Python behavior tests; `scripts/` contains repository operations that have no natural bus target, including real native process-management implementations. Keep one portable implementation where it suffices, retain native implementations only for actual platform requirements, and make the canonical CI target a check-only composition of named validation lanes.

**Tech Stack:** Python 3.11+, argparse, pytest, PowerShell and Bash only for their real native process-management requirements, existing .NET and web build tools, Wild Bunch check-only validation hook.

**Specification:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially command-bus adoption and repository scripting; [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 03; current AOM `command-bus` and `tracked-validation-hook` definitions at immutable commit `a9d9f280316a87ac66cb2653384bc30a683603f0`.

**Execution Strategy:** `executing-plans` with Native inline execution. Commit this plan, row/version bookkeeping, completed-predecessor retirement, and the stale standard-provenance correction first; then implement the tightly coupled CLI, certification, routing, wrapper retirement, and behavioral-test changes in this worktree.

## Global Constraints

- Target `develop` from merged PR #190 at `0d3e181ed0cc0c5df66a671cca2255702b82b4eb`; advance `Directory.Build.props` from `0.1.0-dev.5` to `0.1.0-dev.6` exactly once for this PR.
- Row 02 is complete in merged PR #190; classify its entire delivered scope and retire `.agents/plans/2026-10-08-docs-custody.md` in the first substantive commit while preserving the baseline specification, roadmap, and historical investigation evidence.
- Update roadmap row 02 with verified PR #190 merge evidence, set row 03 executing, and link this plan.
- The tracked-hook subscription already pins current AOM `main` at `a9d9f280316a87ac66cb2653384bc30a683603f0`. Preserve its candidate-preserving, no-repair/no-stage policy and update the stale baseline-spec statement that cites an older command-bus revision and claims the hook standard expects normalization.
- Adopt the AOM command-bus standard from the same immutable AOM commit in the repository subscription and self-certification; do not copy the optional starter or change unrelated subscriptions.
- Preserve exact child output and exit status, reject missing/conflicting/unsupported modes before target work, make target help useful without performing work, and keep check operations from changing maintained files.
- The canonical gate is fail-fast and orders checks from cheapest to most expensive. Cheap diff, static, repository-contract, format, and lint checks run before test suites or expensive build/test setup when their prerequisites allow; future lint lanes from roadmap row 04 must be ahead of behavioral tests. Aggregate diagnostics remain a manual troubleshooting operation only.
- Keep command-bus and repository-checker implementations and behavior tests under `tools/` and `tools/tests/`; the current `tools/tests/test_run.py` is already correctly placed, and no Python tooling tests belong in .NET application tests.
- Remove only thin/redundant entrypoints: `scripts/ci-preflight.sh`, `scripts/ci-preflight.ps1`, `scripts/image_asset_pipeline.sh`, `scripts/image_asset_pipeline.ps1`, and `scripts/image_asset_pipeline.py`. Retain `scripts/dev-servers.sh`, `scripts/dev-servers.ps1`, and `tools/postgres-dev.ps1` because they perform actual platform/service lifecycle work.
- Retire paired-filename, wrapper-existence, and non-asserting wrapper-invocation tests instead of replacing them with another filename or source-shape detector. Keep behavior proof for the command bus and tracked hook.
- Update durable command ownership in an ADR and maintain the ADR catalogue in the same PR; update all affected human and agent routes and remove stale wrapper instructions.
- Do not add formatter/linter implementation or post-edit formatting hooks; those belong to row 04. Keep current hook checks check-only.
- Use `py -3 tools/run.py ci --check` for final local validation after satisfying its declared PostgreSQL and toolchain prerequisites; do not store test output as a repository receipt.

## Review Focus

1. The published CLI actually refuses missing, conflicting, unknown, and unsupported requests before invoking work, and help, argument forwarding, child output, and exact exit codes are observable behaviors.
2. CI and pre-commit invoke only non-mutating validation; no apply/setup action is hidden under the CI target, and the current AOM hook certification matches the implementation.
3. Retired wrappers have no remaining callers or documentation, while retained native scripts each have a concrete platform or service requirement.
4. Removed tests were shape/existence detectors, and surviving tests establish command-bus or hook behavior without freezing incidental filenames or private check ordering; the gate itself still follows the required cheapest-first, fail-fast order.
5. The new ADR records the durable tools/scripts ownership decision and material wrapper retirement without turning implementation inventories into durable architecture.

---

## Task 1: Bootstrap this successor slice and retire completed documentation custody

**Files:** Create `.agents/plans/2026-10-08-command-bus-and-scripts.md`; modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, and `Directory.Build.props`; delete `.agents/plans/2026-10-08-docs-custody.md`.

- [x] Confirm PR #190 is merged to `develop` at `0d3e181ed0cc0c5df66a671cca2255702b82b4eb` and classify the predecessor's complete documentation-custody scope as delivered.
- [x] Mark roadmap row 02 done with PR #190 evidence, retire its completed plan link, set row 03 executing, and link this plan.
- [x] Set the sole authored application version to `0.1.0-dev.6`.
- [x] Correct the baseline specification's stale AOM provenance: the current command-bus and tracked-hook definitions are at AOM commit `a9d9f280316a87ac66cb2653384bc30a683603f0`; the tracked-hook standard requires candidate-preserving checks and explicit repair operations, not hook-time normalization.
- [x] Commit only this plan/bootstrap, roadmap/spec correction, version advancement, and predecessor retirement before implementation.

## Task 2: Adopt and implement the command-bus CLI contract

**Files:** Modify `tools/run.py`, `tools/tests/test_run.py`, `.agents/contracts/operating-standards.json`, `.agents/contracts/standards-certification.md`, `.agents/contracts/repo-standards-commands.json`, and any canonical command references changed by the selected CLI.

- [x] Add the immutable `command-bus` subscription from AOM commit `a9d9f280316a87ac66cb2653384bc30a683603f0` and keep the existing current `tracked-validation-hook` pin unchanged.
- [x] Implement truthful top-level help, target discovery, target-specific help, explicit mode selection, early rejection of unknown/conflicting/unsupported requests, target argument forwarding, and exact subprocess exit propagation.
- [x] Expose the supported focused .NET, web, Python-tooling, hook-setup, and canonical CI operations as named targets, with modes limited to meaningful behavior; keep CI as a check-only composition and keep local hook installation as an explicitly named apply operation.
- [x] Make failure output identify the failed target/check and the exact focused repair or recheck command without changing the child process output or status.
- [x] Add behavior tests for help without work, missing/invalid/conflicting modes without work, successful dispatch, faithful argument forwarding, child output/status propagation, and expected command failure.
- [x] Remove assertions that freeze the private CI step vector or the old diagnostics command spelling; retain independently meaningful gate behavior at the hook and hosted-CI boundaries.
- [x] Apply the user's decision: the canonical hook/CI gate fails fast, orders checks from cheapest to most expensive, and keeps aggregate diagnostics manual-only. The first executable row-03 checks now include the cheap whitespace check; roadmap row 04 must place lint before tests.
- [x] Self-certify the bus inventory, modes, help, argument/output/status behavior, target ownership, drift controls, and the distinction between mechanical checks and semantic assessment.

## Task 3: Rationalize standalone scripts and test ownership

**Files:** Move the repository checker implementations, their behavior tests, and Python tooling requirements into `tools/`; delete redundant wrappers and obsolete tests; modify `scripts/README.md`, `src/WildBunch.Assets/docs/asset-operations.md`, `.github/workflows/ci.yml`, and any retained guidance with inbound references; preserve native process scripts.

- [x] Remove the CI preflight wrappers because they only launch the canonical bus check.
- [x] Remove the root image-pipeline wrappers and compatibility shim; keep the asset-local Python implementation as the only image-pipeline entrypoint and correct its operation guide.
- [x] Remove `scripts/tests/test_script_entrypoints.py` and `scripts/tests/test_power_shell_wrappers.py` because they preserve paired filenames and wrapper existence without proving useful behavior.
- [x] Move `check_agent_routers.py`, `check_operating_standards.py`, `check_plugin_subscriptions.py`, their retained negative behavior tests, skill-custody behavior tests, and Python tool requirements into `tools/` and `tools/tests/`; preserve hook integration tests with script behavior tests, preserve validator negatives, and remove only the redundant `is_file()` assertion over a glob result.
- [x] Remove weak workflow-string/private-runner-step detectors when the corresponding behavior is already covered by the real command-bus and hook behavior tests; rely on the hosted required status check for live workflow execution.
- [x] Keep the Windows and Bash dev-server implementations only as distinct native process-management implementations, retain the shared PostgreSQL PowerShell lifecycle tool, and document each exception's purpose.
- [x] Rewrite `scripts/README.md` around only surviving operations, remove claims that every script is idempotent/safe, and explain the `tools/` command-bus versus standalone `scripts/` boundary.
- [x] Search repository-wide for removed wrapper names and obsolete direct agent-facing gate commands; update or retire each live reference without rewriting historical investigation statements that clearly identify themselves as historical.

## Task 4: Route the command bus and record its durable ownership

**Files:** Modify root `AGENTS.md`, `README.md`, `CONTRIBUTING.md`, relevant testing/PR guidance, and `docs/decisions/README.md`; add `tools/README.md`, `tools/AGENTS.md`, and the next ADR.

- [x] Make `tools/` immediately discoverable as the command bus in human and agent entrypoints, with concise links to the target inventory and certification.
- [x] Add a thin scoped `tools/AGENTS.md` only if needed to route implementers to the bus guide and relevant unslop/ADR obligations; keep it within the scoped-router contract.
- [x] Record the durable decision that named repository build/test/lint/format/validation work belongs to the bus and that scripts remain for justified standalone/native operations; record the wrapper retirements as dated history without storing a script inventory as architecture.
- [x] Add the ADR to the authored catalogue with a one-sentence summary and correct links/status history for any predecessor decision it clarifies.
- [x] Remove empty headings or empty template sections encountered in touched guidance; do not add a checker that asserts section presence or absence.
- [x] Update the command-bus self-certification and affected playbooks/runbooks so contributors can discover the bus, invoke the correct mode, and preserve the no-mutation hook rule.

## Task 5: Verify and prepare the PR

**Files:** All Task 1-4 changes.

- [x] Run focused command-bus tests and script/hook behavior tests, inspect their output, and correct any failure.
- [x] Run the canonical local check gate with its real prerequisites and confirm the staged-candidate hook leaves the candidate and unrelated working state unchanged.
- [x] Search the final diff for stale command-bus pins, removed wrapper names, direct gate instructions, empty template sections, and inaccurate certification claims.
- [x] Review ADR freshness against the exact diff and update the author decision-record check; the independent reviewer must repeat it.
- [x] Complete a fresh whole-branch code review and address every actionable finding; the reviewer found none, so no post-review fix rerun was needed. Publish PR #191 to `develop` and move it from Draft to Ready after review; the hosted canonical gate passed for head `d8210e2276f84cc93f620760f2b0843493c6b400` in [workflow run 37758195778](https://github.com/HarleyBartles/wild-bunch/actions/runs/37758195778).
- [ ] Keep this plan through its completing PR; the next successor slice will classify it for retirement.
