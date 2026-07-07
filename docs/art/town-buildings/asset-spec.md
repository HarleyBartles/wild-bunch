# Town Building Asset Spec

This document defines the repo-facing contract for town-hub asset art.

## Asset homes

- Source custody:
  - `src/WildBunch.Assets/source/town-hub-buildings/`
  - `src/WildBunch.Assets/source/town-hub-roads/`
  - `src/WildBunch.Assets/source/town-hub-ground/`
- Staging and normalization:
  - `src/WildBunch.Assets/staging/town-hub-buildings/`
  - `src/WildBunch.Assets/staging/town-hub-roads/`
  - `src/WildBunch.Assets/staging/town-hub-ground/`
- Final sprites:
  - `src/WildBunch.Assets/sprites/town-hub-buildings/`
  - `src/WildBunch.Assets/sprites/town-hub-roads/`
  - `src/WildBunch.Assets/sprites/town-hub-ground/`

The `sprites/` tree is the shippable output home. The `staging/` tree is for
working files, studies, and other intermediate art that is not ready to ship.
The `source/` tree is the custody home for full-size inputs, tile masters, and
family turnarounds.
The web project may publish from `src/WildBunch.Assets/` into `src/WildBunch.Web/public/assets/`, but that web tree is delivery output only, not the working home.

When source art is intended to be cut out into transparent assets, generate it
on a strong green chroma-key background rather than white so the cutout pass
can preserve light foreground details.

When the pipeline is promoted, the `sprites/` tree mirrors the staged
tier/family layout from `staging/` so the final assets stay easy to trace back
to their source cut.

## Asset tracks

### `town-hub-buildings`

- The `town-hub-buildings` track holds the filler-building families.
- The two required families are `background-house` and `background-shop`.
- Each family uses the same 5-view turnaround and the same four prosperity tiers
  used by the named town buildings.
- These are supporting buildings, so the town read should stay secondary to the
  named building set.

### `town-hub-roads`

- The `town-hub-roads` track holds the road-network tiles.
- Road tiles do not use prosperity tiers.
- Road variation comes from mirror pairs, topology, edge pairing, and end pieces.
- Keep road tiles tile-safe and seam-safe through source, staging, and sprites.
- Keep every road tile at the full 80x50 canvas in source, staging, and sprites;
  copy promotion must preserve the canvas, not crop, trim, or rescale it.
- Major road tiles only mirror horizontally and keep a right-way-up read.
- Spur tiles mirror both ways and still connect cleanly to other spur tiles in
  either mirror direction.

### `town-hub-ground`

- The `town-hub-ground` track holds dirt and landform tiles.
- Dirt variation comes from base textures, prop-baked tiles, and the larger
  landform set.
- Dirt tiles do not use prosperity tiers.
- Keep the dirt set tile-safe and seam-safe through source, staging, and
  sprites.
- Keep every dirt tile at the full 80x50 canvas in source, staging, and sprites;
  copy promotion must preserve the canvas, not crop, trim, or rescale it.
- Dirt tiles tile with other dirt tiles on all sides, and mirrored dirt tiles
  still need to tile cleanly.

## Naming

- Use the building family slug first.
- Use the view or stage second.
- Keep names lowercase and hyphenated.

Recommended pattern:

- `general-store/front.png`
- `general-store/profile.png`
- `general-store/rear.png`
- `general-store/front-oblique.png`
- `general-store/rear-oblique.png`

Apply the same pattern to `background-house`, `background-shop`, `general-store`,
`sheriff-office`, `saloon`, and `telegraph-office`.

## Source references vs shippable output

- Source references are images still being explored or normalized.
- Shippable output is the final sprite set that matches the style bible, footprint contract, and turnaround contract.
- Do not treat a `staging/` image as final art just because it is visually close.

## Prosperity tiers

- The shipping building families are reused across prosperity tiers.
- `destitute` and `boomtown` are the ends of the ladder.
- `poor` is the midpoint bridge between `destitute` and `prosperous`.
- `prosperous` remains the polished middle-high tier.
- Roads and dirt do not get prosperity tiers.
- If a promoted sprite reads like the wrong tier, regenerate it instead of forcing it through promotion.

## Promotion check

Before moving an image from `staging/` to `sprites/`, check that:

- the family read matches the style bible
- the footprint stays within the family contract
- the view is the intended one
- the background and framing are clean
- the file is ready to ship without extra cleanup
