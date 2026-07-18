---
name: inspecting-the-environment
description: Use when about to take action and environment constraints could change
  the next step — discovers shell syntax, worktree state, repo state, path style,
  CLI availability, auth, connectors, mutation authority, and protected surfaces before
  proceeding.
metadata:
  source-id: inspecting-the-environment
  source-path: sources/first_party/skills/inspecting-the-environment/SKILL.md
  provenance-name: Inspecting The Environment first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when about to take action and environment constraints could change the
    next step — discovers shell syntax, worktree state, repo state, path style, CLI
    availability, auth, connectors, mutation authority, and protected surfaces before
    proceeding.
  use_when:
  - Use when about to take action and environment constraints could change the next
    step — discovers shell syntax, worktree state, repo state, path style, CLI availability,
    auth, connectors, mutation authority, and protected surfaces before proceeding.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---
# Inspecting the Environment

Use this skill when about to take action and the operating environment could
change what the right next step is. Discover the constraints, record a concise
environment decision, then hand off to the workflow that actually does the work.

## Core job

Before action, select and record the operating surface:

1. which environment constraints matter for this task;
2. what the current state of each constraint is;
3. whether any constraint changes the next action.

If no constraint changes the next action, say so and proceed. Do not stall on
environment inspection when the environment is already known and stable.

## Environment dimensions

Check the dimensions that could change the next action for this task. Skip
dimensions that are clearly irrelevant.

- **Shell syntax.** Am I in bash or PowerShell? This changes command syntax,
  path separators, environment variable access, and quoting rules. On Windows,
  default to PowerShell unless the environment proves otherwise.
- **Filesystem and worktree state.** Am I in a git worktree? What branch? Is
  the working tree clean? Am I in the main checkout or an isolated worktree?
  This affects whether I can commit, what branch I'm on, and whether I need to
  create a worktree first. **Worktree location verification:** If in a worktree,
  check whether the worktree path matches the repo's declared canonical worktree
  root. Look for an `AGENTS.md` or skill guidance declaring a canonical location
  (e.g., `../_agent-worktrees/<repo-name>`). If the worktree is in a non-canonical
  location, report the mismatch and recommend moving it before proceeding with
  substantive work.
- **Current repo and branch state.** What repo am I in? What's the current
  branch? Is it tracking a remote? Is the remote up to date? This affects push,
  PR creation, and mergeability.
- **Path style.** Windows paths or POSIX paths? This affects file references,
  command arguments, and tool invocations. Normalize to the platform's native
  style for commands that care, and use forward slashes for tools that accept
  both.
- **CLI availability.** What CLIs are available? `py -3`, `gh`, `git`, `npm`?
  Check before assuming a tool exists. Use `py -3` for Python on this repo, not
  `python3` or `python` unless the environment proves they work.
- **Auth state.** Am I authenticated to GitHub? Linear? Other connectors? This
  affects whether I can create PRs, read issues, or make API calls. Check auth
  before attempting operations that require it.
- **Connector and MCP availability.** What MCP servers are available? What
  tools do they expose? This affects what external systems I can reach. List
  available connectors before assuming one is present.
- **Browser and runtime constraints.** Is a browser available? What runtimes
  are installed? This affects whether I can run tests, build projects, or
  interact with web surfaces.
- **Mutation authority.** Do I have write access to this repo? Am I in a
  read-only scope? What directories am I allowed to modify? This affects
  whether I can commit, push, or write files at all.
- **Generated and protected surfaces.** Which directories are generated (do
  not edit by hand) vs source custody (editable)? This affects where I write
  changes. Generated surfaces are downstream outputs; source custody is the
  edit point.

## Capability surface discovery

Tool, connector, and capability listings may be truncated, paginated,
filtered, or scoped. Absence from the visible portion is not proof that a
capability is unavailable.

If the needed tool is not visible, continue discovery before concluding it is
missing:

- inspect any continuation, truncation, or readback surface;
- retry with a narrower server, namespace, connector, label, or query;
- search likely action families instead of one guessed name;
- distinguish read/query tools from mutation tools when the surface mixes
  both.

Only report a capability as unavailable after focused discovery has actually
tested the relevant surface. Otherwise preserve uncertainty.

## Composition

Start with `@using-superpowers` as the workflow-selection entrypoint.

Use `@connector-safety` before any mutation or blocked-write recovery,
including GitHub writes, Linear writes, file mutations, or other high-risk
operations discovered during environment inspection.

Do not use this skill to replace `connector-safety`, GitHub proof skills,
Linear shaping, or any specialist workflow. It discovers the operating surface;
it does not execute the work itself.

## Inspection rules

- Check the smallest set of dimensions that could change the next action.
- Record a concise environment decision: what constraints matter, what state
  they're in, and whether they change the next step.
- If a constraint is unknown and could matter, check it before proceeding.
- If a constraint is known and stable, do not re-check it redundantly.
- Prefer read-before-write: inspect the environment before mutating it.
- Treat blocked or missing capabilities as a signal to narrow scope or ask your
  human partner, not as a reason to skip the check.
- Do not claim an environment is safe for an action without verifying the
  constraint that the action depends on.

## Authority split

This skill discovers and records the operating environment. It does not:
- authorize mutations (that's `connector-safety`);
- prove GitHub state (that's `github-operations`);
- shape Linear issues (that's `linear-issue-shaping`);
- implement code or execute workflows (that's the specialist workflow's job);
- replace source-of-truth verification (that's `verification-before-completion`).

It hands off to the appropriate workflow once the environment decision is
recorded.
