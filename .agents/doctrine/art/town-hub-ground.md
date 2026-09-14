# Town Hub Ground Art Doctrine

The human-facing source of truth is the ground bible set in
`src/WildBunch.Assets/docs/bibles/ground/`; use those files for canonical
palette, seam, and family rules. Do not restate the whole style bible here.

## Working rules

- Keep dirt, road, spur, path, and prop art inside the same western palette
  contract.
- Keep road and ground tiles seam-safe and mirror-safe.
- Keep prop sprites as standalone transparent assets.
- Do not add placement, jitter, or spawn logic here; that belongs to the play
  surface, not the asset contract.

## Custody note

- `source/` is for master artwork and family sources.
- `staging/` is for reviewable 80x50 intermediates.
- `sprites/` is for final promoted outputs.
- For the road and ground tracks, keep the seam contract stable before promoting
  anything downstream.
