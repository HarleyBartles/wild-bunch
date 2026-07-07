# Town Hub Dirt Style Bible

The dirt family is the base terrain contract for the town hub. It should look
like compacted western ground that can repeat without drawing attention to the
pattern.

## Visual contract

- Surface: dusty, worn, compacted dirt with small stones, grit, and occasional
  dry scrub.
- Variance: use subtle differences between dirt tiles so the set feels natural
  without becoming noisy.
- Read: the dirt tile should support roads, spurs, paths, props, and buildings
  without competing with them.
- Edge behavior: dirt tiles must tile cleanly on all four sides, and mirrored
  dirt variants must still tile cleanly.

## Dirt family rules

- Keep the texture broad and even enough to survive repetition.
- Small rocks, tiny ruts, and sparse weeds are welcome when they stay
  understated.
- Leave room for roads and paths to sit over or beside the dirt without forcing
  a hard border.
- Keep the dirt family distinct from road paving and from prop sprites.

## Prompt-ready guardrails

- Do: Make the dirt read as believable compacted western ground with light
  surface variation, a few stones, and sparse scrub.
- Do not: Do not turn dirt into a road, a trail, a plaza, or a decorative
  painted field; do not bake props into the dirt plate in new work.

- Do: Make the tile body safe for repetition on all sides and across mirrored
  copies.
- Do not: Do not introduce a strong directional texture, a large landmark, or a
  border that breaks the tile contract.
