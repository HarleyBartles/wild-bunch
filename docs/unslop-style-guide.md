# Unslop Style Guide

This guide keeps Wild Bunch sounding like a hand-built Western adventure game instead of a generic AI-generated product shell.

## What We Want

- Concrete over abstract.
- Game terms over platform terms.
- One clear thought per sentence.
- Copy that sounds like a person who knows this game world.
- Small, boring implementation with specific player-facing flavor.

## What Slop Looks Like Here

Avoid language that could fit any app:

- "thin command surface"
- "hero metrics"
- "dashboard"
- "workflow"
- "surface"
- "command center"
- "unlock the full experience"
- "streamlined"
- "intuitive"

Those phrases do not tell the player anything specific about Wild Bunch.

## Voice Rules

- Prefer plain statements over marketing copy.
- Prefer game actions over system descriptions.
- Prefer actual nouns from the game: town, trail, case file, warrant, wanted poster, sheriff, horse, inventory.
- Keep metaphors controlled. One strong metaphor is enough; do not stack three.
- Write like the world has weight. Do not write like a product demo.

## UI Copy Rules

- Title screens should say what the player can do, not what the UI is.
- Buttons should name the action in the game world.
- Helper text should explain consequence or state, not architecture.
- Status text should be brief and factual.
- If a label would make sense in a SaaS admin panel, rewrite it.

Good examples:

- "Start a new hunt"
- "Open case file"
- "Read wanted posters"
- "Check local records"
- "Travel one day"

Avoid:

- "Open dashboard"
- "View session state"
- "Execute action"
- "Navigate to panel"

## Visual Language Rules

- Pick one visual thesis and commit to it.
- Use texture, contrast, and proportion intentionally.
- Do not rely on the default glassmorphism dark dashboard look unless it is clearly justified.
- Avoid UI chrome that feels copied from generic admin templates.
- Make the layout feel like a field kit, ledger, board, or notebook if that fits the screen.

## Code and Naming Rules

- Use domain nouns first, framework nouns second.
- Prefer one canonical formatter or describer for a concept.
- Do not create helpers just to sound modular.
- If a helper exists only to make generated code look tidy, remove it.
- Keep orchestration roots honest. They may be large if the domain actually requires it, but they should not accumulate vague abstractions.

## Docs Rules

- State decisions plainly.
- Use short examples from this repo instead of abstract policy paragraphs when possible.
- Avoid repeating the same idea in three forms.
- If a document exists to guide contributors, make it actionable within the first few lines.

## Sentence Shape

- Vary sentence length.
- Prefer simple declarative statements.
- Do not start every paragraph with the same framing phrase.
- Cut filler words such as "leverages", "robust", "seamless", "comprehensive", and "thin".

## Review Check

Before merging new copy or documentation, ask:

1. Would a player recognize this as Wild Bunch language?
2. Could this have been pasted into another repo with almost no change?
3. Does the sentence say something specific, or just sound polished?
4. Is there one clear action, or three layers of abstraction?

If the answer to any of those is shaky, rewrite it more concretely.
