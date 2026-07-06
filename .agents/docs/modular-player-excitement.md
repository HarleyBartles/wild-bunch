# Modular Player Excitement

## Doctrine

Modular player excitement is achieved through boring implementation.

The game should feel surprising, alive, and highly variable to the player, but that excitement should come from recombining predictable modules rather than from unpredictable code. Feature work should prefer small, stable, testable primitives that can later be authored, constrained, validated, and replayed through Cockpit or scenario tooling.

This doctrine is especially relevant to future Cockpit sandbox authoring work, including the long-range #45 direction, but it does not mean that feature is active or implemented now.

## What Boring Means

Boring implementation means:

- explicit state
- clear aggregate ownership
- narrow read models
- deterministic validation surfaces
- reproducible scenario shapes
- lawful module boundaries

The goal is not to make the game bland. The goal is to make the implementation dependable so the authored experience can stay rich.

## What Excitement Should Come From

Player-facing variety should emerge from combinations of boring primitives such as:

- case facts
- clues
- warrants and wanted posters
- route profiles
- encounter envelopes
- role mappings
- resource pressure
- authored beats
- randomness policies
- validation rules

Those primitives can be recombined into varied adventures, surprising combinations, authored scenario flexibility, and replayability across Boring, Classic, Adventurous, and Wild randomness envelopes.

## Good Shape

- Wanted posters are structured artifacts powered by warrants and case facts, so future UIs can render them differently without changing the legal truth model.
- Boring scenario fixtures and deterministic seeds make adventurous playback safer because known shapes can be tested.
- Travel day variety should come from route profile, pressure bands, encounter envelopes, and authored event pools, not ad hoc branches.

## Bad Shape

- A custom adventure bypasses CaseFile and GameSession rules with bespoke script code.
- A scenario editor allows arbitrary hidden truth mutations without validation.
- A one-off feature adds exciting text but no reusable primitive, no tests, and no future authoring seam.
- Player-facing variety is created by implementation chaos, hidden coupling, or clever randomness that cannot be explained or replayed.

## Implementation Rule

Keep modules composable. Keep aggregate boundaries lawful. Prefer deterministic validation routes. Make excitement emerge from recombination and constraints. Avoid one-off bespoke adventure logic.

This doctrine should guide current Wild Bunch work even before #45 becomes active: build present systems as reusable game primitives, not as single-use special cases.
