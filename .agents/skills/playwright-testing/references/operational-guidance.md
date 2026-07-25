# Playwright Testing operational guidance

## When to apply

Use when the playwright-testing skill loaded and the question needs more than the SKILL.md summary:
- selecting stable locators,
- structuring fixtures and page objects,
- configuring retries and CI reporting,
- debugging flaky tests.

## Selectors

- Prefer Playwright locators in this order: `getByRole`, `getByLabel`, `getByText`, `getByPlaceholder`, `getByTestId`.
- Use ARIA roles and accessible names that match how users perceive the element.
- Avoid brittle XPath and CSS chains; if a `data-testid` is required, make it stable and semantic.

## Fixtures

- Extend `test` with fixtures for shared page objects, API clients, and authentication state.
- Keep fixtures small and composable; avoid fixtures that perform too much work.
- Use worker-scoped fixtures for expensive one-time setup (e.g., building an app) and test-scoped fixtures for clean state.

## Retries

- Enable `retries` in `playwright.config.ts` for CI; keep local runs at zero for fast feedback.
- Treat retries as a diagnostic, not a fix; investigate every flaky test.
- Use `expect` assertions with auto-waiting instead of `setTimeout` or `waitForFunction` loops.

## Reporting

- Use the built-in `list`, `line`, `dot`, or `html` reporters for CI and local runs.
- Record `trace: 'retain-on-failure'` and `screenshot: 'only-on-failure'` to debug CI failures.
- Publish `playwright-report/` as a CI artifact; use `merge-reports` to combine sharded results.

## Related references

- Playwright docs: https://playwright.dev/
- W3C WebDriver: https://www.w3.org/TR/webdriver/
