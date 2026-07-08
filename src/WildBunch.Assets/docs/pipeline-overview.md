# Town Building Pipeline Overview

The town-building pipeline turns approved reference work into a consistent town-hub asset set.

The town-building guidance is split across:

- `src/WildBunch.Assets/docs/bibles/buildings/buildings-bible-master.md`
- `src/WildBunch.Assets/docs/bibles/buildings/background-buildings-bible.md`
- `src/WildBunch.Assets/docs/bibles/buildings/prosperity-bible.md`
- `src/WildBunch.Assets/docs/bibles/buildings/general-store-bible.md`
- `src/WildBunch.Assets/docs/bibles/buildings/sheriff-office-bible.md`
- `src/WildBunch.Assets/docs/bibles/buildings/saloon-bible.md`
- `src/WildBunch.Assets/docs/bibles/buildings/telegraph-office-bible.md`

The split is three asset tracks:

- `town-hub-buildings`
- `town-hub-roads`
- `town-hub-ground`

Prosperity tiering is part of the building contract: `poor` sits between
`destitute` and `prosperous`, and tier differences should come from finish and
maintenance rather than new silhouettes or camera changes. Roads and dirt do
not use prosperity tiers.

What belongs in git:

- the human-facing docs in `src/WildBunch.Assets/docs/`
- generated indexes
- final sprite output in `src/WildBunch.Assets/production/sprites/town-hub-buildings/`, `src/WildBunch.Assets/production/tiles/town-hub-roads/`, and `src/WildBunch.Assets/production/tiles/town-hub-ground/`
- intermediate work in `src/WildBunch.Assets/staging/town-hub-buildings/`, `src/WildBunch.Assets/staging/town-hub-roads/`, and `src/WildBunch.Assets/staging/town-hub-ground/` when it is intentionally kept for review or reuse
- full-size source custody in `src/WildBunch.Assets/source/town-hub-buildings/`, `src/WildBunch.Assets/source/town-hub-roads/`, and `src/WildBunch.Assets/source/town-hub-ground/`

The web bundle can later copy promoted sprites into `src/WildBunch.Web/public/assets/`, but that tree is not the working asset home.

What should never be treated as final art:

- rough studies
- partially normalized frames
- scratch exports in `staging/`
- any image that does not match the style bible and asset spec

How to review outputs:

- compare the image against the intended track and family read
- confirm the view, footprint, and tile edge contract are correct
- check that the asset is clean enough to promote or copy forward
- keep the review focused on shape, readability, seam safety, and contract match

Promotion into `src/WildBunch.Assets/production/sprites/town-hub-buildings/` is handled by `python src/WildBunch.Assets/scripts/image_asset_pipeline.py promote-sprites --input-root src/WildBunch.Assets/staging/town-hub-buildings --out-root src/WildBunch.Assets/production/sprites/town-hub-buildings`.

Road and ground tiles stay tile-safe through copy promotion from `staging/` to
`production/tiles/` after seam checks. Their contract is about edge matching
and tessellation, not image cutting, and they keep the full 80x50 canvas in
every home so copy promotion never trims, rescales, or recenters them. The current
tile contract is mirror tiling only: major roads mirror horizontally, spurs
mirror both ways, and dirt tiles tile on all sides with mirrored variants still
seam-safe.
