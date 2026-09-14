# Repository skills policy

## Source custody

- `.agents/plugins/marketplace.json` is the authored plugin subscription.
- `.agents/plugins/marketplace-source` pins the external marketplace source.
- `.agents/skills/.provenance.json` and skill directories not named in
  `repo.local_skills` are generated projections; do not edit them by hand.
- `.agents/plugins/game-studio/skills/` is the authored source for the locally
  vendored `game-studio` plugin.
- Exact names under `repo.local_skills` identify repository-local authored
  skills. A name or prefix alone never establishes custody.

## Registration invariant

Every repository-local skill is named exactly once in `repo.local_skills`, its
directory and frontmatter name match that entry, and marketplace provenance
does not claim it. Repository-local skills may retain descriptive
`wild-bunch-*` names, but that prefix is not reserved or protected.
