# Town Hub Spur Style Bible

The spur family is the target side-road set that will branch off the main road.
It should feel lighter and rougher than the main road while still sharing the
same western surface language.

For seam rules, use `src/WildBunch.Assets/docs/bibles/tiling-bible.md`. For
canonical-side rules, use
`src/WildBunch.Assets/docs/bibles/directional-mirroring-bible.md`.

## Visual contract

- Surface: a ragged horizontal spur road, narrower and rougher than the main
  road but still clearly road-like.
- Orientation: horizontal spur band in the current contract.
- Edge behavior: the east and west edges must hit the correct center seam for
  spur-to-spur tiling; the north and south edges should remain dirt-friendly
  where the family contract needs them to.
- Terminal rule: the eastern end cap should taper into dirt at the terminal end
  rather than stopping as a hard cut.
- Full bleed: the spur art must reach the tile edges cleanly. Do not leave a
  white border, margin, or frame around the tile.

## Spur family rules

- Keep the spur visibly subordinate to the main road.
- Keep the road band centered enough to read as an offshoot, not a second main
  street.
- Preserve the same dusty western palette as the rest of the ground family.
- Let the spur family carry junction flavor without losing seam safety.
- Keep the interior raggedness and wear obvious so the spur feels less maintained
  than the main road.
- Keep the end cap readable as the place where the spur dies into dirt.

## Prompt-ready guardrails

- Do: Make the spur read like a horizontal branch road that can join the main
  road and still repeat cleanly at the exact edge center.
- Do not: Do not make the spur as wide or as important as the main road; do not
  add a center divider, a second lane, or a new road class.

- Do: Keep the top and bottom edges dirt-friendly, and let the terminal end
  fade into dirt.
- Do not: Do not add a hard outer border, a decorative curb, a stop-sign-like
  cutoff, a seam that breaks when the tile is mirrored, or any border around
  the tile.
