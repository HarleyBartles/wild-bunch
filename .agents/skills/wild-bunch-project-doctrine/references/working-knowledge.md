# Working Knowledge

Consolidated from the Wild Bunch repo root `AGENTS.md` at commit `a65ca6c2`. All paths are relative to the wild-bunch repo root.

## Worktree and Scratch locations

- Worktrees for this repo should be placed in `Z:\_agent-worktrees\wild-bunch` (centralized location outside the repo). This is a declared preference that should be respected by the using-git-worktrees skill.
- **CRITICAL: Scratch files must be placed in `Z:\_agent-scratch\wild-bunch\<branch-name>`** where `<branch-name>` matches the worktree/branch name. This scratch space is disposable and not persistent beyond the agent's session. Agents must clean up their scratch folder when cleaning up their worktree. **Never commit scratch artifacts to the repo root** — see `.agents/docs/artifact-policy.md` for details.

## Scripts are first-class surfaces

Before reporting environmental issues (PostgreSQL not running, dev servers not started, etc.), read `scripts/AGENTS.md` and use the provided scripts.

- **Deterministic workflow scripts (dev servers, PostgreSQL, skill sync, index mesh)**: `scripts/AGENTS.md` — **MUST read before running ad-hoc commands or reporting environmental issues**. Scripts handle PostgreSQL setup, dev server management, and other repo operations idempotently.

## Required reading before specific work types

- Architecture-sensitive work: `.agents/INDEX.md`, `.agents/docs/architecture-hygiene.md`, `.agents/unslop/backend-architecture.md`
- **Architecture guardrails (must read before touching GameSession, persistence, or domain logic)**: `.agents/docs/architecture-guardrails.md`
- **Dev-enabled action pattern (must read before implementing dev controls that affect play actions)**: `docs/adr/ADR-0036-dev-enabled-action-pattern.md`
- **Coding discipline (must read before writing code)**: `.agents/docs/coding-discipline.md`
- **Frontend standards (must read before implementing or reviewing frontend work)**: `.agents/docs/frontend-standards.md`
- **Validation policy (must read before writing or reviewing tests)**: `.agents/docs/validation-policy.md`
- **Design guide (must read before brainstorming or writing a design spec)**: `.agents/docs/guides/design-guide.md`
- **Implementing guide (must read before implementing or dispatching implementer subagents)**: `.agents/docs/guides/implementing-guide.md`
- **Planning guide (must read before planning multi-step work)**: `.agents/docs/guides/planning-guide.md`
- **Code review guide (must read for code reviewers)**: `.agents/docs/guides/code-review-guide.md`
- Web UI/play-surface work: `src/WildBunch.Web/AGENTS.md`, `src/WildBunch.Web/.agents/unslop/play-surface-ui.md`
- Dev overlay work: `.agents/docs/dev-overlay-doctrine.md`, `.agents/unslop/dev-overlay.md`
