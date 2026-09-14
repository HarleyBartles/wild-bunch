# UI browser-check runbook

Use this Wild Bunch overlay after the selected browser or verification skill
owns the portable checking workflow.

## Local launch route

- Ensure shared PostgreSQL: `.\tools\postgres-dev.ps1 ensure`
- Manage this worktree's API and Vite servers with
  `.\scripts\dev-servers.ps1 ensure|status|stop`.
- Canonical API: `http://localhost:5275`
- Canonical web app: `http://localhost:5173`
- Shared PostgreSQL: `localhost:5435`

The helper records process IDs, ports, URLs, worktree path, and branch in
`.local/dev-servers/state.json`. Reuse only a healthy server recorded for the
current worktree. Never stop another worktree's server. When canonical ports
are occupied, use the helper's reported alternate ports and report the actual
URLs.

## Wild Bunch proof

- Prove the frontend reaches the API belonging to the same worktree before
  accepting browser evidence.
- Use a deterministic seed or known scenario when visible behavior depends on
  session state.
- Keep automated validation separate from browser evidence.
- Report the worktree, branch, actual API and frontend URLs, scenario, observed
  result, and any console or network errors.
- Stop worker-started dev servers with `.\scripts\dev-servers.ps1 stop`; leave
  the shared PostgreSQL service running.

The browser-check trigger remains the one recorded in
[ADR-0022](../../docs/adr/ADR-0022-ui-browser-checks-are-a-manual-evidence-lane.md).
