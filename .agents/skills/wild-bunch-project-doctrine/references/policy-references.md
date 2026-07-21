# Policy References

All paths are relative to the repository root. Read the smallest policy that owns the decision.

## Script discovery

Read `scripts/AGENTS.md` and `scripts/README.md` before replacing repository tooling with ad hoc commands.

| Need | Repository route |
| --- | --- |
| PostgreSQL setup or validation | `scripts/postgres-dev.ps1` |
| Development servers | `scripts/dev-servers.ps1` |
| Index mesh generation | `scripts/generate_index_mesh.py` or `.ps1` |
| Agent-skill refresh or check | `scripts/install_agent_skills.py` or `.ps1` |
| Image asset processing | `scripts/image_asset_pipeline.py` or `.ps1` |
| Local CI-equivalent preflight | `scripts/ci-preflight.ps1` |

## Policy map

- `.agents/docs/workflow-policy.md` — git workflow, completion, PRs, and issue-goal evidence.
- `.agents/docs/validation-policy.md` — validation lanes, test scope, and CI preflight.
- `.agents/docs/artifact-policy.md` — durable, generated, evidence, and scratch artifacts.
- `.agents/docs/architecture-guardrails.md` — `GameSession`, event sourcing, persistence, setup phase, and seed codec boundaries.
- `.agents/docs/coding-discipline.md` — scope, architecture-stack, and refactoring discipline.
- `.agents/docs/worker-environment.md` — connectors, images, local services, and cleanup.
- `.agents/docs/skill-authoring-policy.md` — classification, authoring, review, migration, and retirement of repo-local `wild-bunch-*` skills.
- `.agents/docs/repo-skills-policy.md` — marketplace refreshes and the boundary that preserves repo-local Wild Bunch skills outside marketplace provenance.
- `.agents/docs/mesh-policy.md` — documentation mesh ownership and generated navigation.
- `.agents/docs/guides/design-guide.md` — design work.
- `.agents/docs/guides/planning-guide.md` — implementation planning.
- `.agents/docs/guides/implementing-guide.md` — implementation.
- `.agents/docs/guides/code-review-guide.md` — code review.

Check `docs/adr/` and `.agents/docs/workflow-policy.md` before treating an ADR as current architecture truth.
