# Code review runbook

Use this local overlay after `using-superpowers-plus` routes review to
`requesting-code-review` or another review owner.

## Wild Bunch review concerns

- Domain and persistence changes must preserve the DDD, CQRS, event-sourcing,
  replay, upcasting, projection-version, and load-funnel rules in
  [architecture guardrails](../docs/architecture-guardrails.md) and
  [event-sourcing integrity policy](../docs/event-sourcing-integrity-policy.md).
- Frontend changes must follow [frontend standards](../docs/frontend-standards.md)
  and protect the player-facing play surface.
- Tests must use the correct test kind and assert observable behavior under
  [validation policy](../docs/validation-policy.md).
- Apply the repo-local profiles in `.agents/unslop/` and the relevant portable
  profile from `unslop-profiles`.
- Update ADRs when an architectural decision changes and regenerate the mesh
  when routed or indexed files change.

## Evidence

- Use `py -3 tools/run.py ci --check` only for deliberate uncommitted or CI-parity
  proof; a successful normal commit already carries staged-snapshot hook evidence.
- Confirm current GitHub PR checks before approval.
- Report findings with the P0-P3 priority taxonomy and exact file/line evidence.

