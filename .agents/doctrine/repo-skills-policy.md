# Repository skills policy

## Source custody

- `.agents/plugins/marketplace.json` is the authored plugin subscription.
- `.agents/plugins/marketplace-source` pins the external marketplace source.
- `.agents/skills/.provenance.json` and non-`wild-bunch-*` skill directories are
  generated projections; do not edit them by hand.
- `.agents/plugins/game-studio/skills/` is the authored source for the locally
  vendored `game-studio` plugin.
- `.agents/skills/wild-bunch-*/` contains repository-local authored skills.

## Refresh contract

Run the repository's canonical apply capability after changing the plugin
subscription, pinned source, local plugin, or repo-local skill source:

```powershell
py -3 tools\run.py ci --apply
```

Then stage the intended tree and use the normal hooked commit. The hook checks
the installed projection against the configured source and provenance.

## Local namespace

Repo-local skills must use the `wild-bunch-*` prefix and be named exactly in
`repo.local_skills`. A marketplace refresh must preserve those directories and
must not include them in marketplace provenance.
