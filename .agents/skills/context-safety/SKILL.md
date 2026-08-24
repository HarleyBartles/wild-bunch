---
name: context-safety
description: Use when a text write is expected to exceed the safe threshold for the
  remaining session context, when a document is very large or context-heavy, or when
  a normal editor write path would be brittle.
metadata:
  source-id: context-safety
  source-path: codex-marketplace/plugins/repo-worker-pack/skills/context-safety/SKILL.md
  provenance-name: Context Safety first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: very large text write safety, bounded composition, compaction boundaries, and atomic replacement.
  use_when:
  - Use when a text write is expected to exceed 2,000 lines or 1 MB of UTF-8 text.
  - Use when inline composition would risk consuming the remaining session context.
  - Use when safe staging and atomic replacement are required for a large text write.
  - Use when `/compact` should happen only after durable state has been preserved.
  do_not_use_when:
  - Do not use when the change is small and can be written directly.
  - Do not use when the task is unrelated to large or context-heavy text writes.
  related_skills:
  - repo-worker-base
  - connector-safety
license: MIT
---

# Context Safety

Use this skill when a text write may be large enough to make a normal editor write path brittle, or when inline composition would risk exhausting the remaining session context.
Use when a document may exceed the safe threshold or when the main session should not carry the whole composition inline.
target 2,000 lines per chunk. absolute red limit max 4,000 lines per chunk.

## Core rule

Estimate the write before you write it.

If the payload is small, a normal temp-file write is fine.
If the payload is large, switch to a chunked temp-file write path before any bytes are written.
Validate the temp file after the write completes, then atomically replace the target.

Do not write the whole payload to the temp file first and decide later.

## Tool-call boundaries

Treat each tool call as a checkpoint. Before a large read, write, or composition step, preserve the durable state that the next tool call will need.

If the next step would require a lot of inline context, stop at the boundary and move the work into a fresh bounded write, a clean-context subagent, or a sectioned append path.

## Compaction boundaries

Use `/compact` only at deliberate phase boundaries after the durable state for the current phase has been preserved.

Do not treat `/compact` as a universal rescue button. If compaction is needed in the middle of active composition, checkpoint the inputs and end the current phase first.

## Pre-composition context pressure

Before composing a large document, decide whether the composition itself will exceed the safe threshold.

Treat a write as context-risky when either of these is true:

- the output is likely to exceed about 2,000 lines;
- the output is likely to exceed about 1 MB of UTF-8 text.

When context-risky:

1. Do not compose the whole document as one inline string in the main session.
2. Prefer a clean-context worker/subagent write with only the required inputs.
3. Or generate the document in bounded sections with sequential append calls, keeping each section near the 2,000-line target and well below the 4,000-line ceiling.
4. Still apply the existing chunked/temp-file write mechanics inside the chosen path.

If the output is expected to land around 1,500 lines or more, split it into smaller chunks before starting so the chunks stay under the target and comfortably below the limit.

## Large-write threshold

Treat a write as large when either of these is true:

- more than 2,000 lines;
- more than 1 MB of UTF-8 text.

If a chunk would exceed 4,000 lines, split it before writing.

If a write is expected to land around 1,500 lines or more, split it into smaller chunks before starting so the chunks come in under the 2,000-line target and stay well under the hard limit.

## Safe sequence

1. Estimate line count and byte size from the content in memory.
2. Choose the write path before opening the temp file.
3. For small payloads, write the whole content to a temp file in one shot.
4. For larger payloads, write the temp file in chunks or append loops.
5. Re-open and validate the completed temp file.
6. Atomically replace the target only after validation passes.

## Python pattern

```python
from pathlib import Path

TARGET_LINES = 2000
HARD_LIMIT = 4000
LARGE_BYTES = 1_000_000


def iter_line_chunks(lines: list[str], chunk_lines: int = TARGET_LINES):
    for start in range(0, len(lines), chunk_lines):
        yield lines[start:start + chunk_lines]


def write_large_text(target: Path, text: str, scratch_root: Path) -> None:

    text = text.replace("\r\n", "\n").replace("\r", "\n")
    lines = text.splitlines()
    byte_size = len(text.encode("utf-8"))
    ends_with_newline = text.endswith("\n")
    chunk_lines = 1500 if len(lines) >= 3000 else TARGET_LINES if len(lines) > TARGET_LINES else len(lines)
    is_large = len(lines) > TARGET_LINES or byte_size > LARGE_BYTES

    tmp = scratch_root / f"{target.name}.tmp"

    if is_large:
        with tmp.open("w", encoding="utf-8", newline="\n") as handle:
            for chunk_index, chunk in enumerate(iter_line_chunks(lines, chunk_lines=chunk_lines)):
                if len(chunk) > HARD_LIMIT:
                    raise RuntimeError("chunk exceeds the absolute hard limit")
                handle.write("\n".join(chunk))
                is_last_chunk = chunk_index == ((len(lines) - 1) // chunk_lines)
                if not is_last_chunk or ends_with_newline:
                    handle.write("\n")
    else:
        with tmp.open("w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)

    with tmp.open("r", encoding="utf-8", newline="\n") as handle:
        completed = handle.read()
    if completed != text:
        raise RuntimeError("temp file validation failed")
    if len(completed.splitlines()) != len(lines):
        raise RuntimeError("line count validation failed")
    if tmp.stat().st_size != byte_size:
        raise RuntimeError("byte size validation failed")

    tmp.replace(target)
```

## Windows notes

- Keep temp files on the same volume as the target so `Path.replace()` stays atomic. `sdd-workspace` produces an off-repo scratch that is a sibling of the main checkout, which is normally on the same volume.
- Prefer explicit `encoding="utf-8"` and `newline="\n"` for text generation.
- If a tool or editor has trouble with a very large file, route through a script instead of the interactive editor.
- If the repo has a safer existing helper for batch writes, use that helper instead of inventing a second path.

## Decision test

If you would be tempted to say "write first, check size later", stop and branch to the large-write path before any write starts.

If you would be tempted to compose a large document inline in the main session context, stop and route to a clean-context worker/subagent or section-by-section append path before composition starts.

## Scratch folder for large temporary outputs

For large temporary outputs that don't need to be committed, use the centralized off-repo scratch provided by `subagent-workspace/scripts/sdd-workspace` (or `sdd-workspace.ps1` on Windows). It resolves `<main-checkout>/../_agent-scratch/<branch>/<plan-basename>/`, which is always outside the repo tree and on the same volume as the working tree.

### When to use scratch folder vs. bounded composition

Use the scratch folder when:

- The output is temporary and will be discarded after the session
- The output is large intermediate data (logs, temporary analysis results, intermediate artifacts)
- The output doesn't need to be committed to the repo
- The output is disposable workspace material

Use bounded composition when:

- The output needs to be committed to the repo
- The output is durable state that should persist
- The output is part of the final deliverable
- The output needs to be under version control

### Scratch folder properties

- **Disposable**: Not persistent beyond the agent's session
- **Outside repo**: Prevents accidental commits
- **Per-branch**: Matches worktree/branch name for isolation
- **Same volume as repo**: Staging files there keeps `Path.replace()` atomic
- **Auto-cleanup**: Agents must clean up scratch folder when cleaning up worktree
- **Not for durable work**: Use the repo for persistent changes

### Usage pattern

1. Resolve the scratch folder: run `subagent-workspace/scripts/sdd-workspace` with no plan file and capture the printed path.
2. Write large temporary outputs (e.g. the `.tmp` staging file in `write_large_text`) to that scratch folder.
3. Use the temporary outputs as needed during the session.
4. Clean up the scratch folder when work is complete (when cleaning up worktree).

### Cleanup guidance

Agents must clean up their scratch folder when cleaning up their worktree. This ensures the scratch space remains clean and does not accumulate orphaned temporary files across sessions.
