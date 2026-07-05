# AGENTS.md

## Project
- Wild Bunch is a C#/.NET Western adventure game in `HarleyBartles/wild-bunch`.
- Workers branch from current `main` and publish work through a PR.
- Root index: `INDEX.md`
- Docs index: `docs/INDEX.md`

## Required Working Knowledge
- Architecture-sensitive work: `.agents/INDEX.md`, `.agents/architecture-hygiene.md`, `.agents/unslop/backend-architecture.md`
- **Architecture guardrails (must read before touching GameSession, persistence, or domain logic)**: `.agents/docs/architecture-guardrails.md`
- **Coding discipline (must read before writing code)**: `.agents/docs/coding-discipline.md`
- **Frontend standards (must read before implementing or reviewing frontend work)**: `.agents/docs/frontend-standards.md`
- **Validation policy (must read before writing or reviewing tests)**: `.agents/docs/validation-policy.md`
- **Write-tool phantom files (must read before batch writes on Windows)**: `.agents/doctrine/write-tool-phantom-files.md`
- **Implementing guide (must read before implementing or dispatching implementer subagents)**: `.agents/docs/implementing-guide.md`
- **Planning guide (must read before planning multi-step work)**: `.agents/docs/planning-guide.md`
- **Code review guide (must read for code reviewers)**: `.agents/docs/code-review-guide.md`
- **Deterministic workflow scripts (dev servers, PostgreSQL, skill sync, index mesh)**: `scripts/AGENTS.md` — inspect this folder before running ad-hoc commands for repo operations
- Web UI/play-surface work: `src/WildBunch.Web/AGENTS.md`, `src/WildBunch.Web/.agents/unslop/play-surface-ui.md`
- Dev overlay work: `.agents/dev-overlay/DOCTRINE.md`, `.agents/unslop/dev-overlay.md`

## Required Skills - Workflow Routing

**Invoke these skills before relevant work. `/using-superpowers` is the primary workflow entrypoint and routes to specialist skills.**

### Session Bootstrap
- `/using-superpowers` - Use when starting any conversation - establishes how to find and use skills, requiring skill invocation before ANY response including clarifying questions
- `/work-mode-router` - Use when a project context begins, a session resumes, or a request may involve coding dispatch, workers, issues, artifacts, verification, or publication
- `/inspecting-the-environment` - Use when about to take action and environment constraints could change the next step — discovers shell syntax, worktree state, repo state, path style, CLI availability, auth, connectors, mutation authority, and protected surfaces before proceeding
- `/using-git-worktrees` - Use when starting feature work that needs isolation from current workspace or before executing implementation plans

**Note**: `/inspecting-the-environment` is part of the trimmed `superpowers+` surface and should be kept explicit for environment-constraint discovery.

### Linear & GitHub Work
- `/using-linear` - Use when working with the Linear connector surface, choosing the right tool call, or finding create/update tools exposed under `save_*` rather than `create_*` or `update_*`
- `/linear-issue-shaping` - Use when Linear-backed issue, project, and document shaping: create or update worker-ready Linear issues, inspect Linear comments/attachments/state, prepare paste-ready worker handoffs when explicitly requested, and route GitHub PR proof after a PR exists
- `/github-operations` - Use when verify GitHub repository evidence without taking over coding workflow routing. Use after a Linear/Codex task has a GitHub PR, branch, commit, review, merge, status, or file-state question; when checking publication proof, PR diff scope, mergeability, CI/status evidence, final main state, or GitHub-specific closure proof
- `/repo-worker-base` - Use for fresh-main discipline, worktree isolation, branch and PR hygiene, validation evidence, or publication proof

### Anti-Slop & Quality
- `/unslop-plus` - Use when apply domain-specific anti-slop profiles for common software development workflows, with thirteen portable profiles for writing, technical-writing, implementation-plans, code-review, worker-returns, debugging, frontend-react, frontend-ui, api-design, architecture, testing, security-review, and cleanup-custody
- `/connector-safety` - Use when a connector or tool call is blocked, rejected, safety-filtered, permission-rejected, or when a planned action could be sensitive or destructive
- `/verification-before-completion` - Use before claiming work is complete, fixed, or passing, before committing or creating PRs

### Planning & Execution
- `/brainstorming` - Use before any creative work - creating features, building components, adding functionality, or modifying behavior
- `/writing-plans` - Use when you have a spec or requirements for a multi-step task, before touching code
- `/test-driven-development` - Use when implementing any feature or bugfix, before writing implementation code
- `/systematic-debugging` - Use when encountering any bug, test failure, or unexpected behavior, before proposing fixes

### Repo-Specific Skills
- `/wild-bunch-project-doctrine` - Use before any repo-sensitive change, when work touches worker dispatch, worker return verification, source-truth claims, or issue-goal conformance
- `/repo-worker-base` - Use for fresh-main discipline, worktree isolation, branch and PR hygiene, validation evidence, or publication proof
- `/wild-bunch-dotnet-architecture` - Use when applying Wild Bunch .NET architecture guardrails for C#/.NET repo work touching GameSession live-play flows, persistence, or CQRS/read models
- `/wild-bunch-domain-modeling` - Use when applying Wild Bunch project-scoped domain guidance for DDD tactical modeling, GameSession boundaries, player wallet or inventory, or travel rules
- `/wild-bunch-browser-game` - Use when work touches browser delivery, HUD design, Phaser/TypeScript/Vite, DOM overlays, playtest evidence, or dev-server checks

**Note**: For Linear/GitHub/architecture/anti-slop routing, use direct skills and repo-local doctrine instead of compositional middlemen. The retired `*-superpowers` compositional skills have been removed from the marketplace.

### Architecture Skills (must invoke before touching domain, persistence, or command/query handlers)
- `/ddd` - DDD tactical patterns: aggregates, value objects, domain events, strongly-typed IDs. The aggregate root enforces invariants and returns Result objects for failures.
- `/cqrs-event-sourcing` - CQRS and Event Sourcing patterns: command/query separation, events as source of truth, projections for reads.
- `/event-driven-architecture` - Event-driven architecture patterns for domain events and projections.
- `/clean-architecture` - Layered .NET system structure: Domain, Application, Infrastructure, Api projects, dependency inversion.
- `/wild-bunch-dotnet-architecture` - Wild Bunch-specific .NET architecture guardrails: GameSession as aggregate root, event-sourced command flows, JSON snapshot cache, persistence boundaries.
- `/wild-bunch-domain-modeling` - Wild Bunch domain modeling: GameSession boundaries, player wallet/inventory, travel rules, clue/journal flows, hidden culprit truth.
- `/ef-core` - Entity Framework Core patterns when persistence work touches DbContext, migrations, or queries.
- For architecture work, inspect current source and canonical repo decisions, then invoke the smallest relevant specialist skill above. **Do not hand-roll non-DDD, non-CQRS, or non-event-sourced solutions. The skills and ADRs are the authority, not the repo's current code.**

## Specialist Skill Discovery
When work touches architecture, domain modeling, persistence, frontend, browser-game delivery, testing, or other specialist areas:
1. First inspect the current repo source and canonical repo decisions
2. Use `/using-superpowers` or skill discovery to find and invoke the smallest relevant specialist skill
3. Do not treat the skills catalog as a complete list of specialist skills

**For the complete skills inventory, see [`.agents/docs/skills-catalog.md`](.agents/docs/skills-catalog.md)**

## Policy Reference
Use these reference files when working in specific areas:

- **[`.agents/docs/workflow-policy.md`](.agents/docs/workflow-policy.md)** - Use when managing git workflow, claiming completion, publishing PRs, or verifying issue-goal alignment
- **[`.agents/docs/validation-policy.md`](.agents/docs/validation-policy.md)** - Use when running validation, debugging CI failures, or deciding test coverage scope. Documents the repo's five test kinds (unit, integration, game-content, API, brute-force) and when to use each.
- **[`.agents/docs/artifact-policy.md`](.agents/docs/artifact-policy.md)** - Use when creating agent artifacts, managing screenshots/evidence, or working with unslop profiles
- **[`.agents/docs/architecture-guardrails.md`](.agents/docs/architecture-guardrails.md)** - Use when making architecture decisions, touching GameSession, modifying persistence, or working with seed codecs
- **[`.agents/docs/coding-discipline.md`](.agents/docs/coding-discipline.md)** - Use when writing code, deciding scope boundaries, or refactoring
- **[`.agents/docs/worker-environment.md`](.agents/docs/worker-environment.md)** - Use when working with connectors, handling images, running dev services, or managing worker cleanup
- **[`.agents/docs/repo-skills-policy.md`](.agents/docs/repo-skills-policy.md)** - Use when syncing marketplace skills or working with the skill vendoring system
- **[`.agents/docs/mesh-policy.md`](.agents/docs/mesh-policy.md)** - Use when working with the documentation mesh (AGENTS.md, INDEX.md, README files)
- **[`.agents/docs/implementing-guide.md`](.agents/docs/implementing-guide.md)** - **Must read for implementers.** Standards to read before coding, skills to invoke, TDD discipline, pre-completion verification, PR/Linear/plan honesty, and subagent dispatch guidance.
- **[`.agents/docs/planning-guide.md`](.agents/docs/planning-guide.md)** - **Must read for planners.** Standards to read before planning, skills to invoke, plan structure requirements, artifact placement, and plan review checklist.

## ADR Log Freshness
- The ADR log at `docs/adr/` must represent the system as it exists today. See [`.agents/docs/workflow-policy.md`](.agents/docs/workflow-policy.md) for freshness check requirements.
