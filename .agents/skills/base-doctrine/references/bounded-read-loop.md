# Bounded read loop doctrine

Use this reference when skill, reference, repo, issue, or doctrine reading risks becoming its own task instead of supporting the next action.

## Core rule

Every read must have a declared purpose. Stop reading when the next lawful action is known, when conflict is found, or when additional reading is no longer changing the decision.

Reading is useful only when it changes one of these decisions:

- route: which skill, tool, repo, issue, or workflow owns the task;
- authority: whether the latest user instruction authorizes the next action;
- scope: what object, file, issue, package, or repo is in bounds;
- evidence: what source proves or disproves a claim;
- validation: which checks or proof surfaces are required;
- blocker: what prevents action and what would unblock it.

## Minimal read order

Prefer the smallest authoritative surface that can answer the current question.

1. Current user request and active project bootstrap.
2. The specific target object when named, such as an issue, PR, file, skill, project, or document.
3. The most specific owning skill or repo-local instruction.
4. One adjacent reference only when the owning surface explicitly routes there.
5. Broader search only when no exact target exists or exact reads contradict each other.

Do not start by reading every installed skill, every repo index, every AGENTS file, every reference in a skill, or every incubation document.

## Stop rules

Stop reading and act or report when:

- the next action is known and lawful;
- the next action is blocked by missing authority, missing tool route, or conflicting source truth;
- two authoritative surfaces conflict;
- three reads have not changed route, authority, scope, evidence, validation, or blocker state;
- a more specific skill or project surface has taken ownership;
- the user asked for an ordinary answer and no source evidence is needed.

If two surfaces conflict, do not reconcile by reading indefinitely. Name the conflict, identify the stronger source if clear, and ask or route if not clear.

## Exact anti-patterns

Avoid these read-loop defaults:

- reading every skill in a plausible stack before classifying the request;
- loading GitHub verification doctrine before a branch, PR, commit, or file route exists;
- loading dispatch-prep before the user asks for dispatch, worker routing, or a worker-facing packet;
- reading all incubation docs after enough exists to classify authority state;
- using broad repo search as a substitute for opening the exact issue, PR, or file once known;
- reading a second project's doctrine because it shares vocabulary with the current task;
- treating connector or plugin availability as a reason to inspect sources for ordinary chat;
- continuing to read after a blocked mutation when the safe retry shape is already known.

## Read basis report

For workflow-sensitive work, return a compact read basis when useful:

```text
Read basis: <surfaces read>
Why enough: <route/authority/scope/evidence decision made>
Not read: <nearby surfaces deliberately skipped>
Remaining uncertainty: <none or bounded uncertainty>
```

Do not use this report for lightweight ordinary chat unless the user asks how the answer was grounded.
