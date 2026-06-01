# Wild Bunch Testing Posture

Wild Bunch uses a layered testing posture so each change can be proven at the smallest useful level first, then backed up at higher levels when the behavior crosses boundaries.

## Testing Lanes

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

## How To Choose The Smallest Useful Lane

- Start with the smallest lane that can prove the behavior you changed.
- Add higher-level coverage when the behavior crosses an API boundary, a persistence boundary, or a visible UI boundary.
- Keep tests deterministic when the scenario depends on shape-sensitive setup, generated data, or a specific journey/state.
- Avoid relying on incidental default setup when the shape of the state matters to the assertion.
- Keep hidden-truth and public-surface boundaries explicit so tests prove the right thing and do not leak internal state.

## Minimum Acceptable Coverage For New Code

- Domain and application changes need relevant unit coverage, or backend acceptance coverage when the behavior is best proven through the API slice.
- API slice changes need backend acceptance coverage.
- Composed backend workflows need backend integration coverage.
- Frontend rendering or interaction changes need frontend unit or acceptance coverage.
- Visible multi-call UI or game-flow changes need frontend integration coverage or manual browser evidence when automated tests do not fully prove the visible flow.
- Docs-only, tooling-only, or validation-posture changes may use docs checks plus build or smoke checks where appropriate.

## How Manual Browser Evidence Fits

Manual browser evidence is one lane in the evidence model, not the whole model.

- It is useful when you need to prove what a user can see and click in the running app.
- It does not replace unit, acceptance, integration, or provider/storage tests.
- It should be reported separately from automated validation.
- It is especially useful for visible UI/game-flow changes when automated tests do not fully prove the user-facing result.

For the worker-facing operational route, see [UI Browser Check Playbook](../.agents/ui-browser-check-playbook.md).

## Local Run Context

The repo-verified local UI route uses these targets:

- API: `dotnet run --project src/WildBunch.Api --launch-profile http`
- API URL: `http://localhost:5275`
- Web: `npm run dev` from `src/WildBunch.Web`
- Web URL: `http://localhost:5173`

Before using the local browser route, make sure the repo-local PostgreSQL lane is ready:

```powershell
.\scripts\postgres-dev.ps1 install-tools
.\scripts\postgres-dev.ps1 setup
```

The API launch profile already supplies `ConnectionStrings__WildBunchPostgresDb` for the normal local run path.

## References

- [ADR-0022 UI browser checks are a manual evidence lane](adr/ADR-0022-ui-browser-checks-are-a-manual-evidence-lane.md)
- [Wild Bunch Testing Lanes](testing-lanes.md)
- [Local PostgreSQL](local-postgresql.md)
