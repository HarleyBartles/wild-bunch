# UI Browser Check Playbook

This playbook is the human-facing route for local UI browser checks in Wild Bunch. It keeps browser evidence separate from automated validation and gives you a repeatable way to exercise visible game flows.

## When To Use It

Use a browser check when:

- Harley explicitly asks for browser verification.
- The change touches visible UI or a game-flow path and automated tests do not fully prove the user-facing result.
- The bug or closeout concern depends on what a user can see or click in the running app.
- The work changes local run setup, frontend/backend integration, or UI routing.

You can usually skip a browser check when:

- The change is backend-only and has no visible UI surface.
- The change is docs, tooling, or test infrastructure.
- The change already has adequate automated validation and no visible flow changed.

## Validation Taxonomy

Keep these lanes separate when you report results.

### Backend

- `unit`: fast isolated tests for domain, application, aggregate rules, handlers, mappers, policies, value objects, and small services.
- `acceptance`: one API slice through the backend boundary from request in to response out.
- `integration`: composed backend workflows across multiple API calls, such as start game, inspect actions, travel, or resolve encounters.
- `manual`: local exploratory backend verification using debug mode, breakpoints, curl, HTTP files, Swagger, or Postman.

### Frontend

- `unit`: Vitest-level tests for frontend functions, hooks, small components, formatters, state helpers, and UI logic.
- `acceptance`: a rendered frontend slice against an API contract or a small group of API calls.
- `integration`: larger orchestrated frontend workflows and rendered areas across multiple API calls.
- `manual`: run both backend and UI projects locally and exercise the workflow as a user would in a prod-like scenario.

## Local Run Route

The repo already verifies these local launch targets:

- API launch profile: `dotnet run --project src/WildBunch.Api --launch-profile http`
- API URL: `http://localhost:5275`
- Web dev server: `npm run dev` from `src/WildBunch.Web`
- Web URL: `http://localhost:5173`

Before you launch the API locally, make sure the repo-local PostgreSQL lane is ready:

```powershell
.\scripts\postgres-dev.ps1 install-tools
.\scripts\postgres-dev.ps1 setup
```

The API launch profile already supplies `ConnectionStrings__WildBunchPostgresDb` for the normal local run path, so you should not need to export it manually for the standard setup.

## Browser Check Workflow

1. Start the backend with the verified API launch profile.
2. Start the web client dev server.
3. Open `http://localhost:5173` in your browser.
4. Use a known scenario or deterministic seed when the workflow depends on session state.
5. Exercise the visible UI/game-flow path end to end.
6. Check for console errors and obvious network failures if the workflow is interactive.
7. Record the expected result and the observed result separately.

## Checklist

- Confirm the app loads at the expected URL.
- Confirm the relevant control is visible and usable.
- Confirm the click or action changes the UI the way the workflow expects.
- Confirm the backend state change is reflected back in the UI when that is part of the flow.
- Confirm there are no unexpected console or network errors.
- Capture a screenshot only when it helps explain the result.

## Reporting Format

Report browser checks separately from automated validation. A useful closeout block looks like this:

```text
Manual browser check:
- backend command:
- frontend command:
- environment/database:
- browser URL:
- seed/scenario/session:
- workflow exercised:
- expected result:
- observed result:
- console/network errors:
- screenshot:
- final status: passed | failed | skipped | blocked
```

## Lawful Skip Language

If you skip the browser check, say why clearly:

- `skipped` when the lane was not required for this change.
- `blocked` when local setup, the browser, or the backend was unavailable.

Do not imply a skipped browser check passed. Keep the manual lane separate from the automated test results.

## References

- [ADR-0022 UI browser checks are a manual evidence lane](adr/ADR-0022-ui-browser-checks-are-a-manual-evidence-lane.md)
- [Wild Bunch Testing Lanes](testing-lanes.md)
- [Local PostgreSQL](local-postgresql.md)
