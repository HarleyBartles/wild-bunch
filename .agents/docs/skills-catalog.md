# Skills Catalog

Essential repo workflow routing skills. See root AGENTS.md for critical skills that must be invoked before relevant work.

## Specialist Skill Discovery

This catalog is not a complete list of specialist skills. When work touches architecture, domain modeling, persistence, frontend, browser-game delivery, testing, or other specialist areas:

1. First inspect the current repo source and canonical repo decisions
2. Use `/using-superpowers` or skill discovery to find and invoke the smallest relevant specialist skill
3. Do not treat this catalog as a complete list of specialist skills

For architecture specifically, inspect the repo's canonical architecture decisions in `.agents/docs/architecture-guardrails.md` and current source, then discover the relevant architecture pattern skill around what you are actually changing. Doctrine routes agents to inspect and discover; it does not pre-enumerate generic specialist skills that will drift over time.

## Workflow & Session Bootstrap

| Skill | Use When |
|-------|----------|
| `/using-superpowers` | Use when starting any conversation - establishes how to find and use skills, requiring skill invocation before ANY response including clarifying questions |
| `/work-mode-router` | Use when cross-runtime bootstrap router for new project sessions and workflow-sensitive starts after repo adoption. Use when a project context begins, a session resumes, or a request may involve continuity ingress, repo/source evidence, coding dispatch, workers, issues, artifacts, verification, skill/package work, mutation, or publication |
| `/inspecting-the-environment` | Use when about to take action and environment constraints could change the next step — discovers shell syntax, worktree state, repo state, path style, CLI availability, auth, connectors, mutation authority, and protected surfaces before proceeding |
| `/using-git-worktrees` | Use when starting feature work that needs isolation from current workspace or before executing implementation plans - ensures an isolated workspace exists via native tools or git worktree fallback |

**Note**: `/inspecting-the-environment` is part of the trimmed `superpowers+` surface and should be kept explicit for environment-constraint discovery.

## Linear & GitHub Work

| Skill | Use When |
|-------|----------|
| `/using-linear` | Use when working with the Linear connector surface, choosing the right tool call, or finding create/update tools exposed under `save_*` rather than `create_*` or `update_*` |
| `/linear-issue-shaping` | Use when Linear-backed issue, project, and document shaping: create or update worker-ready Linear issues, inspect Linear comments/attachments/state, prepare paste-ready worker handoffs when explicitly requested, and route GitHub PR proof after a PR exists |
| `/github-operations` | Use when verify GitHub repository evidence without taking over coding workflow routing. Use after a Linear/Codex task has a GitHub PR, branch, commit, review, merge, status, or file-state question; when checking publication proof, PR diff scope, mergeability, CI/status evidence, final main state, or GitHub-specific closure proof |

## Anti-Slop & Quality

| Skill | Use When |
|-------|----------|
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
| `/wild-bunch-project-doctrine` | Use when bootstrapping the Wild Bunch repo posture before any repo-sensitive change. Establishes source-truth posture, worker dispatch and return verification, issue-goal conformance, world setup, seeded identity, difficulty, entropy, working knowledge (worktree/scratch locations, script discovery), skill routing, and policy references. Use when chat summaries, session busters, worker reports, or issue comments might be mistaken for live repo truth. Routes to specialist skills for domain-specific work. |
| `/repo-worker-base` | Use when thin repo hygiene entrypoint for Codex workers in Harley's workspace. Use when a Codex worker is working in any repository in Harley's workspace and needs fresh-main discipline, worktree isolation, branch and PR hygiene, validation evidence, or publication proof |
| `/wild-bunch-dotnet-architecture` | Use when applying Wild Bunch .NET architecture guardrails for C#/.NET repo work touching GameSession live-play flows, application orchestration, infrastructure persistence, CQRS/read models, event-stream plus snapshot-cache state, database-table pressure, or framework leakage |
| `/wild-bunch-domain-modeling` | Use when applying Wild Bunch project-scoped domain guidance for DDD tactical modeling, GameSession boundaries, player wallet or inventory, clue or journal flows, hidden culprit truth, horse and saddle rules, water handling, or JourneyLoop and trail-day progression |
| `/wild-bunch-browser-game` | Use when bridge Wild Bunch to browser-game implementation and QA when work touches browser delivery, HUD design, Phaser/TypeScript/Vite, DOM overlays, playtest evidence, dev-server checks, screenshot QA, or installed browser verification tooling |

**Note**: For Linear/GitHub/architecture/anti-slop routing, use direct skills and repo-local doctrine instead of compositional middlemen:
- Linear work routes through `/using-linear`, `/linear-issue-shaping`, and repo doctrine/source-truth guidance
- GitHub/PR proof routes through `/github-operations`, `/repo-worker-base`, and Wild Bunch source-truth doctrine
- Anti-slop work routes to relevant `.agents/unslop/` profiles and direct review/verification skills
- Architecture work tells workers to inspect current source and canonical repo decisions, then use `/using-superpowers` or skill discovery to invoke the smallest relevant specialist skill

## Architecture & Quality

For architecture work, inspect current source and canonical repo decisions, then use `/using-superpowers` or skill discovery to invoke the smallest relevant specialist skill.

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

## Utility

| Skill | Use When |
|-------|----------|
| `/context-safety` | Use when large or context-heavy text writes need bounded composition, deliberate compaction boundaries, safe staging, and atomic replacement. Use when a write may exceed the safe threshold or when inline composition risks exhausting context |
