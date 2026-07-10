# WildBunch.Assets

This project holds the canonical asset source tree for generated town-hub art.

Current layout:

- `source/` - custody of the full-size source assets for each track
- `staging/` - reviewable scratch, cut, and normalization output
- `production/` - final custody root, with `sprites/` and `tiles/` beneath it
- `scripts/` - asset-local helper scripts for staging and promotion

The current town-hub tracks are `town-hub-buildings`, `town-hub-roads`, and
`town-hub-ground`. The buildings track holds the filler-building families;
the road and ground tracks hold tile families.

Use `INDEX.md` for the exact inventory of files and directories; this README
describes the shape and purpose of the project rather than enumerating every
asset.

The web project consumes shipped assets after promotion. It is not the place
to keep working asset files.

When sprites or tiles are ready to ship, the web bundle/publish step copies
them into `src/WildBunch.Web/public/assets/` as delivery output only.
