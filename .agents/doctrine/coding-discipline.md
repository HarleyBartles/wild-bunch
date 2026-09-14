# Coding Discipline

## Code style guidelines

This document defines the code style and coding discipline for Wild Bunch. See the sections below for scope, architecture-stack, and refactoring rules.

Scratch files belong in `Z:\_agent-scratch\wild-bunch\<branch-name>`, never in
the repo root.

## Scope Discipline
- Do only the requested slice.
- No opportunistic broad refactors.
- No unrelated feature work.
- If a needed design decision is missing, return `BLOCKED` or `AMBER` rather than inventing broad architecture.

## Architecture discipline

- [Architecture guardrails](architecture-guardrails.md) and current source own
  Wild Bunch architecture decisions. Portable techniques do not override them.
- Do not move gameplay mutation out of `GameSession` merely to satisfy a generic
  pattern.

## Modular Excitement Doctrine
- Modular player excitement is achieved through boring implementation.
- Build player-facing surprise, variety, and authorship from composable, validated primitives rather than from bespoke adventure chaos.

## Coding Discipline
- Keep slices small and mainline-friendly; if a file is getting bulky, extract the pure helper, factory, or renderer before it becomes a god object.
- Avoid letting aggregate roots, endpoint files, React panels, and builders accumulate unrelated responsibilities.
- Extract pure helpers around aggregate behavior instead of moving its mutation
  boundary.
- Prefer one canonical algorithm or formatter over duplicate versions that can drift.
- When you touch a surface, leave it cleaner or explicitly report why the cleanup is deferred.
- Backend remains authoritative for gameplay state; React renders server state instead of inventing it.
- For deterministic seed, world, or travel behavior, prefer characterization tests before refactoring.
- Current cockpit/debug shell UI sections are temporary scaffolding; do not over-refactor them for their own sake while they remain temporary.
- Real replacement UI/screens should follow the decomposition rules from the cleanup track: focused hooks, small components, backend-authoritative mutation paths, clear command/state boundaries, and reducers only when coupled command-legality state truly warrants them.
