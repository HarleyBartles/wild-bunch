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

Normal coding work now routes through the repo-backed worker flow by default. Legacy chat/YAML dispatch stacks are Plan B only. Do not load old dispatch-family skills merely because your human partner says `dispatch`; route coding work to `/using-superpowers` with the discovered mode from this skill and let `/using-superpowers` choose the implementation lane. `work-mode-router` only classifies the mode from durable evidence.

For worker starts, classify the durable route state before any implementation lane choice. A prompt such as `Pick up {{issue.identifier}} from Linear. Start with /work-mode-router.` must be enough to infer one of the worker route states below from durable Linear/repo evidence.

### Worker route states

Inspect these durable markers when classifying worker route state:

- route-state block in the Linear preflight or implementation brief;
- plan PR URL and current PR state;
- plan repo path under `.agents/docs/superpowers/plans/`;
- plan approval and merge evidence;
- approved plan commit;
- last staleness-check evidence.

| Route state | Durable markers | Meaning | Action |
| --- | --- | --- | --- |
| `preflight_needed` | Route-state block says preflight or is absent, and there is no approved plan PR, merged plan, approved plan commit, or fresh staleness evidence. | The issue still needs preflight shape. | Hand the discovered mode to `/using-superpowers` with preflight context. The worker should inspect current source, produce or repair the repo-resident plan, open a plan-only PR, update Linear route state with plan path/PR/status, and stop before implementation. `/using-superpowers` owns lane selection; `work-mode-router` must not choose the Superpowers lane itself. |
| `preflight_complete_pending_approval` | Plan file exists under `.agents/docs/superpowers/plans/`, plan PR exists, route-state block says pending approval, and approval or merge evidence is absent. | The plan is ready for approval but not execution. | Stop and report pending approval. Hand the discovered mode to `/using-superpowers` only as stopping context. Do not select an implementation lane. |
| `approved_plan_execution_ready` | Approved plan is merged to `main`, plan path/PR/commit evidence exists, and the staleness check passes against current source. | The approved plan is ready to execute. | Hand the discovered mode to `/using-superpowers` with execution context. `/using-superpowers` owns Superpowers lane choice. |
| `stale_plan_repair_needed` | Approved plan exists, plan PR or merge evidence exists, and the staleness check fails but the drift is repairable inside the approved scope. | The plan is stale but repairable in the execution branch. | Hand the discovered mode to `/using-superpowers` with repair context. Repair stays in the execution branch unless the scope changes materially. |
| `blocked_ambiguous` | Durable markers conflict, are missing, or cannot prove approval, merge, or current staleness state. | The worker cannot route safely from durable evidence. | Stop and report blocked or ambiguous. Do not select an implementation lane. |

`work-mode-router` must not choose `/writing-plans`, `/executing-plans`, SDD, TDD, or any other implementation lane itself. It classifies the durable state and hands that discovered mode to `/using-superpowers`; `/using-superpowers` owns the Superpowers implementation-lane choice.

Gates are backstops, not the primary teaching surface. Future GPT should understand why a workflow gate exists before the gate has to catch a failure. Breaking a gate is bad because it may spend scarce resources, mutate protected source, collapse ambiguity, launder reports into truth, create false closure, or push work away from the correct production boundary.

## First classification

Classify the current request into the smallest sufficient mode:

- `ordinary_chat`: acknowledgement, ping, lightweight preference discussion, side chat, or meta that does not require source evidence.
- `continuity_ingress`: session buster, continuity export, resume packet, or next-session block.
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

- `repo_worker_coding` -> `/using-superpowers` with the discovered mode. `work-mode-router` only supplies the durable mode classification; `/using-superpowers` owns the workflow-lane choice after that. Do not hard-route to `/writing-plans`, `/executing-plans`, or any other Superpowers lane here.
- `gpt_native_skillwork` -> `skill-creator` for authored skill content, then `writing-skills` for cross-repo wording and doctrine checks when relevant. Do not delegate GPT-native skillwork to a cloud agent unless the editable source is known to live in a worker-accessible repo and the task is explicitly repo-backed.
- `github_proof` -> the repo/GitHub proof surface after a GitHub artifact exists. Do not use repo/GitHub proof to decide worker state or issue routing.
- `linear_control` -> `linear` for connector mechanics: create/update/fetch/comment/project/status/label/document work.
- `verification_or_reporting` -> the narrow downstream skill that owns the decision, such as the validation decision surface, `tps-ingress`, or `tps-reporting`.
- `legacy_plan_b` -> the compact legacy dispatch stack only after the default route has been rejected or unavailable.

Use project bootstrap or project doctrine only when the active project actually matches the project wrapper and the current task needs local law.

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

When the active project or workspace reserves a shape, lower workflow skills must yield to that rule. In worker-control contexts, YAML-shaped blocks are reserved for lawful send-ready legacy dispatches, session busters, and user-explicit YAML artifacts. Do not use YAML blocks for ordinary assessments, plans, buster summaries, status notes, or conversational analysis. Use prose, a small markdown table, a JSON code block, or another clearly non-dispatch shape instead.

This guard is not a ban on structure. It prevents attention and copy/paste failures where a non-dispatch assessment looks like something a worker should execute, or where a non-continuity note looks like a session buster.

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

When your human partner provides a session buster, continuity export, resume packet, or next-session block, run the project bootstrap first when applicable, then route the block through the relevant session-buster ingress skill. Do not act directly on recommended next actions until ingress separates verified state, fallback state, source claims, open queues, and user instructions.

For coding work, prefer durable Linear issue IDs, worker state, PR IDs, and next checks over bulky packet prose. Linear, GitHub, and repo guidance are the normal continuity surfaces; session busters are fallback continuity.

## Output behavior

For ordinary first-turn use, do not print a long bootstrap audit. Read the relevant surfaces, then answer or route compactly.

For explicit audits, system-prompt work, or bootstrap-skill updates, report in prose or another non-reserved shape unless your human partner explicitly requests YAML. If a structured sample is useful, prefer JSON.

## Boundaries

Do not use this skill to execute project work directly. Do not mutate repos, post comments, generate or edit images, build artifacts, create dispatches, delegate Codex, or close issues from bootstrap alone. Use the specific skill that owns the task.
