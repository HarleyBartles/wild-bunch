# Town Hub Props

This subtree is the source-custody home for the town-hub prop sprite set.

- Keep the canonical full-size prop sources here (1024x1024 for future scaling flexibility).
- Use matching homes under `src/WildBunch.Assets/staging/town-hub-ground/props/` and
  `src/WildBunch.Assets/production/sprites/town-hub-ground/props/` for reviewable and shipped
  copies.
- Props are normalized to 80x50 canvas to match the dirt tile grid.
- Props are standalone transparent sprites that sit over dirt tiles, not baked into ground plates.

Do not use the web public folder as the working asset home.
