# ADR-0041 Public and Preprod Developer Capability Boundary

## Status

`planned`

## Dated History

- `2026-10-08` - Planned a public player-only environment and an owner-restricted preprod environment for the existing developer capabilities.

## Decision Type

`operations`, `security`

## Related ADRs

- `partially supersedes`: ADR-0030
- `related to`: ADR-0031, ADR-0032

## Context

The game has developer commands and UI for one owner, but a public browser game must not expose those capabilities to ordinary players. Hiding the controls or relying on the development runtime mode does not establish an access boundary.

## Decision

The public environment admits any user with a verified Google account and exposes player APIs and UI only. Preprod admits only the owner's verified identity and exposes player capabilities plus the existing developer APIs and UI. The two environments have separate data and credentials. Developer access is an environment capability, not a public developer-role feature in the gameplay model.

## Rationale and Alternatives

The owner is the only developer, and public developer controls provide no player value. A separate preprod environment preserves those controls for integration and release-candidate work while keeping them outside the public game.

## Consequences

Each environment must enforce its admission and capability boundary in server configuration and shipped frontend artifacts. This is a planned deployment decision, not a claim about current 0.1.0 application behavior or hosted access control.

## Successors and Surviving Scope

This record partially supersedes ADR-0030's direction to expose developer controls in the general game surface. ADR-0030's contextual developer overlay and distinct developer endpoint namespace remain applicable inside an explicitly developer-enabled environment. ADR-0031 and ADR-0032 remain the domain decisions for their respective developer controls.
