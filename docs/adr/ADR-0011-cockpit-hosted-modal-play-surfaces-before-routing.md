# ADR-0011 Cockpit-Hosted Modal Play Surfaces Before Routing

## Status

planned

## Dated Status History

- 2026-06-01 - planned: future major play surfaces should open as cockpit-hosted
  modals before they become permanent routes.

## Decision Type

ui, architecture

## Related ADRs

- `depends on`: ADR-0002, ADR-0007

## Context

The current repo does not expose a dedicated cockpit route or a mature modal
play shell in source. The design direction requested for this ADR is about how a
future play surface should evolve without jumping straight to route promotion.

## Decision Drivers

- The cockpit shell should remain the first host for temporary play surfaces.
- New play experiences should be testable before they become canonical routes.
- UI structure should grow deliberately instead of being route-first by default.
- The repo should not claim a route exists when it does not.

## Decision Summary

Future major play surfaces should first appear as cockpit-hosted modal overlays
with real API/query/component structure, then be promoted to canonical routes
only when the product is ready.

## Detailed Decision Breakdown

This planned rule keeps the cockpit shell as a staging area for new play flows.
It allows the product to establish behavior, legality, and state boundaries
before committing to permanent routing.

Because no cockpit route exists in the current source, this ADR is explicitly
about a future UI pattern rather than an implementation claim.

## Options Considered and Rejected

- Promote every major play surface directly into a permanent route.
- Keep new play surfaces unstructured until routing is final.
- Treat the cockpit as irrelevant to future play flow design.

## When a Rejected Option Would Have Been Better

Route-first UI would only be better if the new surface were already stable and
obviously canonical. Unstructured temporary UI would only be better for a tiny
throwaway debug panel.

## Benefits

- New play surfaces can be proven before routing hardens.
- The cockpit can absorb short-lived experimentation.
- Future route decisions become more deliberate.

## Accepted Tradeoffs

- The UI path is one step less direct than route-first implementation.
- The cockpit shell has to stay lightweight enough to serve as a staging host.

## Risks

- A temporary modal could linger too long if the route promotion decision is
  never revisited.
- The repo could accidentally imply a cockpit route exists before one is built.

## Consequences for Future Work

When a new major play surface appears, the first implementation slice should be
modal and cockpit-hosted unless a later ADR says otherwise.

## Implementation Status or Plan

Planned only. There is no dedicated cockpit route in the current source
surfaces.

## Related Stable Source Surfaces

- `src/WildBunch.Api/Games/`
- `src/WildBunch.Application/Games/`
- `tests/WildBunch.Integration.Tests/`

## Proof of Implementation or Explicit Non-Implementation

There is no current cockpit route or modal play shell in the source tree. This
ADR records the intended future direction only.

## Review Triggers

- When a new major play surface is ready to leave the cockpit shell.
- When route promotion becomes the clearer default for a specific flow.
