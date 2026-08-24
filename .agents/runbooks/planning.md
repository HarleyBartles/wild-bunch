# Planning Guide

Use this reference when planning work in the Wild Bunch repo â€” before writing an implementation plan, before touching code. This guide covers the planner workflow: what to read before planning, what skills to invoke, what a plan must contain, and where plan artifacts go.

## Before You Begin: Read the Standards

A plan that doesn't account for the repo's standards will produce implementations that fail review. Read these before planning:

- **[`.agents/docs/coding-discipline.md`](../coding-discipline.md)** â€” scope discipline (do only the requested slice, no opportunistic refactors), architecture stack discipline (DDD/CQRS/event-sourcing is mandatory). The plan must respect these boundaries.
- **[`.agents/docs/architecture-guardrails.md`](../architecture-guardrails.md)** â€” read before planning any work that touches GameSession, persistence, or domain logic. The plan must not hand-roll non-DDD solutions.
- **[`.agents/docs/frontend-standards.md`](../frontend-standards.md)** â€” read before planning any frontend work. The plan must respect the styling stack, routing conventions, and play-surface UI rules.
- **[`.agents/docs/validation-policy.md`](../validation-policy.md)** â€” read before planning test coverage. The plan must specify the right test kind for each test and follow the test quality standards.
- **[`.agents/docs/workflow-policy.md`](../workflow-policy.md)** â€” read for the GREEN checklist and PR workflow. The plan must produce work that can pass the GREEN gate.

## Skills to Invoke

- Invoke `/brainstorming` before any creative work, then invoke `/writing-plans` once the spec is ready.
- Invoke the relevant architecture skills before planning work that touches domain, persistence, or command/query handlers.
- Invoke `/wild-bunch-browser-game` before planning browser delivery, HUD design, Phaser/TypeScript/Vite, or DOM overlay work.

## Plan Structure

Every implementation plan in this repo must contain:

- **Task breakdown** â€” the work divided into independently implementable tasks, each with a clear scope and no shared mutable state between tasks (for SDD parallel execution).
- **Exact code** â€” each task step contains the exact code to write, not prose descriptions. Implementers should be transcribing, not designing.
- **File structure** â€” which files to create, modify, or delete. Each file has one clear responsibility.
- **Test cases** â€” each task specifies the test cases to write, with TDD ordering (failing test first, then implementation). Specify the test kind (unit, integration, etc.) per `.agents/docs/validation-policy.md`.
- **Commit messages** â€” each task specifies its commit message.
- **Expected interim state** â€” tasks that leave the build in a temporarily broken state (e.g. tsc errors fixed by a later task) must document this explicitly so the implementer knows it's expected.
- **SDD confidence rating** â€” the plan includes a confidence rating (0-10) reflecting how well-specified the tasks are for subagent-driven execution. This rating must be the result of an honest execution confidence assessment (see below), not a self-assigned number.

## Plan Artifact Placement

Plans go in `.agents/superpowers/plans/` with a descriptive filename (e.g. `2026-07-05-bunch-124-implementation.md`). See `.agents/docs/artifact-policy.md` for full artifact placement rules.

Session artifacts (task briefs, reports, review diffs) go in `.agents/superpowers/sdd/<plan-name>/`.

Do not create loose files at repo root. Do not place agent-generated artifacts under `docs/` or product source folders.

## Plan Review

Before executing a plan, run through this checklist. Each item is a general principle â€” the examples are illustrative, not exhaustive.

### Structural integrity
1. **Parent-child rendering compatibility.** If the plan nests routes, components, or UI elements hierarchically, verify the parent actually renders its children. Example: a parent route component must render `<Outlet />` for child routes to work â€” if it doesn't, use flat siblings instead. This applies to any framework with a parent-renders-child contract (TanStack Router outlets, React context providers, layout wrappers).
2. **Lifecycle completeness.** For every UI element the plan adds that has a show/dismiss lifecycle (modals, notices, overlays, query params, state flags), verify the plan includes a way to clear or dismiss it. A notice that appears but can't be dismissed is a bug. A query param that's set but never cleared will leak into navigations.
3. **Cleanup on unmount/navigation.** If the plan adds state that persists across route changes (URL params, global state, subscriptions), verify the plan includes cleanup logic. State that leaks across routes causes subtle bugs that are hard to trace.

### Test infrastructure
4. **Test isolation.** Do the test cases use fresh instances of shared state (routers, stores, providers) rather than module-level singletons? Shared singletons cause test ordering flakes. Example: use a `createAppRouter()` factory in tests, not the shared `router` singleton.
5. **Async timing.** Do the test cases account for lazy-loaded components, async state resolution, and deferred renders? Tests that assert on lazy-loaded components need extended timeouts or `findBy*` queries, not synchronous `getBy*` queries.
6. **Test kind selection.** Is each test case using the right test kind per `.agents/docs/validation-policy.md`? Unit tests for isolated logic, integration tests for HTTP pipeline, game-content tests for seed pipelines, brute-force for distribution invariants.

### Execution safety
7. **Interim state documentation.** Are temporary build breaks between tasks (tsc errors, failing tests, missing imports) documented so the implementer knows they're expected? An undocumented interim break will cause the implementer to waste time debugging a "failure" that's planned.
8. **Task independence.** Can each task be executed independently without shared mutable state between tasks? If task B depends on task A's state changes, that must be documented. SDD parallel execution requires independence.

## Execution Confidence Assessment (required before reporting ready)

Before reporting a plan as ready for execution, the planner must honestly assess the plan's execution confidence. This is not a formality â€” it is a verification step that catches gaps the planner would otherwise discover too late.

### How to assess

For each task in the plan, ask: **"If a competent implementer (or subagent) executed this task exactly as written, would they produce the right thing without needing to discover and solve problems in flight?"**

Verify the plan's assumptions against the actual source code, not against the planner's memory or earlier exploration. Specifically:

1. **Verify every file path, class name, and method signature the plan references.** Open the files. Confirm the types, signatures, and shapes match what the plan assumes. A plan that references `SeedWorldCatalog.CreateWorld` when the class is now `SeedWorldFactory` is a plan that will derail the implementer.
2. **Verify every "follows the X pattern" claim.** Read the referenced pattern (e.g. `PhaserMapHost`). Is the pattern concrete enough to replicate? Does the plan specify the prop interface, or does it leave the implementer to design it?
3. **Verify every "new code" claim.** If the plan says "create `TownLayoutGenerator`", confirm nothing similar already exists. If the plan says "add `BuildingKind` enum", confirm it doesn't already exist.
4. **Verify every DTO, snapshot, and mapping boundary.** Read the current DTO, snapshot, and mapper. Are the fields the plan expects to extend actually there? Are there pre-existing sync issues (e.g. C# DTO has fields that TypeScript DTO doesn't) that the plan would inherit or worsen?
5. **Verify every integration site.** If the plan says "post-process in `MapGenerator.Generate`", open that method and confirm the integration is possible as described â€” the source is in scope, the return type supports `with` expressions, the call site is where the plan says it is.
6. **Identify underspecified design decisions.** If a task requires the implementer to make visual, algorithmic, or interface design decisions that aren't specified in the plan, that's a gap. "Simple grid-based layout" without dimensions, spacing, or visual concept is a gap. "Follows PhaserMapHost pattern" without specifying the exact props is a gap.

### Gap closure obligation

If the assessment finds gaps, the planner must **close the obvious gaps before reporting the plan as ready**. This means:

- **Stale references** (renamed classes, deleted files, moved methods): fix them in the plan.
- **Missing verification**: verify against current source and update the plan with the correct names, signatures, and shapes.
- **Underspecified algorithms**: specify the algorithm concretely enough that the implementer is transcribing, not designing. For layout generators, specify dimensions, spacing, slots, and visual concept. For projections, specify the projection rules.
- **Underspecified interfaces**: define the exact prop interface, method signatures, or type shapes the implementer should write.
- **Pre-existing sync issues or bugs**: investigate and resolve them, or explicitly flag them as blockers in the ready report. Do not let the plan inherit a known issue silently.
- **Underspecified visual/interaction design**: specify camera, rendering, animation, and interaction details concretely enough that the implementer is building to spec, not improvising.

If a gap cannot be closed without making a design decision that requires user input, flag it explicitly in the ready report as an open question. Do not report the plan as ready with silent gaps.

### Ready report

When reporting a plan as ready, include:

1. **Confidence rating (0-10)** â€” how confident the planner is that the plan will deliver the right thing first time without needing to discover and solve problems in flight.
2. **Direct execution confidence** â€” confidence for a single implementer working sequentially with the ability to ask clarifying questions.
3. **SDD confidence** â€” confidence for subagent-driven execution where implementers cannot ask clarifying questions and must work from the plan alone.
4. **Gap closure summary** â€” what gaps were found during assessment and how they were closed.
5. **Open questions** â€” any gaps that could not be closed without user input, with enough context for the user to make a decision.

If the SDD confidence is below 7, do not report the plan as SDD-ready. Either refine the plan or report it as direct-execution-only with the reasons why.

## Pre-Handoff Confidence Floor

Before handing a plan back to the user for an execution-lane decision, the planner must check whether the plan is already strong enough to avoid avoidable in-flight invention.

- If the planner cannot honestly rate the plan at least `8/10`, do not offer the execution-choice handoff yet.
- Keep verifying source, tightening file paths, closing open questions, or splitting the work until the confidence reaches the floor or the remaining gap is explicitly user-owned.
- If an open question materially changes scope, sequence, or file targets, surface it before the handoff rather than burying it in the ready report.
- The handoff should only happen once the plan is both honest and as de-risked as the current source allows.

This floor is a repo-local workflow rule layered on top of the writing-plans skill. It does not replace the skill's output shape; it gates when the output may be handed back for execution choice.

## What a Plan is NOT

- A plan is not a design document â€” design decisions should be made during brainstorming, not during planning.
- A plan is not a spec â€” the spec comes from the Linear issue and brainstorming. The plan translates the spec into implementable tasks.
- A plan is not a license to scope-creep â€” follow `.agents/docs/coding-discipline.md` scope rules. If the plan discovers work outside the issue's scope, flag it for a Linear issue, don't expand the plan.

