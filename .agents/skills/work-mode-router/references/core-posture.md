# Core Posture

## Core posture

Bootstrap is orientation and classification, not source inspection. A project-relevant bootstrap is mandatory once at new-session start when a project context is active or the first user task is project-scoped. Bootstrap must classify the current request before evidence-route, connector, mutation, artifact, worker, or downstream skill decisions.

**Session resume verification:** When a session resumes from a previous conversation (continuity ingress, summary block, or inherited worktree state), verify the worktree location before proceeding with substantive work. Check whether the current worktree path matches the repo's declared canonical worktree root (e.g., `../_agent-worktrees/<repo-name>` per `AGENTS.md` or `repo-worker-base` guidance). If the worktree is in a non-canonical location, move it with `git worktree move` before continuing. Do not inherit a wrong-location worktree as a given — the previous session may have created it without invoking `using-git-worktrees`.

**Skill invocation at session resume:** The `using-superpowers` skill's "invoke before ANY response" rule applies at session resume, not just at new session start. A continued session must still invoke bootstrap skills before substantive work — the previous session's skill invocations do not carry forward.

Normal coding work now routes through the repo-backed worker flow by default. Legacy chat/YAML dispatch stacks are Plan B only. Do not load old dispatch-family skills merely because your human partner says `dispatch`.


## Repository-worker handoff

For repository-backed work, the mandatory handoff is:

`work-mode-router` -> `repo-worker-base` -> matching baseline reference and local `.agents/guides/` guide -> Superpowers lane.

The router owns first classification and durable route-state decisions. `repo-worker-base` owns portable repo hygiene, composition, source/projection, validation, and publication boundaries. The consuming repository's matching local stage guide owns only its paths, commands, exclusions, CI, and exceptions. Superpowers owns stage technique and lane execution. The downstream lane must not run from the generic base alone when a local guide exists.

This pairing applies across the full repo-worker surface:

- planning: `planning-baseline.md` + `.agents/guides/planning-guide.md` -> `/writing-plans`;
- implementation: `implementation-baseline.md` + `.agents/guides/implementing-guide.md` -> `/executing-plans` or `/subagent-driven-development`;
- source evidence: the baseline for the active stage + its local guide -> the selected evidence or implementation lane;
- publication: `implementation-baseline.md` + `.agents/guides/implementing-guide.md` -> the publication-capable execution lane;
- review: `code-review-baseline.md` + `.agents/guides/code-review-guide.md` -> `/requesting-code-review`.

Design uses the same contract with `design-baseline.md` and `.agents/guides/design-guide.md` before `/brainstorming`. If the consuming repository has no local stage guide, `repo-worker-base` records that fallback and the baseline remains required. Once this router has classified the request, do not invoke `work-mode-router` recursively; hand the established mode to `repo-worker-base`, then continue through the paired baseline, local guide, and Superpowers lane.

`work-mode-router` only classifies and hands off. It does not perform repo hygiene, select stage technique, or execute project work; `/using-superpowers` owns the downstream lane choice after the base/baseline/local-guide pairing is established.

For worker starts, classify the durable route state before any implementation lane choice. A prompt such as `Pick up {{issue.identifier}} from Linear. Start with /work-mode-router.` must be enough to infer one of the worker route states in `route-states.md` from durable Linear/repo evidence.
