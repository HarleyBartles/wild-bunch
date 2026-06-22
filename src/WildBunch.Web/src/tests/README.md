# Frontend test conventions

## Location

All Vitest frontend tests live under `src/tests/`, not beside production code.

```
src/tests/
  *.test.tsx        # component and integration tests
  *.test.ts         # unit tests (api, codecs, utilities)
  test-utils/
    setup.ts        # Vitest setup file (jest-dom matchers)
    renderHelpers.tsx  # shared render helpers (QueryClient, GameSessionProvider)
    factories.ts       # shared DTO factories (createSession, createJournal, createStoreOffers)
```

## Rules

- **No colocated tests.** Production folders (`components/`, `hooks/`, `api/`, `shell/`, `state/`, `ui/`, `routes/`) must not contain `*.test.ts` or `*.test.tsx` files.
- **Shared helpers belong here.** Render helpers, DTO factories, and mocks live under `test-utils/`, not beside production code.
- **Import from `../`.** Tests import production code via relative paths back to `src/` (e.g. `../components/StartGamePanel`, `../api/types`).
- **Mock paths stay relative to production.** `vi.mock("../api/wildBunchApi", ...)` resolves to the production module regardless of where the test file lives.

## Running tests

```sh
npm test            # run all tests once
npm run test:watch  # watch mode
```

Vitest config is in `vite.config.ts`. The setup file is `src/tests/test-utils/setup.ts`.
