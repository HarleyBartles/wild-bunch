# WildBunch.Assets

This project holds the canonical asset source tree for generated art.

Current layout:

- `source/` - custody of the full-size source assets for each family
- `staging/` - reviewable scratch, cut, and normalization output
- `sprites/` - final promoted sprite assets

Each top-level bucket can host multiple asset families. Today that includes
town buildings; future tile families should follow the same structure.

Use `INDEX.md` for the exact inventory of files and directories; this README
describes the shape and purpose of the project rather than enumerating every
asset.

The web project consumes shipped assets after promotion. It is not the place
to keep working asset files.

When sprites are ready to ship, the web bundle/publish step copies them into
`src/WildBunch.Web/public/assets/` as delivery output only.
