# AGENTS.md

## Project
- Wild Bunch is a C#/.NET Western adventure game in `HarleyBartles/wild-bunch`.
- This repo is mainline-only.

## Mainline-only Rule
- Final accepted work must be on `main`.
- Temporary worker branches are only execution surfaces.
- Do not return `GREEN` from branch-only work.
- If pushing `main` is blocked, return `AMBER` or `BLOCKED` with exact branch/commit evidence and the reason.

## Source of Truth
- Current repo state is the source of truth.
- Worker reports, issue comments, conversation summaries, and session notes are not proof.
- Always report exact branch, commit, remote head, and changed files.

## Validation
- Run `dotnet build`.
- Run `dotnet test`.
- Run `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api` when persistence may be affected, or as standing validation unless clearly irrelevant.
- Report warnings separately from failures.

## GREEN Standard
- `GREEN` requires implementation, validation, publication to `main`, remote head proof, a clean worktree, and issue-goal conformance.
- Passing tests alone is not `GREEN`.
- A commit existing is not `GREEN`.
- A branch push is not `GREEN` in this repo.

## Issue-Goal Conformance
- Restate the task as observable repo state.
- Run falsification checks.
- Compare claim vs observed state.
- Do not close or claim closure unless explicitly asked and fully proven.

## Architecture Guardrails
- `GameSession` is the live-play aggregate root.
- Game mutations should flow through `GameSession` or the established aggregate route.
- Wallet and Inventory are concrete player state; avoid generic supplies.
- Hidden culprit truth remains internal.
- Clue, journal, and wanted-poster flows stay stable unless directly in scope.
- Horse and saddle are separate inventory concepts.
- Mounted travel requires a living/non-lame horse plus saddle.
- Travel advances one trail day at a time; do not reintroduce instant multi-day travel.

## Persistence / Model Posture
- POCO domain models are fine when they keep the domain plain, composable, and naturally serializable.
- Do not couple domain models to EF/table shape.
- Runtime session persistence is JSON snapshot-oriented today.
- Do not normalize runtime session state into many DB tables unless explicitly directed.
- Persistence adapters may map the domain to JSON now and tables later without forcing domain refactors.
- In this greenfield repo, current mainline model correctness wins over old-save or legacy internal compatibility.
- Dev database drop/recreate is allowed when a current snapshot or schema shape changes and a reset is the cleanest path.
- Do not add compatibility shims for obsolete old saves or internal models unless Harley explicitly asks for one.
- Serializer optionality should exist only for current-domain reasons, not as a default legacy-save support layer.
- When a task calls for replacement, fully replace the old internal model instead of layering a compatibility adapter over it.

## Scope Discipline
- Do only the requested slice.
- No opportunistic broad refactors.
- No unrelated feature work.
- If a needed design decision is missing, return `BLOCKED` or `AMBER` rather than inventing broad architecture.

## Worker Environment
- The worker environment uses PowerShell, so do not use `&&` for command chaining.
- Run commands separately or use PowerShell-safe sequencing when multiple commands are needed.

## Return Format
- Status: `GREEN` | `AMBER` | `RED` | `BLOCKED`
- Branch
- Final main commit hash
- Remote main head hash
- Changed files
- Validation commands and results
- Clean worktree status
- Issue-goal conformance notes
- Known caveats or next recommended slice
