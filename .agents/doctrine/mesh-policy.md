# Mesh policy

## Owned surfaces

- Root `AGENTS.md` is the five-section repository router.
- Scoped routing lives under `.devin/rules/`.
- Repo-specific doctrine and contracts live under `.agents/doctrine/` and
  `.agents/contracts/`.
- Stage-specific repository deltas live under `.agents/runbooks/`.
- `INDEX.md` and `INDEX.json` files are generated navigation, not operative law.

Classify authored agent material by role:

- doctrine says what must remain true in this repository;
- contracts define executable or independently consumed agreements;
- runbooks contain repo-local procedures and deltas from portable workflows;
- ordinary human explanation belongs under root `docs/`;
- completed audits, trackers, plans, and reports leave the live tree once their
  enduring decisions or rules have been promoted.

Live agent guidance states current rules, configuration, and ownership directly.
Corrective history and migration narrative belong in Git history or an ADR, not
in live policy notes or examples.

## Freshness invariant

Do not hand-edit generated indexes. When a routed file is added, moved, or
removed, the generated mesh must change in the same commit.

## Wild Bunch deltas

- Repo-local skills are listed exactly in `.agents/plugins/marketplace.json`
  under `repo.local_skills`; naming conventions do not establish custody.
- Marketplace-installed skills are generated projections under `.agents/skills/`;
  their source and provenance are owned by the marketplace configuration and
  pinned submodule.
- Active plans/specifications/roadmaps use `.agents/plans/`, `.agents/specs/`,
  and `.agents/roadmaps/`. Temporary evidence and worker artifacts use the
  branch-scoped `_agent-scratch` workspace.
