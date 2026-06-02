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

Use the repo-verified launch path:

- API: `dotnet run --project src/WildBunch.Api --launch-profile http`
- API URL: `http://localhost:5275`
- Web: `npm run dev` from `src/WildBunch.Web`
- Web URL: `http://localhost:5173`

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
