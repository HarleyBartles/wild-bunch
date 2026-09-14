# UI browser-check runbook

## When

Browser behavior, layout, interaction, or player-flow evidence is required.

## Required skills

- `/game-playtest`
- `/wild-bunch-browser-game` when client state authority is in question.
- `/playwright-testing` when the change includes automated browser tests.

## Composition

Resolve state authority when needed, launch the matching worktree services, and
let `/game-playtest` own interactive evidence. Add `/playwright-testing` only
for test implementation or diagnosis.

## Doctrine and contracts

[Frontend standards](../doctrine/frontend-standards.md) and applicable scoped
anti-slop contracts bind the observed surface.

## Local commands and paths

- PostgreSQL: `.\tools\postgres-dev.ps1 ensure` at `localhost:5435`
- Servers: `.\scripts\dev-servers.ps1 ensure|status|stop`
- Canonical API/web URLs: `http://localhost:5275` and `http://localhost:5173`
- Worktree server state: `.local/dev-servers/state.json`

Reuse only healthy state recorded for this worktree. Use helper-reported
alternate ports when canonical ports are occupied. Stop worker-started servers;
leave shared PostgreSQL running.

## Evidence contract

Report worktree, branch, actual URLs, API/worktree match, deterministic scenario,
observed result, and console/network errors separately from automated proof.

## Prohibited combinations

- Do not stop another worktree's server.
- Do not accept a screenshot alone as behavioral proof.
