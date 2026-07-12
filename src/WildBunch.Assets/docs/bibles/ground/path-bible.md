# Town Hub Path Style Bible

The path family is the target thin connector set that will run from buildings to
roads or spurs. It should stay visibly lighter and narrower than spur art, with
a clear read as a walked-in trail rather than a paved surface.

For seam rules, use `src/WildBunch.Assets/docs/bibles/tiling-bible.md`.

## Visual contract

- Surface: worn footpath or light dirt trail with minimal paving.
- Strength: lighter and less dominant than the main road and the spur family.
- Color read: the path should remain the lighter connector tone in the family,
  clearly distinct from the browner spur track.
- Edge behavior: path pieces should still blend into dirt and road-adjacent
  surfaces without a hard seam.
- Keep the path width consistent at the join edge so a matching path tile can
  continue it cleanly.

## Path family rules

- Keep the path narrow, practical, and readable.
- Use only enough texture to show that the ground has been worn by traffic.
- Keep the path distinct from a full road surface.
- Use the same dusty western palette as the rest of the ground family.
- Keep the path width consistent where another path tile must continue it.

## Prompt-ready guardrails

- Do: Make the path look like a thin, worn connector that can bridge buildings
  to road-adjacent terrain.
- Do not: Do not make the path look like a second road, a spur, or a decorative
  gravel plaza.

- Do: Keep the surface subtle enough that it can sit inside larger dirt
  compositions without stealing focus.
- Do not: Do not add heavy paving, broad striping, or any seam-breaking border.

- Do: Keep the path visually lighter than the spur family and lighter than any
  spur-path-cross junction that uses it.
- Do not: Do not let the path inherit the darker browner spur tone.
