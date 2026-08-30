---
name: writing-profile-engine
description: Use when writing-specific fatigue or voice profiles need lawful discovery, validation, or transparent deterministic evaluation.
metadata:
  source-id: writing-profile-engine
  source-path: codex-marketplace/plugins/writing-pack/skills/writing-profile-engine/SKILL.md
  provenance-name: Writing profile engine first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Writing-specific profile discovery, validation, and evaluation
  related_skills:
    - writing
    - writing-style
    - writing-with-clarity
license: MIT
---
# Writing Profile Engine

Use this engine only for writing-specific profiles below lawful
`references/profiles/` roots. It reports inspectable signals and bounded
findings; it does not identify authorship, score detectors, ban words, or edit
the input.

## Workflow

1. Run `scripts/discover_profiles.py --json` to list profiles.
2. Run `scripts/validate_profiles.py --json` before evaluation. Treat errors as
   blocking. Treat expired review dates as a downgrade warning.
3. Run `scripts/evaluate_profile.py --profile PATH --input PATH --json`.
4. Read evidence, spans, rationale, preservation conditions, and the smallest
   repair. A `candidate` needs contextual review; `abstain` is a valid result.
5. Apply any accepted repair through `$writing-style`, then run the final
   `$writing-with-clarity` gate.

The commands are read-only. Run each command with `--help` for its bounded
interface. Profiles carry their executable rules and preserve predicates; the
engine does not select behavior by pattern ID. The profile schema is in
`assets/schemas/writing-profile.schema.json`, bundled durable source IDs are in
`references/source-authority.json`, and the result contract is in
`references/result-contract.md`.

## Boundaries

- Do not turn a phrase match into a repair without the profile's contextual
  threshold and task context.
- Do not change clear, factual, or authorised prose merely to vary it.
- Do not infer identity, personality, culture, intent, or text origin.
- Do not retain source prose in a voice card.
- If the rule set cannot support a judgment, return `abstain`.
