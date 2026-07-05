# Implementing Guide

Use this reference when implementing work in the Wild Bunch repo — whether as a direct implementer or as a controller dispatching implementer subagents. This guide covers the implementer workflow: what to read before starting, what skills to invoke, and what to verify before claiming done.

## Before You Begin: Read the Standards

Read these standards documents before writing any code. The root `AGENTS.md` lists them under Required Working Knowledge — they are injected into your context automatically, but you must actually read them:

- **[`.agents/docs/coding-discipline.md`](coding-discipline.md)** — scope discipline, architecture stack discipline (DDD/CQRS/event-sourcing is mandatory, not optional), refactoring rules. Read before writing any code.
- **[`.agents/docs/frontend-standards.md`](frontend-standards.md)** — styling stack, play-surface UI, source truth, dev overlay, routing conventions. Read before any frontend work.
- **[`.agents/docs/validation-policy.md`](validation-policy.md)** — test kinds, validation commands, and test quality standards (no flaky tests, real behavior not mocks, pristine output). Read before writing or modifying tests.
- **[`.agents/doctrine/write-tool-phantom-files.md`](../doctrine/write-tool-phantom-files.md)** — the write-tool phantom-file bug on Windows. Read before batch writes. Clean up phantom files before committing.
- **[`.agents/docs/architecture-guardrails.md`](architecture-guardrails.md)** — read before touching GameSession, persistence, or domain logic.

## Skills to Invoke

Invoke these skills before relevant implementation work:

- **Architecture work** (touching domain, persistence, or command/query handlers): `/ddd`, `/cqrs-event-sourcing`, `/event-driven-architecture`, `/clean-architecture`, `/wild-bunch-dotnet-architecture`, `/wild-bunch-domain-modeling`. Do not hand-roll non-DDD, non-CQRS, or non-event-sourced solutions.
- **Frontend work**: `/wild-bunch-browser-game` for browser delivery, Phaser, Vite, or DOM overlays. Apply `.agents/unslop/play-surface-ui.md` for player-facing surfaces.
- **Test writing**: `/test-driven-development` before implementing any feature or bugfix. `/testing` for .NET test infrastructure guidance.
- **Debugging**: `/systematic-debugging` before proposing fixes for any bug, test failure, or unexpected behavior.

## TDD Discipline

When implementing a feature or bugfix:
1. Write a failing test first
2. Verify it fails for the right reason
3. Implement the minimum code to make it pass
4. Verify the test passes
5. Run the full suite to check for regressions

Record TDD evidence in your report: the RED command and failure output, then the GREEN command and passing output.

## Pre-Completion Verification

Before claiming work is done, verify:

- **All tests pass:** `npx vitest run` from `src/WildBunch.Web/` for frontend, `dotnet test` for backend. Run the full suite, not just the focused test.
- **Build succeeds:** `npm run build` for frontend, `dotnet build` for backend.
- **Type-check clean:** `npx tsc --noEmit` for frontend work (no new errors).
- **No flaky tests:** Run the full suite at least once. If a test passes in isolation but fails under full-suite load, it's flaky — fix it before claiming done. See `.agents/docs/validation-policy.md` Test Quality Standards for common causes.
- **Workspace clean:** No phantom files, no stray debug artifacts, no uncommitted scratch files. See `.agents/doctrine/write-tool-phantom-files.md`.
- **INDEX.md regenerated:** If files were added or removed, run `python scripts/generate_index_mesh.py`.
- **No secrets committed:** Check your diff for credentials, API keys, or connection strings.

## PR, Linear, and Plan Honesty

Implementation agents are responsible for keeping PR bodies, Linear issues, and plans honest about the work they contain. This is not optional — it is part of completing the work.

- **PR bodies must be honest.** The PR body must accurately describe what the PR contains — no more, no less. Do not claim work is done if it isn't. Do not omit scope changes, deferred work, or known issues. If the PR's scope diverged from the original plan, the PR body must say so and explain why. If work was deferred, the PR body must flag it and reference the Linear issue tracking the deferral.
- **Linear issues must be updated when scope changes.** If the implementation discovers that the issue's scope needs to expand, shrink, or shift, update the Linear issue to reflect the actual work. Do not silently deliver something different from what the issue requested. If the work is complete but the scope changed, add a comment to the issue explaining the change.
- **Plans must be checked off and committed with the implementation PR.** When execution completes, mark all plan checkboxes (`- [ ]`) as done (`- [x]`) — but only after verifying that the associated plan item was actually delivered in the final PR. Do not mark items complete based on intent or in-progress work. The plan file must be committed with the PR so reviewers can see what was planned vs. what was delivered. See `.agents/superpowers/AGENTS.md` for plan artifact rules.
- **Deferred work must be tracked.** If the implementation encounters a problem that has a cheap fix (under 10 minutes), fix it — do not defer. If the fix is genuinely large, create a Linear issue to track it and reference the issue in the PR body and the task report. Silent deferral is not acceptable.

## When Dispatching Subagents

If you are a controller dispatching implementer subagents:
- Include the relevant standards doc paths in the subagent prompt — the subagent gets the AGENTS.md tree automatically, but calling out the specific docs that apply to the task ensures they read them
- Include the task brief path
- Specify the model explicitly per the SDD skill's Model Selection guidance
- After the subagent completes, check for phantom files in `.worktrees/` and the worktree root before proceeding
