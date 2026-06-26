# Dev Overlay Doctrine — State/Action Boundary, Panel Ownership, and Mesh Discipline

This is the binding agent-facing doctrine for the Dev Overlay and Playtest Control Plane. All dev-overlay work must follow it. The Linear document "Doctrine — dev overlay state/action boundary" is the upstream source; this file is the repo-persisted version that future workers encounter through the agents mesh.

## 1. State/action boundary

The dev overlay may mutate game state to set up, inspect, stabilize, or clear the conditions needed for a playtest. It must not force normal gameplay actions or directly fabricate their outcomes.

- **Allowed:** mutate state so the desired consequence can be reached through normal play.
- **Not allowed:** force the gameplay action or final gameplay result itself.

The overlay sets up descriptors, known facts, staged state, resources, location, route state, encounter state, RNG policy, and other preconditions. Then the normal game resolves the player action through its existing commands, rules, validation, and events.

This mirrors the backend command/query split. Dev overlay commands are explicit state-mutation commands with dev-event receipts. Normal gameplay commands remain normal gameplay commands. A dev command must not masquerade as the player having taken a gameplay action.

### Thought experiment

For every proposed dev control, ask:

> Am I changing state so the normal gameplay action can happen, or am I forcing the gameplay action/result to have happened?

If "I am changing state," the control is probably valid. If "I am causing the gameplay action/result," the control is probably invalid.

**Valid dev state controls:** force a wanted suspect to be the current saloon POI, mark a suspect as known in the casefile, stabilize POI descriptors, set inventory/wallet/location/route/time state, lock RNG, clear a pending forced descriptor after consumption.

**Invalid gameplay-action forcing:** force the sheriff to accept a prisoner, force bounty payout, force accusation success, mark a suspect as captured without the take-in flow, mark a trail encounter as resolved without playing its normal resolution.

## 2. Panel ownership model

Dev panels are organized around owned game-domain nodes, not around incidental endpoint shapes.

A panel owns one primary node or domain surface. It may deeply inspect and manipulate that owned node. It may show related nodes and expose light controls for them, but deep edits to a related node belong in that node's owning panel.

Rule of thumb: deep controls belong to the panel that owns the noun; light controls are contextual shortcuts for adjacent nouns.

A related-node control must either perform one narrow, obvious adjacent action or focus/open the owning panel for that node. One panel must not become a universal editor.

### Expected ownership direction

- **Session dev** owns game/session-level setup: difficulty, randomness, entropy/seed posture, current phase, high-level scenario setup.
- **Session Audit** owns event/log/read-model inspection — primarily read-heavy.
- **Player dev** owns player state: wallet/cash, inventory, health/condition, player-owned flags.
- **Casefile dev** owns player knowledge: clues, known warrants/posters, known aliases, learned descriptors, known suspect links, current casefile contents versus available hidden truth.
- **Suspect dev** owns suspect truth/configuration: true culprit assignment, descriptors, aliases, bounty/warrant facts, status, false-lead role, saloon eligibility/presence, suspect-level gates.
- **Saloon dev** owns saloon encounter state: active POI, pending override, source spent/unspent where supported, clear/reset active POI, force next look-around result, candidate selection, confrontation setup, saloon-specific encounter/gate behavior.
- **Travel dev** owns journey/trail state.
- **Horse dev** becomes first-class only when horse behavior warrants its own panel; until then, horse controls live under Player or Travel dev as adjacent setup controls.

## 3. Related panel visibility and defaults

Contextual does not mean only one panel appears. The overlay must show the current surface's owning panel plus panels that own immediately related nodes.

A panel should be visible when it owns the current surface, owns a domain node currently present on that surface, or owns a domain node linked by a visible panel. If a dev panel links to or displays another panel's owned node, the owning panel must be available in that context once that panel exists.

For saloon, the expected related panel set is at least Saloon dev, Suspect dev, and Casefile dev. Session Audit may be globally available, but it must not win the default over a surface-specific or node-specific panel.

### Default panel selection

Default panel selection should prefer the strongest current surface owner:

- Saloon surface → Saloon dev.
- Trail/journey surface → Travel dev.
- Sheriff/casefile-heavy surface → Casefile dev once available.
- Store/player-inventory-heavy surface → Player or Inventory dev depending on panel split.
- Session Audit defaults only when nothing more specific is available.

## 4. Layout doctrine

The dev overlay may become large enough to support real playtest work, but must not gratuitously dominate or waste space.

- **Compact mode** may stack sections vertically.
- **Expanded mode** is a workbench and should use available width to reduce unnecessary height. Do not build a very tall single-column panel while large space is unused to the right.
- Expanded panels should compose state, entity data, and controls into cards/columns/regions with local scroll areas for long lists (suspects, clues, case facts, audit events).
- The overlay shell should cap height and permit internal scrolling; individual panels are responsible for making their expanded layout useful.

### Expand/shrink label semantics

The toggle button label describes the action the user will take, not the current state:

- When the overlay is currently **compact**, the button says **"Expand"** (click to expand).
- When the overlay is currently **expanded**, the button says **"Shrink"** (click to shrink).

## 5. Hidden truth and normal API boundary

The overlay may show hidden truth and internal diagnostics through dev-only query endpoints. Normal player APIs and read models must not newly leak hidden truth or gain dev-only mutation powers.

Hidden truth should be useful, not merely sensational. For saloon/case/suspect work, useful hidden truth includes eligibility gates, case truth, current player-known state, currently hidden-but-available facts, clue links, suspect descriptors, warrant/bounty facts, and why a candidate can or cannot appear.

### True culprit doctrine (gate-aware)

The true culprit is **not permanently barred** from appearing as a saloon POI. If the current implementation gates the culprit out until a killer-release gate exists or opens, dev UI and DTOs must say the current gate/eligibility state rather than "true culprit can never appear."

## 6. Suspect, warrant, and casefile composition

Suspect descriptors include warrant-shaped facts where those facts are part of the suspect's make-up: bounty amount, wanted/poster facts, aliases, identifying marks, descriptor clues, and other facts a player may use to reason about identity.

Casefile state and suspect truth are related but not the same:

- **Suspect truth** is what is true in the run.
- **Player casefile state** is what the current playthrough knows.
- **Available-but-not-known facts** are game truth that could enter the casefile through clue collection or dev setup.

Casefile dev owns deep player-knowledge manipulation. Suspect dev owns deep suspect-truth manipulation. Saloon dev may provide light controls for the active POI/suspect/casefile facts when needed to test the saloon loop.

## 7. Backend authority

Dev mutations go through backend commands, `GameSession` or the established aggregate route, and immutable dev events. The frontend dev overlay UI sends explicit dev-control intents to the backend; it must not locally fake player progress, inject final results into the UI, or bypass the game command path.

Dev commands produce dev-event receipts that are part of the event stream. Normal gameplay commands remain normal gameplay commands. A dev command must not masquerade as the player having taken a gameplay action.

## 8. Context mismatch detection

If the visible UI surface says one thing (e.g., saloon) but the aggregate action context says another (e.g., SheriffOffice or a stale context), the dev overlay must not present that contradiction as normal. Either fix the stale transition or show both the UI surface and the aggregate context with a clear mismatch warning.

## 9. Closeout proof

Dev-overlay work must provide:

- Event-stream proof for dev force → normal gameplay consumption → normal gameplay outcome.
- Screenshots showing the panel in compact and expanded mode.
- Screenshots showing default panel selection for the surface.
- Screenshots showing candidate dropdowns (not raw ID typing) for any force control.
- Screenshots showing resolved names and domain meaning for displayed fields.
- Test results for backend domain/application/API and frontend tests.
