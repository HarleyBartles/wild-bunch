# Dev overlay playbook

## When

Adding or changing a Wild Bunch developer control or panel.

## Required capabilities

- Develop focused behavior tests, run browser playtests, and verify implementation evidence.
- Apply frontend composition and styling guidance when browser surfaces change.

## Optional capabilities

- None.

## Required repository-owned skills

- dev-control-boundary
- wild-bunch-domain-modeling
- wild-bunch-dotnet-architecture
- wild-bunch-browser-game

## Optional repository-owned skills

- None.

## Composition

1. Use `/dev-control-boundary` to classify the control as lawful state
   preparation and assign its owning panel.
2. Use `/wild-bunch-domain-modeling` and `/wild-bunch-dotnet-architecture` to
   add the backend command, aggregate route, immutable dev event, and receipt.
3. Use `/wild-bunch-browser-game` plus only the declared frontend capabilities
   needed to expose the control without fabricating local outcome state.
4. Use behavior-focused test development for focused backend/frontend proof and
   interactive gameplay browser validation to consume the prepared state through normal gameplay.
5. Use evidence-based result verification to assemble the exact proof contract.

## Doctrine and contracts

- [Dev-overlay doctrine](../doctrine/dev-overlay.md)
- Before dev-overlay work, read [dev-overlay guards](../unslop/dev-overlay.md) in full, plus [backend](../unslop/backend-architecture.md) for commands/events and [play-surface UI](../unslop/play-surface-ui.md) for browser/state concerns. Maintain encountered patterns through the [unslop loop](../unslop/README.md#record-and-improve).
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

## Runbook routing

- [Design](../runbooks/design.md)
- [Implementing](../runbooks/implementing.md)
- [Decision records](decision-records.md) - when a developer-control invariant is made, changed, or retired.
