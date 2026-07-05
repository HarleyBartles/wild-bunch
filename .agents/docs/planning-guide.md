# Planning Guide

Use this reference when planning work in the Wild Bunch repo — before writing an implementation plan, before touching code. This guide covers the planner workflow: what to read before planning, what skills to invoke, what a plan must contain, and where plan artifacts go.

## Before You Begin: Read the Standards

A plan that doesn't account for the repo's standards will produce implementations that fail review. Read these before planning:

- **[`.agents/docs/coding-discipline.md`](coding-discipline.md)** — scope discipline (do only the requested slice, no opportunistic refactors), architecture stack discipline (DDD/CQRS/event-sourcing is mandatory). The plan must respect these boundaries.
- **[`.agents/docs/architecture-guardrails.md`](architecture-guardrails.md)** — read before planning any work that touches GameSession, persistence, or domain logic. The plan must not hand-roll non-DDD solutions.
- **[`.agents/docs/frontend-standards.md`](frontend-standards.md)** — read before planning any frontend work. The plan must respect the styling stack, routing conventions, and play-surface UI rules.
- **[`.agents/docs/validation-policy.md`](validation-policy.md)** — read before planning test coverage. The plan must specify the right test kind for each test and follow the test quality standards.
- **[`.agents/docs/workflow-policy.md`](workflow-policy.md)** — read for the GREEN checklist and PR workflow. The plan must produce work that can pass the GREEN gate.

## Skills to Invoke

- **`/brainstorming`** — invoke before any creative work (creating features, building components, adding functionality, or modifying behavior). Explore user intent, requirements, and design before planning.
- **`/writing-plans`** — invoke when you have a spec or requirements for a multi-step task, before touching code.
- **Architecture skills** (`/ddd`, `/cqrs-event-sourcing`, `/event-driven-architecture`, `/clean-architecture`, `/wild-bunch-dotnet-architecture`, `/wild-bunch-domain-modeling`) — invoke before planning work that touches domain, persistence, or command/query handlers. The plan must model aggregates, value objects, domain events, and command/query handlers correctly.
- **`/wild-bunch-browser-game`** — invoke before planning browser delivery, HUD design, Phaser/TypeScript/Vite, or DOM overlay work.

## Plan Structure

Every implementation plan in this repo must contain:

- **Task breakdown** — the work divided into independently implementable tasks, each with a clear scope and no shared mutable state between tasks (for SDD parallel execution).
- **Exact code** — each task step contains the exact code to write, not prose descriptions. Implementers should be transcribing, not designing.
- **File structure** — which files to create, modify, or delete. Each file has one clear responsibility.
- **Test cases** — each task specifies the test cases to write, with TDD ordering (failing test first, then implementation). Specify the test kind (unit, integration, etc.) per `.agents/docs/validation-policy.md`.
- **Commit messages** — each task specifies its commit message.
- **Expected interim state** — tasks that leave the build in a temporarily broken state (e.g. tsc errors fixed by a later task) must document this explicitly so the implementer knows it's expected.
- **SDD confidence rating** — the plan includes a confidence rating (0-10) reflecting how well-specified the tasks are for subagent-driven execution.

## Plan Artifact Placement

Plans go in `.agents/superpowers/plans/` with a descriptive filename (e.g. `2026-07-05-bunch-124-implementation.md`). See `.agents/superpowers/AGENTS.md` for full artifact placement rules.

Session artifacts (task briefs, reports, review diffs) go in `.agents/superpowers/sdd/<plan-name>/`.

Do not create loose files at repo root. Do not place agent-generated artifacts under `docs/` or product source folders.

## Plan Review

Before executing a plan, run through this checklist. Each item is a general principle — the examples are illustrative, not exhaustive.

### Structural integrity
1. **Parent-child rendering compatibility.** If the plan nests routes, components, or UI elements hierarchically, verify the parent actually renders its children. Example: a parent route component must render `<Outlet />` for child routes to work — if it doesn't, use flat siblings instead. This applies to any framework with a parent-renders-child contract (TanStack Router outlets, React context providers, layout wrappers).
2. **Lifecycle completeness.** For every UI element the plan adds that has a show/dismiss lifecycle (modals, notices, overlays, query params, state flags), verify the plan includes a way to clear or dismiss it. A notice that appears but can't be dismissed is a bug. A query param that's set but never cleared will leak into navigations.
3. **Cleanup on unmount/navigation.** If the plan adds state that persists across route changes (URL params, global state, subscriptions), verify the plan includes cleanup logic. State that leaks across routes causes subtle bugs that are hard to trace.

### Test infrastructure
4. **Test isolation.** Do the test cases use fresh instances of shared state (routers, stores, providers) rather than module-level singletons? Shared singletons cause test ordering flakes. Example: use a `createAppRouter()` factory in tests, not the shared `router` singleton.
5. **Async timing.** Do the test cases account for lazy-loaded components, async state resolution, and deferred renders? Tests that assert on lazy-loaded components need extended timeouts or `findBy*` queries, not synchronous `getBy*` queries.
6. **Test kind selection.** Is each test case using the right test kind per `.agents/docs/validation-policy.md`? Unit tests for isolated logic, integration tests for HTTP pipeline, game-content tests for seed pipelines, brute-force for distribution invariants.

### Execution safety
7. **Interim state documentation.** Are temporary build breaks between tasks (tsc errors, failing tests, missing imports) documented so the implementer knows they're expected? An undocumented interim break will cause the implementer to waste time debugging a "failure" that's planned.
8. **Task independence.** Can each task be executed independently without shared mutable state between tasks? If task B depends on task A's state changes, that must be documented. SDD parallel execution requires independence.
9. **SDD confidence rating.** Rate the plan 0-10 for how well-specified the tasks are for subagent-driven execution. If below 7, refine the plan — add more exact code, more test detail, or more file structure before executing.

## What a Plan is NOT

- A plan is not a design document — design decisions should be made during brainstorming, not during planning.
- A plan is not a spec — the spec comes from the Linear issue and brainstorming. The plan translates the spec into implementable tasks.
- A plan is not a license to scope-creep — follow `.agents/docs/coding-discipline.md` scope rules. If the plan discovers work outside the issue's scope, flag it for a Linear issue, don't expand the plan.
