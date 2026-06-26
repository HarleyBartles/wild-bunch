# Wild Bunch Web Play-Surface Unslop Profile

Project-specific anti-slop profile for `src/WildBunch.Web`.

Use this profile before designing, implementing, reviewing, or dispatching work on Wild Bunch web play surfaces, HUD/shell placement, overlays, cockpit transitions, modal surfaces, journal/case/wanted surfaces, travel UI, player-facing copy, and React state ownership.

## Purpose

Stop generic React/product UI defaults from entering the Wild Bunch web app.

A good Wild Bunch web surface should help Harley playtest a real game moment, understand what happened, or make the next choice. It should feel like a specific playable Western adventure surface, not a SaaS dashboard, debug cockpit panel, generic modal library demo, decorative landing page, or AI-generated “polished” screen.

## Profile Scope

This profile is a review filter, not a complete product spec. It does not replace the active issue goal, repo architecture, accessibility requirements, or Harley's explicit direction. If those conflict, stop and reconcile the conflict instead of blindly applying this document.

Quick scan before touching player-facing web UI:

- Playable value first.
- Durable player UI belongs in HUD/shell or another player-facing route, not the debug cockpit.
- The surface should feel game-native, not like product chrome.
- Do not force visible modal titles or header chrome.
- Do not invent frontend-only game truth.
- Name state ownership explicitly.
- Preserve accessibility, focus, responsive, empty, loading, error, and disabled behavior where relevant.

## Source Profiles Applied

This profile combines:

- Unslop+ `frontend-react`
- Unslop+ `frontend-ui`
- Wild Bunch web play-surface guidance
- Current repo examples from the Journal HUD/shell/modal implementation
- Harley's constraints: no forced modal titles, no Journal-only house style, playable value first, no theme veneer, no frontend-invented truth

## Core Rule

Player-facing game UI is a play surface.

A play surface should read like something the player character could use, carry, consult, see, or act through inside the game. It does not need heavy skeuomorphism, but it must belong to the game world more than to a debug cockpit or product dashboard.

## Avoid Patterns

### 1. Dashboard drift

Avoid UI that turns player surfaces into status dashboards.

Do not add:

- decorative counters that repeat visible content
- `entries: N` labels unless the count affects a player decision
- generic status-board labels
- dashboard cards for data that should read as a journal, poster, town notice, trail note, saddlebag item, store counter, or player action surface
- raw DTO-shaped sections
- debug headings such as `Activity log` when the game concept is `Journal`

Bad shape:

> A Journal modal with `Journal entries: 1`, `Recent activity`, `Status`, and a generic feed count.

Better shape:

> A Journal surface with the date/place and grouped day entries, using only text the player would plausibly have written, remembered, carried, or consulted.

### 2. Cockpit leakage

Do not place durable player-facing UI in `DebugCockpitRoute` just because that is currently convenient.

The dev cockpit can stay utilitarian. Durable play surfaces belong in the game HUD, shell, overlay system, or a future player-facing route.

Bad shape:

> Add a new Journal button and modal to the debug cockpit.

Better shape:

> Add the Journal trigger to the HUD/shell and render it through the shared overlay path.

### 3. Product chrome copy

Avoid copy that sounds like SaaS or product UI rather than the game.

Avoid filler such as:

- `Notes from the trail`
- `Find the culprit before the law closes in`
- `Journal entries: 1`
- `Activity log`
- `Your progress`
- `Overview`
- `Dashboard`
- `Explore insights`
- `Manage your case`

Use plain concrete language:

- `Journal`
- `Wanted posters`
- `Case file`
- `Day 5, Morning in Tumbleweed`
- `Started out in Tumbleweed.`

Copy should orient the player, preserve meaning, or support a choice. If it only makes the page feel busier, cut it.

### 4. Modal chrome pretending to be game UI

Do not force product-style modal titles, subtitles, descriptions, or header chrome onto play surfaces.

A modal or overlay is only a delivery mechanism. The thing inside it should still read as an in-world game surface: a journal page, wanted poster, case file, town notice, saddlebag note, store counter, trail map, saloon exchange, or similar.

Avoid:

- mandatory visible modal titles just because a dialog component has a title slot
- subtitles that explain the obvious
- repeated titles where the surface already identifies itself
- corporate-modal header chrome competing with the in-world surface
- styling the modal frame as the main object instead of the play surface inside it

Prefer:

- make the surface content carry its own identity
- use the smallest accessible label needed for screen readers and focus management
- hide, soften, or omit visible modal chrome when it competes with the game surface
- keep close/back controls available without turning the surface into product UI

Accessibility rule: dialogs still need an accessible name. That does not require a big visible product-style title. Use a surface-native heading, visually hidden label, `aria-label`, or concise accessible label when that better fits the game surface.

### 5. Theme veneer

Western nouns and sepia styling do not make a surface in-world.

Avoid product UI wearing a cowboy hat:

- `Sheriff Dashboard`
- `Trail Insights`
- `Case Overview`
- `Outlaw Metrics`
- `Journey Analytics`
- `Progress Hub`

Prefer a surface that maps to something in the fiction or to a concrete player action.

A wanted-poster surface should feel like posters or notices. A store surface should feel like a store counter or buying exchange. A trail surface should help the player understand the trail day, route, risk, supplies, and choice. A saloon surface should support the social/action moment rather than become a CRM panel for NPCs.

### 6. Over-generalizing from Journal

The Journal is an example, not the house style for every surface.

Do not make every game surface look like a notebook, parchment card, or diary page. A wanted poster, case file, store counter, town notice, trail map, saddlebag, saloon interaction, and trail-day choice can each have its own shape.

The shared rule is: player-usable, game-native, and task-supporting.

### 7. Generic React abstraction

Do not create reusable infrastructure before the feature proves it needs it.

Avoid:

- generic `Modal`, `Card`, `Dashboard`, `Panel`, `Surface`, `Layout`, or `Provider` abstractions without current repeated use
- flexible prop APIs for future variants
- context providers for local UI state
- reducers for simple overlay state
- custom hooks that only wrap one component
- `useMemo` or `useCallback` as reflexive performance theater

Prefer:

- a small feature component with the domain name, such as `JournalSurface`
- parent-owned state where the parent owns the interaction, such as shell-owned overlay state
- existing shared primitives when they already exist, without letting their old names force product/cockpit semantics
- new abstractions only after at least two real uses or a clear architectural boundary

### 8. State ownership drift

Every UI slice must name where state lives.

State choices should be explicit:

- Local component state for small UI-only toggles
- Shell/HUD state for global overlay ownership
- React Query/server state for API data
- URL state only when the location should be shareable or navigable
- Domain/API state only when the game state changes

Bad shape:

> The Journal opens from cockpit local state while the HUD also has overlay buttons and the shell has global overlays.

Better shape:

> The shell owns active overlay state; the HUD sends the Journal open intent; the overlay layer renders the selected surface.

### 9. Frontend-invented truth

A play surface may rephrase player-known data, but it must not create new canonical facts.

Do not infer or display:

- hidden culprit truth
- suspect certainty the domain has not exposed
- learned/unlearned flags the game does not model
- backend-only ids as player facts
- frontend-only canonical case state
- player knowledge flags that do not exist in the domain
- `confidence`, `known`, or `solved` states unless the backend/read model explicitly exposes them as player-known

If the UI needs a new player-known fact, stop and shape the backend/read-model change instead of inventing it in React.

### 10. Hidden-truth leakage

Never render hidden game truth in player-facing UI.

Player-facing surfaces must not show:

- `trueCulpritId`
- `isTrueCulprit`
- `linkedSuspectIds`
- `killerReleaseState`
- backend-only ids
- raw DTO/debug dumps
- hidden culprit truth
- internal suspect linkage that the player has not learned
- mechanics-only fields unless explicitly translated into player-facing language

Tests should prove absence of these leaks when a surface could accidentally expose them.

### 11. Visual polish without playable value

Do not accept `looks better` or `more polished` as enough.

A UI change should help Harley playtest the game. It should do at least one of:

- reveal player-known state clearly
- support a real player choice
- show a consequence of a recent action
- make a game loop easier to understand
- expose a meaningful difference between states
- reduce confusion in a current playtest flow

Visual style supports playable value. It does not replace it.

### 12. Visual polish without interaction proof

Do not accept `looks modern` as a UI requirement.

A UI change must name:

- the player task
- primary action
- secondary actions
- disabled/loading/empty/error states where relevant
- keyboard/focus behavior for dialogs and overlays
- mobile behavior
- hidden-truth safety for game surfaces

Visual style supports those constraints. It does not replace them.

## Prefer Patterns

### 1. Play-surface-first design

Start by naming the in-world surface.

Examples:

- Journal: a notebook the character could carry
- Wanted posters: a town notice-board surface
- Case file: a player-known investigation board
- Travel diary: a record of what happened on the trail
- Store/trader surface: an in-world buying exchange, not an ecommerce dashboard
- Trail-day choice: a moment on the trail, not a generic decision modal
- Saloon interaction: a social/action moment, not an NPC CRM record

Then design only the chrome needed for that surface.

### 2. HUD/shell placement for durable player UI

Durable player features should enter through the game HUD or shell, not debug cockpit scaffolding.

Use the cockpit only for:

- temporary developer flows
- inspection utilities
- scaffolding while the real player surface is not shaped yet

When a feature becomes real playable value, move it out of cockpit ownership.

### 3. Small concrete components

Prefer feature-named components over generic components.

Good:

- `JournalSurface`
- `CaseFileSurface`
- `WantedPosterSurface`
- `TravelRoutesPanel`

Risky unless already justified:

- `DashboardSurface`
- `GenericOverlay`
- `InfoCard`
- `EntityPanel`
- `SurfaceRenderer`

### 4. Existing overlay behavior without inherited chrome

When using modal overlays, preserve:

- `role="dialog"` / `aria-modal="true"` when it is a true modal
- an accessible name for the dialog
- Escape close
- backdrop close when appropriate
- focus moves into the dialog
- focus returns to the previous control
- mobile layout that does not trap content offscreen

Do not force visible modal title/subtitle chrome. Treat the modal as a way to present a game surface, not as the surface itself.

If the current shared overlay primitive has title/header slots, use them only when they serve the play surface. Otherwise use a surface-native heading, a visually hidden label, or another accessible naming mechanism.

### 5. Plain concrete copy

Prefer title + context + content.

Good:

- `Journal`
- `Day 5, Morning in Tumbleweed`
- `Started out in Tumbleweed.`
- `Wanted posters`
- `Posters read from town notice boards.`

Bad:

- `Activity log`
- `Journal entries: 1`
- `Notes from the trail`
- `Find the culprit before the law closes in`
- `Dashboard`
- `Overview`
- `Progress tracker`

### 6. Behavior-first tests

Tests should prove behavior and safety, not implementation trivia.

Good test assertions:

- HUD owns the Journal trigger
- Debug cockpit does not expose the Journal surface
- Journal opens as a dialog
- Journal groups entries by day
- Journal does not render hidden/internal markers
- Escape/backdrop/focus behavior remains intact
- Mobile screenshot or browser proof exists for modal surfaces

Weak test assertions:

- CSS class exists
- arbitrary component internals exist
- mocked data appears without checking player meaning
- hidden fields are absent only because the fixture does not contain them

## Implementation Checklist

Before implementation:

- Name the play surface.
- Name the player task.
- Name what playable value this adds.
- Identify state ownership.
- Identify the data source.
- Identify whether the data is player-known or hidden/internal.
- Identify hidden-truth risks.
- Decide whether this belongs in HUD/shell, cockpit, route, or domain flow.
- Check existing components before creating new infrastructure.
- Decide whether visible modal/header chrome is game-native or should be omitted/softened.

During implementation:

- Start with the smallest feature-named component.
- Use existing typed API/client/query conventions.
- Use existing overlay/modal behavior unless it is insufficient.
- Do not let shared overlay primitive naming or header slots force cockpit/product chrome.
- Keep copy concrete and in-world.
- Remove decorative counters and dashboard labels.
- Avoid memoization/context/reducers unless the current behavior needs them.
- Preserve loading, empty, error, disabled, accessibility, and responsive behavior where relevant.
- Do not invent frontend-only game truth.

Before review:

- Run frontend validation.
- Check browser behavior, including mobile when layout changed.
- Check keyboard/focus behavior for overlays.
- Search rendered UI/tests for hidden-truth leaks.
- Confirm the dev cockpit did not become the durable player surface.
- Confirm any visible modal title/header chrome earns its place as game-native surface identity.
- Confirm the PR body reflects the actual implementation, not a stale plan.

## Review Questions

Ask these before accepting a web UI change:

1. What player task does this surface support?
2. What playable value does this add for Harley's playtesting?
3. Does the surface feel like something in the game world, or like product chrome?
4. Is this durable player UI placed in HUD/shell rather than dev cockpit?
5. Does every label/counter/callout help the player understand or act?
6. Is a visible modal title/header actually game-native, or just product modal chrome?
7. Is state ownership explicit and minimal?
8. Did the implementation avoid generic abstraction until needed?
9. Are loading, empty, error, disabled, keyboard/focus, and mobile states handled where relevant?
10. Does the UI avoid backend ids, hidden culprit truth, raw DTOs, and debug fields?
11. Does the UI avoid inventing player knowledge or canonical game facts in React?
12. Do tests prove player behavior and hidden-truth safety?
13. Does the PR body accurately describe the implementation and validation?

## Acceptance Checks

This profile passes only if it would stop a worker from:

- building a generic dashboard when a game surface is needed
- putting durable player UI into the debug cockpit
- forcing visible modal titles/header chrome onto game surfaces
- copying the Journal look into every surface
- adding decorative counters or filler copy
- creating generic React infrastructure before demand
- hiding unclear state ownership inside custom hooks/providers
- omitting keyboard/focus/mobile/empty/error behavior for overlays
- leaking hidden game truth into player-facing UI
- inventing frontend-only case/player knowledge
- polishing a surface without playable value
- calling a UI slice complete without browser evidence or meaningful tests
