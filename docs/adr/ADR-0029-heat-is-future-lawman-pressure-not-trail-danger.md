# ADR-0029 Heat Is Future Lawman Pressure, Not Trail Danger

## Status

`live`

## Dated Status History

- 2026-06-25 - live (BUNCH-85 correction): Heat model simplified to town-time-only. Heat increases by 1 only when a full in-town day rolls over (turn 4 → next day turn 1). Heat resets to 0 when leaving town (starting a journey). Heat does not change on the trail — trail events and trail encounters do not affect heat. High/low heat has no mechanical effect yet. `PursuitHeatBand` removed from encounter generation, encounter resolution, and the day-plan seed string. All `IncreaseHeat` calls removed from trail-event and encounter paths. `PursuitHeat` added to `JourneyStarted` and `TownActionContextEntered` events for snapshot round-trip fidelity. Player-facing labels changed from "Heat" to "Lawman heat".
- 2026-06-24 - live (BUNCH-85 initial): Heat reframed as future lawman pursuit pressure. Per-travel-day route-risk heat increase removed. Bad-luck trail-event heat increases removed. Encounter run/fight/bribe heat increases temporarily retained. `PursuitHeatBand` temporarily retained in encounter generation.

## Decision Type

gameplay, architecture

## Related ADRs

- `depends on`: ADR-0013, ADR-0020, ADR-0028
- `related to`: BUNCH-85

## Context

The heat model lived in `PursuitState.Heat` with no ADR defining what heat means. Several mechanics made heat behave as trail/route danger rather than future lawman pressure:

1. **Per-travel-day route-risk heat increase**: every travel day raised heat by route risk level — a direct "heat = route danger" mechanic. A quiet day on a moderate-risk route raised heat by 2; a quiet day on a high-risk route raised it by 3.

2. **Private-hardship trail-event heat increases**: bad-luck trail events (washout, dust storm, spooked horse, hard miles) carried heat increases. These events are private hardship, not noisy/visible/witnessed incidents.

3. **Encounter heat increases**: encounter run/fight/bribe resolutions increased heat, treating trail encounters as lawman-attention-generating incidents.

4. **`PursuitHeatBand` in encounter generation**: a heat band (Calm/Wary/Hot/Hunted) derived from current heat influenced foe profiles and bribe costs in encounter generation, coupling trail encounters to heat level.

ADR-0013 warned that "travel wording could become misleading if it starts implying future systems like richer lawman or route-simulation mechanics." ADR-0020 and ADR-0028 both reference "future pursuit/lawman" sub-aggregates as expected boundaries. No ADR defined heat's meaning until ADR-0029.

## Decision Drivers

- Heat must mean future lawman pursuit pressure, not trail danger, route danger, generic encounter risk, or reputation. (BUNCH-85)
- Travel should not raise heat just because the route is risky. (BUNCH-85)
- Trail events and trail encounters should not affect heat. (Harley correction, 2026-06-25)
- High/low heat has no mechanical effect yet — no lawman system exists. (Harley correction, 2026-06-25)
- The lawman pursuit system is not implemented in this slice. (BUNCH-85 non-goals)

## Decision

**Heat is future lawman pursuit pressure.** It represents lawman/town attention that accumulates from time spent in town. A future lawman system will consume heat as a pressure clock; until then no lawman catches the player on the trail.

**Heat model (simple, first version):**
- Heat increases by **+1 only when a full in-town day rolls over** (turn 4 → next day turn 1).
- Heat **resets to 0** when leaving town (starting a journey).
- Heat **does not change on the trail** — trail events and trail encounters do not affect heat.
- **High/low heat has no mechanical effect yet.** No lawman system exists.
- Heat **starts counting again** when the player reaches the next town and spends time there.

**Removed heat sources:**
- Per-travel-day route-risk heat increase.
- Bad-luck trail-event heat increases (washout, dust storm, spooked horse, hard miles).
- Encounter run/fight/bribe heat increases.
- `PursuitHeatBand` influence on encounter generation and resolution.
- `PursuitHeatBand` from the `TravelDayGenerationContext` seed string.

**Retained identifiers:**
- `PursuitState`, `PursuitHeat`, `PursuitStateDto`, `pursuitState` (persistence component). Clarified via XML docs and this ADR rather than renamed.

**Out of scope:**
- Lawman pursuit system, lawman encounters, lawman AI, route interception, custody, arrest, escape, town alarm escalation, wanted-state redesign.
- Any mechanical effect from high/low heat.

## Consequences

- Travel no longer raises heat for route risk. A quiet day on any route leaves heat unchanged.
- Trail events no longer raise heat. A washout, dust storm, spooked horse, or hard miles leaves heat unchanged.
- Trail encounters (run, fight, bribe) no longer raise heat.
- `PursuitHeatBand` no longer exists in encounter generation or the day-plan seed. Encounter outcomes are determined by route profile, difficulty, and entropy — not by heat level.
- Heat only changes at two moments: town-day rollover (+1) and journey start (reset to 0).
- `PursuitHeat` is carried on `JourneyStarted` and `TownActionContextEntered` events so heat survives snapshot round-trips without pre-event mutation.
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
