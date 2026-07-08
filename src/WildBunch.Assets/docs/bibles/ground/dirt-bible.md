# Town Hub Dirt Style Bible

The dirt family is the base terrain contract for the town hub. It should look
like compacted western ground that can repeat without drawing attention to the
pattern.

For seam rules, use `src/WildBunch.Assets/docs/bibles/tiling-bible.md`.

## Visual contract

- Surface: dusty, worn, compacted dirt with small stones, grit, and occasional
  dry scrub.
- Variance: use subtle differences between dirt tiles so the set feels natural
  without becoming noisy.
- Read: the dirt tile should support roads, spurs, paths, props, and buildings
  without competing with them.
- Edge behavior: dirt tiles must tile cleanly on all four sides.
- The 3 dirt variants are one tiling family and must stay visually distinct
  while remaining repeat-safe.

## Dirt family rules

- Keep the texture broad and even enough to survive repetition.
- Small rocks, tiny ruts, and sparse weeds are welcome when they stay
  understated.
- Leave room for roads and paths to sit over or beside the dirt without forcing
  a hard border.
- Keep the dirt family distinct from road paving and from prop sprites.
- Keep edge zones neutral and repeat-safe so dirt can join any matching dirt
  edge without revealing the seam.
- Keep variation in the interior of the tile rather than at the seam.

## Prompt-ready guardrails

- Do: Make the dirt read as believable compacted western ground with light
  surface variation, a few stones, and sparse scrub.
- Do not: Do not turn dirt into a road, a trail, a plaza, or a decorative
  painted field; do not bake props into the dirt plate in new work.

- Do: Make the tile body safe for repetition on all sides.
- Do not: Do not introduce a strong directional texture, a large landmark, or a
  border that breaks the tile contract.

- Do: Make each dirt variant visibly distinct without giving it a directional
  front or back.
- Do not: Do not make any dirt tile depend on one edge being "top" or "bottom"
  in order to read correctly.
