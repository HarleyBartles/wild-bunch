# AGENTS.md

## Project
- Wild Bunch is a C#/.NET Western adventure game in `HarleyBartles/wild-bunch`.
- This repo is mainline-only.
- Docs index: `docs/INDEX.md`
- Required working knowledge for architecture-sensitive work: `.agents/INDEX.md`, `.agents/architecture-hygiene.md`

## Mainline-only Rule
- Final accepted work must be on `main`.
- Temporary worker branches are only execution surfaces.
- Do not return `GREEN` from branch-only work.
- If pushing `main` is blocked, return `AMBER` or `BLOCKED` with exact branch/commit evidence and the reason.

## Source of Truth
- Current repo state is the source of truth.
- Worker reports, issue comments, conversation summaries, and session notes are not proof.
- Always report exact branch, commit, remote head, and changed files.

## Connector / Tool Safety
- Read-only verification must stay read-only.
- Do not call GitHub mutation tools while inspecting repo state, reassessing an issue, or preparing a dispatch.
- Treat tools named `create_*`, `update_*`, `delete_*`, `add_*`, `remove_*`, `lock_*`, `unlock_*`, or low-level Git primitives such as `create_tree` / `create_commit` as mutation routes.
- `create_tree` is not a repo-listing tool. If a tree/listing read route is unavailable, use `fetch_file`, `fetch`, `search`, `compare_commits`, issue readers, and commit/status readers instead.
- Workers do not close GitHub issues; they only return source-backed closeout evidence and recommendations.

## Validation
- Run `dotnet build`.
- Run `dotnet test`.
- Run `dotnet tool restore` before EF validation commands when the repo-local tool manifest is used.
- Run `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api` when persistence may be affected, or as standing validation unless clearly irrelevant.
- Report warnings separately from failures.

## Testing Posture
- New or updated real application behavior should normally include test coverage in the same slice.
- If coverage is skipped, state the reason explicitly and keep the gap narrow and deliberate.
- Debug-only or temporary prototype surfaces, including the current cockpit/debug shell, may use lighter-weight coverage while they remain debug-only.

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
- Travel, journey, and encounter DTO mapping should live in `TravelMapper`; `GameSessionMapper` should delegate rather than duplicate that shape.
- Wallet and Inventory are concrete player state; avoid generic supplies.
- Hidden culprit truth remains internal.
- Clue, journal, and wanted-poster flows stay stable unless directly in scope.
- Horse and saddle are separate inventory concepts.
- Mounted travel requires a living/non-lame horse plus saddle.
- Travel advances one trail day at a time; do not reintroduce instant multi-day travel.
- Keep temporary cockpit/debug-shell UI light; do not spend architecture cleanup effort polishing it for its own sake.

## Persistence / Model Posture
- POCO domain models are fine when they keep the domain plain, composable, and naturally serializable.
- Do not couple domain models to EF/table shape.
- Runtime session persistence is JSON snapshot-oriented today.
- Snapshot codecs belong in `WildBunch.Persistence` and should be split by coherent domain area when they get unwieldy.
- Do not normalize runtime session state into many DB tables unless explicitly directed.
- Persistence adapters may map the domain to JSON now and tables later without forcing domain refactors.
- In this greenfield repo, current mainline model correctness wins over old-save or legacy internal compatibility.
- Dev database drop/recreate is allowed when a current snapshot or schema shape changes and a reset is the cleanest path.
- Do not add compatibility shims for obsolete old saves or internal models unless Harley explicitly asks for one.
- Serializer optionality should exist only for current-domain reasons, not as a default legacy-save support layer.
- When a task calls for replacement, fully replace the old internal model instead of layering a compatibility adapter over it.
- Repo-local database artifacts should live under repo-root `.local/`, never under `src/`.

## Scope Discipline
- Do only the requested slice.
- No opportunistic broad refactors.
- No unrelated feature work.
- If a needed design decision is missing, return `BLOCKED` or `AMBER` rather than inventing broad architecture.

## Modular Excitement Doctrine
- Modular player excitement is achieved through boring implementation.
- Build player-facing surprise, variety, and authorship from composable, validated primitives rather than from bespoke adventure chaos.

## Coding Discipline
- Keep slices small and mainline-friendly; if a file is getting bulky, extract the pure helper, factory, or renderer before it becomes a god object.
- Avoid letting aggregate roots, endpoint files, React panels, and builders accumulate unrelated responsibilities.
- Do not move gameplay mutation out of `GameSession` just to satisfy SOLID; extract pure helpers around it instead.
- Prefer one canonical algorithm or formatter over duplicate versions that can drift.
- When you touch a surface, leave it cleaner or explicitly report why the cleanup is deferred.
- Backend remains authoritative for gameplay state; React renders server state instead of inventing it.
- For deterministic seed, world, or travel behavior, prefer characterization tests before refactoring.
- Current cockpit/debug shell UI sections are temporary scaffolding; do not over-refactor them for their own sake while they remain temporary.
- Real replacement UI/screens should follow the decomposition rules from the cleanup track: focused hooks, small components, backend-authoritative mutation paths, clear command/state boundaries, and reducers only when coupled command-legality state truly warrants them.

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
