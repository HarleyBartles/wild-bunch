### Devin Desktop dispatch contract

The only caller-controllable subagent control in Devin Desktop is the `run_subagent` dispatch `profile`. The runtime selects the actual model, reasoning effort, context tier, and any paid route. Do not attempt to specify those in the `task` prompt or elsewhere.

`run_subagent` accepts:

- `profile`: `subagent_explore` (read-only) or `subagent_general` (full tool access)
- `task`: the instruction
- `title`: short human-readable label
- `is_background`: launch in the background for parallel work
- `resume`: continue a previous subagent

The runtime assigns the same model as the parent session. Do not encode current model names or versions in prompts, task briefs, or rationale; they may change.

### Selecting the dispatch profile

- `subagent_explore` — read-only exploration, research, inventories, scans, technical review, code review, and any task that does not require file edits or command execution.
- `subagent_general` — implementation, mutation, file edits, command execution, validation, and any task that requires write or exec access.

A task that mixes read-heavy exploration with mutation is normally `subagent_general` with bounded mutation. Use `subagent_explore` only when the work is genuinely read-only.

### Task routing

| Task | Dispatch |
|---|---|
| Live source exploration / planning (read-only) | `subagent_explore` |
| Planning that will be implemented by the same subagent | `subagent_general` |
| Mechanical / approved implementation | `subagent_general` |
| Hidden root-cause bug | `subagent_general` with broad investigation and bounded mutation |
| Screenshot / frontend diagnosis | `subagent_general` if interactive tooling is needed, else `subagent_explore` |
| Technical code review | `subagent_explore` with fresh context |
| Architecture / intent challenge | `subagent_explore` with a focused, non-overlapping prompt |
| Large repo / diff context pressure | Decompose across `subagent_explore` and `subagent_general`; there is no paid context tier |
| Retry after a failed subagent | Refine the prompt, narrow scope, or decompose; do not retry by "changing model" |

### Deviation from shared policy

The shared policy's free/included/metered and cost-preference rules do not apply in Devin Desktop because the runtime does not expose paid or metered choices. Route by capability and access need only.

### What not to do

- Do not specify a model name, version, reasoning level, context tier, or paid route. The tool has no such parameters.
- Do not select `subagent_general` for purely read-only work; it broadens the permission surface unnecessarily.
- Do not select `subagent_explore` for tasks that must write files or run commands.
- Do not treat `is_background` as a model or reasoning selector; it only controls parallel launch.
- Do not request paid context; no such option exists.
