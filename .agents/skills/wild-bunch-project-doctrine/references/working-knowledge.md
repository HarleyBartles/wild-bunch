# Working Knowledge

All paths are relative to the repository root unless shown as absolute.

## Workspace

- Linked worktrees: `Z:\_agent-worktrees\wild-bunch\<task-name>`.
- Disposable scratch: `Z:\_agent-scratch\wild-bunch\<branch-name>`.
- Never commit scratch output or create nested worktrees.
- Verify locations with Git; do not infer them from a script path or process directory.

## Script discovery

Read `scripts/AGENTS.md` and `scripts/README.md` before running ad hoc environment commands. Repository scripts own development servers, PostgreSQL, skill refresh, index mesh generation, image processing, and CI preflight.

## Read by task

| Task | Required local references |
| --- | --- |
| Any implementation | `.agents/docs/coding-discipline.md`, `.agents/docs/guides/implementing-guide.md` |
| Architecture, domain, or persistence | `.agents/docs/architecture-guardrails.md`, `.agents/docs/architecture-hygiene.md`, `.agents/unslop/backend-architecture.md` |
| Tests or CI | `.agents/docs/validation-policy.md` |
| Frontend | `.agents/docs/frontend-standards.md`, `src/WildBunch.Web/AGENTS.md` |
| Player-facing browser UI | `src/WildBunch.Web/.agents/unslop/play-surface-ui.md` |
| Dev overlay | `.agents/docs/dev-overlay-doctrine.md`, `.agents/unslop/dev-overlay.md` |
| Dev-enabled actions | `docs/adr/ADR-0036-dev-enabled-action-pattern.md` |
| Design | `.agents/docs/guides/design-guide.md` |
| Planning | `.agents/docs/guides/planning-guide.md` |
| Review | `.agents/docs/guides/code-review-guide.md` |
| Agent artifacts | `.agents/docs/artifact-policy.md` |
| Repo-local skills | `.agents/docs/skill-authoring-policy.md`, `.agents/docs/repo-skills-policy.md` |

Inspect each path and the live source it governs before retaining current-state claims.
