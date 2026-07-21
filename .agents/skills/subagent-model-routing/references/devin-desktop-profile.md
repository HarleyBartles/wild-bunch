Use a SWE-first policy for repository software work. Do not preserve GLM merely to give every model a lane.

### Runtime-observed model inventory

Initial profile data:

* SWE-1.7 — free/included, approximately 262K context, multimodal in the observed runtime, reasoning available through Max.
* GLM-5.2 — free/included at approximately 200K context, High reasoning available, text-only in the observed runtime; optional 1M context costs approximately $0.60 per million input tokens.
* SWE-1.6 — free/included fallback.
* SWE-1.6 Fast — metered at approximately $0.30 per million input tokens and $1.50 per million output tokens.

These are environment observations and may change. Current runtime inventory overrides stale profile values. Public provider evidence and local evaluation should be used when deliberately revising the profile.

### SWE-1.7 reasoning ceiling

Max reasoning is in scope for SWE-1.7 but is reserved for exceptional subagent tasks that need the additional reasoning budget. The normal plan and chat agent uses Medium reasoning. Do not use Max for routine navigation, scans, or ordinary bounded implementation.

### SWE-1.7 — default repo parent, planner, engineer, and technical reviewer

Use SWE-1.7 High for:

* persistent repo-backed parent/orchestration;
* live-source exploration;
* source-grounded planning and SDD decomposition;
* normal and difficult implementation;
* difficult debugging;
* integration;
* technical code review;
* multimodal, screenshot, frontend, and visual engineering work.

Use SWE-1.7 Medium for:

* mechanical edits;
* exact repetitive transformations;
* low-judgment bounded implementation.

Do not treat SWE-1.7 as a code typist. It may identify root causes, hidden requirements, plan drift, and edge cases. It must report material plan drift or omitted constraints rather than blindly execute or silently broaden scope.

Its thoroughness can create scope pressure. Require broad enough investigation to understand the problem but bounded mutation. Adjacent improvements become findings unless required for correctness.

For technical code review, prefer a fresh-context SWE-1.7 High reviewer. Describe this as fresh-context independence, not model-family diversity.

### GLM-5.2 — optional distinct textual/architecture challenger

Do not assign GLM a routine code-review lane when SWE-1.7 High is better suited to live-repository technical review.

Use GLM-5.2 High only when its distinct lens is materially useful for:

* product or architecture reasoning not dominated by live code manipulation;
* challenging issue, specification, plan, or architectural assumptions;
* higher-level intent and plan-conformance review;
* cross-document semantic consistency;
* large text-only synthesis;
* deliberately model-diverse review of reasoning that may be shared within the SWE model family.

For ordinary technical correctness questions—root cause, tests, regressions, repository conventions, edge cases, and diff scope—use SWE-1.7 High.

Model diversity alone is not sufficient reason to choose GLM. Choose the review question first.

### GLM paid 1M context

Treat 1M context as a paid context escalation, not a default model upgrade.

Require explicit paid authorization and evidence that:

* the task is text-only;
* 200K plus indexes, search, source maps, plans, targeted reads, and decomposition is insufficient;
* narrowing would materially lose important cross-source relationships;
* the agent records why the larger context is necessary.

Do not buy 1M context merely because the repository is large.

### SWE-1.6 and SWE-1.6 Fast

Do not create default lanes merely because these models are available.

Use SWE-1.6 as a fallback for:

* SWE-1.7 outage or unavailability;
* quota/rate-limit differences observed in practice;
* regression reproduction;
* a measured local case where SWE-1.6’s behaviour is preferable.

Use SWE-1.6 Fast only when latency, quota, outage, or evaluation evidence justifies paying for it. Do not choose it merely because it is inexpensive while SWE-1.7 is free and adequate.

The skill must allow later evaluation evidence to promote or retire these fallback roles without changing shared doctrine.

### Devin review policy

Normal change:

1. SWE-1.7 High performs or coordinates the repo work.
2. Fresh-context SWE-1.7 High performs technical review when warranted.
3. Deterministic validation proves the result.

Consequential architecture/security/concurrency/migration change:

1. SWE-1.7 High performs live-repository technical analysis and implementation.
2. Fresh-context SWE-1.7 High performs technical review.
3. GLM-5.2 High may perform a non-overlapping architecture/intent/assumption challenge.
4. Deterministic checks remain the proof surface.

GLM is additive only when a distinct review question exists. It does not replace the SWE technical reviewer.
