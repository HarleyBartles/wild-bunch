# Validation doctrine

Before work governed by this doctrine, follow the [unslop playbook](../playbooks/unslop.md) to select applicable profiles. Consult the [decision-record playbook](../playbooks/decision-records.md) when work makes, changes, corrects, or materially removes a durable decision.

This file records Wild Bunch test-lane ownership and invariants. The executable
sequence and environment setup live in the [testing playbook](../playbooks/testing.md).

## Repository test lanes

- `WildBunch.Domain.Tests` and `WildBunch.Application.Tests`: isolated domain
  and application behavior.
- `WildBunch.Api.Tests`: API-specific contracts and configuration.
- `WildBunch.GameContent.Tests`: deterministic seed codec, setup pipeline, and
  distribution guardrails.
- `WildBunch.Integration.Tests`: full HTTP and persistence flows against real
  PostgreSQL; user-facing flows live under `Acceptance/`.
- `src/WildBunch.Web`: Vitest component/route behavior, Chromium/Playwright
  native browser behavior, TypeScript checking, and production build.

The Playwright lane owns native browser contracts that JSDOM cannot observe,
including modal focus behavior and rendered viewport geometry. Run it locally
with `npm --prefix src/WildBunch.Web run test:browser` after installing
Chromium with `npx --prefix src/WildBunch.Web playwright install chromium`.
Hosted CI runs `npm ci --prefix src/WildBunch.Web`, then installs Chromium and
its system dependencies with `npx playwright install --with-deps chromium`
from the web project before the tracked commit gate.

Tests follow the source-layer namespace and folder. Repository fixtures and
builders live under `TestInfrastructure/`; fakes and stubs live under
`TestDoubles/`. Seeded generator tests use explicit deterministic seeds rather
than sampling until a desired outcome appears.

Tests rendering `RouterProvider` use `createAppRouter()`, never the shared
router singleton, because TanStack Router retains state between tests. Async
lazy-route assertions use an appropriate explicit wait.

`TestResults/`, `node_modules/`, and other ignored outputs remain excluded.
