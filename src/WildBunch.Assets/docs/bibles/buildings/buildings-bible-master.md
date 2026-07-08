# Town Building Master Bible

This is the master map and shared contract for town-building art. Use it to
find the right bible before writing prompts or updating assets.

## Contract map

| Document | Owns |
| --- | --- |
| `buildings-bible-master.md` | Shared building rules for the whole family and the routing map for the set |
| `background-buildings-bible.md` | Rules for the background-house and background-shop support families |
| `prosperity-bible.md` | Shared prosperity-tier rules for every building family |
| `general-store-bible.md` | General-store identity and family-specific rules |
| `sheriff-office-bible.md` | Sheriff office identity and family-specific rules |
| `saloon-bible.md` | Saloon identity and family-specific rules |
| `telegraph-office-bible.md` | Telegraph office identity and family-specific rules |

## Current master set

The canonical building families are:

- `background-house`
- `background-shop`
- `general-store`
- `sheriff-office`
- `saloon`
- `telegraph-office`

## Shared building contract

- Camera: top-down with a slight oblique tilt.
- Presentation: pixel art, not painted concept art.
- Footprint: normalized to 60x50.
- Source art intended for cutout should sit on a strong green chroma-key
  background, not white, so edge cutout stays clean and deterministic.
- Turnaround: five views per building family.
- Shared turnaround set: front, profile, rear, front-oblique, rear-oblique.
- The front-oblique view is the hero view for review and prompt anchoring.
- Shape read: roof and massing first, trim and detail second.
- Layout freedom: doors, windows, and side details may vary so different towns
  can reuse the same building family.

## Shared guidance

- Keep the family readable at town scale.
- Keep the silhouette simple enough to survive the five-view turnaround.
- Keep the read western, practical, and grounded.
- Keep the roof plane and massing dominant over ornamental clutter.
- Keep the source art ready for cutout, normalization, and promotion through
  the asset pipeline.
- Keep the source art on a strong green chroma-key background when it will be
  cut out later.

## Prompts and review

- Use the family-specific bible together with the prosperity bible when
  generating or reviewing named buildings.
- Use the background bible together with the prosperity bible when generating
  or reviewing filler buildings.
- If a family rule appears to conflict with the shared building contract, the
  family-specific bible wins for that family only.
- If a tier rule appears to conflict with a family identity rule, the prosperity
  bible wins for the tier read and the family bible wins for silhouette and
  cues.
