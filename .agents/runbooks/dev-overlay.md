# Dev overlay runbook

## When

Adding or changing a Wild Bunch developer control or panel.

## Required skills

- `/dev-control-boundary`
- `/wild-bunch-domain-modeling`
- `/wild-bunch-dotnet-architecture`
- `/wild-bunch-browser-game`
- `/react`, `/game-ui-frontend`, and `/web-styling` when the change crosses
  their frontend boundaries.
- `/test-driven-development`
- `/game-playtest`
- `/verification-before-completion`

## Composition

1. Use `/dev-control-boundary` to classify the control as lawful state
   preparation and assign its owning panel.
2. Use `/wild-bunch-domain-modeling` and `/wild-bunch-dotnet-architecture` to
   add the backend command, aggregate route, immutable dev event, and receipt.
3. Use `/wild-bunch-browser-game` plus only the declared frontend capabilities
   needed to expose the control without fabricating local outcome state.
4. Use `/test-driven-development` for focused backend/frontend proof and
   `/game-playtest` to consume the prepared state through normal gameplay.
5. Use `/verification-before-completion` to assemble the exact proof contract.

## Doctrine and contracts

- [Dev-overlay doctrine](../doctrine/dev-overlay.md)
- [Dev-overlay anti-slop contract](../contracts/unslop/dev-overlay.md)
- [Dev-overlay proof contract](../contracts/dev-overlay-proof.md)

## Local commands and paths

- Frontend controls: `src/WildBunch.Web/src/dev/`
- Browser/server route: [UI browser check](ui-browser-check.md)
- Automated gate: [testing](testing.md)

## Evidence contract

- [ ] Every field in `dev-overlay-proof.md` is present.
- [ ] The dev command and immutable dev-event receipt identify prepared state.
- [ ] A normal gameplay command consumes that state and produces the outcome.
- [ ] Browser evidence and automated evidence are reported independently.

## Prohibited combinations

- Do not force a normal gameplay action or result.
- Do not treat locally fabricated UI state as proof.
