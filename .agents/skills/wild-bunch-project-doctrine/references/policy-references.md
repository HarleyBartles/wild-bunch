# Policy References

Consolidated from the Wild Bunch repo root `AGENTS.md` at commit `a65ca6c2`. All paths are relative to the wild-bunch repo root.

## Script Discovery

Scripts in `scripts/` are first-class surfaces. Before reporting environmental issues or running ad-hoc commands, read `scripts/AGENTS.md` and use the provided scripts.

When you encounter:
- PostgreSQL connection errors or missing database → read `scripts/AGENTS.md` and use `postgres-dev.ps1`
- Dev servers not running → read `scripts/AGENTS.md` and use `dev-servers.ps1`
- Need to regenerate index mesh → read `scripts/AGENTS.md` and use `generate_index_mesh.py` or `generate_index_mesh.ps1`
- Need to sync marketplace skills → read `scripts/AGENTS.md` and use `install_agent_skills.py` or `install_agent_skills.ps1`
- Need image asset processing → read `scripts/AGENTS.md` and use `image_asset_pipeline.py` or `image_asset_pipeline.ps1`

**Do not report "environmental issue" or "missing tooling" without first checking `scripts/AGENTS.md`.** The scripts folder is the canonical way to perform these operations and must be treated as a first-class discovery surface.

## Policy Reference

Use these reference files when working in specific areas:

- **`.agents/docs/workflow-policy.md`** — Use when managing git workflow, claiming completion, publishing PRs, or verifying issue-goal alignment.
- **`.agents/docs/validation-policy.md`** — Use when running validation, debugging CI failures, or deciding test coverage scope. Documents the repo's five test kinds (unit, integration, game-content, API, brute-force) and when to use each.
- **`.agents/docs/artifact-policy.md`** — Use when creating agent artifacts, managing screenshots/evidence, or working with unslop profiles.
- **`.agents/docs/architecture-guardrails.md`** — Use when making architecture decisions, touching GameSession, modifying persistence, or working with seed codecs.
- **`.agents/docs/coding-discipline.md`** — Use when writing code, deciding scope boundaries, or refactoring.
- **`.agents/docs/worker-environment.md`** — Use when working with connectors, handling images, running dev services, or managing worker cleanup.
- **`.agents/docs/skill-authoring-policy.md`** — Use when classifying, creating, renaming, reviewing, migrating, or retiring repo-local `wild-bunch-*` skills and when checking how they interact with the marketplace refresh boundary.
- **`.agents/docs/repo-skills-policy.md`** — Use when syncing marketplace skills or working with the skill vendoring system.
- **`.agents/docs/mesh-policy.md`** — Use when working with the documentation mesh (AGENTS.md, INDEX.md, README files).
- **`.agents/docs/guides/implementing-guide.md`** — **Must read for implementers.** Standards to read before coding, skills to invoke, TDD discipline, pre-completion verification, PR/Linear/plan honesty, and subagent dispatch guidance.
- **`.agents/docs/guides/planning-guide.md`** — **Must read for planners.** Standards to read before planning, skills to invoke, plan structure requirements, artifact placement, and plan review checklist.
- **`.agents/docs/guides/design-guide.md`** — **Must read for brainstormers and spec authors.** Standards to read before turning ideas into design specs, including the spec self-review and handoff confidence floor.

## ADR Log Freshness

The ADR log at `docs/adr/` must represent the system as it exists today. See `.agents/docs/workflow-policy.md` for freshness check requirements.
