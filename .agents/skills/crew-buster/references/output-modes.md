# Output Modes

Use this reference when choosing how much structure to show.

## Light mode

Use light mode for ordinary chat and most introspective prompts.

Shape:

```text
The material Crew lens is <Lens>. <One or two sentences explaining the finding.>
The boring route is <route>, or the blocker is <blocker>.
```

Examples:

- "This is a Writ problem: the end product sounds like a skill, but the actual governing surface may be a repo playbook. First prove the artifact form, then pick the route."
- "Index is not satisfied yet. We need the current issue state and whether the skill is already installed before Klause can select Plan A."
- "Receipt should capture this. The right home is a Will issue, not a Wild Bunch issue, unless we are deliberately using Wild Bunch as a temporary local receipt."

Do not dump all six lenses if only one or two are relevant.

## Formal mode

Use formal mode when explicitly requested or when the plan has high mutation, dispatch, issue, skill, repo, package, or closeout consequence.

Prefer prose, compact headings, a small markdown table, or JSON code blocks. Do not default to YAML in workspaces where YAML is reserved for dispatches/session busters unless the user explicitly asks for YAML or the requested artifact is itself lawful YAML.

A formal assessment should include only the material lenses. If all lenses are material, include:

- topic;
- source/evidence posture;
- Index finding;
- Silk finding;
- Writ finding, including artifact shape when relevant;
- Klause selected start, Goal A, and Plan A;
- Rollback recovery/Goal B/booby-prize posture;
- Receipt durable home and whether the record was written, proposed, or not needed;
- result and next action.

Use `not applicable` for lenses that truly do not matter. Do not mark a lens green just because it was not inspected.

## Specialist handoff mode

If another skill owns the next decision, return a route rather than doing that skill's job.

Examples:

- `boring-buster`: one issue/proposal readiness.
- current dispatch gate: send-ready worker packet proof.
- `tps-reporting`: partitioning reports, worker returns, and evidence from truth.
- repo/GitHub proof surface: GitHub evidence/mutation route discipline.
- `skill-creator`: GPT-native skill authorship.

Suggested wording:

```text
Crew-buster result: defer to <skill>. <Lens> found <reason>. The next action is <smallest route>.
```

## Avoid ceremony drift

Do not make the Crew frame heavier than the user request. If the answer can be two useful sentences, use two useful sentences.
