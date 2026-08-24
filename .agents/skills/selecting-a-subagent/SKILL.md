---
name: selecting-a-subagent
description: Use when choosing a child subagent profile, model, reasoning level, or
  context mode for a task.
metadata:
  source-id: selecting-a-subagent
  source-path: codex-marketplace/plugins/superpowers-plus/skills/selecting-a-subagent/SKILL.md
  provenance-name: Selecting A Subagent first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when choosing a child subagent profile, model, reasoning level, or context
    mode, or when retrying failed work by changing profile, model, reasoning, or context.
  use_when:
  - Use before calling `spawn_agent` or an equivalent subagent tool.
  - Use when creating or selecting a named subagent configuration.
  - Use when recommending a child model, reasoning level, or context mode.
  - Use when retrying failed work by changing model, reasoning, or context.
  - Use when choosing a custom subagent profile such as `reviewer`, `reviewer-fixes`,
    `reviewer-strong`, `reviewer-security`, `reviewer-skills`, `reviewer-plans`,
    `reviewer-mesh`, `reviewer-scripts`, `implementer`, or
    `implementer-strong`.
  - Use when selecting an implementation, code-review, architecture-review, or adjudication
    agent.
  do_not_use_when:
  - Do not use to switch the current parent session when the runtime cannot change
    models mid-session.
  - Do not use when another more specific skill owns the task.
  related_skills:
  - dispatching-parallel-agents
  - risk-gates
  - repo-worker-base
  - inspecting-the-environment
license: MIT
---
# Selecting a Subagent

Use this skill before choosing a child subagent route. Detect the live dispatch
contract, load the shared policy and exactly one matching environment profile,
then choose the least escalated route the runtime actually exposes.

## Runtime contract

1. Detect the active child-dispatch contract.
2. Inventory the models, reasoning values, context controls, and capacity actually exposed.
3. Load `references/shared-policy.md` and exactly one matching profile.
4. Treat current runtime inventory as authoritative over stale profile metadata.
5. Choose the least escalated adequate exposed route; do not infer price or entitlement.
6. Record the profile, model or inheritance, reasoning or inheritance, context mode, rationale, and material limitation.
7. State explicitly when a desired route could not be enforced.

Routing chooses a route; it does not authorize delegation. Follow the current
task, environment, and repository rules before calling a child-dispatch tool.

## Profiles

| Live dispatch signature | Profile |
|---|---|
| `multi_agent_v1__spawn_agent` with Boolean `fork_context` | `references/codex-multi-agent-v1-profile.md` |
| `spawn_agent` with `fork_turns` | `references/codex-multi-agent-v2-profile.md` |
| Devin Desktop | `references/devin-desktop-profile.md` |
| Unknown or non-Codex runtime | `references/generic-free-first-profile.md` |

## Installing the custom profiles

The `.md` profile assets in `assets/` are Devin Desktop custom profiles. They are
not used by Codex; for Codex, use the `references/codex-multi-agent-v1-profile.md`
or `references/codex-multi-agent-v2-profile.md` mappings.

If you want to use the Devin Desktop custom profiles, run the helper to install
the shipped `.md` assets into the user-global Devin Desktop agents directory:

```
py -3 .agents/skills/selecting-a-subagent/scripts/install_profiles.py --apply
```

The helper overwrites shipped profiles only when they have changed and leaves any
other files in the target directory untouched. The default target is:

- macOS/Linux: `~/.config/devin/agents/`
- Windows: `%APPDATA%\devin\agents\`

Use `--target` to install to a different directory, such as a consumer
repository's `.agents/agents/` directory.

Do not install repo-local `<lens>.md` profiles from the pack. The consumer repo
authors its own `.agents/agents/reviewer-<lens>.md` files (or omits them) for its
own domain-specific surfaces.

## Common custom subagent profile dispatch

| Task | Profile |
|---|---|
| Most review tasks, focused re-reviews, and architecture challenges | `reviewer` |
| Full branch/PR diff review where the whole branch is in scope | `reviewer-strong` |
| Security and PII lens in a full-branch/PR diff | `reviewer-security` |
| `SKILL.md`/reference/prompt-robustness lens | `reviewer-skills` |
| Plans, specs, roadmaps, or `.agents/plans` and `.agents/specs` changes | `reviewer-plans` |
| `INDEX.md`, generated mesh, or `repo-standards` surfaces | `reviewer-mesh` |
| Script safety, CLI compliance, shebangs, or `--check`/`--apply` classification | `reviewer-scripts` |
| Small, tightly focused reviews or coherent single-responsibility re-review diffs | `reviewer-fixes` |
| Repo-specific lens for surfaces not covered by the portable set | `.agents/agents/reviewer-<lens>.md` (see below) |
| Bounded implementation / bugfix | `implementer` |
| Implementation that needs more reasoning or broader context | `implementer-strong` |

The orchestrator must provide a `<diff_path>` and optional `<pr_description>` to any
reviewer profile. The reviewer subagent does not resolve the diff itself.

## Lens dispatch from `## Applies to`

Every lens profile in `reviewer-*.md` profiles in the Devin Desktop agents search path (portable or repo-local)
should include a `## Applies to` section with:

- `inputs:` — required and optional `run_subagent` placeholders.
- `globs:` — path globs that, if matched in the diff, make the lens relevant.
- `keywords:` — keyword triggers that make the lens relevant.

When selecting one or more lenses for a PR or a branch diff, read the relevant
profile files and match them in this order:

1. Input match: if the orchestrator provides an input listed under `## Applies to`
   for that lens (e.g. `<plan_path>` for `reviewer-plans`), the lens applies.
2. Glob match: if any changed file matches a glob, the lens applies.
3. Keyword match: if the PR title/body or diff summary contains a keyword, the
   lens applies.
4. Default dispatch: if none of the above triggers a lens, dispatch `reviewer-strong`
   for the whole-branch pass.

Prefer the least escalated lens that covers the diff. For broad, multi-surface
branches, include all matching lenses rather than a single generalist.

## Repo-specific lens profiles

A consumer repo can extend the portable lens set by authoring a hand-edited
`.agents/agents/reviewer-<lens>.md` override. These are not installed by the
marketplace; they are repo-local and take precedence over vendor profiles.

Use this when the repo has domain-specific surfaces that a generic lens cannot
cover. For example, one consumer might add a `.agents/agents/reviewer-marketplace.md`
lens for pack generation, another might add `reviewer-domains.md` for domain canon,
or `reviewer-tests.md` for a test harness. These are not part of the portable pack.

When `iterative-review` runs, it should discover each `reviewer-*.md` profile from
 the Devin Desktop agents search path, evaluate the `## Applies to` section against
the diff, PR description, and any provided inputs, and dispatch only the matching
lenses plus `reviewer-strong`.

## Vendor and third-party profiles

This skill ships first-party portable subagent `.md` profiles under
`codex-marketplace/plugins/superpowers-plus/skills/selecting-a-subagent/assets/`.
Run `py -3 .agents/skills/selecting-a-subagent/scripts/install_profiles.py --apply`
to copy them to the Devin Desktop user-global agents directory
(`~/.config/devin/agents/` or `%APPDATA%\devin\agents\` on Windows). Use
`--target <dir>` to install elsewhere; the default target is the canonical
surface for shared, portable profiles.

When choosing a profile, apply the Devin Desktop agents search path; later
directories in this list override earlier ones:

1. Built-in profiles documented in `references/devin-desktop-profile.md`.
2. User-global profiles (`~/.config/devin/agents/` or `%APPDATA%\devin\agents\` on Windows).
3. `.devin/agents/<name>.md` user- or repo-local hand-authored overrides.
4. `.agents/agents/<name>.md` plugin-local or vendor profiles.

No skill should create or pressure the consumer to create `.devin/agents/`.
`.agents/agents/` remains available for plugin-local or vendor profiles staged by
other marketplace tooling.

See `references/vendor-profile-packaging.md` for the packaging contract and the
full consumer search-path order.

## Common pressure

When the obvious choice is unclear or contested, read `references/pressure-scenarios.md` first.
