# Town Building Pipeline Overview

The town-building pipeline turns approved reference work into a consistent sprite set for the four canonical building families.

What belongs in git:

- the human-facing docs in `docs/art/town-buildings/`
- generated indexes
- final sprite output in `src/WildBunch.Assets/town-buildings/sprites/`
- intermediate work in `src/WildBunch.Assets/town-buildings/_pipeline/` when it is intentionally kept for review or reuse

The web bundle can later copy promoted sprites into `src/WildBunch.Web/public/assets/`, but that tree is not the working asset home.

What should never be treated as final art:

- rough studies
- partially normalized frames
- scratch exports in `_pipeline/`
- any image that does not match the style bible and asset spec

How to review outputs:

- compare the image against the building family read
- confirm the view and footprint are correct
- check that the sprite is clean enough to promote
- keep the review focused on shape, readability, and contract match
