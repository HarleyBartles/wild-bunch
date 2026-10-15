# ADR-0047 Add Playwright for Native Browser Contracts

## Status

`live`

## Dated History

- `2026-10-15` - Added Playwright Test with Chromium for automated native browser proof of nested modal focus, background inactivity, dismissal and restoration, and visible narrow-viewport geometry.
- `2026-10-15` - Clarified that the Playwright lane also covers selected retained player flows when browser interaction, real API state, and page-reopen recovery need repeatable end-to-end proof.

## Decision Type

`testing`

## Related ADRs

- `partially supersedes`: ADR-0017
- `related to`: ADR-0022, ADR-0043

## Context

Vitest with JSDOM establishes component and route behavior but does not implement native dialog top-layer behavior, modal inertness, browser focus restoration, or rendered geometry. The accepted 0.1.0 web contract retains keyboard and focus behavior for supported controls, and manual browser evidence alone does not provide a repeatable regression gate.

## Decision

Use Playwright Test with one Chromium project for automated browser behavior that depends on native dialog, focus, inertness, or layout semantics, and for selected retained player flows whose browser interaction, real API state, and page-reopen recovery need repeatable end-to-end proof. Keep Vitest with Testing Library as the component and route behavior lane, and keep the manual browser evidence lane in ADR-0022 separate. A passing Chromium suite proves only the browser cases it actually asserts; it is not cross-browser certification or a complete accessibility conformance claim.

## Rationale and Alternatives

Chromium supplies the platform dialog, focus, inertness, and layout behavior that JSDOM omits while keeping this regression lane small. A multi-engine matrix would expand the baseline cost without evidence that this bounded contract needs it; JSDOM and source/CSS assertions cannot replace the native browser behavior.

## Consequences

The browser suite requires a Playwright-matched Chromium installation locally and in hosted CI, and its tests run through the repository command bus. Use it selectively for native browser contracts and retained player flows that need the real browser-to-API recovery boundary; it does not replace focused component, domain, application, or persistence tests. Changes to the browser-engine matrix require a later evidence-backed decision.
