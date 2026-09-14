# Mesh policy

## Owned surfaces

- Root `AGENTS.md` is the five-section repository router.
- Scoped routing lives under `.devin/rules/`.
- Repo-specific doctrine and contracts live under `.agents/doctrine/` and
  `.agents/contracts/`.
- Stage-specific repository deltas live under `.agents/runbooks/`.
- `INDEX.md` and `INDEX.json` files are generated navigation, not operative law.

## Generation and validation

Use the canonical runner to regenerate and validate the complete mesh:

```powershell
py -3 tools\run.py ci --apply
py -3 tools\run.py ci --check
```

Do not hand-edit generated indexes. When a routed file is added, moved, or
removed, the generated mesh must change in the same commit.

## Wild Bunch deltas

- Repo-local skills use the `wild-bunch-*` namespace and are listed exactly in
  `.agents/plugins/marketplace.json` under `repo.local_skills`.
- Marketplace-installed skills are generated projections under `.agents/skills/`;
  their source and provenance are owned by the marketplace configuration and
  pinned submodule.
- Active plans/specifications/roadmaps use `.agents/plans/`, `.agents/specs/`,
  and `.agents/roadmaps/`. Temporary evidence and worker artifacts use the
  branch-scoped `_agent-scratch` workspace.
