# Town Building Pipeline Overview

The town-building pipeline turns approved reference work into a consistent sprite set for the four canonical building families.

Prosperity tiering is part of the same contract: `poor` sits between `destitute` and `prosperous`, and tier differences should come from finish and maintenance rather than new silhouettes or camera changes.

What belongs in git:

- the human-facing docs in `docs/art/town-buildings/`
- generated indexes
- final sprite output in `src/WildBunch.Assets/sprites/town-buildings/`
- intermediate work in `src/WildBunch.Assets/staging/town-buildings/` when it is intentionally kept for review or reuse
- full-size source custody in `src/WildBunch.Assets/source/town-buildings/`

The web bundle can later copy promoted sprites into `src/WildBunch.Web/public/assets/`, but that tree is not the working asset home.

What should never be treated as final art:

- rough studies
- partially normalized frames
- scratch exports in `staging/`
- any image that does not match the style bible and asset spec

How to review outputs:

- compare the image against the building family read
- confirm the view and footprint are correct
- check that the sprite is clean enough to promote
- keep the review focused on shape, readability, and contract match

Promotion into `src/WildBunch.Assets/sprites/town-buildings/` is handled by `python scripts/image_asset_pipeline.py promote-sprites --input-root src/WildBunch.Assets/staging/town-buildings --out-root src/WildBunch.Assets/sprites/town-buildings`.
