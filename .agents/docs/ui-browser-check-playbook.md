# UI Browser Check Playbook for Workers

This is the worker-facing browser-check route for Wild Bunch. Use it when a dispatch needs visible UI/game-flow evidence and do not restate setup from scratch unless the local route changed.

For policy and coverage expectations, point human readers to [Wild Bunch Testing Posture](../docs/testing-posture.md).

## Trigger Policy

Require or request a browser check when:

- Harley explicitly asks for browser verification.
- The dispatch changes visible UI or a game-flow path and automated tests do not fully prove the user-facing result.
- The bug or closeout concern depends on what a user can see or click in the running app.
- The dispatch changes local run setup, frontend/backend integration, or UI routing.

Treat browser checks as optional or skip them when:

- The change is backend-only and has no visible UI surface.
- The change is docs, tooling, or test infrastructure.
- The change already has enough automated validation and no visible flow changed.

Browser checks are not a replacement for unit, acceptance, integration, or provider/storage tests. Do not make them the default requirement for every backend-only dispatch.

## Verified Local Route

Use the repo-verified launch path. These are the canonical ports for browser proof — determine them from `launchSettings.json` and `vite.config.ts`/`package.json` before starting any server.

- API: `dotnet run --project src/WildBunch.Api --launch-profile http`
- API URL: `http://localhost:5275` (from `Properties/launchSettings.json`, `http` profile `applicationUrl`)
- Web: `npm run dev` from `src/WildBunch.Web`
- Web URL: `http://localhost:5173` (from `vite.config.ts` `server.port`)
- PostgreSQL: `localhost:5434` (repo-local shared service, via `.\scripts\postgres-dev.ps1 ensure`)

### Dev servers are worktree-owned

Wild Bunch often has parallel workers in isolated git worktrees. Dev servers (API and Vite) are coupled to the worktree source tree they were started from — a server running from one worktree is not proof for a different worktree's branch.

Treat dev servers like the PostgreSQL service (`ensure if down, use if up`), with one critical difference: **reuse is only safe within the same worktree**. The PostgreSQL service is shared across all worktrees; dev servers are not.

Before starting API or Vite, check whether a healthy server is already running for the current worktree:

1. Identify the current worktree path (e.g. `git rev-parse --show-toplevel`).
2. Check whether a process for the canonical port is rooted in the current worktree. Use `Get-NetTCPConnection -LocalPort <port> -State Listen` to get the PID, then `Get-Process -Id <pid>` and inspect the command line / path to determine which worktree it belongs to.
3. If a healthy server is already running for the same worktree, **reuse it** and leave it up. Do not start a second server.
4. If the current worktree's own recorded PIDs are stale or dead (process no longer exists, port not listening), clean them up and start fresh.
5. If the canonical port is occupied by a **different worktree's** server, **do not kill it** — parallel agents may legitimately have their own servers. Instead, allocate a non-conflicting port pair (see below) and report the actual URLs.
6. Never start a second unusable server on an occupied port. If the port is taken, either reuse the same-worktree server or move to a free port — do not blindly start and then discover the collision at screenshot time.

### Port allocation when canonical ports are occupied by another worktree

When the canonical API port (`5275`) or Vite port (`5173`) is occupied by a different worktree's healthy server:

- Allocate the next free port pair (e.g. API `5276`, Vite `5174`). Keep the API and Vite ports within one of each other for readability.
- For the API: pass `--urls http://localhost:<port>` to `dotnet run` and set `ConnectionStrings__WildBunchPostgresDb` in the same process.
- For Vite: Vite auto-increments when the configured port is busy, but the CORS policy in `src/WildBunch.Api/DependencyInjection.cs` only allows `localhost:5173` and `127.0.0.1:5173`. If you use a non-canonical Vite port, either:
  - Set `VITE_API_BASE_URL` to point the frontend at your worktree's API port (bypassing the Vite proxy), and ensure the API's CORS policy allows the Vite origin, or
  - Add the Vite port to the CORS allowed origins for the duration of the browser check (do not commit this change).
- Report the actual API URL and frontend URL in the browser-proof return. Do not claim canonical-port evidence when an alternate port was used.

### Canonical topology is binding when ports are free

- Browser proof must use the canonical dev topology (API `localhost:5275`, Vite `localhost:5173`, PostgreSQL `5434`) when those ports are free.
- When canonical ports are occupied by another worktree's healthy server, alternate ports are allowed **if and only if** the worker reports the actual URLs, worktree path, and branch in the browser-proof return. Silent fallback is not allowed.
- Screenshots from a miswired frontend/API setup (e.g. frontend pointing at a different worktree's API, or CORS blocking the request) do not count as evidence. Before taking screenshots, prove the frontend can reach the API through the configured base URL (e.g. the prologue or another known endpoint returns real data through the browser's fetch path).
- Do not use a server from a different worktree as proof for this branch. Browser proof must exercise the code in the worker's current worktree.

If the persistent PostgreSQL lane is not already ready, set it up first:

```powershell
.\scripts\postgres-dev.ps1 install-tools
.\scripts\postgres-dev.ps1 setup
```

The API launch profile already injects `ConnectionStrings__WildBunchPostgresDb` for the normal local route.

## Worker Checklist

- Launch the backend and frontend locally.
- Open the web app in a browser at the verified URL.
- Exercise the visible workflow end to end.
- Confirm the user-facing state matches the expected result.
- Inspect console and network errors when the flow is interactive.
- Use a deterministic seed or known scenario when the workflow depends on session state.
- Record any worker-started long-running helpers you launched for the check, including API servers, Vite dev servers, browsers, test servers, watchers, tunnels, containers, preview servers, or browser kernels.
- Stop or otherwise account for those worker-owned helpers before returning `GREEN`.
- If you started port-bound helpers, verify every port used during the check is no longer listening, including alternate Vite dev/preview ports such as `4173`, or explain why a remaining listener is not worker-owned.
- Close browser tabs or windows created for the check when the environment exposes that action.
- If the check touched `C:/WORK/**`, include repo/file-lock posture in the cleanup evidence. Use handle evidence when available; if handle tooling is unavailable, say so and provide process/command-line fallback proof.
- If you cannot clean up worker-owned helpers safely, return `AMBER` or `BLOCKED` with exact process, port, browser, and repo-lock evidence.

## Reporting Contract

Keep automated and manual evidence separate.

### Automated Validation

Report build, unit, acceptance, integration, and provider/storage results in their own section. Do not fold them into browser evidence.

### Manual Browser Evidence

Report the following fields when a browser check is performed:

- backend command used
- frontend command used
- environment/database configuration used
- worktree path (e.g. `C:/WORK/repo-workspace/wild-bunch/.worktrees/bunch-75-exec`)
- branch
- API URL actually used (e.g. `http://localhost:5275` or `http://localhost:5276` if canonical port was occupied)
- frontend URL actually used (e.g. `http://localhost:5173` or `http://localhost:5174`)
- whether each server was reused (already running for this worktree) or freshly started
- browser URL opened
- seed/scenario/session used, if any
- workflow exercised
- expected result
- observed result
- console/network errors checked, if applicable
- cleanup evidence for any worker-started long-running helpers
- screenshots only when useful
- final status: passed, failed, skipped, or blocked

### Cleanup Evidence

When long-running helpers were started for the check, include a cleanup proof block with:

- started helpers: process id, process name, command line, and port when applicable
- stopped helpers: process id, process name, command line, and stop result
- post-cleanup process scan for likely worker-owned server, browser, watcher, and test-helper processes rooted in the repo or validation command line
- post-cleanup port scan for every port used during validation, not just the default API/web ports
- repo/file-lock posture for `C:/WORK/**` validation, including handle-tool result when available or a stated fallback when unavailable
- browser tabs or windows closed
- remaining known worker-owned processes, if any

Do not return `GREEN` with only a bare claim such as "helpers stopped" or "ports clean". If a later user finds a worker-owned helper from the validation run after `GREEN`, the cleanup lane was false-green even if the product behavior was correct.

### Lawful Skip

If browser verification is not required or cannot be performed, say so plainly and explain why. Do not mark a skipped browser check as passed.

## Return Language

Prefer concise, source-backed language in worker returns:

- `browser check passed` when the visible flow was exercised and observed to behave correctly.
- `browser check skipped` when the lane was not required for this dispatch.
- `browser check blocked` when the browser, backend, or local setup was unavailable.

Keep that statement separate from `dotnet build`, `dotnet test`, and any frontend test results.

## References

- [ADR-0022 UI browser checks are a manual evidence lane](../docs/adr/ADR-0022-ui-browser-checks-are-a-manual-evidence-lane.md)
- [Wild Bunch Testing Posture](../docs/testing-posture.md)
- [Wild Bunch Testing Lanes](../docs/testing-lanes.md)

## Dev-server helper script

Use `.\scripts\dev-servers.ps1` to manage worktree-owned dev servers. It automates the worktree-owned posture above:

- `.\scripts\dev-servers.ps1 ensure` — checks whether a healthy API + Vite pair is already running for the current worktree (via a worktree-local state file at `.local/dev-servers/state.json`). If so, reuses them and prints the actual URLs. If the state file is stale (PIDs dead), cleans up and starts fresh. If canonical ports are occupied by another worktree, allocates non-conflicting fallback ports and prints the actual URLs.
- `.\scripts\dev-servers.ps1 status` — prints the current worktree's recorded server state (PIDs, ports, URLs, health).
- `.\scripts\dev-servers.ps1 stop` — stops this worktree's servers and clears the state file. Does not touch other worktrees' servers.

The script records PIDs, ports, URLs, worktree path, and branch in the state file so browser-proof returns can cite exact evidence. Prefer this script over manual port management.
