# Town Hub Ground Asset Routing

This subtree is the source-custody root for town-hub ground tiles and props.

Before editing, generating, or promoting any town-hub ground assets in this
subtree, read the controlling style bible set first:

- `src/WildBunch.Assets/docs/bibles/AGENTS.md`
- `src/WildBunch.Assets/docs/bibles/ground/ground-bible-master.md`
- the matching family bible under `src/WildBunch.Assets/docs/bibles/ground/`
  that matches the asset you are working on
- `.agents/art/town-hub-ground/DOCTRINE.md`
- `src/WildBunch.Assets/AGENTS.md`
- `.agents/docs/asset-pipeline/selection-cut-normalization.md`

If a style bible looks stale, misleading, incomplete, or wrong while you are
working, fix the bible as part of the same task instead of deferring the
correction.
Do not add new naming branches here; keep dirt, road, spur, path, and props
naming inside the existing ground bible family and routing tables.

Keep source custody here, intermediate work in
`src/WildBunch.Assets/staging/town-hub-ground/`, and promoted tiles or props
in `src/WildBunch.Assets/production/tiles/town-hub-ground/`.
