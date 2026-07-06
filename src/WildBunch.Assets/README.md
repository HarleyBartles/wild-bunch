# WildBunch.Assets

This project holds the canonical asset source tree for generated art.

Current layout:

- `town-buildings/_pipeline/` - staging and review output
- `town-buildings/sprites/` - final sprite assets

The web project consumes shipped assets after promotion. It is not the place
to keep working asset files.

When sprites are ready to ship, the web bundle/publish step copies them into
`src/WildBunch.Web/public/assets/` as delivery output only.
