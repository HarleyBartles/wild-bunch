# UI browser-check playbook

## When

Browser behavior, layout, interaction, or player-flow evidence is required.

## Required capabilities

- Run browser playtests and automated browser checks when the task calls for them.

## Required repository-owned skills

- wild-bunch-browser-game (when client state authority is in question).

## Composition

1. Use `/wild-bunch-browser-game` when the test requires a decision about
   server, client, or presentation ownership.
2. Start PostgreSQL and this worktree's API/web servers with the helpers below;
   prove the browser points at the matching worktree API.
3. Use interactive gameplay browser validation with a deterministic scenario to exercise behavior,
   layout, and interaction while recording console/network state.
4. Add automated browser test implementation only when implementing or diagnosing automated
   browser coverage, and keep that result separate from manual playtest proof.
5. Stop only this worktree's worker-started servers after evidence is captured.

## Doctrine and contracts

[Frontend standards](../doctrine/frontend-standards.md) binds the observed surface. Before browser checks, read [play-surface UI guards](../unslop/play-surface-ui.md) in full; dev diagnostics also require [dev-overlay guards](../unslop/dev-overlay.md). Use [backend guards](../unslop/backend-architecture.md) when evaluating API truth and [the observation loop](../unslop/README.md#record-and-improve) for distinct failures and guard outcomes.

## Local commands and paths

- PostgreSQL: `.\tools\postgres-dev.ps1 ensure` at `localhost:5435`
- Servers: `.\scripts\dev-servers.ps1 ensure|status|stop`
- Canonical API/web URLs: `http://localhost:5275` and `http://localhost:5173`
- Worktree server state: `.local/dev-servers/state.json`

Reuse only healthy state recorded for this worktree. Use helper-reported
alternate ports when canonical ports are occupied. Stop worker-started servers;
leave shared PostgreSQL running.

## Evidence contract

- [ ] Worktree, branch, actual API URL, and actual web URL are recorded.
- [ ] The frontend-to-API worktree match is proven.
- [ ] The deterministic scenario and observed interaction result are recorded.
- [ ] Console/network errors and automated test results are reported separately.
- [ ] Only this worktree's worker-started servers were stopped.

## Prohibited combinations

- Do not stop another worktree's server.
- Do not accept a screenshot alone as behavioral proof.

## Runbook routing

- [Implementing](../runbooks/implementing.md)
- [Code review](../runbooks/code-review.md)
- [Decision records](decision-records.md) - when player-visible interaction or browser evidence policy changes durably.
