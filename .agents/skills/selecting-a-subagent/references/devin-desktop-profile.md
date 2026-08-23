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

### Custom subagent profiles (Devin Desktop only)

The `.md` profile assets under `assets/` are Devin Desktop custom profiles. They
are not used by Codex; for Codex, use `references/codex-multi-agent-v1-profile.md`
or `references/codex-multi-agent-v2-profile.md`.

Devin Desktop searches the following locations, in order: `~/.config/devin/agents/` (or `%APPDATA%\devin\agents\` on Windows) for user-global profiles, `.devin/agents/` for user/repo-local hand-authored overrides, and `.agents/agents/` for plugin-local profiles installed by marketplace packs. Each profile is a named `.md` file: `reviewer.md`, `reviewer-fixes.md`, `reviewer-strong.md`, `implementer.md`, `implementer-strong.md`, etc. A skill can dispatch to a custom profile using the `profile:` argument to `run_subagent`.

The `selecting-a-subagent` helper installs shipped profiles to the user-global
agents directory by default. `.devin/agents/` is reserved for user-managed,
repo-local overrides that should not be created, modified, or removed by any
skill; `.agents/agents/` remains available for plugin-local profiles installed
by other marketplace tooling.

| Task | Dispatch |
|---|---|
| Most reviews, architecture challenges, and focused re-reviews | `run_subagent profile: reviewer` |
| Full branch/PR diff review where the whole branch is in scope | `run_subagent profile: reviewer-strong` |
| Small, tightly focused reviews or coherent single-responsibility re-review diffs | `run_subagent profile: reviewer-fixes` |
| Bounded implementation / bugfix | `run_subagent profile: implementer` |
| Implementation that needs more reasoning or broader context | `run_subagent profile: implementer-strong` |

Reviewer dispatches must pass a prepared `<diff_path>` and optional `<pr_description>` in the task; the reviewer subagent does not resolve the diff itself.

| Task | Dispatch |
|---|---|
| Broad read-only exploration | `subagent_explore` |
| Broad mixed work | `subagent_general` |

Custom profiles may declare `model:` in their `.md` profile file. The runtime honors that model when the subagent is launched, but the tool set is also constrained by the profile `name` and is cached; edits may not take effect until the IDE is restarted. `allowed-tools` describes the expected tool set, but the runtime may expose fewer tools. For example, `reviewer-strong` on `glm-5-2` has `exec`, `grep`, `read`, `find_file_by_name`, and `write`; `reviewer-fixes` on `swe-1-6` has the same tools. To create an off-repo file from a profile that does not expose `write`, use `exec`.

### Vendor and third-party profiles

Marketplace packs can ship third-party subagent `.md` profile assets under
`assets/profiles/`. First-party portable profiles are maintained in
`codex-marketplace/plugins/superpowers-plus/skills/selecting-a-subagent/assets/`
and installed to the user-global agents directory by
`install_profiles.py --apply`.

A repo-local `.devin/agents/<name>.md` override wins over a user-global or
plugin-installed profile of the same name, but the installer does not create,
modify, or remove `.devin/agents/`. Plugin-local profiles staged in `.agents/agents/`
by other marketplace tooling are also considered. See `vendor-profile-packaging.md`
for the packaging contract and the full consumer search-path order.

### What not to do

- Do not specify a model name, version, reasoning level, context tier, or paid route. The tool has no such parameters.
- Do not select `subagent_general` for purely read-only work; it broadens the permission surface unnecessarily.
- Do not select `subagent_explore` for tasks that must write files or run commands.
- Do not treat `is_background` as a model or reasoning selector; it only controls parallel launch.
- Do not request paid context; no such option exists.
