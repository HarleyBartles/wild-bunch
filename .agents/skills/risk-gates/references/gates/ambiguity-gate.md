# Ambiguity Gate

Use this gate before acting when unresolved ambiguity could cause the wrong scope, target, source route, authority, artifact, worker handoff, or answer.

Ambiguity is not automatically bad. The gate decides whether the ambiguity is harmless, internally resolvable, needs a user decision, or blocks action.

## Ambiguity families

Check only families that materially affect the next action:

- Request ambiguity: unclear intent, success condition, priority, or acceptance criteria.
- Referent ambiguity: unclear `it`, `this`, path, issue, branch, asset, artifact, person, model, version, or environment.
- Scope ambiguity: unclear inclusion/exclusion boundary, mutation boundary, non-goals, or slice ownership.
- Source ambiguity: unclear source of truth, stale source, conflicting sources, or chat-only context being treated as durable truth.
- Authority ambiguity: unclear who may decide, approve, mutate, publish, close, canonicalize, override, or defer.
- Output ambiguity: unclear expected return shape, destination surface, proof route, or artifact format.
- Time ambiguity: unclear relative dates, current state, latest status, or sequence dependency.
- Vocabulary ambiguity: project terms, skill names, aliases, and legacy names that may map to different current surfaces.

## Workflow

1. State the next action that depends on interpretation.
2. Identify material ambiguous terms, sources, boundaries, or choices.
3. Resolve forced or harmless ambiguity internally when current context, source hierarchy, or user instruction leaves one safe route.
4. If using an assumption, label it and keep the next step reversible.
5. Ask only for decisions that remain genuinely open and material.
6. Block rather than invent when the ambiguity affects irreversible mutation, source truth, canon, publication, dispatch, money/time commitments, or user intent.

## Safe internal resolution

Resolve internally when:

- the user gave an explicit target elsewhere in durable context;
- repo paths or issue identifiers make the intended target unambiguous;
- non-goals exclude the tempting interpretation;
- one interpretation would violate scope, authority, or project doctrine;
- the ambiguous detail does not affect the immediate safe next step.

## Interactive queue shape

When user input is needed, keep the queue compact:

- Ambiguity: what is unclear.
- Risk: what goes wrong if GPT guesses.
- Recommendation: GPT's preferred interpretation, if one is safer.
- Decision needed: the concrete choice.

Do not ask the user to approve policy-forced decisions or to restate information already available in durable source.

## Output posture

For visible results, separate:

- ambiguity found;
- internal resolution or assumption used;
- remaining user decision, if any;
- safe next action or blocker.

## Boundaries

Do not convert ambiguity gating into broad planning. Do not keep asking clarification questions after a safe narrow next step exists. Do not treat a convenient guess as green when the ambiguity affects durable mutation, canon, dispatch, or proof.
