# Town Building Asset Spec

This document defines the repo-facing contract for town-building art.

## Asset homes

- Source custody: `src/WildBunch.Assets/source/town-buildings/`
- Staging and normalization: `src/WildBunch.Assets/staging/town-buildings/`
- Final sprites: `src/WildBunch.Assets/sprites/town-buildings/`

The `sprites/` tree is the shippable output home. The `staging/` tree is for working files, studies, and other intermediate art that is not ready to ship. The `source/` tree is the custody home for full-size inputs and family turnarounds.
The web project may publish from `src/WildBunch.Assets/` into `src/WildBunch.Web/public/assets/`, but that web tree is delivery output only, not the working home.

When the pipeline is promoted, the `sprites/` tree mirrors the staged tier/family layout from `staging/` so the final assets stay easy to trace back to their source cut.

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

Apply the same pattern to `sheriff-office`, `saloon`, and `telegraph-office`.

## Source references vs shippable output

- Source references are images still being explored or normalized.
- Shippable output is the final sprite set that matches the style bible, footprint contract, and turnaround contract.
- Do not treat a `staging/` image as final art just because it is visually close.

## Prosperity tiers

- The shipping asset families are reused across prosperity tiers.
- `destitute` and `boomtown` are the ends of the ladder.
- `poor` is the midpoint bridge between `destitute` and `prosperous`.
- `prosperous` remains the polished middle-high tier.
- If a promoted sprite reads like the wrong tier, regenerate it instead of forcing it through promotion.

## Promotion check

Before moving an image from `staging/` to `sprites/`, check that:

- the family read matches the style bible
- the footprint stays within the 60x50 contract
- the view is the intended one
- the background and framing are clean
- the file is ready to ship without extra cleanup
