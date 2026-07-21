# Coding Discipline

Use this reference when writing code, deciding scope boundaries, or refactoring.

**Artifact Placement**: Before creating any files (scratch notes, code reviews, temporary documents), read `.agents/docs/artifact-policy.md` for guidance on where to place agent-generated artifacts. Scratch files must go in `Z:\_agent-scratch\wild-bunch\<branch-name>`, never in the repo root.

## Scope Discipline
- Do only the requested slice.
- No opportunistic broad refactors.
- No unrelated feature work.
- If a needed design decision is missing, return `BLOCKED` or `AMBER` rather than inventing broad architecture.

## Architecture Stack Discipline
- This repo uses DDD + CQRS + Event Sourcing. These are the established patterns, not one option among many.
- Do not hand-roll non-DDD solutions: no anemic domain models, no services that mutate aggregates from outside, no bypassing the aggregate root for gameplay mutations.
- Do not hand-roll non-CQRS solutions: no mixing read logic into command handlers, no mixing mutation logic into query handlers, no scraping aggregate state for reads instead of using projections.
- Do not hand-roll non-event-sourced solutions: no direct state mutations outside the event-sourced route, no skipping `Apply` methods, no adding mutable state that isn't restored from events.
- When unsure how to apply these patterns, invoke the `/ddd`, `/cqrs`, `/event-sourcing`, `/event-driven-architecture`, `/wild-bunch-dotnet-architecture`, and `/wild-bunch-domain-modeling` skills. They are the reference sources for correct patterns, not the repo's current code (which may have legacy residue).
- Do not assume the repo's current code is the pattern to follow. The skills and ADRs are the authority. If code and skills disagree, the skills win.
- Do not create placeholder state. If a value is not yet known, make the field nullable and set it when the value becomes known. Placeholder values that get silently replaced are an anti-pattern.

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
