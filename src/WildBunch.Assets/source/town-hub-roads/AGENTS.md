# Town Hub Roads Asset Routing

This subtree is the source-custody root for town-hub road tiles.

Before editing, generating, or promoting any town-hub road assets in this
subtree, read:

- `src/WildBunch.Assets/docs/asset-operations.md` — project-level layout, required reading, and rules
- `src/WildBunch.Assets/docs/bibles/ground/ground-bible-master.md`
- `src/WildBunch.Assets/docs/bibles/ground/road-bible.md`
- `src/WildBunch.Assets/docs/bibles/ground/spur-bible.md`
- `src/WildBunch.Assets/docs/bibles/ground/path-bible.md`
- `src/WildBunch.Assets/docs/asset-spec.md`
- `.agents/docs/art/town-hub-ground-art-doctrine.md`
- `.agents/docs/asset-pipeline/selection-cut-normalization.md`

If a style bible, asset spec, or doctrine looks stale, misleading, incomplete, or wrong while you are
working, fix it as part of the same task instead of deferring the
correction.

Keep source custody here, review copies in
`src/WildBunch.Assets/staging/town-hub-roads/`, and final tile copies in
`src/WildBunch.Assets/production/tiles/town-hub-roads/`.
Do not add new naming branches here; keep road and path naming inside the
existing ground bible family and routing tables.
