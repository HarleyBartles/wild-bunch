# Skill Routing

Consolidated from the Wild Bunch repo root `AGENTS.md` at commit `a65ca6c2`. Invoke these skills before relevant work. `/using-superpowers` is the primary workflow entrypoint and routes to specialist skills.

## Session Bootstrap

- `/using-superpowers` — Use for workflow-sensitive project/repo work. Establishes how to find and use skills, requiring skill invocation before repo-sensitive responses. Not required for ordinary chat or questions that do not touch repo work.
- `/work-mode-router` — Use when a project context begins, a session resumes, or a request may involve coding dispatch, workers, issues, artifacts, verification, or publication.
- `/inspecting-the-environment` — Use when about to take action and environment constraints could change the next step. Discovers shell syntax, worktree state, repo state, path style, CLI availability, auth, connectors, mutation authority, and protected surfaces before proceeding.
- `/using-git-worktrees` — Use when starting feature work that needs isolation from current workspace or before executing implementation plans.

**Note**: `/inspecting-the-environment` is part of the trimmed `superpowers+` surface and should be kept explicit for environment-constraint discovery.

## Linear & GitHub Work

- `/using-linear` — Use when working with the Linear connector surface, choosing the right tool call, or finding create/update tools exposed under `save_*` rather than `create_*` or `update_*`.
- `/linear-issue-shaping` — Use when Linear-backed issue, project, and document shaping: create or update worker-ready Linear issues, inspect Linear comments/attachments/state, prepare paste-ready worker handoffs when explicitly requested, and route GitHub PR proof after a PR exists.
- `/github-operations` — Use when verifying GitHub repository evidence without taking over coding workflow routing. Use after a Linear/Codex task has a GitHub PR, branch, commit, review, merge, status, or file-state question; when checking publication proof, PR diff scope, mergeability, CI/status evidence, final main state, or GitHub-specific closure proof.
- `/repo-worker-base` — Use for fresh-main discipline, worktree isolation, branch and PR hygiene, validation evidence, or publication proof.

## Anti-Slop & Quality

- `/unslop-plus` — Use when applying domain-specific anti-slop profiles for common software development workflows, with thirteen portable profiles for writing, technical-writing, implementation-plans, code-review, worker-returns, debugging, frontend-react, frontend-ui, api-design, architecture, testing, security-review, and cleanup-custody.
- `/connector-safety` — Use when a connector or tool call is blocked, rejected, safety-filtered, permission-rejected, or when a planned action could be sensitive or destructive.
- `/verification-before-completion` — Use before claiming work is complete, fixed, or passing, before committing or creating PRs.

## Planning & Execution

- `/brainstorming` — Use before any creative work: creating features, building components, adding functionality, or modifying behavior.
- `/writing-plans` — Use when you have a spec or requirements for a multi-step task, before touching code.
- `/test-driven-development` — Use when implementing any feature or bugfix, before writing implementation code.
- `/systematic-debugging` — Use when encountering any bug, test failure, or unexpected behavior, before proposing fixes.

## Repo-Specific Skills

- `/wild-bunch-project-doctrine` — Use before any repo-sensitive change, when work touches worker dispatch, worker return verification, source-truth claims, or issue-goal conformance. Establishes posture first, then routes to specialist skills.
- `/repo-worker-base` — Use for fresh-main discipline, worktree isolation, branch and PR hygiene, validation evidence, or publication proof.
- `/wild-bunch-dotnet-architecture` — Use when applying Wild Bunch .NET architecture guardrails for C#/.NET repo work touching GameSession live-play flows, persistence, or CQRS/read models.
- `/wild-bunch-domain-modeling` — Use when applying Wild Bunch project-scoped domain guidance for DDD tactical modeling, GameSession boundaries, player wallet or inventory, or travel rules.
- `/wild-bunch-browser-game` — Use when work touches browser delivery, HUD design, Phaser/TypeScript/Vite, DOM overlays, playtest evidence, or dev-server checks.

**Note**: For Linear/GitHub/architecture/anti-slop routing, use direct skills and repo-local doctrine instead of compositional middlemen. The retired `*-superpowers` compositional skills have been removed from the marketplace.

## Architecture Skills (must invoke before touching domain, persistence, or command/query handlers)

- `/ddd` — DDD tactical patterns: aggregates, value objects, domain events, strongly-typed IDs.
- `/cqrs-event-sourcing` — CQRS and Event Sourcing patterns: command/query separation, events as source of truth, projections for reads.
- `/event-driven-architecture` — Event-driven architecture patterns for domain events and projections.
- `/clean-architecture` — Layered .NET system structure: Domain, Application, Infrastructure, Api projects, dependency inversion.
- `/wild-bunch-dotnet-architecture` — Wild Bunch-specific .NET architecture guardrails: GameSession as aggregate root, event-sourced command flows, JSON snapshot cache, persistence boundaries.
- `/wild-bunch-domain-modeling` — Wild Bunch domain modeling: GameSession boundaries, player wallet/inventory, travel rules, clue/journal flows, hidden culprit truth.
- `/ef-core` — Entity Framework Core patterns when persistence work touches DbContext, migrations, or queries.
- For architecture work, inspect current source and canonical repo decisions, then invoke the smallest relevant specialist skill above. Do not hand-roll non-DDD, non-CQRS, or non-event-sourced solutions. Current live source is truth about current behaviour; skills and ADRs provide constraints and intended architecture whose freshness must be checked against source and current requirements.

## Specialist Skill Discovery

When work touches architecture, domain modeling, persistence, frontend, browser-game delivery, testing, or other specialist areas:
1. First inspect the current repo source and canonical repo decisions.
2. Use `/using-superpowers` or skill discovery to find and invoke the smallest relevant specialist skill.
3. Do not treat the skills catalog as a complete list of specialist skills.

For the complete skills inventory, see `.agents/docs/skills-catalog.md` in the wild-bunch repo.
