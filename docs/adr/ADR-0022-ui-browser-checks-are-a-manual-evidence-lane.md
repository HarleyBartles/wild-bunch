# ADR-0022 UI browser checks are a manual evidence lane

## Status

live

## Dated Status History

- 2026-06-01 - live: the repo now records UI browser checks as a manual evidence lane with separate human and worker guidance.

## Decision Type

ui, testing, process

## Related ADRs

- `depends on`: ADR-0004, ADR-0015, ADR-0016, ADR-0017
- `informs`: ADR-0001
- `related to`: ADR-0011, ADR-0021

## Context

Issue #37 asked for a stable route that future human and agent dispatches can reference when a UI or game-flow change needs a local browser check. The repo already distinguishes backend validation lanes, frontend test lanes, and explicit PostgreSQL validation, but it did not yet have a durable policy for manual browser evidence.

The closeout lesson from issue #5 was that browser evidence is most useful when it is easy to repeat, easy to report, and clearly separate from automated validation results. This ADR turns that lesson into a stable policy surface instead of leaving it in an issue thread.

## Decision Drivers

- UI work needs a human-readable route for local browser checks.
- Worker dispatches need a precise evidence policy they can reference without restating setup.
- Browser checks should stay distinct from automated unit, acceptance, and integration tests.
- Backend and frontend validation levels need separate definitions so manual browser evidence does not blur the lanes.
- Documentation must serve both humans running the app locally and agents preparing dispatches.

## Decision Summary

UI browser checks are a manual evidence lane, not an automated validation substitute. The repo now keeps a human-facing playbook and a worker-facing playbook that both point at the same local run route, checklist, and reporting contract. Browser checks are required only when the UI/game-flow change or Harley explicitly calls for them; they are optional or skippable for backend-only, docs, or tooling work when automated validation is sufficient.

## Detailed Decision Breakdown

The manual browser lane exists to prove what a user can see and click in the running app. It is intentionally separate from the automated testing stack described in ADR-0017. A browser check can support confidence in a UI slice, but it does not replace unit, acceptance, integration, or provider/storage tests.

The repo documents backend validation as unit, acceptance, integration, and manual; and frontend validation as unit, acceptance, integration, and manual. That split keeps the manual browser lane from swallowing the automated lanes. In practice, a browser check should be reported as user-facing evidence, while the automated lanes remain reported as test/build evidence.

The playbook route is now captured in source so a worker can say "follow the browser-check playbook" instead of rediscovering commands, ports, or evidence format. The human-facing doc is written for a developer or Harley running the app locally. The worker-facing doc is written for future dispatches and includes trigger policy, skip policy, and report format.

## Options Considered and Rejected

- Treat browser checks as an informal conversation artifact with no durable repo doc.
- Fold browser evidence into automated validation results.
- Make browser checks mandatory for every UI-related dispatch.
- Hide the route in an issue thread instead of source-controlled docs.

## When a Rejected Option Would Have Been Better

An informal route would only be better for a one-off exploratory session, not for a recurring repo workflow. Mandatory browser checks would only be better if every UI dispatch were high-risk visible-flow work, which is not the repo posture.

## Benefits

- Future dispatches can point to a stable browser-check route.
- Humans get a repeatable local run and evidence checklist.
- Workers can report skipped browser checks without pretending they passed.
- Backend and frontend validation stay distinct and legible.

## Accepted Tradeoffs

- The repo now maintains an extra pair of docs for a manual lane.
- Some UI dispatches will still report browser checks as skipped or blocked.

## Risks

- If the local run ports or launch profiles change, the playbook must be updated.
- If future workers ignore the reporting split, manual evidence could still be blurred with automated results.

## Consequences for Future Work

Future UI or game-flow dispatches may require browser checks when the visible flow is part of the change, when the issue explicitly asks for them, or when Harley requests them. Backend-only dispatches should not start requiring browser checks by default. Closeout reports should keep automated validation evidence separate from manual browser evidence and should state lawful skips plainly.

## Implementation Status or Plan

Live. The playbook docs and repo indices now carry the route.

## Related Stable Source Surfaces

- `docs/ui-browser-check-playbook.md`
- `.agents/ui-browser-check-playbook.md`
- `docs/testing-lanes.md`
- `docs/local-postgresql.md`
- `src/WildBunch.Api/Properties/launchSettings.json`
- `src/WildBunch.Web/package.json`
- `docs/adr/README.md`
- `.agents/INDEX.md`

## Proof of Implementation or Explicit Non-Implementation

The repo now contains a human-facing browser-check playbook, a worker-facing browser-check playbook, and index links to both. The API launch profile and web client package scripts provide the verified local run targets that the playbooks reference.

## Review Triggers

- When the API or web launch commands change.
- When the local UI port changes.
- When the repo adopts automated browser testing and the manual lane needs to be re-scoped.
