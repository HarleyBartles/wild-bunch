# Town Hub Props Style Bible

The props family is the target standalone western object set that sits over
dirt. These assets are separate transparent sprites, not dirt-baked tiles.

## Visual contract

- Presentation: isolated transparent sprite with a clean cutout-friendly
  silhouette.
- Read: each prop should be unmistakable on its own and still feel like it came
  from the same western ground world.
- Texture: weathered wood, dry plant matter, stone, or worn utility material as
  appropriate to the prop.
- Background: no dirt plate, no full terrain chunk, no scene backdrop.
- Footprint: normalized to 60x50 canvas for consistency with building sprites.

## Prop family rules

- Keep the prop centered and readable inside its own transparent bounds.
- Let the silhouette carry the object identity first, then add surface detail.
- Keep the object small-scale and practical rather than theatrical.
- Match the same dusty western palette and material logic used by the ground
  families.

## Prompt-ready guardrails

- Do: Make the prop a standalone cutout sprite with alpha and enough padding
  for clean use over dirt.
- Do not: Do not bake the prop into a terrain tile, and do not surround it with
  a dirt plate, a landscape chunk, or a fixed placement context.

- Do: Keep the object readable at town scale and consistent with the same
  western world as the ground tiles.
- Do not: Do not turn the prop into a full scene, a vignette, or a decorative
  background asset.
