---
name: connector-safety
description: Use when use this skill to keep connector and tool-side-effect work safe,
  auditable, and boring when a connector or tool call is blocked, rejected, safety-filtered,
  permission-rejected, schema-rejected, or validation-rejected, when a planned action
  could be sensitive, destructive, permission-changing, or easy to over-bundle, or
  when mutation work should follow discover -> read -> write -> verify or step back
  up the connector discovery chain.
metadata:
  source-id: connector-safety
  source-path: sources/first_party/skills/connector-safety/SKILL.md
  provenance-name: Connector Safety first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when use this skill to keep connector and tool-side-effect work safe,
    auditable, and boring when a connector or tool call is blocked, rejected, safety-filtered,
    permission-rejected, schema-rejected, or validation-rejected, when a planned action
    could be sensitive, destructive, permission-changing, or easy to over-bundle,
    or when mutation work should follow discover -> read -> write -> verify or step
    back up the connector discovery chain.
  use_when:
  - Use when use this skill to keep connector and tool-side-effect work safe, auditable,
    and boring when a connector or tool call is blocked, rejected, safety-filtered,
    permission-rejected, schema-rejected, or validation-rejected, when a planned action
    could be sensitive, destructive, permission-changing, or easy to over-bundle,
    or when mutation work should follow discover -> read -> write -> verify or step
    back up the connector discovery chain.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---
# Connector Safety

Use this skill to keep connector and tool-side-effect work safe, auditable, and boring when a connector or tool call is blocked, rejected, safety-filtered, permission-rejected, schema-rejected, or validation-rejected, or when a planned action could be sensitive, destructive, permission-changing, or easy to over-bundle.

## Automatic trigger

If a connector, tool, or safety layer blocks a call, stop and switch into `connector-safety` recovery automatically.

Do not treat the block as a request to paraphrase the payload from memory, guess a new shape, or keep retrying the same mutation surface.

## Core rule

Treat connector or tool safety blocks as signals to narrow, clarify, verify, or stop. Do not frame the safety layer as an adversary and do not try to bypass it.

A blocked mutation is not proof that the mutation happened. A planned mutation is not proof of authorization. A retry is lawful only when it is materially safer, narrower, clearer, or more auditable than the failed call.

After a block, do not retry by paraphrasing the failed payload or rebuilding it from memory.

## Authority-gated side-effect fields

Some connectors expose fields that change execution authority, handoff control, or who can act next. Treat those fields as high-risk side effects even when the surrounding write looks routine.

For Linear, `delegate` is one such field. Only write it when your human partner explicitly asks to delegate the issue to a named agent or explicitly asks for Linear native delegation on that issue.

Do not infer `delegate` from `send`, `run`, `worker-ready`, `Devin-ready`, `for Devin`, `campaign-sized`, `start`, `worker`, `agent`, or similar wording.

Do not use `!`-prefixed labels as a proxy for delegation or worker pickup.

## Discovery-before-mutation rule

For side-effecting connector work, use the connector itself to discover the narrowest safe mutation target before writing, then rediscover and read back the mutated target before the next mutation.

Default mutation law:

1. Discover the bounded parent surface with read-only calls.
   * Find the team, project, repository, folder, calendar, draft, issue, PR, document, or parent object using the narrowest available filters.
   * Prefer exact slugs, keys, IDs, team filters, project filters, owner filters, and limits.
   * Do not jump from session memory or chat knowledge straight to a write when a connector read can cheaply confirm the target.
2. Read the exact target object using the discovered stable identifier.
   * Read the exact issue, document, PR, draft, event, file, or record that will be mutated.
   * Confirm current state, relations, attachments, documents, comments, or equivalent context where relevant.
3. Write one bounded mutation using the discovered identifier.
   * Use one side effect per call.
   * Use narrow payloads.
   * Do not bundle status, assignment, body rewrite, comments, relations, or document creation unless the connector requires it.
4. Rediscover the mutated thing from the parent identifier or a bounded search.
   * Treat the mutation as incomplete until the changed object is found again from durable connector state.
   * Use the parent surface, exact IDs, or bounded search to find the fresh target state.
5. Read back the freshly discovered target.
   * Confirm the post-mutation state before the next write.
   * Claim success only from the mutation result or readback.

This is the normal BAU loop, not just blocked-write recovery. The recovery ladder below adds the extra steps needed when a call is rejected or safety-filtered.

Even when the user or prior chat provides likely correct values, perform read-only discovery when practical. The discovery calls are part of the safety proof.

## Use this skill when

Use this skill when the task involves:

- a blocked connector/tool write;
- a side-effecting connector action such as creating, updating, deleting, sending, moving, assigning, renaming, archiving, merging, or publishing;
- deciding whether to retry after a tool or connector block;
- reducing a large mutation into smaller safer steps;
- writing or reviewing guidance for connector-safe recovery;
- reporting a blocked action without laundering it into completion.

Do not use this skill for ordinary read-only lookup unless the read is part of a blocked-write recovery or sensitive side-effect plan.

## Safe action ladder

1. Confirm current authority from the latest user request and the relevant durable surface.
2. Inspect the smallest relevant current state before writing when practical, including a discover/read chain when the target can be confirmed cheaply.
3. Prefer one side effect per call: create, update body, rename, relate, move, assign, or close as separate steps.
4. Keep payloads narrow and specific. Avoid bundling broad tool-control instructions, unrelated doctrine, and multiple mutations in one call.
5. If a call is blocked, do not claim success. Read back current state when safe to determine whether anything changed.
6. Retry only with a materially safer shape, such as smaller content, fewer fields, a non-destructive read probe, an ID instead of a name, or separate create-then-enrich steps.
7. Stop after repeated narrow failures, destructive ambiguity, unsupported schema errors, or unclear authority.
8. Report the blocker with enough detail for the user or next actor to continue safely.

## Post-create read-chain requirement

A successful create response is evidence that the object was created, but it is not always enough to justify immediate follow-up mutations against that object.

When a workflow creates a parent object and then needs to add child objects, attachments, comments, documents, relations, status changes, or other follow-up mutations, run a fresh discover/read chain before the next write when practical.

Example pattern:

1. Create the parent object with a narrow payload.
2. Discover the parent through the connector using bounded filters, such as team, project, folder, repository, owner, title, or issue key.
3. Read the exact parent object with its stable ID.
4. Confirm the parent is on the expected durable surface and has the expected current state.
5. Create or update one child object using the stable parent ID.
6. Read back the parent or child object before claiming success.

This matters even when the create response includes the new object ID. The read-only discovery path proves that the target exists in the current durable connector surface before the agent switches back into write mode.

If a follow-up write blocks after a successful create, step back into discovery rather than retrying the same write. Discover the bounded surface, read the parent, then retry only with a narrower or more auditable mutation.

## Minimal create, then enrich

When creating child objects such as documents, comments, drafts, attachments, tasks, or records, prefer a minimal create followed by a targeted update when the full payload is large or previously blocked.

* Create the object with the stable parent ID, title, and short placeholder body.
* Use the returned child object ID for enrichment.
* Update only that child object's content.
* Read back the parent or child after enrichment.

This is safer than creating a large fully populated child object in one call because the parent binding and child identity are proven before the larger content mutation.

## Blocked-write recovery ladder

When a connector write is blocked, do not claim success. Follow this explicit recovery ladder:

1. Acknowledge that the mutation did not happen.
2. Step back to bounded parent discovery before any retry.
3. Discover the target from that parent surface and read the exact target.
4. Read dependent connector vocabulary before writing: labels, statuses, projects, folders, users, branches, milestones, or other connector-owned values.
5. Retry once only with one narrower safer mutation using the discovered stable values.
6. Read back from the mutated target.
7. Stop after repeated parent-discovered failure and report observed state.

Include a shortcut guard: a retry from memory or a stale target reference does not count as full recovery. Full recovery means parent discovery -> target discovery -> exact target read -> dependent vocabulary read if needed -> one narrower safer mutation -> rediscover mutated thing -> readback.

Concrete Linear examples:

- issue mutation: `Linear team discovery -> issue discovery from team -> exact issue read -> one narrow issue field mutation -> rediscover the issue from the team or issue key -> read back`
- project mutation: `Linear team or workspace discovery -> project discovery -> exact project read -> one narrow project mutation -> rediscover the project -> read back`
- document mutation: `Linear team or workspace discovery -> document discovery -> exact document read -> one narrow document mutation -> rediscover the document -> read back`
- milestone mutation: `Linear team discovery -> milestone discovery -> exact milestone read -> one narrow milestone mutation -> rediscover the milestone -> read back`
- relation or blocker mutation: `Linear team discovery -> source issue discovery -> target issue discovery -> exact relation or blocker read if available -> one narrow relation mutation -> rediscover the source issue or relation from the team -> read back`
- label mutation: `Linear team discovery -> label discovery -> exact label read -> one narrow label mutation -> rediscover the label from the team -> read back`
- status mutation: `Linear team discovery -> status vocabulary discovery -> exact status read -> one narrow status mutation -> rediscover the status from the team -> read back`
- comment mutation: `Linear issue discovery -> exact issue read -> one narrow comment mutation -> rediscover the issue comment thread -> read back`
- assignee mutation: `Linear team discovery -> issue discovery -> user vocabulary discovery if needed -> exact issue read -> one narrow assignee mutation -> rediscover the issue from the team -> read back`

## Upstream discovery fallback

When a read, write, or readback is blocked even though a likely stable identifier is known, do not keep retrying the same target-level call. Step back to the nearest parent discovery surface that can prove the target exists in the current connector state.

Use the connector-observed chain:

parent discovery -> bounded target discovery -> exact target read -> one bounded mutation -> readback.

Parent discovery surfaces can be team, project, repository, folder, workspace, branch, owner, or another connector-native object that proves the target belongs to the current state.

If post-mutation readback blocks, step back to the same parent discovery chain used before the mutation, then re-read the target from that chain.

A retry after a blocked write or readback should usually move one step earlier in the chain:

* if write or readback blocks, step back to the parent discovery surface and read the parent or target again;
* if target read blocks, discover the bounded surface with narrower filters or a parent surface that proves the target exists;
* if workspace search blocks, use team/project/repo/folder/workspace/branch/owner filters;
* if large create blocks, create a minimal child object first, then update by returned child ID;
* if follow-up child write blocks after a successful parent create, rediscover and read the parent before retrying;
* if repeated bounded discovery or writes block, stop and report.

## Mutation classes

Use stricter posture as side effects increase.

- Low-risk writes: comments, draft notes, non-destructive metadata, or compact document updates. Narrow once, retry once or twice if safe.
- Medium-risk writes: issue status, assignment, labels, project moves, document renames, calendar drafts, email drafts. Separate fields and verify after mutation.
- High-risk writes: sends, deletes, archives, merges, closes, publishes, permission changes, irreversible or externally visible actions. Require clear user authorization and do not retry ambiguously.

## Exact-state guarded high-risk writes

For high-risk connector writes such as merge, close, delete, publish, send, archive, or permission-changing actions, prefer an exact-state guard when the connector supports one.

Use this ladder:

1. Confirm current user authority from the latest message.
2. Read the target object immediately before the write.
3. Extract the exact current-state guard where available, such as:
   - PR head SHA for merge;
   - current draft or message ID for send;
   - current file blob SHA for update or delete;
   - current issue, event, or comment ID for status or comment mutation.
4. Make one narrow write call containing only:
   - stable target identifier;
   - requested action;
   - exact-state guard, if available;
   - no optional prose, status summaries, labels, unrelated comments, or bundled mutations unless the connector requires them.
5. Read back the target object after the write.
6. Report success only from the mutation result or readback.

If the first write is blocked, retry only when the next attempt is materially safer. Adding an exact-state guard, replacing a fuzzy target with a stable ID, removing optional fields, or splitting bundled mutations are safer shapes. Repeating the same payload is not.

## Invalid-attempt distinction

A malformed schema, typo, invalid JSON payload, wrong field, or incomplete tool argument is not a meaningful blocked-write attempt. Classify it as `invalid_attempt`, correct the payload once, and then perform the clean guarded call if authority and target state still hold.

Do not use an invalid attempt as evidence that the connector or safety layer rejected the actual authorized action. Do not keep retrying malformed calls. If the corrected clean call blocks, then treat that as the real blocked mutation.

## Post-success closeout writes

After a high-risk external mutation succeeds, treat tracking closeout as a separate mutation.

1. Verify the high-risk mutation in the target system.
2. Prepare the narrowest durable update, such as issue status only or a compact evidence comment only.
3. If a status update blocks, do not weaken the primary proof. Report that the primary mutation succeeded and the closeout mutation blocked.
4. Prefer a compact evidence comment only when it is lower-risk, explicitly useful, and authorized by the current context.
5. Never claim an issue was closed, marked done, or updated unless that write is verified.

## Documentation and safety internals

When recovering from a blocked connector write, do not search for ways to bypass, defeat, or explain internal safety classifiers. Use documentation only to confirm supported connector schema, product behavior, or safer state guards.

Prefer connector-state evidence over safety speculation: read the target, narrow the payload, add an exact-state guard, retry once if materially safer, read back, and stop after repeated narrow failures.

Report only observable facts: attempted action, target, authority, result, readback, and next safe action. Do not claim exact hidden classifier triggers.

## Blocked-write report shape

When a connector/tool action blocks or remains uncertain, report:

```text
Attempted action: <what was attempted>
Target: <system and object>
Authority used: <latest user instruction or durable authorization>
Observed result: <tool response, block, invalid attempt, or no response>
Verification: <readback performed or why not>
Safe retry attempted: <narrower retry, corrected invalid attempt, or none>
Final state: <done / not done / unknown>
Next safe action: <manual action, narrower retry, missing authorization, or blocker>
```

Keep this report factual. Do not include hidden policy speculation or claim exact classifier triggers.

## Retry guidance

A retry must change the risk shape. Good retries include:

- create a minimal object first, then enrich it;
- update only the title or only the body;
- remove unrelated context from the payload;
- split destructive and non-destructive work;
- use a known stable ID instead of fuzzy name matching;
- use an exact-state guard such as PR head SHA, file blob SHA, or current object ID;
- correct a malformed or invalid payload once, then run the clean guarded call;
- do a harmless read probe before another mutation;
- ask for explicit confirmation when authority is ambiguous.

Bad retries include:

- repeating the same blocked payload;
- treating malformed calls as proof of safety rejection;
- adding language about bypassing, defeating, or working around safety;
- using another connector surface to smuggle the same blocked mutation;
- claiming completion from a planned action, chat summary, or stale state;
- continuing after destructive ambiguity.

## Handoff and evidence

For durable work, leave compact evidence in the proper return surface only after the action is actually performed or definitively blocked.

Do not update issue closeout, status, or proof surfaces to say a package, mutation, send, merge, close, or install happened until the actual handoff or mutation is complete and verified.

When install, package, or artifact handoff is involved, verify the exact file/path/hash or tool response immediately before presenting it. A planned path is not proof.

## Stop signs

Stop and report instead of retrying when:

- the user has not authorized the side effect;
- the target object is ambiguous;
- the operation is destructive, irreversible, externally visible, or permission-changing and the result is uncertain;
- the connector reports an unsupported schema or missing capability;
- repeated narrower attempts fail;
- readback contradicts the expected result;
- the only available next step would be to bypass, hide, or misrepresent a safety layer.
