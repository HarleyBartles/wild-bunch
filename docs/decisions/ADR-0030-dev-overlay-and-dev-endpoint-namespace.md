# ADR-0030 Contextual Developer Controls and Separate API Namespace

## Status

`partially superseded`

## Dated History

- `2026-06-25` - Chose a contextual developer overlay attached to the game surface, a separate developer API namespace, and a centralized access guard.
- `2026-06-26` - Added the saloon developer surface and clarified that deliberately guarded developer DTOs may expose hidden truth without changing the player-facing boundary.
- `2026-10-08` - Partially superseded the general-surface developer access direction through ADR-0041: public deployment is player-only, and existing developer capabilities belong in owner-restricted preprod.

## Decision Type

`architecture`, `ui`, `security`

## Related ADRs

- `depends on`: ADR-0007, ADR-0028
- `partially supersedes`: ADR-0016
- `partially superseded by`: ADR-0041
- `related to`: ADR-0031, ADR-0032, ADR-0036

## Context

A standalone developer cockpit competed with normal play. Playtesting also needs developer diagnostics and controls that may reveal hidden state, while ordinary player APIs must remain limited to player knowledge.

## Decision

Developer controls are a contextual surface attached to the game experience and remain distinct from ordinary player navigation. Developer APIs use a separate namespace and one centralized access guard. Developer responses use distinct DTOs; hidden truth may be exposed only through deliberately scoped developer capabilities. None of these allowances apply to player APIs or player-facing projections.

The contextual controls and namespace are available only in an explicitly developer-enabled environment. ADR-0041 governs their planned deployment boundary: public is player-only, while preprod is restricted to the owner's verified identity.

## Rationale and Alternatives

A separate developer cockpit duplicates player flow and breaks playtest context. Reusing player endpoints for diagnostics makes hidden-truth policy depend on callers and frontend discipline. A contextual surface and distinct guarded API keep the development capability visible and bounded.

## Consequences

A developer surface may inspect or control game state only when its environment and centralized guard allow it. Developer state and DTOs never become a second game authority or a shortcut into player-facing reads. Specific panel layouts and endpoint inventories are implementation details owned by current source and doctrine.

## Successors and Surviving Scope

ADR-0016's general client stack survives; this record replaces its cockpit-era assumption for developer tools with a contextual overlay. ADR-0041 partially supersedes any direction to expose developer capabilities in a public deployment. The contextual overlay, separate developer namespace, centralized guard, and player-versus-developer truth boundary survive inside a developer-enabled environment.
