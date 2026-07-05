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

Before executing a plan:
1. Check for route tree nesting issues (does a parent route component render an `<Outlet />`? If not, child routes won't render — use flat siblings instead).
2. Check for missing dismiss/cleanup functionality (if the plan adds a UI element with a query param, does it include a way to clear it?).
3. Check for test infrastructure gaps (do the test cases use the right router setup? Do they account for lazy-loaded components?).
4. Check for interim state documentation (are tsc errors between tasks documented?).
5. Rate the SDD confidence — if below 7, refine the plan before executing.

## What a Plan is NOT

- A plan is not a design document — design decisions should be made during brainstorming, not during planning.
- A plan is not a spec — the spec comes from the Linear issue and brainstorming. The plan translates the spec into implementable tasks.
- A plan is not a license to scope-creep — follow `.agents/docs/coding-discipline.md` scope rules. If the plan discovers work outside the issue's scope, flag it for a Linear issue, don't expand the plan.
