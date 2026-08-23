Apply these rules in every environment:

* Resolve ambiguity through brainstorming, specification, or planning before escalating model capability.
* Choose by judgment, consequence, reversibility, verification burden, context need, modality, and task type—not task size alone.
* Prefer the least escalated route exposed by the active runtime.
* Do not infer pricing, included usage, or entitlement from a model name or route.
* Treat model, reasoning effort, and context allocation as separate decisions.
* Use the lowest reasoning effort that is reliably adequate.
* Ultra reasoning is forbidden for GPT-5.6 (Codex) routes; other runtimes define their own Max ceiling in their profile.
* Preserve reviewer independence where it adds value, but distinguish:
  * fresh-context independence;
  * model-family diversity;
  * deterministic verification.
* Do not call two agents “independent models” merely because they have separate contexts.
* Escalate once deliberately by model, reasoning, context, or review type; do not loop retries on the same route.
* Investigate broadly enough to understand cause, but mutate only the smallest surface required by the approved goal. Report adjacent findings instead of silently expanding scope.
* Do not manufacture a role for every available model. Inferior or redundant models may remain fallbacks only.
* Do not use a stronger model to compensate for an underdefined task.

If the user supplies a budget constraint, record it as user-provided policy.
Otherwise, do not claim that a route is free, included, metered, or approved.
