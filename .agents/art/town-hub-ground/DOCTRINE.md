# Town Hub Ground Doctrine

Use this file as the agent-facing control surface for town-hub ground work. Keep
it operational. The human-facing source of truth is the ground style bible set
in `docs/art/town-hub-ground/`; use those files for canonical palette, seam, and
family rules. Do not restate the whole style bible here.

## Read order

- Read `docs/art/town-hub-ground/style-bible.md` first.
- Then read the matching family bible:
  - `docs/art/town-hub-ground/dirt-style-bible.md`
  - `docs/art/town-hub-ground/road-style-bible.md`
  - `docs/art/town-hub-ground/spur-style-bible.md`
  - `docs/art/town-hub-ground/path-style-bible.md`
  - `docs/art/town-hub-ground/props-style-bible.md`
- If a task touches the source custody root, also read
  `src/WildBunch.Assets/source/town-hub-ground/AGENTS.md` and
  `src/WildBunch.Assets/AGENTS.md`.

## Working rules

- Keep dirt, road, spur, path, and prop art inside the same western palette
  contract.
- Keep road and ground tiles seam-safe and mirror-safe.
- Keep prop sprites as standalone transparent assets.
- Do not add placement, jitter, or spawn logic here; that belongs to the play
  surface, not the asset contract.
- If an asset family drifts away from the written contract, rewrite the prompt
  before generating another pass.

## Custody note

- `source/` is for master artwork and family sources.
- `staging/` is for reviewable 80x50 intermediates.
- `sprites/` is for final promoted outputs.
- For the road and ground tracks, keep the seam contract stable before promoting
  anything downstream.
