# Linear connector compatibility notes

Read this reference before project assignment, taxonomy conversion, sync verification, cleanup, or any operation where ChatGPT connector behavior matters.

## Proven read routes in Harley's workspace

The connector successfully listed teams, projects, issue statuses, and migrated issues by label.

## Proven write routes

The connector successfully:

- created a Linear issue;
- updated issue titles;
- updated issue priorities;
- updated issue states;
- created a project;
- updated and canceled a project;
- assigned imported issues to a project by project ID;
- created Linear documents attached to a project.

## Label reads and writes

For Linear issue-label reads, prefer small paginated reads using the team key rather than the display name.

Safe read probe in Harley's workspace:

```json
{"team":"WILL","limit":5}
```

Then continue with the returned `cursor`, keeping `team` as the team key or the verified team UUID.

Observed behavior:

- `team: "WILL"` worked for reading Will Workspace labels where the display-name form was unreliable or blocked.
- Small limits such as `5` or `20` worked and made the call easier to recover from.
- Broad label inventory calls or display-name team strings can trigger tool-layer blocking.

For team-scoped label writes:

1. Call `list_teams` first if the team key or UUID is not already known.
2. Use `list_issue_labels({"team":"<team-key>","limit":5})` as the first read probe.
3. Paginate with the returned cursor using the same team key or verified UUID.
4. After read proof, create or update labels one label at a time with the verified team UUID.


## Project status reality check

Project status should reflect the status of its child issues. Keep a project `In Progress` only when at least one child issue is actually in an in-progress workflow state. If the project contains only `Backlog` and `Todo` issues, use `Planned`. Use `Completed` only when the project outcome is done, and `Canceled` only when deliberately abandoned.

Before changing many issue statuses or moving issues into projects, check whether project status should be updated afterward so Linear does not advertise inactive work as active.

## Project assignment

Project assignment by project name may block or be unreliable. Project assignment by project ID worked in the trial.

When assigning to a project:

1. Create or fetch the project.
2. Capture the returned project ID.
3. Use that ID in the issue update.

## Project clearing limitation

The exposed `save_issue` schema rejected `project: null`; it required `project` to be a string. Do not promise that ChatGPT can clear project membership unless a different route is verified.

Safe fallback for accidental/test project residue:

- restore issue-native fields such as title, status, and priority;
- rename the test project with `CANCELED` or another inert marker;
- set project state to canceled if available;
- record remaining project membership as residue and ask Harley to clear it manually in the UI if needed.

## Tool-layer blocking

Some Linear calls were blocked by the runtime/tool layer until Harley refreshed the tool. If a previously working Linear class of operation blocks suddenly, treat refresh/retry as a live recovery path before concluding the connector lacks the capability.

## GitHub import behavior

Imported GitHub issues appeared in Linear with:

- Linear identifiers such as `HAR-241`;
- GitHub backlink attachments;
- migrated labels from GitHub;
- preserved descriptions.

Updating Linear title/status/priority on imported issues worked. Whether those updates sync back to GitHub must be verified separately through GitHub or the Linear UI; do not infer writeback from Linear success alone.

## Reversal proof

Title, priority, and status mutations were reversible through the connector. Project membership clearing was not proven and was blocked by schema.
