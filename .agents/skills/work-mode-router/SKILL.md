---
name: work-mode-router
description: Use when cross-runtime bootstrap router for new project sessions and
  workflow-sensitive starts after repo adoption. Use when a project context begins,
  a session resumes, or a request may involve continuity ingress, repo/source evidence,
  coding dispatch, workers, issues, artifacts, verification, skill/package work, mutation,
  or publication. Owns first classification, ordinary-chat escape hatch, bounded skill-read
  stop rules, and routing normal coding work to /using-superpowers with the discovered mode
  instead of legacy dispatch stacks.
metadata:
  source-id: work-mode-router
  source-path: sources/first_party/skills/work-mode-router/SKILL.md
  provenance-name: Work Mode Router first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when cross-runtime bootstrap router for new project sessions and workflow-sensitive
    starts after repo adoption. Use when a project context begins, a session resumes,
    or a request may involve continuity ingress, repo/source evidence, coding dispatch,
    workers, issues, artifacts, verification, issue work, skill/package work, mutation,
    or publication. Owns first classification, ordinary-chat escape hatch, bounded skill-read
    stop rules, and routing normal coding work to /using-superpowers with the discovered mode
    instead of legacy dispatch stacks.
  use_when:
  - Use when cross-runtime bootstrap router for new project sessions and workflow-sensitive
    starts after repo adoption. Use when a project context begins, a session resumes,
    or a request may involve continuity ingress, repo/source evidence, coding dispatch,
    workers, issues, artifacts, verification, issue work, skill/package work, mutation,
    or publication. Owns first classification, ordinary-chat escape hatch, bounded skill-read
    stop rules, and routing normal coding work to /using-superpowers with the discovered mode
    instead of legacy dispatch stacks.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---

# Work Mode Router

Use this skill as the cross-runtime bootstrap router for new project sessions and workflow-sensitive starts. It classifies the current request, preserves an ordinary-chat escape hatch, and routes to the smallest controlling skill surface before substantive work.

This skill is not a doctrine store and does not execute project work. It does not replace project bootstrap skills, project doctrine skills, source-specific skills, `/using-superpowers`, GitHub proof skills, artifact skills, or package skills.

## Core posture

Bootstrap is orientation and classification, not source inspection. A project-relevant bootstrap is mandatory once at new-session start when a project context is active or the first user task is project-scoped. Bootstrap must classify the current request before evidence-route, connector, mutation, artifact, worker, or downstream skill decisions.

**Session resume verification:** When a session resumes from a previous conversation (continuity ingress, summary block, or inherited worktree state), verify the worktree location before proceeding with substantive work. Check whether the current worktree path matches the repo's declared canonical worktree root (e.g., `../_agent-worktrees/<repo-name>` per `AGENTS.md` or `repo-worker-base` guidance). If the worktree is in a non-canonical location, move it with `git worktree move` before continuing. Do not inherit a wrong-location worktree as a given — the previous session may have created it without invoking `using-git-worktrees`.

**Skill invocation at session resume:** The `using-superpowers` skill's "invoke before ANY response" rule applies at session resume, not just at new session start. A continued session must still invoke bootstrap skills before substantive work — the previous session's skill invocations do not carry forward.

Normal coding work now routes through the repo-backed worker flow by default. Legacy chat/YAML dispatch stacks are Plan B only. Do not load old dispatch-family skills merely because your human partner says `dispatch`; route coding work to `/using-superpowers` with the discovered mode from this skill and let `/using-superpowers` choose the implementation lane. `work-mode-router` only classifies the mode from durable evidence.

For worker starts, classify the durable route state before any implementation lane choice. A prompt such as `Pick up {{issue.identifier}} from Linear. Start with /work-mode-router.` must be enough to infer one of the worker route states below from durable Linear/repo evidence.

### Worker route states

Inspect these durable markers when classifying worker route state:

- route-state block in the Linear issue body or attached document;
- plan PR URL and current PR state;
- plan repo path under `.agents/superpowers/plans/`;
- plan approval and merge evidence;
- approved plan commit;
- last staleness-check evidence.

| Route state | Durable markers | Meaning | Action | Workflow Phase |
| --- | --- | --- | --- | --- |
| `worktree_isolation_needed` | No worktree exists or worktree is stale | Workspace isolation required | Route to `/using-git-worktrees` | Phase 0 |
| `design_needed` | Ask is unclear, no spec exists, or spec is below confidence floor | Design spec needed before planning | Route to `/brainstorming` + repo design guide | Phase 1 |
| `design_signoff_pending` | Spec exists but not rated 9/10+ | Design needs sign-off before planning | Stop and request design sign-off | Phase 1a |
| `planning_needed` | Design signed off OR ask is clear, no plan exists | Implementation plan needed | Route to `/writing-plans` + repo planning guide | Phase 2 |
| `plan_signoff_pending` | Plan exists but not approved for implementation | Plan needs sign-off before execution | Stop and request plan sign-off | Phase 2a |
| `approved_plan_execution_ready` | Approved plan is merged to `main`, plan path/PR/commit evidence exists, staleness check passes | The approved plan is ready to execute | Hand to `/using-superpowers` with execution context | Phase 3 |
| `implementation_in_progress` | PR exists, implementation branch active | Implementation phase active | Route to implementing guide skills | Phase 3 |
| `code_review_needed` | PR raised, implementation complete | Code review required | Route to `/requesting-code-review` + repo code review guide | Phase 4 |
| `preflight_needed` | (existing) Route-state block says preflight or is absent, and there is no approved plan PR, merged plan, approved plan commit, or fresh staleness evidence. | The issue still needs preflight shape. | Hand the discovered mode to `/using-superpowers` with preflight context. The worker should inspect current source, produce or repair the repo-resident plan, open a plan-only PR, update Linear route state with plan path/PR/status, and stop before implementation. `/using-superpowers` owns lane selection; `work-mode-router` must not choose the Superpowers lane itself. | Phase 1-2 |
| `preflight_complete_pending_approval` | (existing) Plan file exists under `.agents/superpowers/plans/`, plan PR exists, route-state block says pending approval, and approval or merge evidence is absent. | The plan is ready for approval but not execution. | Stop and report pending approval. Hand the discovered mode to `/using-superpowers` only as stopping context. Do not select an implementation lane. | Phase 2a |
| `stale_plan_repair_needed` | (existing) Approved plan exists, plan PR or merge evidence exists, and the staleness check fails but the drift is repairable inside the approved scope. | The plan is stale but repairable in the execution branch. | Hand the discovered mode to `/using-superpowers` with repair context. Repair stays in the execution branch unless the scope changes materially. | Phase 3 |
| `blocked_ambiguous` | (existing) Durable markers conflict, are missing, or cannot prove approval, merge, or current staleness state. | The worker cannot route safely from durable evidence. | Stop and report blocked or ambiguous. Do not select an implementation lane. | N/A |

`work-mode-router` classifies the current workflow phase and may identify the phase-appropriate workflow skill (design, planning, code review). It does not choose implementation-lane strategy (SDD, TDD, or direct implementation) where `/using-superpowers` owns that decision. Phase routing to design, planning, sign-off, or code review is workflow-phase classification, not implementation-lane selection.

Gates are backstops, not the primary teaching surface. Future GPT should understand why a workflow gate exists before the gate has to catch a failure. Breaking a gate is bad because it may spend scarce resources, mutate protected source, collapse ambiguity, launder reports into truth, create false closure, or push work away from the correct production boundary.

## Working Mode Phases

When a repo has this skill installed, coding work follows this structured workflow:

| Phase | Purpose | Entry Condition | Exit Condition | Sign-off Gate | Superpowers Skill | Guide Reference |
| --- | --- | --- | --- | --- | --- | --- |
| 0 - Worktree Isolation | Isolated workspace | Starting repo work | Worktree created/verified | N/A | `/using-git-worktrees` + `/inspecting-the-environment` | N/A |
| 1 - Design | Create spec if ask unclear | Ask is unclear or complex | Spec exists following repo design guide | Design sign-off (9/10+) | `/brainstorming` | `.agents/docs/guides/design-guide.md` |
| 1a - Design Sign-off | Rate design for planning | Design complete | 9/10+ rating achieved | Planning agent can create full plan without improvising | N/A | Design guide handoff section |
| 2 - Planning | Write plan using repo guide | Design signed off OR ask is clear | Plan exists following repo planning guide | Plan sign-off | `/writing-plans` | `.agents/docs/guides/planning-guide.md` |
| 2a - Plan Sign-off | Rate plan for implementation | Plan complete | Passes implementation rubric | Implementing agent can follow plan without improvising | N/A | Planning guide execution confidence |
| 3 - Implement | Follow repo implementing guide | Plan signed off | Implementation complete, PR raised | N/A | `/executing-plans` and/or `/subagent-driven-development` | `.agents/docs/guides/implementing-guide.md` |
| 4 - Code Review | Follow repo code review guide | PR raised | Review complete, merge decision | N/A | `/requesting-code-review` | `.agents/docs/guides/code-review-guide.md` |

## Superpowers Workflow Mapping

For each workflow phase, route to the corresponding superpowers skill:

| Workflow Phase | Superpowers Skill | Notes |
| --- | --- | --- |
| Worktree Isolation | `/using-git-worktrees` + `/inspecting-the-environment` | Use `/using-git-worktrees` for workspace isolation, `/inspecting-the-environment` for environment constraint discovery before action |
| Design | `/brainstorming` | Use before any creative work - explores user intent, requirements and design before implementation |
| Planning | `/writing-plans` | Use when you have a spec or requirements for a multi-step task, before touching code |
| Execution | `/executing-plans` and/or `/subagent-driven-development` | Use `/executing-plans` for written implementation plans in separate sessions with review checkpoints. Use `/subagent-driven-development` for executing implementation plans with independent tasks in the current session |
| Code Review | `/requesting-code-review` | Use when completing tasks, implementing major features, or before merging to verify work meets requirements |

### General Routing Guidance

For general guidance on which superpowers workflow to route a piece of work to, use `/using-superpowers`. This skill composes the workflow phase classification from work-mode-router with the appropriate superpowers lane selection.

### Repo Guide Integration

Repo-specific guides (`.agents/docs/guides/design-guide.md`, `.agents/docs/guides/planning-guide.md`, etc.) may provide additional repo-specific guidance on which superpowers skills to invoke and how to adapt them to the repo's conventions. When repo guide guidance conflicts with this canonical mapping, the repo guide takes precedence for that specific repo.

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

- `worktree_isolation_needed` -> `/using-git-worktrees` for workspace isolation
- `design_needed` -> `/brainstorming` + repo design guide (`.agents/docs/guides/design-guide.md` if present)
- `design_signoff_pending` -> Stop and request design sign-off using design guide handoff rubric
- `planning_needed` -> `/writing-plans` + repo planning guide (`.agents/docs/guides/planning-guide.md` if present)
- `plan_signoff_pending` -> Stop and request plan sign-off using planning guide execution confidence assessment
- `approved_plan_execution_ready` -> `/using-superpowers` with execution context. `/using-superpowers` owns Superpowers lane choice (SDD, TDD, or direct implementation)
- `implementation_in_progress` -> `/executing-plans` or `/subagent-driven-development` based on plan shape, plus repo implementing guide (`.agents/docs/guides/implementing-guide.md` if present)
- `code_review_needed` -> `/requesting-code-review` + repo code review guide (`.agents/docs/guides/code-review-guide.md` if present)
- `repo_worker_coding` -> `/using-superpowers` with the discovered mode (existing)
- `gpt_native_skillwork` -> `skill-creator` for authored skill content, then `writing-skills` for cross-repo wording and doctrine checks when relevant. Do not delegate GPT-native skillwork to a cloud agent unless the editable source is known to live in a worker-accessible repo and the task is explicitly repo-backed.
- `github_proof` -> the repo/GitHub proof surface after a GitHub artifact exists. Do not use repo/GitHub proof to decide worker state or issue routing.
- `linear_control` -> `using-linear` for connector mechanics: create/update/fetch/comment/project/status/label/document work.
- `verification_or_reporting` -> the narrow downstream skill that owns the decision, such as the validation decision surface, `risk-gates` (feedback gate), or `base-doctrine` (report hygiene).
- `legacy_plan_b` -> the compact legacy dispatch stack only after the default route has been rejected or unavailable.

Use project bootstrap or project doctrine only when the active project actually matches the project wrapper and the current task needs local law.

## Workflow Enforcement

This skill enforces the structured workflow by:

1. **Phase classification**: Before any coding work, classify the current workflow phase from durable evidence (Linear route state, repo artifacts, guide existence)
2. **Gate enforcement**: Do not proceed to the next phase without meeting the exit condition and sign-off gate
3. **Guide discovery**: When a repo has guides in `.agents/docs/guides/`, reference them explicitly. When absent, fall back to generic workflow but still enforce the phase structure
4. **Sign-off rubrics**: Use the confidence floors from the guides (9/10+ for design handoff, execution confidence assessment for plan handoff)
5. **Route state updates**: When phase transitions occur, update Linear route state to reflect the new phase

The workflow is **enforced** in the sense that agents must classify their current phase and meet sign-off gates before proceeding. It is **not enforced** in the sense of blocking tool use—the agent is responsible for following the workflow correctly.

## Golden-gate reminder

Before worker delegation or legacy packet creation, require a surface check:

1. What is the editable target?
2. Can the proposed worker actually access and change that target?
3. Where will durable evidence return: Linear, GitHub, package artifact, repo commit, or another source?
4. Is this implementation work, GPT-native skillwork, research, connector/UI setup, or side discovery?
5. Is the normal Linear-backed worker route available and suitable?

If the target is ChatGPT-native installed skill state, account/UI settings, plugin marketplace selection, or pure planning, do not send it to a cloud agent as a repo worker task unless there is a separate repo-backed source target.

## Output-shape attention guard

At bootstrap time, preserve workspace-reserved artifact shapes. Output form can imply authority.

When the active project or workspace reserves a shape, lower workflow skills must yield to that rule. In worker-control contexts, YAML-shaped blocks are reserved for lawful send-ready legacy dispatches, continuity artifacts, and user-explicit YAML artifacts. Do not use YAML blocks for ordinary assessments, plans, gate summaries, status notes, or conversational analysis. Use prose, a small markdown table, a JSON code block, or another clearly non-dispatch shape instead.

This guard is not a ban on structure. It prevents attention and copy/paste failures where a non-dispatch assessment looks like something a worker should execute, or where a non-continuity note looks like a continuity artifact.

## Bounded skill-read stop rule

After the current request has been classified and the controlling skill surfaces have been read, stop reading skills and act. Do not load additional skills merely because they are adjacent, project-flavoured, safety-sounding, or appeared in prior workflow memory.

A new skill may be loaded only when all of these are true:

1. The current task has an unresolved decision.
2. The already-read controlling skill does not own that decision.
3. The candidate skill name/description directly matches the unresolved decision.
4. The skill is project-compatible with the active repo or task.

Before loading any additional skill, classify internally: `missing_decision`, `already_read_owner`, `candidate_owner`, and `project_compatibility`. If that cannot be stated concretely, do not read the skill.

Hard stop: if your human partner asks GPT to stop reading skills, stop immediately and continue from already available context unless a safety or legal blocker exists.

## Project-wrapper compatibility

Never load a project-specific wrapper skill unless its project matches the active task's project or your human partner explicitly asks for cross-project skill work.

A project wrapper with a similar function name is not a fallback. Wrong-project doctrine is noise and may create false constraints.

Project-specific skills must not own generic dispatch doctrine after repo adoption. They should add local domain constraints, validation preferences, protected surfaces, and source-truth posture, then route worker control through cross-runtime `linear-issue-shaping`.

## Reference loading

Load `references/source-and-evidence-posture.md` only when the classified task actually requires source evidence, connector/tool-surface diagnosis, repository claims, unavailable-route claims, or audit output about what was inspected.

When returning or revising a full system prompt, load `base-doctrine` for the system-prompt contract, including character-limit discipline and source-honesty expectations.

Load `base-doctrine/references/output-artifact-shape.md` when an output-shape rule, reserved artifact form, YAML-vs-non-YAML decision, worker-copy attention guard, or artifact-form authority conflict is material.

## System prompt contract

System prompts should:

- identify the assistant posture and project context;
- require one-time project bootstrap as the mediator for new project sessions and substantive project work;
- preserve an ordinary-chat escape hatch after bootstrap classification;
- route normal coding work to Linear and its golden gate;
- list only the minimum routing invariants that must be active before a skill loads;
- direct GPT to doctrine-bearing skills for detailed project law;
- avoid duplicating detailed doctrine inline;
- avoid becoming a second project handbook.

## Session handoff posture

When your human partner provides a continuity export, resume packet, or next-session block, run the project bootstrap first when applicable, then route the block through the relevant continuity ingress surface. Do not act directly on recommended next actions until ingress separates verified state, fallback state, source claims, open queues, and user instructions.

For coding work, prefer durable Linear issue IDs, worker state, PR IDs, and next checks over bulky packet prose. Linear, GitHub, and repo guidance are the normal continuity surfaces; continuity exports are fallback continuity.

## Output behavior

For ordinary first-turn use, do not print a long bootstrap audit. Read the relevant surfaces, then answer or route compactly.

For explicit audits, system-prompt work, or bootstrap-skill updates, report in prose or another non-reserved shape unless your human partner explicitly requests YAML. If a structured sample is useful, prefer JSON.

## Boundaries

Do not use this skill to execute project work directly. Do not mutate repos, post comments, generate or edit images, build artifacts, create dispatches, delegate Codex, or close issues from bootstrap alone. Use the specific skill that owns the task.
