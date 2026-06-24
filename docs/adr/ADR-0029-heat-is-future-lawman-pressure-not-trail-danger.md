# ADR-0029 Heat Is Future Lawman Pressure, Not Trail Danger

## Status

`live`

## Dated Status History

- 2026-06-24 - live (BUNCH-85): Heat reframed as future lawman pursuit pressure. Per-travel-day route-risk heat increase removed (`GameSession.PrepareTravelDayAdvance` no longer calls `PursuitState.IncreaseHeat` from `RouteProfile.Risk`). Heat increases removed from all four bad-luck trail events (washout, dust-choked outfit, spooked horse, hard miles) because they are private hardship / generic route difficulty, not noisy/visible/witnessed incidents. Encounter run/fight/bribe heat increases retained as visible/noisy incidents. `PursuitState`, `PursuitHeat`, `PursuitHeatBand`, and the `pursuitState` persistence component name retained as low-value-to-rename with clarifying XML docs. `TrailEventHeatIncrease` field retained in `TravelRulesProfile` as a reserved knob for future noisy/witnessed trail events. Active travel cooling (heat decreases per travel day) is deferred to a follow-up issue — this slice does NOT claim travel cools heat by distance. Lawman pursuit system stays a seam. HUD, field report, and travel diary labels changed from "Heat" to "Lawman heat".

## Decision Type

gameplay, architecture

## Related ADRs

- `depends on`: ADR-0013, ADR-0020, ADR-0028
- `related to`: BUNCH-85

## Context

The heat model lived in `PursuitState.Heat` with no ADR defining what heat means. Two mechanics made heat behave as trail/route danger rather than future lawman pressure:

1. **Per-travel-day route-risk heat increase** (`GameSession.PrepareTravelDayAdvance`): every travel day raised heat by `Math.Max(1, (int)RouteProfile.Risk)` — a direct "heat = route danger" mechanic. A quiet day on a moderate-risk route raised heat by 2; a quiet day on a high-risk route raised it by 3. This contradicted the BUNCH-85 design decision that heat is not current route danger.

2. **Private-hardship trail-event heat increases**: all four bad-luck trail events (washout, dust-choked outfit, spooked horse, hard miles) carried `heatIncrease: TrailEventHeatIncrease`. These events are private hardship or generic route difficulty — a washout on a lonely trail, a dust storm stripping supplies, a canyon echo spooking a horse, hard miles on a mean trail. None are noisy, visible, witnessed, or attention-generating in a way that would draw lawman attention.

ADR-0013 warned that "travel wording could become misleading if it starts implying future systems like richer lawman or route-simulation mechanics." ADR-0020 and ADR-0028 both reference "future pursuit/lawman" sub-aggregates as expected boundaries. No ADR defined heat's meaning until ADR-0029.

## Decision Drivers

- Heat must mean future lawman pursuit pressure, not trail danger, route danger, generic encounter risk, or reputation. (BUNCH-85)
- Travel should not raise heat just because the route is risky. (BUNCH-85)
- Heat increases should come from noisy, visible, witnessed, or attention-generating behavior — not private hardship or generic route difficulty. (Harley correction, 2026-06-24)
- Active travel cooling is a separate feature, not this slice. (Harley correction, 2026-06-24)
- The lawman pursuit system is not implemented in this slice. (BUNCH-85 non-goals)
- Identifier renaming (`PursuitState` → `LawmanPressureState`) would force a persisted-component migration with no clear semantic payoff. "Pursuit" is close to "lawman pursuit pressure" and not actively misleading. (Decision A — approved to keep stable)

## Decision

**Heat is future lawman pursuit pressure.** It represents lawman/town attention that accumulates from noisy, visible, witnessed, or attention-generating behavior (fights, bribes, confrontations, conspicuous trail incidents). A future lawman system will consume heat as a pressure clock; until then no lawman catches the player on the trail.

**Removed heat sources:**
- Per-travel-day route-risk heat increase (`RouteProfile.Risk` → `IncreaseHeat`).
- Bad-luck trail-event heat increases (washout, dust-choked outfit, spooked horse, hard miles) — private hardship, not noisy/visible/witnessed.

**Retained heat sources:**
- Encounter run/fight/bribe heat increases — visible/noisy incidents that draw future lawman attention.
- `TrailEventHeatIncrease` field in `TravelRulesProfile` — reserved as a tuning knob for future noisy/witnessed trail events. No current event wires it in.

**Retained identifiers (Decision A — keep stable):**
- `PursuitState`, `PursuitHeat`, `PursuitHeatBand`, `PursuitStateDto`, `pursuitState` (persistence component). Clarified via XML docs and this ADR rather than renamed.

**Deferred:**
- Active travel cooling (heat decreases per travel day) — follow-up issue.
- Lawman pursuit system, lawman encounters, lawman AI, route interception, custody, arrest, escape, town alarm escalation, wanted-state redesign — all out of scope.

## Consequences

- Travel no longer raises heat for route risk. A quiet day on any route leaves heat unchanged.
- Private-hardship trail events no longer raise heat. A washout, dust storm, spooked horse, or hard miles leaves heat unchanged.
- Encounter run/fight/bribe still raises heat — these are visible/noisy incidents.
- `PursuitHeatBand` (Calm/Wary/Hot/Hunted) still affects foe profiles and bribe costs — a hot player draws tougher/more-greedy riders. This is lawman-pressure-shaped attention, not trail danger.
- Characterization tests updated to encode the new heat semantics.
- Player-facing labels changed from "Heat" to "Lawman heat" in HUD, field report, and travel diary.
- No persistence migration required — `pursuitState` component name and `PursuitStateSnapshot` shape are unchanged.

## Implementation Status or Plan

Implemented in BUNCH-85. See `.superpowers/plans/2026-06-24-bunch-85-heat-lawman-pressure.md`.

## References

- BUNCH-85: Reframe heat as future lawman pressure, not trail danger
- ADR-0013: Travel journey is a session-owned aggregate subtree (warned about travel wording drift)
- ADR-0020: Aggregate domain authority and root persistence posture (future pursuit/lawman sub-aggregates)
- ADR-0028: Onion, DDD, CQRS, Event Sourcing, and projections posture (PursuitHeat event fields, future pursuit/lawman)
