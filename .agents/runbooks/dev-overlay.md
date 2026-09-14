# Dev overlay runbook

## When

Adding or changing a Wild Bunch developer control or panel.

## Required skills

- `/dev-control-boundary`
- `/wild-bunch-domain-modeling`
- `/wild-bunch-dotnet-architecture`
- `/wild-bunch-browser-game`
- `/test-driven-development`
- `/game-playtest`
- `/verification-before-completion`

## Composition

Establish lawful state preparation and panel ownership first. Implement the
backend command and immutable dev event, then compose only the frontend
capabilities required (`/react`, `/game-ui-frontend`, `/web-styling`). Exercise
the prepared state through normal gameplay and verify it through playtest.

## Doctrine and contracts

- [Dev-overlay doctrine](../doctrine/dev-overlay.md)
- [Dev-overlay anti-slop contract](../contracts/unslop/dev-overlay.md)
- [Dev-overlay proof contract](../contracts/dev-overlay-proof.md)

## Local commands and paths

- Frontend controls: `src/WildBunch.Web/src/dev/`
- Browser/server route: [UI browser check](ui-browser-check.md)
- Automated gate: [testing](testing.md)

## Evidence contract

Return every field required by `dev-overlay-proof.md`, including dev-event,
normal-gameplay consumption, browser, and automated proof.

## Prohibited combinations

- Do not force a normal gameplay action or result.
- Do not treat locally fabricated UI state as proof.
