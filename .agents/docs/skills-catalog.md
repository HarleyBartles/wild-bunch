# Skills Catalog

Complete inventory of repo skills with "use when" guidance. See root AGENTS.md for critical skills that must be invoked before relevant work.

## Workflow & Session Bootstrap

| Skill | Use When |
|-------|----------|
| `/using-superpowers` | Use when starting any conversation - establishes how to find and use skills, requiring skill invocation before ANY response including clarifying questions |
| `/work-mode-router` | Use when cross-runtime bootstrap router for new project sessions and workflow-sensitive starts after repo adoption. Use when a project context begins, a session resumes, or a request may involve continuity ingress, repo/source evidence, coding dispatch, workers, issues, artifacts, verification, skill/package work, mutation, or publication |
| `/inspecting-the-environment` | Use when about to take action and environment constraints could change the next step — discovers shell syntax, worktree state, repo state, path style, CLI availability, auth, connectors, mutation authority, and protected surfaces before proceeding |
| `/using-git-worktrees` | Use when starting feature work that needs isolation from current workspace or before executing implementation plans - ensures an isolated workspace exists via native tools or git worktree fallback |

## Linear & GitHub Work

| Skill | Use When |
|-------|----------|
| `/using-linear` | Use when working with the Linear connector surface, choosing the right tool call, or finding create/update tools exposed under `save_*` rather than `create_*` or `update_*` |
| `/linear-superpowers` | Use when shaping Linear issues, issue tracks, and worker packets so they name the smallest applicable Superpowers workflow skill, explain why it applies, and name the evidence required to prove it was followed |
| `/linear-issue-shaping` | Use when Linear-backed issue, project, and document shaping: create or update worker-ready Linear issues, inspect Linear comments/attachments/state, prepare paste-ready worker handoffs when explicitly requested, and route GitHub PR proof after a PR exists |
| `/github-superpowers` | Use when shaping GitHub-facing work so it starts with @using-superpowers, selects the smallest applicable specialist workflow, and keeps GitHub proof, review routing, publication proof, and final main-state verification bound to github-operations |
| `/github-operations` | Use when verify GitHub repository evidence without taking over coding workflow routing. Use after a Linear/Codex task has a GitHub PR, branch, commit, review, merge, status, or file-state question; when checking publication proof, PR diff scope, mergeability, CI/status evidence, final main state, or GitHub-specific closure proof |

## Anti-Slop & Quality

| Skill | Use When |
|-------|----------|
| `/unslop-superpowers` | Use when shaping Linear issues, GitHub proof, worker returns, or closeout language needs repo-specific anti-slop controls, profile discovery or refresh, concrete evidence requirements, or a narrow direct-to-main unslop profile update |
| `/unslop-plus` | Use when apply domain-specific anti-slop profiles for common software development workflows, with thirteen portable profiles for writing, technical-writing, implementation-plans, code-review, worker-returns, debugging, frontend-react, frontend-ui, api-design, architecture, testing, security-review, and cleanup-custody |
| `/connector-safety` | Use when connector or tool call is blocked, rejected, safety-filtered, permission-rejected, schema-rejected, or validation-rejected, when a planned action could be sensitive, destructive, permission-changing, or easy to over-bundle, or when mutation work should follow discover -> read -> write -> verify or step back up the connector discovery chain |
| `/verification-before-completion` | Use when about to claim work is complete, fixed, or passing, before committing or creating PRs - requires running verification commands and confirming output before making any success claims; evidence before assertions always |

## Planning & Execution

| Skill | Use When |
|-------|----------|
| `/brainstorming` | Use before any creative work - creating features, building components, adding functionality, or modifying behavior. Explores user intent, requirements and design before implementation |
| `/writing-plans` | Use when you have a spec or requirements for a multi-step task, before touching code |
| `/executing-plans` | Use when you have a written implementation plan to execute in a separate session with review checkpoints |
| `/subagent-driven-development` | Use when executing implementation plans with independent tasks in the current session |
| `/boring-loop` | Use when coordinating a boring work loop, picking the next smallest safe move, or preventing false-green repo work |
| `/dispatching-parallel-agents` | Use when facing 2+ independent tasks that can be worked on without shared state or sequential dependencies |

## Repo-Specific Skills

| Skill | Use When |
|-------|----------|
| `/wild-bunch-project-doctrine` | Use when bootstrap the wild bunch repo posture before any repo-sensitive change. use when work touches harleybartles/wild-bunch, worker dispatch, worker return verification, source-truth claims, issue-goal conformance, world setup, seeded setup, difficulty, entropy, or when chat summaries, session busters, worker reports, or issue comments might be mistaken for live repo truth |
| `/repo-worker-base` | Use when thin repo hygiene entrypoint for Codex workers in Harley's workspace. Use when a Codex worker is working in any repository in Harley's workspace and needs fresh-main discipline, worktree isolation, branch and PR hygiene, validation evidence, or publication proof |
| `/wild-bunch-dotnet-architecture` | Use when applying Wild Bunch .NET architecture guardrails for C#/.NET repo work touching GameSession live-play flows, application orchestration, infrastructure persistence, CQRS/read models, event-stream plus snapshot-cache state, database-table pressure, or framework leakage |
| `/wild-bunch-domain-modeling` | Use when applying Wild Bunch project-scoped domain guidance for DDD tactical modeling, GameSession boundaries, player wallet or inventory, clue or journal flows, hidden culprit truth, horse and saddle rules, water handling, or JourneyLoop and trail-day progression |
| `/wild-bunch-browser-game` | Use when bridge Wild Bunch to browser-game implementation and QA when work touches browser delivery, HUD design, Phaser/TypeScript/Vite, DOM overlays, playtest evidence, dev-server checks, screenshot QA, or installed browser verification tooling |

## Architecture & Design

| Skill | Use When |
|-------|----------|
| `/architecture-superpowers` | Use when shaping architecture decisions and review packets that need a compositional gate over Superpowers instead of a new doctrine surface |
| `/ddd` | Use when modeling a .NET domain with aggregates, aggregate roots, value objects, domain events, domain services, strongly-typed IDs, or repositories that persist aggregate roots |
| `/clean-architecture` | Use when building or reviewing a layered .NET system that uses Domain, Application, Infrastructure, and Api projects, dependency inversion, use case handlers, domain entities with behavior, or infrastructure as a plugin |
| `/cqrs-event-sourcing` | Use when building audit-required systems, implementing temporal queries, or designing high-scale applications with complex domain logic |
| `/event-driven-architecture` | Use when designing distributed systems, microservices communication, or systems requiring eventual consistency and scalability |
| `/vertical-slice` | Use when organizing a .NET application by feature rather than layer, working in a feature-folder codebase, or needing guidance on endpoint grouping and handler patterns for Mediator, Wolverine, or raw handler classes |
| `/database-design-patterns` | Use when designing database schemas, optimizing query performance, or implementing data persistence layers at scale |

## .NET Development

| Skill | Use When |
|-------|----------|
| `/modern-csharp` | Use when writing new C# 14 code, reviewing existing code for modernization, or needing guidance on primary constructors, collection expressions, the field keyword, extension members, records, pattern matching, spans, or raw string literals |
| `/ef-core` | Use when working with Entity Framework Core, DbContext configuration, migrations, interceptors, compiled queries, ExecuteUpdateAsync, ExecuteDeleteAsync, value converters, query optimization, or LINQ queries |
| `/testing` | Use when writing .NET tests, setting up test infrastructure, reviewing test coverage, or needing guidance on xUnit, WebApplicationFactory, Testcontainers, snapshot testing, the AAA pattern, WireMock, or FakeTimeProvider |
| `/test-driven-development` | Use when implementing any feature or bugfix, before writing implementation code |

## Frontend & Browser Game

| Skill | Use When |
|-------|----------|
| `/feature-sliced-design` | Use when the task involves organizing project structure with FSD layers, deciding where code belongs, placing static assets (images, icons, fonts, PDFs), grouping closely related slices, defining public APIs and import boundaries, resolving cross-imports or evaluating the @x pattern, deciding whether to create or remove an entity, evaluating whether the entities layer is needed at all, deciding whether logic should remain local or be extracted, migrating from FSD v2.0 or a non-FSD codebase, integrating FSD with frameworks (Next.js App Router and Pages Router, Nuxt, Vite, Astro), or implementing common patterns such as authentication, API handling, Redux, and TanStack Query (React Query) within FSD |
| `/game-studio` | Use when the user needs stack selection and workflow planning across design, implementation, assets, and playtesting before moving to a specialist skill |
| `/web-game-foundations` | Use when the user needs engine choice, simulation and render boundaries, input model, asset organization, or save/debug/performance strategy |
| `/phaser-2d-game` | Use when the user wants a Phaser, TypeScript, and Vite stack for scenes, gameplay systems, cameras, sprite animation, and DOM-overlay HUD patterns |
| `/three-webgl-game` | Use when the user wants imperative scene control in TypeScript or Vite with GLB assets, loaders, physics, and low-level WebGL debugging |
| `/react-three-fiber-game` | Use when the user wants pmndrs-based scene composition, shared React state, and 3D HUD integration inside a React app |
| `/game-ui-frontend` | Use when the user asks for HUDs, menus, overlays, responsive layouts, or visual direction that must protect the playfield |
| `/game-playtest` | Use when the user asks for smoke tests, screenshot-based verification, browser automation, HUD or overlay review, or structured issue-finding in a browser game |
| `/webapp-testing` | Use when verifying frontend functionality, debugging UI behavior, capturing browser screenshots, and viewing browser logs |
| `/react-performance-optimization` | Use when optimizing slow React applications, reducing bundle size, or improving user experience with large datasets |

## Asset Pipelines

| Skill | Use When |
|-------|----------|
| `/sprite-pipeline` | Use when the user asks for full-strip generation from approved source frames, consistent anchor and scale normalization, or preview assets for browser-game animation |
| `/web-3d-asset-pipeline` | Use when the user asks for GLB or glTF shipping work, including Blender cleanup and export, collision or LOD setup, compression, texture packaging, and runtime validation |

## Debugging & Code Review

| Skill | Use When |
|-------|----------|
| `/systematic-debugging` | Use when encountering any bug, test failure, or unexpected behavior, before proposing fixes |
| `/requesting-code-review` | Use when completing tasks, implementing major features, or before merging to verify work meets requirements |
| `/receiving-code-review` | Use when receiving code review feedback, before implementing suggestions, especially if feedback seems unclear or technically questionable - requires technical rigor and verification, not performative agreement or blind implementation |

## Branch & Completion

| Skill | Use When |
|-------|----------|
| `/finishing-a-development-branch` | Use when implementation is complete, all tests pass, and you need to decide how to integrate the work - guides completion of development work by presenting structured options for merge, PR, or cleanup |

## Documentation & Skills

| Skill | Use When |
|-------|----------|
| `/writing-skills` | Use when creating new skills, editing existing skills, or verifying skills work before deployment |
| `/base-doctrine` | Use when cross-runtime doctrine store for cross-project operating invariants not owned by a more specific skill. Use when work involves system-prompt limits, tool/source evidence honesty, durable doctrine routing, bounded skill/reference read loops, correction/trust posture, canonical agent asset source truth, or output artifact-shape authority such as reserved YAML, dispatch/session-buster confusion, worker-copy attention guards, and lower-skill format conflicts |

## UI/UX

| Skill | Use When |
|-------|----------|
| `/interaction-design` | Use when designing user interactions, workflows, or interface patterns |
| `/ux-review` | Use when reviewing user experience, usability, or interface design |
| `/accessibility-audit` | Use when auditing accessibility compliance, WCAG standards, or inclusive design |

## Utility

| Skill | Use When |
|-------|----------|
| `/context-safety` | Use when large or context-heavy text writes need bounded composition, deliberate compaction boundaries, safe staging, and atomic replacement. Use when a write may exceed the safe threshold or when inline composition risks exhausting context |
