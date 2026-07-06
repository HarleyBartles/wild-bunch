# Town Building Style Bible

The town-building set is a small, readable western asset family for top-down play.

## Visual contract

- Camera: top-down with a slight oblique tilt.
- Presentation: pixel art, not painted concept art.
- Footprint: normalized to 60x50.
- Turnaround: 5 views per building family. The canonical set is front-facing oblique (roof + front), side, back, 45-degree facing camera, and 45-degree facing away from camera; mirrored variants fill the 8-way placement contract.
- Shape read: roof and massing first, trim and detail second.
- Layout freedom: doors, windows, and other side details may vary so different towns can reuse the same building family.

## Prosperity ladder

- `destitute`: roughest and most worn end of the ladder.
- `poor`: midpoint bridge between destitute and prosperous; repaired and serviceable, but plainly aged, lightly ornamented, and visibly below the polished middle tier.
- `prosperous`: maintained, polished, and clearly better kept than poor, but not yet the richest expression.
- `boomtown`: richest and most ornate end of the ladder.

`poor` is not a new family, camera, or footprint contract. It is the same four canonical families rendered at a middle-low maintenance level.

## Tier expression by family

Use the same four tiers for every family. The differences are in maintenance, ornament, and material finish, not in camera or footprint.

### General store

- `destitute`: sparse, patched retail frontage; faded or missing trim; porch and display cues are present but worn; the building reads as provision trade under strain.
- `poor`: repaired and open for business; modest porch goods, workable signage, intact windows and doors, but little polish; still plainly below the prosperity of a neat town store.
- `prosperous`: tidy commercial frontage; clearer signage; balanced porch display; more finished trim and better-kept clapboard; the store looks dependable and established.
- `boomtown`: full commercial confidence; the richest signage, trim, and display cues allowed by the family; strongest porch presence without changing the footprint.

### Sheriff office

- `destitute`: official read is preserved, but the building is rough, plain, and tired; badge or star cue is present but battered; porch and rail details are minimal.
- `poor`: serviceable law-office read with repaired wood, restrained official markers, and modest porch structure; it should feel functional and underfunded rather than polished.
- `prosperous`: firmer authority read; cleaner facade, stronger badge/star treatment, more deliberate porch rhythm, and better-maintained official details.
- `boomtown`: richest official expression; strongest authority markers, more decorative trim, and the most finished porch and facade the family can hold.

### Saloon

- `destitute`: hospitality read remains visible, but the frontage is rough, weathered, and under-maintained; porch rhythm and social cues survive with minimal finish.
- `poor`: modest public house with repaired boards, usable porch, and simple social cues; it should feel worn but still welcoming and active.
- `prosperous`: lively and respectable saloon frontage; better rhythm, clearer ornament, cleaner railings, and a stronger social destination read.
- `boomtown`: most expressive saloon variant; richest porch and facade cues, strongest social presence, and the most ornament the family can support without becoming a different building.

### Telegraph office

- `destitute`: communications function survives, but the building is rough and plainly serviced; pole, notice, and utility cues are minimal or battered.
- `poor`: compact and practical with repaired walls, usable communications hardware, and restrained notices; the building should feel busy enough to operate, not polished enough to impress.
- `prosperous`: better-kept administrative frontage; cleaner utility cues, clearer notices, and stronger order in the facade.
- `boomtown`: most finished telegraph expression; richest utility and administrative cues, but still compact and practical.

## Prompt-ready guardrails

Use these as copyable prompt blocks when generating or revising town-building art.

- Do: Keep the image as a top-down slight oblique pixel-art western building with a 60x50 footprint and a five-view turnaround, and make the prosperity tier visible through finish, maintenance, ornament, and signage rather than through camera changes or a different building family.
- Do not: Do not switch to a street-level view, a flat elevation, a painterly treatment, a different footprint, a different turnaround count, or a new building family; do not let the tier read through labels, captions, borders, or post-processing.

- Do: For the general store, keep a commerce-first read with porch or awning, merchandise frontage, and clear goods cues, and vary the amount of polish so the same family can read as destitute, poor, prosperous, or boomtown without changing its core shape.
- Do not: Do not let the general store drift into a sheriff office, saloon, or telegraph office read; do not remove the retail frontage language, and do not replace the commerce cues with official markers, entertainment cues, or communications hardware.

- Do: For the sheriff office, keep the authority-first read with a restrained facade, plain official windows, and any badge or jail-bar cues needed to keep it unmistakably law-enforcement rather than retail.
- Do not: Do not add storefront clutter, stacked goods, broad retail signage, or saloon-style social frontage; do not let the sheriff office collapse into the general store silhouette or a generic frontier shack.

- Do: For the saloon, keep the hospitality and social read with public frontage, porch rhythm, and a stronger entertainment-facing silhouette, while varying polish by tier instead of changing the building identity.
- Do not: Do not strip away the social frontage, do not turn the saloon into a store or office, and do not over-formalize it into a civic building that loses the saloon read.

- Do: For the telegraph office, keep the communications and administration read with compact practical frontage, posted notices, and service-oriented cues that remain legible at town scale.
- Do not: Do not turn the telegraph office into a store or saloon, do not over-ornament it into a boomtown showpiece that loses its practical identity, and do not remove the communication cues that make the family distinct.

If a generated asset does not match the tier description above, the prompt should be rewritten rather than asking visual inspection to reinterpret the tier after the fact.

## Current master set

The canonical building families are:

- general store
- sheriff office
- saloon
- telegraph office

No other building families are part of the current master set.

## Style boundaries

- Keep each family distinct at town scale.
- Keep the silhouette simple enough to survive 5-view turnaround work.
- Keep the read western, practical, and grounded.
- Leave room for prosperity tiers by varying finish, maintenance, signage, trim, and ornament rather than the family identity, camera, or footprint contract.
- Use visual inspection to confirm the generated image obeys the written contract, not to discover what the contract means.
