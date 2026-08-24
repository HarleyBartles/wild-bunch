---
name: playwright-testing
description: Use when writing, reviewing, or debugging Playwright end-to-end tests
  for web applications.
metadata:
  source-id: playwright-testing
  source-path: codex-marketplace/plugins/frontend-pack/skills/playwright-testing/SKILL.md
  provenance-name: Playwright Testing first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when writing, reviewing, or debugging Playwright end-to-end tests for
    web applications.
  use_when:
  - Use when writing or reviewing Playwright end-to-end tests.
  - Use when choosing selectors, fixtures, or retry and reporting strategies.
  - Use when running tests across browsers or integrating with CI.
  do_not_use_when:
  - Do not use when another more specific skill owns the task.
  related_skills:
  - frontend-ux
  - wcag
  - web-styling
license: MIT
---

# Playwright Testing

Use this skill for end-to-end web testing with Playwright: selectors, fixtures, retries, and reporting.

## When to Use

- Writing, reviewing, or debugging Playwright end-to-end tests.
- Choosing selectors, fixtures, or retry and reporting strategies.
- Running tests across browsers or integrating with CI.

## Core Pattern

1. Favor user-facing locators: `getByRole`, `getByText`, `getByLabel` before CSS/XPath.
2. Use page object models or fixtures to centralize selectors and setup.
3. Keep tests independent; reset state with `test.use` or fixture-scoped setup.
4. Configure retries for flaky suites and shard jobs in CI for parallelism.
5. Use built-in reporters and traces; inspect `trace.zip` on failure.
6. Avoid sleeps; rely on auto-waiting assertions and explicit expectations.

## Common Mistakes

- Using brittle CSS selectors that break on visual changes. → Prefer accessible locators and `data-testid` only as a last resort.
- Sharing mutable state across tests. → Isolate state through fixtures or per-test setup.
- Ignoring flakiness instead of root-causing it. → Enable tracing, retries, and review timing or race conditions.

Load `references/operational-guidance.md` for deeper coverage of selectors, fixtures, retries, and reporting.
