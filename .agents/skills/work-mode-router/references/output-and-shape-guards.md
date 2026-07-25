# Output and Shape Guards

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
