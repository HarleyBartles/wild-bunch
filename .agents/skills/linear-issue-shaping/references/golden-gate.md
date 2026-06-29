# Golden Gate

Read before delegating a Linear issue to a repo worker, nudging a worker, or treating a planning issue as executable worker work.

## Decision

Delegate to a repo worker only when all answers are yes:

1. The latest user message authorizes execution or dispatch, not just discussion.
2. The target is a repo-backed coding/docs/config surface that the worker can clone, edit, validate, and publish as a PR.
3. The Linear issue identifies the repo or implementation surface clearly enough for a worker.
4. The task can be completed inside the worker environment without private ChatGPT skill-library mutation, manual UI-only actions, unavailable local resources, or hidden credentials.
5. The expected output can return through Linear comments plus a GitHub PR publication path.
6. The issue body is bounded enough that the worker is executing, not deciding product strategy or architecture from scratch.
7. Any required human gate is explicit.
8. For repo work that needs a campaign shape, the issue body or handoff includes the worktree isolation gate (see `campaign-shape.md`).

If any answer is no, do not delegate. Route to planning, native skill maintenance, connector setup, UI instructions, research, or a legacy fallback as appropriate.

## Campaign capability

Repo workers are not the old narrow single-worker lane. A campaign issue can handle chunky repo campaigns, many related subtasks in one PR, and subagent-style work where available. A campaign issue may be larger than a small worker-ready issue. Do not reject a repo worker delegation merely because the issue is chunky. Do not encode "a worker can do anything." Workers remain bounded by issue scope, repo access, protected surfaces, validation, PR proof, and publication rules.

## Blip catchers

Block delegation when the work is mainly:

- creating, updating, validating, or packaging ChatGPT native skills, unless the editable skill source is repo-backed and the issue explicitly targets that repo;
- changing ChatGPT custom instructions, memory, project instructions, or connector settings;
- researching docs or product behavior without repo edits;
- asking Harley to click a UI control, install an app, grant permissions, or configure an account;
- requiring local-only files, private desktop state, or secrets unavailable to the worker;
- deciding broad doctrine before the doctrine has been shaped into concrete repo-backed edits.

GPT-native skillwork is not routed to a worker merely because it touches skill text. Route it only when the editable source is repo-backed and the issue explicitly targets that repo.

Crew, project routers, and native skill-maintenance routes are allowed to stop the dispatch before this skill delegates. Treat that as correct friction, not a failure.

## Gate result language

Use one of these outcomes:

- `pass_delegate`: issue is executable by Codex and current user authorized dispatch.
- `hold_native_route`: the task belongs to GPT-native skill, connector, UI, research, or planning work.
- `hold_shape_issue`: the task may be Codex-executable but the Linear issue is not worker-ready.
  - `hold_unavailable_surface`: the task target is not accessible/publishable from the current cloud-agent route.
  - `legacy_plan_b`: the Linear-backed worker route is unavailable or explicitly not in use.

For holds, name the next concrete action. Do not delegate "just to see."
