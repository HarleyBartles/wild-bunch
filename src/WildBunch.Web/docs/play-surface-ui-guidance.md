# Play Surface UI Guidance

These notes capture the design language learned while shaping player-facing surfaces in the web app.

## What A Play Surface Is

- A play surface is a player-facing, in-world UI surface.
- It should read like something the player character could use, carry, or consult inside the game.
- It does not need full skeuomorphism, but it must clearly belong to the game world rather than to a debug cockpit or product dashboard.

## Placement

- Put durable player UI in the game HUD or game shell.
- Do not tack real player-facing surfaces onto the dev cockpit.
- Temporary cockpit scaffolding can stay utilitarian, but durable surfaces should live where the player expects to find them in the game.

## Copy Rules

- Prefer plain, concrete language.
- Keep the title and the surface content doing the work.
- Avoid filler that sounds like product chrome, dashboard chrome, or status-board labeling.
- Avoid phrases that do not change the player’s understanding of the surface.

## Shapes To Avoid

- Decorative counters that repeat what the surface already shows
- Status labels that do not help the player read the surface
- Filler phrases that only make the page feel busier
- Any label that makes the surface feel like a cockpit panel instead of a play surface

## Preferred Shape

- A clear, task-relevant title
- A simple context line when it helps orientation
- Content grouped by player-meaningful chunks
- Surface text only, unless a tiny field is needed to preserve meaning

## Example: Journal

The Journal is a good example of this rule set.

- It belongs in the game HUD or shell, not the dev cockpit.
- It should feel like a notebook the player character pulled from their saddlebags.
- It should stay unmistakably a journal without needing heavy skeuomorphism.
- It should avoid labels like `Journal entries: 1`, `Notes from the trail`, `The hunt begins`, and `Find the culprit before the law closes in` when those phrases do not help the player read the page.

## Review Question

When adding something to a play surface, ask:

- Does this help the player understand the surface?
- Does this tell them something they would plausibly use or carry in the game?
- Or is it just decorative UI fluff?

If it is fluff, remove it.
