# Town Hub Road Style Bible

The road family is the approved main-road tile set for the town hub. It should
read as a single continuous road when mirrored, not as two unrelated surfaces.

For seam rules, use `src/WildBunch.Assets/docs/bibles/tiling-bible.md`. For
canonical-side rules, use
`src/WildBunch.Assets/docs/bibles/directional-mirroring-bible.md`.

## Visual contract

- Surface: worn western road paving with dust, compacted grit, and weathered
  stone.
- Outer edge: the non-join side should transition into dirt shoulder cleanly.
- The road should read as one continuous band rather than two unrelated
  surfaces.
- Current variants: flat edge, path edge, and spur-cross edge.
- Full bleed: the road art must reach the tile edges cleanly. Do not include a
  white border, margin, or frame around the tile.

## Road family rules

- Treat the road as one contiguous piece across the mirrored seam.
- Keep the seam-side road surface consistent all the way to the edge.
- Keep the outer edge readable as dirt shoulder so it can meet the dirt family
  cleanly.
- Keep the road visually stronger and more paved than the spur or path families.

## Prompt-ready guardrails

- Do: Make the road look like a practical western street surface that reads as
  one continuous road band.
- Do not: Do not draw a center stripe, a divider, or a road-to-dirt seam at the
  join; do not treat the road as a rotation-ready tile at this stage.

- Do: Keep the road edge suitable for adjacent dirt tiles and for smaller
  connector variants.
- Do not: Do not make the road edge collapse into a trail, a plaza, or a prop
  set, and do not leave a border around the tile.
