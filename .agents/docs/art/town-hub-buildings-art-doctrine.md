# Town Hub Buildings Art Doctrine

Use this file as the agent-facing control surface for town-building sprite work. Keep it operational. The human-facing source of truth is the town-building master bible and asset spec in `src/WildBunch.Assets/docs/bibles/buildings/` and `src/WildBunch.Assets/docs/`; use those files for canonical vocabulary, footprint terms, and family names. Do not restate the whole style bible here.

The town-hub asset split is:

- `town-hub-buildings` for filler buildings
- `town-hub-roads` for road-network tiles
- `town-hub-ground` for dirt and landform tiles

The source/staging/sprites custody homes are the working contract, and the
filler-building visibility rule still applies: supporting buildings should stay
visually secondary to the named town buildings.

## Reference image selection

- Prefer references that already match the town-building contract: top-down with a slight oblique tilt, pixel art, and the 60x50 footprint.
- When a source image is intended to be cut out, prefer a strong green chroma-key background over white so the cutout pass can remove the backdrop without damaging light details.
- Accept a reference only if it preserves the family read without forcing a new silhouette.
- Reject street-level views, flat front elevations, painterly concept art, photoreal renders, extreme lens distortion, or anything that hides the roof plane.
- Reject mixed-family references. Each reference set should stay inside one canonical building family.
- Use a clean master front reference for each family, then derive the turnarounds from that anchor instead of letting each view drift independently.

## Camera lock

- The camera must stay in the top-down slight oblique contract.
- Roof mass comes first, wall faces second, trim and clutter third.
- If an image starts reading like a facade, an orthographic elevation, or a level-eye storefront, treat that as a failure and restate the camera contract in the next prompt.
- Do not let the model flatten the roof plane or raise the camera until the building reads like a street-facing illustration.

## Turnaround language

- Use the canonical five-view set: `front`, `profile`, `rear`, `front-oblique`, and `rear-oblique`.
- The master front is the `front-oblique` hero view for the family.
- The 45-degree turnaround pair is `front-oblique` and `rear-oblique`.
- Do not call either diagonal view a front view, side view, or back view.
- Side and back views are separate turnarounds and should preserve the same massing, scale, and footprint.
- Keep the family identity stable across all five views; only the viewing angle should change.

## Prosperity ladder

- Treat `poor` as the midpoint bridge between `destitute` and `prosperous`.
- Keep the same camera, footprint, and five-view contract across every prosperity tier.
- Preserve the family silhouette and identity; only shift maintenance, ornament, paint, trim, signage, and the amount of polish.
- Do not let `poor` drift into a new architecture or a new camera contract.
- When prompting or reviewing, use the family-specific tier ladder in the style bible as the controlling source of truth; visual inspection is for catching defects, not for inferring tier meaning.

## Track rules

- `town-hub-buildings` uses the same 5-view turnaround and four-tier ladder as
  the named building families, but the filler read must stay unobtrusive.
- `background-house` and `background-shop` are supporting buildings, not the
  dominant town feature.
- `town-hub-roads` is a tile family. Keep the work centered on mirroring,
  topology, and clean seam alignment.
- `town-hub-ground` is also a tile family. Keep the work centered on base
  textures, prop-baked tiles, and the landform set.
- Roads and dirt do not use prosperity tiers, so do not add tier language to
  those prompts.
- Road and ground tiles stay full-size 80x50 assets through source, staging,
  and sprites. Copy promotion preserves the canvas and seam edges; do not use
  sprite cutting, trimming, or rescaling on those tracks.
- The tile contract is mirror tiling only: road and dirt tiles must still
  tile cleanly after horizontal or vertical mirroring, but the contract does
  not require rotation-based tiling at this stage.
- When the asset-root AGENTS file points at this doctrine, treat this file as
  the canonical agent-facing contract for the town-hub split.

## Prompt style

- Keep prompts anchored to pixel art, crisp edges, readable silhouette, and a simple western palette.
- Ask for clean rooflines, short readable walls, and a practical small-town read.
- Do not request painted, rendered, cinematic, sketchy, or soft-focus treatment.
- Do not use prompt wording that nudges the result toward a modern storefront, a generic frontier shack, or a front-on illustration.

## Family cues

- General store: commerce-first read, porch or awning, merchandise frontage, goods and display cues.
- Sheriff office: authority-first read, badge or star cue, restrained facade, plain official windows, and any jail-bar or office detail needed to keep it from collapsing into the store.
- Saloon: hospitality and social read, public frontage, porch rhythm, and a stronger entertainment-facing silhouette.
- Telegraph office: communications and administration read, compact practical frontage, posted notices, and service-oriented cues.
- For the sheriff office, remove storefront clutter, stacked goods, display-window language, and broad retail signage. If it still reads like the store, strengthen the official markers before retrying.
- For the filler-building families, keep the town read secondary to the named buildings and keep the five-view, four-tier contract intact.

## Retry rules

- Retry when the image lands too front-facing, too level, too painterly, too loose, or too close to the wrong family.
- The retry prompt should restate: top-down slight oblique, pixel art, 60x50 footprint, and the exact family name.
- For a wrong camera, say to push the roof plane back into dominance and restore the oblique angle.
- For a wrong family read, add the missing cues and remove the misleading ones before trying again.
- Do not rely on cropping or perspective warping to rescue a bad camera contract. Regenerate instead.

## Asset pipeline pointer

- For cut and normalization, use `.agents/docs/asset-pipeline/selection-cut-normalization.md` and the asset-local `src/WildBunch.Assets/scripts/image_asset_pipeline.py` helper, with the repo-root wrapper kept only for compatibility.
- Keep town-building notes focused on family-specific selection and camera rules, not on the shared image pipeline mechanics.
- For roads and ground, prefer seam-safe copy promotion over sprite cutting.
