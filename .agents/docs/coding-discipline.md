# Coding Discipline

Use this reference when writing code, deciding scope boundaries, or refactoring.

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
