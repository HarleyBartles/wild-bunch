# Devin harness capability floor

The version-2 evidence kernel depends on a small set of harness capabilities. `reviewctl doctor` detects the runtime first: it reports `inert` (exit 1) on any non-Devin runtime, and mutation commands refuse off-Devin runtimes with `unsupported-runtime` rather than silently degrading to assertion-based evidence. On Devin Desktop it additionally runs the live row table below when `--scratch-dir` (and optionally `--repo`) are given, and reports `capability-floor-failed` (exit 1) when any row fails.

This document records the empirical basis for each requirement. Two evidence classes are cited:

- **live** - reproduced during Plan 1 Task 0 in the current session.
- **recorded** - verbatim artifacts retained from the capability spike under the review scratch root, hash-bound below.

Recorded artifacts (scratch, disposable):

```text
<scratch-root>/<repo>/<branch>/capability-floor/
  pretool.jsonl       sha256 f490927ba270151168464c45852052f96a180133c66e9738886378e7af028688
  posttool.jsonl      sha256 09355375713fb0b759ae4d1ea96e1b3ab35414f9a21aa66148cf01fb7a479197
  SPIKE-FINDINGS.md   sha256 4bc884f53776731ee4f08e33124eb572b4d9404f2af18d461ef3785717ddde27
```

## Capability matrix

| Capability                                                                                                | Status               | Evidence                                                                                                                                                                                                    |
| --------------------------------------------------------------------------------------------------------- | -------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Devin runtime detected; non-Devin stays inert                                                             | PASS                 | live: `doctor` precondition is `runtime == devin-desktop`; all other runtimes return `unsupported-runtime` and mutate nothing                                                                               |
| PreToolUse/PostToolUse hook records emitted for orchestrator calls                                        | PASS                 | recorded: 263 PreToolUse / 245 PostToolUse records, one session, every call captured (`pretool.jsonl`, `posttool.jsonl`)                                                                                    |
| Hook records carry `session_id`, `prompt_id`, `tool_name`, `tool_input`, `tool_use_id`, `hook_event_name` | PASS                 | recorded: field set verified verbatim on all records; a single `session_id` persisted throughout                                                                                                            |
| Hook records emitted for subagent dispatches and inner subagent tool calls                                | PASS                 | recorded: 12 `run_subagent` launches + 12 completions; inner calls (`edit`, `exec`, `read`, `grep`, `write`) all carry the parent `session_id`                                                              |
| Subagent inner calls carry the correlation field needed for positional attribution                        | PASS-with-constraint | recorded: inner calls share `session_id` but no `agent_id` field exists in hook records; attribution is positional (records between launch and completion), so reviewer dispatches must serialize           |
| `session_id` survives IDE restart; `prompt_id` rotates per prompt                                         | PASS                 | recorded + live: the same `session_id` persisted across restart; 16 distinct `prompt_id` values                                                                                                             |
| `allowed-tools` confines a subagent profile to read/search-only                                           | PASS                 | live: `subagent_explore` reported exactly `code_search, find_file_by_name, get_output, grep, notebook_read, read, web_search`; no `exec`/`write`/`edit`/`webfetch`/`mcp_call_tool`/`run_subagent`           |
| Permission deny rules block canary reads and writes, including inside `exec` command text                 | PASS                 | recorded: `Read(...CANARY-*)`, `Write(...)`, `Edit(...)` deny rules all refused; deny evaluation reaches `exec` command strings                                                                             |
| Deny refusal terminates or blocks the call, not just logs                                                 | PASS                 | recorded: canary write through `exec` ended the turn; canary read through `read` soft-denied and the run continued                                                                                          |
| File tools reject out-of-workspace paths before deny evaluation                                           | PASS                 | recorded: a second confinement layer exists; out-of-workspace `read`/`write` are refused by the tool layer itself                                                                                           |
| `ask_user_question` PostToolUse captures the literal user selection                                       | PASS                 | recorded: response output contained `"selected": ["Alpha"]`, `skipped: false`; `human-decision` witnesses bind the real answer                                                                              |
| `gh` authenticated; check-run/workflow-run remote observation on a pushed SHA                             | PASS                 | live: `gh auth status` active; a check-run record (app `github-actions`) carried `id`, `head_sha`, and `conclusion` fields; workflow-runs carry `id`, `run_attempt`, `run_number`, `head_sha`, `conclusion` |
| `gh pr ready` transitions draft to ready; idempotent; reversible via GraphQL `convertPullRequestToDraft`  | PASS                 | recorded: a disposable PR exercised the full transition, repeat call exited 0 ("already ready"), re-fetch via `isDraft` reconciled, re-draft mutation verified                                              |
| Required subagent profiles resolve from the user-global profile root                                      | PASS                 | live: `subagent_explore` dispatched successfully                                                                                                                                                            |
| Profiles defined inside the reviewed head cannot qualify a dispatch                                       | PASS                 | live: `.devin/agents/self-qualify-probe.md` created in the reviewed tree; `run_subagent profile="self-qualify-probe"` failed to start                                                                       |
| Review scratch creatable under canonical off-repo root                                                    | PASS                 | live: `<scratch-root>/<repo>/<branch>/capability-floor/` created, artifacts written                                                                                                                         |
| Hash-chained witness log: intact chain verifies, tampered entry detected                                  | PASS                 | live: two-entry chain built in `witness-store/`; byte-level edit of entry 0 failed verification                                                                                                             |

## Findings that constrain the design

1. **No `agent_id` in hook records.** Dispatch attribution is positional only. The kernel requires serialized reviewer dispatches; concurrent background dispatches interleave in the transcript and cannot be attributed.
2. **Hooks are observe-and-block only.** A PreToolUse hook emitting `updatedInput` was ignored by the runtime; the tool ran on the original input. Hooks cannot rewrite tool calls.
3. **Hook config loads at session start.** Installing or removing hooks mid-session has no effect until restart. Live verification of hook emission therefore requires the hooks pack to have been installed before the session began; `reviewctl hooks install --scratch-dir <dir>` renders the pack (`record_pretool.py`, `record_posttool.py`, `gate_review_paths.py`, a rendered `hooks.v1.json`, and `hook-env.json`) under the review scratch root, and the `hooks-installed` doctor row checks that the pack exists. The rendered config still must be installed as `.devin/hooks.v1.json` in the reviewed project (or user-global) before the session starts.
4. **Model identity is not self-declared.** `subagent_explore`'s prompt carried no model name; reviewer-model pinning must come from the profile frontmatter at the user-global root, verified by the planned doctor recheck, not from agent self-report.
5. **The path gate is best-effort, not authoritative.** `gate_review_paths.py` emits `{"decision": "block", ...}` on stdout and exits 2 when a tool call touches a deny root, and fails closed (block + exit 2) when the hook payload cannot be assessed. Path keys (`file_path`, `path`, `notebook_path`, `target_file`, `workdir`, `cwd`) are resolved against the call's working directory with environment variables expanded before matching; `command` text is matched boundary-aware so a deny root does not over-match sibling names like `witness-backup`. Deny roots cover the witness, transcripts, and evidence-store directories only - the `acquire/` directory and the state file are excluded because `reviewctl enumerate` and `complete --acquired` must write and read them; their integrity is enforced by the witnessed subject digests, the evidence-manifest checks, and feedback-findings rebinding instead. The state kernel remains the enforcer; the gate exists to stop honest accidents cheaply.
6. **Store permission enforcement is POSIX-only.** On POSIX the witness log and transcript recorders lock directories to 0700 and files to 0600 and refuse `acl-untrusted` pre-existing files that grant group/other access. On Windows - the current Devin Desktop target - those POSIX mode checks are no-ops; tamper-evidence relies on the review scratch living under the user's own profile directory (default NTFS ACLs grant only that user). Treat the scratch root as per-user private by placement, not by ACL audit.
7. **Hook commands are platform-aware.** `reviewctl hooks install` renders `{{IR_PY}}` as `py -3` on Windows and `python3` elsewhere; a host lacking both interpreters is unsupported.

## Live doctor rows

On Devin Desktop, `reviewctl doctor --scratch-dir <dir> [--repo <dir>]` rechecks the floor per row. Each row reports `pass`, `fail`, or `skip` with a `detail` and a `remediation` pointer:

| Row                       | What it verifies                                                              | Fail remediation                                                                               |
| ------------------------- | ----------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `hooks-installed`         | Rendered hooks pack exists under `<scratch>/hooks/`                           | `run \`reviewctl hooks install --scratch-dir <dir>\` then install the rendered hooks.v1.json\` |
| `transcript-dir-writable` | `<scratch>/transcripts/` accepts create/write/delete                          | `create the transcript directory with write access`                                            |
| `witness-log-roundtrip`   | `witness_log.WitnessLog` append + chain verify on the witness dir             | `check scratch-store permissions`                                                              |
| `git-present`             | `git --version` exits 0                                                       | `install git on PATH`                                                                          |
| `repo-non-shallow`        | `git rev-parse --is-shallow-repository` is `false` (skipped without `--repo`) | `fetch full history (git fetch --unshallow)`                                                   |
| `gh-authenticated`        | `gh auth status` exits 0                                                      | `run \`gh auth login\`\`                                                                       |

Verdicts: `inert` off Devin Desktop, `capability-floor-failed` when any row fails, `pass` otherwise. Rows that cannot run without inputs report `skip`.

## doctor verdict for this session

`PASS` on Devin Desktop with the recorded floor above and live rows green. `INERT` on Codex/OpenAI-compatible runtimes by contract.
