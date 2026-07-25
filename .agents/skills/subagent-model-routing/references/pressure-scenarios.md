### Shared failures

1. An underdefined task asks for a stronger model -> return to brainstorming/specification/planning.
2. An agent claims every available model needs a lane -> reject; allow fallback-only models.
3. A failed High attempt is requesting `ultra` -> reject. Allow `max` only when the active profile exposes it and concrete evidence justifies one deliberate escalation; otherwise diagnose and reroute.
4. A runtime cannot enforce selection -> provide a desired-route hint without claiming enforcement.
5. Two same-family agents are called model-independent -> correct the independence description.
6. A large repository triggers paid context automatically -> require retrieval/decomposition and explicit authorization.
7. A strong model investigates adjacent issues -> preserve bounded mutation and report findings.

### Codex MultiAgentV1

 8. Well-specified bounded implementation -> `gpt-5.4` with supported adequate reasoning.
 9. Large read/inventory -> Luna at `medium` unless the live schema says otherwise.
10. Cross-boundary debugging -> Terra at `high`.
11. Security-sensitive migration or concurrency review -> Sol at `high`; escalate through `xhigh` or `max` only with exceptional justification.
12. A task needs full history and Sol -> V1 may use `fork_context: true`; record that backend enforcement semantics are unobserved.
13. 5.5 is proposed as cheaper Sol -> reject the price claim; allow only deliberate diversity or regression use.
14. `gpt-5.4-mini` is requested -> correct to the exact V1 slug `gpt-5.4` when it is exposed.

### Codex MultiAgentV2

15. Full history is requested with a Sol override -> do not silently inherit the parent model and reasoning. Use `fork_turns: "none"` or a positive count with a bounded brief, or keep the task with the parent.
16. A routine bounded task needs a child -> Terra at `medium`; use `high` only when the reasoning need is concrete.
17. Consequential review -> Sol at `high`; use `xhigh` only for exceptional consequence or unresolved disagreement.
18. Luna, 5.5, or 5.4 is requested -> report it as not exposed by the current V2 dispatch surface; do not substitute silently.
19. A different fresh-context reviewer is called model-independent -> correct the claim. Fresh context and model-family diversity are separate properties.

### Devin Desktop

20. New repo feature needs live exploration and planning -> `subagent_explore`; switch to `subagent_general` only for implementation.
21. Product-level textual design discussion without substantial repo work -> `subagent_explore`.
22. Approved mechanical implementation -> `subagent_general`.
23. Hidden root-cause bug -> `subagent_general` with broad investigation but bounded mutation.
24. Screenshot-dependent frontend fault -> `subagent_general` if interactive tooling is needed, else `subagent_explore`.
25. Technical code review -> `subagent_explore` with fresh context.
26. Plan needs architecture / intent challenge -> `subagent_explore` with a non-overlapping prompt.
27. "Parent used one model family, therefore the other must review" -> reject automatic model-family pairing; classify the review question and choose `subagent_explore` or `subagent_general`.
28. "The task is easy, therefore use a weaker/smaller model" -> reject; model is not selectable. Use `subagent_explore` for read-only and `subagent_general` for mutation.
29. "A different/faster/cheaper model is available, therefore use it" -> reject; model, cost, and reasoning are not dispatch dimensions while current dispatches are adequate.
30. Subagent fails and retry by "changing model" is requested -> reject; retry by refining the prompt, narrowing scope, or decomposing.
31. Large diff / repo triggers a request for paid context -> reject; no paid context tier. Decompose across `subagent_explore` and `subagent_general`.
32. Provider benchmark conflicts with repeated local evaluation -> preserve the documented profile until an evaluation-backed update is made; do not drift ad hoc.
