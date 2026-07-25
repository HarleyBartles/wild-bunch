# Route States

## Worker route states

Inspect these durable markers when classifying worker route state:

- route-state block in the Linear issue body or attached document;
- plan PR URL and current PR state;
- plan repo path under `.agents/superpowers/plans/`;
- plan approval and merge evidence;
- approved plan commit;
- last staleness-check evidence.

| Route state | Durable markers | Meaning | Action | Workflow Phase |
| --- | --- | --- | --- | --- |
| `worktree_isolation_needed` | No worktree exists or worktree is stale | Workspace isolation required | Route to `repo-worker-base` -> `worktree-and-branch-policy.md` -> local repository policy -> `/using-git-worktrees` | Phase 0 |
| `design_needed` | Ask is unclear, no spec exists, or spec is below confidence floor | Design spec needed before planning | Route to `repo-worker-base` -> `design-baseline.md` + local `.agents/guides/design-guide.md` -> `/brainstorming` | Phase 1 |
| `design_signoff_pending` | Spec exists but not rated 9/10+ | Design needs sign-off before planning | Stop and request design sign-off | Phase 1a |
| `planning_needed` | Design signed off OR ask is clear, no plan exists | Implementation plan needed | Route to `repo-worker-base` -> `planning-baseline.md` + local `.agents/guides/planning-guide.md` -> `/writing-plans` | Phase 2 |
| `plan_signoff_pending` | Plan exists but not approved for implementation | Plan needs sign-off before execution | Stop and request plan sign-off | Phase 2a |
| `approved_plan_execution_ready` | Approved plan is merged to `main`, plan path/PR/commit evidence exists, staleness check passes | The approved plan is ready to execute | Hand to `repo-worker-base` -> matching baseline + local `.agents/guides/` guide -> `/using-superpowers` with execution context | Phase 3 |
| `implementation_in_progress` | PR exists, implementation branch active | Implementation phase active | Route to `repo-worker-base` -> `implementation-baseline.md` + local `.agents/guides/implementing-guide.md` -> implementing lane skills | Phase 3 |
| `code_review_needed` | PR raised, implementation complete | Code review required | Route to `repo-worker-base` -> `code-review-baseline.md` + local `.agents/guides/code-review-guide.md` -> `/requesting-code-review` | Phase 4 |
| `preflight_needed` | (existing) Route-state block says preflight or is absent, and there is no approved plan PR, merged plan, approved plan commit, or fresh staleness evidence. | The issue still needs preflight shape. | Hand the discovered mode to `repo-worker-base` -> `planning-baseline.md` + local `.agents/guides/planning-guide.md` -> `/using-superpowers` with preflight context. The worker should inspect current source, produce or repair the repo-resident plan, open a plan-only PR, update Linear route state with plan path/PR/status, and stop before implementation. | Phase 1-2 |
| `preflight_complete_pending_approval` | (existing) Plan file exists under `.agents/superpowers/plans/`, plan PR exists, route-state block says pending approval, and approval or merge evidence is absent. | The plan is ready for approval but not execution. | Stop and report pending approval after `repo-worker-base` verifies the planning baseline and local `.agents/guides/planning-guide.md`; do not select an implementation lane. | Phase 2a |
| `stale_plan_repair_needed` | (existing) Approved plan exists, plan PR or merge evidence exists, and the staleness check fails but the drift is repairable inside the approved scope. | The plan is stale but repairable in the execution branch. | Hand the discovered mode to `repo-worker-base` -> `implementation-baseline.md` + local `.agents/guides/implementing-guide.md` -> `/using-superpowers` with repair context. Repair stays in the execution branch unless the scope changes materially. | Phase 3 |
| `blocked_ambiguous` | (existing) Durable markers conflict, are missing, or cannot prove approval, merge, or current staleness state. | The worker cannot route safely from durable evidence. | Stop and report blocked or ambiguous. Do not select an implementation lane. | N/A |

`work-mode-router` classifies the current workflow phase and may identify the phase-appropriate workflow skill (design, planning, code review). It does not choose implementation-lane strategy (SDD, TDD, or direct implementation) where `/using-superpowers` owns that decision. Phase routing to design, planning, sign-off, or code review is workflow-phase classification, not implementation-lane selection.

Gates are backstops, not the primary teaching surface. Future GPT should understand why a workflow gate exists before the gate has to catch a failure. Breaking a gate is bad because it may spend scarce resources, mutate protected source, collapse ambiguity, launder reports into truth, create false closure, or push work away from the correct production boundary.


## First classification

Classify the current request into the smallest sufficient mode:

- `ordinary_chat`: acknowledgement, ping, lightweight preference discussion, side chat, or meta that does not require source evidence.
- `continuity_ingress`: continuity export, resume packet, or next-session block.
- `repo_worker_coding`: coding implementation, repo-backed worker work, issue handoff, PR-gate, PR-created, landed, or wording such as dispatch/worker/agent for coding work.
- `gpt_native_skillwork`: create, update, validate, package, install, or troubleshoot ChatGPT-native skills in the current chat.
- `repo_or_source_evidence`: repository, file, commit, PR, source-truth, publication, or current-state claims.
- `github_proof`: PR/branch/commit/status/review/merge/main verification after a GitHub artifact exists.
- `linear_control`: Linear issue/project/comment/document mechanics without coding worker-state control.
- `artifact_work`: document, spreadsheet, slide, PDF, image, package, receipt, or other artifact production.
- `verification_or_reporting`: QA, closeout posture, validation selection, review-feedback verification, or report hygiene.
- `legacy_plan_b`: non-Linear worker handoff only after the Linear-backed worker route is unavailable, unsuitable, or explicitly rejected.

For `ordinary_chat`, answer directly. Do not inspect connectors, call tools, or load downstream doctrine merely because a connector, file library, uploaded file, indexed source, or tool namespace is present.


## Routing map

- `worktree_isolation_needed` -> `repo-worker-base` + worktree policy/local repository policy -> `/using-git-worktrees` for workspace isolation
- `design_needed` -> `repo-worker-base` + `design-baseline.md` + local `.agents/guides/design-guide.md` -> `/brainstorming`
- `design_signoff_pending` -> Stop and request design sign-off using design guide handoff rubric
- `planning_needed` -> `repo-worker-base` + `planning-baseline.md` + local `.agents/guides/planning-guide.md` -> `/writing-plans`
- `plan_signoff_pending` -> Stop and request plan sign-off using planning guide execution confidence assessment
- `approved_plan_execution_ready` -> `repo-worker-base` + matching baseline/local guide -> `/using-superpowers` with execution context. `/using-superpowers` owns Superpowers lane choice (SDD, TDD, or direct implementation)
- `implementation_in_progress` -> `repo-worker-base` + `implementation-baseline.md` + local `.agents/guides/implementing-guide.md` -> `/executing-plans` or `/subagent-driven-development` based on plan shape
- `code_review_needed` -> `repo-worker-base` + `code-review-baseline.md` + local `.agents/guides/code-review-guide.md` -> `/requesting-code-review`
- `repo_worker_coding` -> `repo-worker-base` + matching baseline/local guide -> `/using-superpowers` with the discovered mode (existing)
- `repo_or_source_evidence` -> `repo-worker-base` + baseline for the active stage/local guide -> the evidence or implementation lane
- `gpt_native_skillwork` -> `skill-creator` for authored skill content, then `writing-skills` for cross-repo wording and doctrine checks when relevant. Do not delegate GPT-native skillwork to a cloud agent unless the editable source is known to live in a worker-accessible repo and the task is explicitly repo-backed.
- `github_proof` -> `repo-worker-base` + implementation or review baseline/local guide -> the repo/GitHub proof surface after a GitHub artifact exists. Do not use repo/GitHub proof to decide worker state or issue routing.
- `linear_control` -> `using-linear` for connector mechanics: create/update/fetch/comment/project/status/label/document work.
- `verification_or_reporting` -> the narrow downstream skill that owns the decision, such as the validation decision surface, `risk-gates` (feedback gate), or `base-doctrine` (report hygiene).
- `legacy_plan_b` -> the compact legacy dispatch stack only after the default route has been rejected or unavailable.

Use project bootstrap or project doctrine only when the active project actually matches the project wrapper and the current task needs local law.
