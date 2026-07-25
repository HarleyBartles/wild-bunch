# Workflow Phases

## Working Mode Phases

When a repo has this skill installed, coding work follows this structured workflow:

| Phase | Purpose | Entry Condition | Exit Condition | Sign-off Gate | Superpowers Skill | Guide Reference |
| --- | --- | --- | --- | --- | --- | --- |
| 0 - Worktree Isolation | Isolated workspace | Starting repo work | Worktree created/verified | N/A | `repo-worker-base` + worktree policy/local repository policy -> `/using-git-worktrees` + `/inspecting-the-environment` | N/A |
| 1 - Design | Create spec if ask unclear | Ask is unclear or complex | Spec exists following repo design guide | Design sign-off (9/10+) | `repo-worker-base` + `design-baseline.md` + local guide -> `/brainstorming` | `.agents/guides/design-guide.md` |
| 1a - Design Sign-off | Rate design for planning | Design complete | 9/10+ rating achieved | Planning agent can create full plan without improvising | N/A | Design guide handoff section |
| 2 - Planning | Write plan using repo guide | Design signed off OR ask is clear | Plan exists following repo planning guide | Plan sign-off | `repo-worker-base` + `planning-baseline.md` + local guide -> `/writing-plans` | `.agents/guides/planning-guide.md` |
| 2a - Plan Sign-off | Rate plan for implementation | Plan complete | Passes implementation rubric | Implementing agent can follow plan without improvising | N/A | Planning guide execution confidence |
| 3 - Implement | Follow repo implementing guide | Plan signed off | Implementation complete, PR raised | N/A | `repo-worker-base` + `implementation-baseline.md` + local guide -> `/executing-plans` and/or `/subagent-driven-development` | `.agents/guides/implementing-guide.md` |
| 4 - Code Review | Follow repo code review guide | PR raised | Review complete, merge decision | N/A | `repo-worker-base` + `code-review-baseline.md` + local guide -> `/requesting-code-review` | `.agents/guides/code-review-guide.md` |


## Superpowers Workflow Mapping

For each workflow phase, route to the corresponding superpowers skill:

| Workflow Phase | Superpowers Skill | Notes |
| --- | --- | --- |
| Worktree Isolation | `repo-worker-base` + worktree baseline/local policy -> `/using-git-worktrees` + `/inspecting-the-environment` | Use the base and worktree policy before workspace isolation or environment discovery |
| Design | `repo-worker-base` + `design-baseline.md` + local guide -> `/brainstorming` | Use before any creative work - explores user intent, requirements and design before implementation |
| Planning | `repo-worker-base` + `planning-baseline.md` + local guide -> `/writing-plans` | Use when you have a spec or requirements for a multi-step task, before touching code |
| Execution | `repo-worker-base` + `implementation-baseline.md` + local guide -> `/executing-plans` and/or `/subagent-driven-development` | Use `/executing-plans` for written implementation plans in separate sessions with review checkpoints. Use `/subagent-driven-development` for executing implementation plans with independent tasks in the current session |
| Code Review | `repo-worker-base` + `code-review-baseline.md` + local guide -> `/requesting-code-review` | Use when completing tasks, implementing major features, or before merging to verify work meets requirements |


## General Routing Guidance

For general guidance on which Superpowers workflow to route a piece of work to, first establish the `repo-worker-base` + matching baseline + local guide pairing, then use `/using-superpowers` for the appropriate lane. Superpowers does not replace the base handoff.


## Repo Guide Integration

Repo-specific guides (`.agents/guides/design-guide.md`, `.agents/guides/planning-guide.md`, etc.) own only repository-specific paths, commands, exclusions, CI, and exceptions. Local guides cannot override or bypass this canonical mapping: `repo-worker-base` -> matching baseline -> local guide -> Superpowers lane.


## Workflow Enforcement

This skill enforces the structured workflow by:

1. **Phase classification**: Before any coding work, classify the current workflow phase from durable evidence (Linear route state, repo artifacts, guide existence)
2. **Gate enforcement**: Do not proceed to the next phase without meeting the exit condition and sign-off gate
3. **Guide discovery**: When a repo has guides in `.agents/guides/`, reference them explicitly. When absent, fall back to generic workflow but still enforce the phase structure. A repository policy may document a retired legacy home as a migration fallback, but `.agents/guides/` remains canonical.
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
