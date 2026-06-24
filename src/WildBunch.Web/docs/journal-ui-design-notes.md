# Journal UI Design Notes

These notes capture the design language learned while shaping the Journal surface.

## What The Journal Is

- The Journal is a player-owned, in-world surface.
- It should read like a notebook the character pulled from their saddlebags, not like a debug panel or a case dashboard.
- It does not need full skeuomorphism today, but it must be unmistakably a journal.

## Placement

- Put real player UI in the game HUD or game shell.
- Do not tack durable player-facing UI onto the dev cockpit.
- The cockpit can stay temporary and utilitarian; the Journal should live where the player expects to find it in the game.

## Copy Rules

- Prefer plain, concrete journal language.
- Keep the title and entry text doing the work.
- Avoid filler that sounds like product chrome or investigative dashboard labels.
- Avoid phrases that do not change the player's understanding of the page.

## Phrases And Patterns To Avoid

- `Journal entries: 1`
- `Notes from the trail`
- `The hunt begins`
- `Find the culprit before the law closes in`
- Any label that makes the page feel like a status board instead of a journal

## Preferred Shape

- A clear Journal title
- A simple date and place line
- Daily entry groups
- Entry text only, unless a tiny field is needed to preserve meaning

## Review Question

When adding something to the Journal, ask:

- Does this help the player understand the page as a journal?
- Does this tell them something they would plausibly have written or carried?
- Or is it just decorative UI fluff?

If it is fluff, remove it.
