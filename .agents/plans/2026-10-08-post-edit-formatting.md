# Post-Edit Formatting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Format supported files on save in VS Code and synchronously format only files changed by supported Codex tool calls before the agent continues.

**Architecture:** VS Code workspace settings select the repository's C# formatter, Ruff, and local Prettier for format-on-save without enabling lint auto-fixes. A trusted Codex project hook snapshots Git worktree-dirty supported paths before Bash, patch/edit, and MCP calls, hashing only paths already dirty; it records tracked source blob IDs from the index as lightweight baselines. Afterward it detects newly changed paths in either the index or worktree, compares their current blob IDs with the pre-call index baseline, and re-hashes pre-existing worktree-dirty paths. It invokes the existing `tools/run.py format --apply <paths>` operation synchronously for changed paths; it never stages files or runs from pre-commit/CI. Successful formatting returns concise changed-path context; failures return the actual diagnostic as blocking PostToolUse feedback, which replaces the model-visible tool result but cannot undo the completed write.

**Tech Stack:** Python standard library and existing `tools/style.py`/`tools/run.py`; Codex project hooks; VS Code workspace settings; existing .NET, Ruff, and Prettier formatter configurations.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), enforceable engineering policy and lifecycle adapter requirements; [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 04; [ADR-0043](../../docs/decisions/ADR-0043-repository-command-bus-ownership-and-gate-order.md).

**Execution Strategy:** `executing-plans` with Native inline execution. The pre/post hook adapter, format-on-save settings, exact affected-path behavior and trusted-runtime proof share one small interface and must be reviewed together; the user authorized JIT plans executed inline, and a fresh whole-branch review gates the PR.

## Global Constraints

- Start from refreshed `origin/develop` at merged PR #192, commit `68b69448a12be3722548d6af1666af634c186c77`, in the fresh canonical worktree for branch `codex/stable-0.1.0-row-04-post-edit-hooks`.
- Advance the only authored product version in `Directory.Build.props` exactly once from `0.1.0-dev.7` to `0.1.0-dev.8` for this PR; do not add a duplicate manifest version.
- In the first commit, commit this plan, update row 04 and this slice's scope in the baseline specification, record PR #192 evidence, and retire the completed predecessor plan `.agents/plans/2026-10-08-code-style-enforcement.md`. Preserve the parent specification and roadmap through this completing PR.
- Use the existing command bus for all agent-triggered formatting. Invoke `py -3 tools/run.py format --apply <paths>` with explicit relative paths, never a whole-repository formatter pass from a lifecycle hook.
- Keep pre-commit and hosted CI check-only, non-mutating and fail-fast. Do not change staged content, stage files, or run format apply from either gate.
- Support Codex Desktop/CLI project hooks only in this slice. Project-local hooks require the user to review and trust the exact definition; until trusted, the repository still enforces format through explicit command-bus use and the non-mutating commit/CI gates.
- Match shell, patch/edit and MCP tool calls. Use `git status --porcelain=v1 -z` to identify changed paths, hash only supported paths already worktree-dirty before the call, and compare those hashes after the call. Record tracked source index blob IDs before the call so newly changed tracked files, including files edited and staged in one tool call, are candidates only when their worktree bytes differ from the pre-call baseline; include newly created untracked source files. Ignore index-only transitions with unchanged worktree bytes. The snapshot must isolate changes made by one tool invocation, including edits to files already dirty before that invocation; never classify every existing dirty file as agent-edited.
- The adapter may format only existing repository-supported source paths selected by `tools/style.py`; ignore deletions, Markdown, generated, vendor, dependency and build outputs. Preserve paths with spaces and use subprocess argument vectors, not shell reconstruction.
- VS Code workspace settings enable formatting only for C#, Python, JavaScript, TypeScript, TSX and SCSS. Do not enable lint fixes or import reorganization on save; semantic lint corrections remain deliberate edits.
- Codex documents `session_id`, `cwd`, `hook_event_name` and `tool_use_id` in the event; synchronous command hooks wait by default, and `PostToolUse` can return `decision: "block"` plus `reason` and `additionalContext` to replace the model-visible tool result after the side effect has happened. The project hook requires trust and cannot undo that side effect. See [Codex Hooks](https://developers.openai.com/codex/hooks).
- VS Code documents `editor.formatOnSave` as an opt-in setting and supports workspace language-specific defaults; `.editorconfig` alone does not trigger formatting. See [VS Code formatting](https://code.visualstudio.com/docs/editing/codebasics) and [workspace settings](https://code.visualstudio.com/docs/configure/settings).

## Review Focus

- A changed file that was already dirty before a tool call is still detected when that tool edits it, while unrelated dirty files are left untouched; prove this with a two-file before/after behavior test.
- Clean tracked edits are detected whether left unstaged or staged by the same tool call, while index-only changes to unchanged worktree bytes are ignored.
- A successful patch, shell write and MCP write returns only after the supported changed file has been formatted and the changed-path feedback is available to the agent; prove each route through the hook adapter.
- A failed formatter reports the actual bus diagnostic and a safe explicit-path recovery route without interpolating hostile filenames into executable shell syntax or claiming that the preceding write was undone; prove this with a failing child-process fixture.
- A synchronous hook never stages or changes the index; prove index identity before and after formatting a staged source file.
- A Codex hook trust gate or missing snapshot leaves an explicit, truthful recovery route and cannot silently format the whole worktree; prove the missing-snapshot failure path.

---

## Task 1: Bootstrap the successor slice and retire the completed style plan

**Files:** Create `.agents/plans/2026-10-08-post-edit-formatting.md`; modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, and `Directory.Build.props`; delete `.agents/plans/2026-10-08-code-style-enforcement.md`.

- [x] Verify PR #192 is merged to `develop` at `68b69448a12be3722548d6af1666af634c186c77`, its reviewed head is `3990eca7c9c6954badea9f5bc4719495be04ded2`, and hosted run `37777709733` passed on that head.
- [x] Compare the completed predecessor plan's full scope with PR #192's merged tree and validation/review evidence; preserve the durable code-style rules already moved into their current owners, then retire the completed plan and replace the stale row 04 link.
- [x] Record that PR #192 delivered `0.1.0-dev.7`; mark the next per-PR version reservation as `0.1.0-dev.8` and make no other version change.
- [x] Record the settled initial support boundary: VS Code workspace format-on-save plus Codex Desktop/CLI synchronous post-tool formatting; unsupported agent runtimes retain explicit command-bus formatting and check-only commit/CI enforcement.
- [x] Commit the plan, version, specification, roadmap and predecessor retirement through the normal check-only hook before implementation begins.

## Task 2: Prove per-tool source-change detection and synchronous formatting

**Files:** Add `.codex/hooks/format_after_edit.py`; add focused behavior tests in `tools/tests/test_format_after_edit_hook.py`.

**Interface:** `main() -> int` decodes the Codex event from stdin and calls `handle_event(event: dict[str, Any]) -> int`. `PreToolUse` stores hashes only for supported worktree-dirty paths and records tracked source index blob IDs, keyed by repository, `session_id` and `tool_use_id`, under the system temporary directory. `PostToolUse` finds currently changed supported paths in the index or worktree, compares newly changed tracked files with their pre-call index blob IDs, includes newly created untracked files, and hashes only paths present in both worktree-dirty snapshots to detect edits to pre-existing dirty files. This detects a source edit staged by the same tool call while excluding index-only transitions with unchanged worktree bytes. It removes the snapshot and invokes the command below with `cwd` set to the repository root. On success it emits `hookSpecificOutput.additionalContext` with changed paths; on failure or missing/mismatched snapshot it emits `decision: "block"` with an accurate recovery instruction and diagnostic. Recovery paths are presented as a JSON array to pass as separate arguments, not interpolated into executable shell syntax. A block replaces the model-visible tool result but cannot reverse the tool side effect.

```python
command = [sys.executable, str(root / "tools/run.py"), "format", "--apply", *changed_paths]
result = subprocess.run(command, cwd=root, capture_output=True, text=True, check=False)
```

- [x] Write behavior tests using a temporary Git repository and fake bus process: an edit to one already-dirty source file formats that file before `main` returns while an unrelated dirty source file stays byte-identical.
- [x] Run the focused test and confirm RED because the adapter does not exist.
- [x] Add tests for new supported files, paths with spaces, deleted files, Markdown/generated exclusions, no-change tool calls, and shell/patch/MCP events using the same before/after contract.
- [x] Add tests proving the exact argument-vector bus invocation does not stage or change the Git index and that an absent, expired or mismatched snapshot reports a focused explicit-format command instead of guessing scope.
- [x] Add a child-process failure case proving Codex receives `decision: "block"`, the actual formatter diagnostic and an explicit-format recovery command, while the feedback truthfully says the write already occurred.
- [x] Use a status-backed snapshot: include clean tracked edits and new untracked files, detect changes to pre-existing dirty files, exclude unrelated dirty files, and ignore index-only transitions with unchanged worktree bytes.
- [x] Detect a clean tracked file edited and staged in the same tool invocation, and keep index-only transitions with unchanged worktree bytes out of formatter candidates.
- [x] Keep paths with shell metacharacters out of executable recovery command strings while returning an actionable command-bus route and exact affected-path data.
- [x] Implement only the snapshot comparison, per-session/tool state cleanup, explicit changed-path command-bus call and concise `PostToolUse` result; do not add a new formatter implementation or lifecycle framework.
- [x] Run the focused tests and verify they pass, including a test that reads the formatted file immediately after the synchronous hook returns.

## Task 3: Configure the supported Codex hook and VS Code save formatters

**Files:** Add `.codex/hooks.json`, `.vscode/settings.json`, and `.vscode/extensions.json`; add narrow `.gitignore` exceptions for the repository-owned Codex hook definition and script; modify `.agents/playbooks/code-style.md`.

- [x] Configure `PreToolUse` and synchronous `PostToolUse` command hooks for Bash, `apply_patch`/Edit/Write aliases and MCP calls; use a POSIX command plus `commandWindows` for the correct Python launcher, resolve the script from the Git root, and keep hooks in `.codex/hooks.json` beside the existing project `config.toml` rather than adding a second inline representation.
- [x] Configure per-language VS Code `editor.formatOnSave` and the selected formatter IDs for C#, Python, JavaScript, TypeScript, TSX and SCSS; recommend only the matching C#, Ruff and Prettier extensions and leave Markdown, lint fixes and import sorting out of save actions.
- [x] Update the code-style playbook to identify the bus as the canonical agent repair route, explain that Codex hooks require exact-definition trust and only supported runtimes autoformat, and preserve explicit bus formatting for other runtimes.
- [x] Validate JSON and TOML syntax and inspect the workspace formatter mapping against `.editorconfig`, `pyproject.toml`, `.prettierrc.json` and the existing project dependencies; run the adapter with synthetic Codex event payloads without bypassing the real hook trust prompt.
- [x] Run the adapter behavior tests and focused Python format/lint checks; verify no hook runs a mutating formatter during pre-commit or CI and no test freezes only hook-config strings.

## Task 4: Validate and prepare the PR

**Files:** All Task 1-3 changes; update the code-style implementation plan only if review identifies a real scope discrepancy before publication.

- [x] Run the focused hook tests, `py -3 tools/run.py ci --check`, and `git diff --check`; confirm the installed extension settings do not add lint auto-fixes or format Markdown.
- [x] Search all changed documentation and code for stale claims that `.editorconfig` formats files by itself, that the hook covers unsupported runtimes, or that pre-commit/CI may mutate files.
- [x] Compare the diff with ADR-0043 and the decision-record playbook; record why the adapter preserves command-bus ownership and introduces no new durable architectural decision.
- [ ] Commit the implementation and guidance through the normal check-only hook, then perform a fresh whole-branch review and fix/re-review all actionable findings, including the performance finding from the initial review.
- [ ] Publish/update a Draft PR to `develop`, advance to Ready only after review and local validation pass, and verify the exact remote head and successful hosted canonical gate before merge.
- [ ] Keep this plan and its governing specification through the completing PR; the next substantive successor slice classifies them for retirement.
