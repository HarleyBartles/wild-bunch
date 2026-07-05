# Town Building Asset Spec

This document defines the repo-facing contract for town-building art.

## Asset homes

- Final sprites: `src/WildBunch.Assets/town-buildings/sprites/`
- Pipeline intermediates: `src/WildBunch.Assets/town-buildings/_pipeline/`

The `sprites/` tree is the shippable output home. The `_pipeline/` tree is for working files, studies, and other intermediate art that is not ready to ship.
The web project may publish from `src/WildBunch.Assets/` into `src/WildBunch.Web/public/assets/`, but that web tree is delivery output only, not the working home.

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
- Do not treat a `_pipeline/` image as final art just because it is visually close.

## Promotion check

Before moving an image from `_pipeline/` to `sprites/`, check that:

- the family read matches the style bible
- the footprint stays within the 60x50 contract
- the view is the intended one
- the background and framing are clean
- the file is ready to ship without extra cleanup
