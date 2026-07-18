# Invariant Gate

Use this gate before an action, answer, plan, dispatch, or durable mutation when binding constraints may be violated.

An invariant is a rule that should remain true across the work. It can come from user instruction, issue scope, repo doctrine, source hierarchy, ownership, safety policy, workflow law, project canon, schema, validation contract, or an accepted decision.

## Invariant families

Check only families materially implicated by the proposed move:

- Authority invariants: who may decide, mutate, approve, publish, close, canonicalize, override, or defer.
- Scope invariants: issue slice, non-goals, protected paths, disallowed imports, and mutation boundaries.
- Source invariants: durable source outranks memory, reports, summaries, or convenient inference when truth matters.
- Workflow invariants: required branch/PR, validation, review, evidence, dispatch, or return-contract steps.
- Data/schema invariants: JSON shape, manifest identity, naming rules, versioning, path conventions, and compatibility.
- Provenance/license invariants: source custody, attribution, clearance, trust posture, and no unreviewed third-party import.
- Canon/doctrine invariants: accepted project truth, generic-vs-project boundary, and no canon drift.
- Safety/privacy invariants: secrets, credentials, user data, protected surfaces, and policy constraints.

## Workflow

1. Name the proposed move.
2. List the binding invariants that could be affected.
3. Identify the source of each invariant when material.
4. Repair internally when the invariant forces one lawful route.
5. Surface only real authority choices.
6. Return green only when the move preserves all material invariants or explicitly routes unresolved ones.

## Internal repair examples

- If an issue says a slice must not import unrelated assets, remove unrelated assets without asking.
- If a schema requires a source path, add the path or block if the source does not exist.
- If a protected surface is in scope but authority is missing, narrow or block.
- If validation is required, do not claim green until validation has run or the limitation is reported.

## Output posture

When visible, report:

- invariant checked;
- source or basis;
- repair made, blocker found, or remaining amber choice;
- next lawful route.

Keep the output proportional. Do not produce a long governance inventory when only one invariant affected the result.

## Boundaries

Do not use invariant gating to invent new rules. Do not override explicit user scope with generic preference. Do not treat an invariant as satisfied because no failure was observed; use source and evidence.
