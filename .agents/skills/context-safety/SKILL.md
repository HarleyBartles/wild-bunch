---
name: context-safety
description: Use when large or context-heavy text writes need bounded composition,
  200-line chunking, deliberate compaction boundaries, and atomic replacement. Use
  when a write may exceed the safe threshold or when inline composition risks
  exhausting context.
metadata:
  source-id: context-safety
  source-path: sources/first_party/skills/context-safety/SKILL.md
  provenance-name: Context Safety first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: large text write safety, bounded composition, compaction boundaries, and atomic replacement
  use_when:
  - Use when composing or editing large text files
  - Use when inline composition would risk consuming the remaining context
  - Use when tool-call boundaries are the right checkpoint for preserving durable state
  - Use when `/compact` should happen only after durable state has been preserved
  - Use when safe staging and atomic replacement are required
  do_not_use_when:
  - Do not use when the change is small and can be written directly
  - Do not use when the task is unrelated to large or context-heavy text writes
  related_skills:
  - repo-worker-base
  - connector-safety
license: MIT
---

# Context Safety

Use this skill when a text write may be large enough to make a normal editor write path brittle, or when inline composition would risk exhausting the remaining session context.
Use when a document may exceed the safe threshold or when the main session should not carry the whole composition inline.
target 200 lines per chunk. absolute red limit max 400 lines per chunk.

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

Before composing a large document, decide whether the composition itself will exceed the session's remaining context budget.

Treat a write as context-risky when either of these is true:

- the output is likely to exceed about 200 lines;
- the session has already accumulated significant subagent output, research, or file reads in context.

When context-risky:

1. Do not compose the whole document as one inline string in the main session.
2. Prefer a clean-context worker/subagent write with only the required inputs.
3. Or generate the document in bounded sections with sequential append calls, keeping each section near the 200-line target and well below the 400-line ceiling.
4. Still apply the existing chunked/temp-file write mechanics inside the chosen path.

If the output is expected to hit the 300 line cutoff or more, split it into smaller chunks before starting so the chunks stay under the target and comfortably below the limit.

## Large-write threshold

Treat a write as large when either of these is true:

- more than 200 lines;
- more than 256 KB of UTF-8 text.

If a chunk would exceed 400 lines, split it before writing.

If a write is expected to land around 300 lines or more, split it into smaller chunks before starting so the chunks come in under the 200-line target and stay well under the hard limit.

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


def iter_line_chunks(lines: list[str], chunk_lines: int = 200):
    for start in range(0, len(lines), chunk_lines):
        yield lines[start:start + chunk_lines]


def write_large_text(target: Path, text: str) -> None:
    lines = text.splitlines()
    byte_size = len(text.encode("utf-8"))
    ends_with_newline = text.endswith("\n")
    chunk_lines = 150 if len(lines) >= 300 else 200 if len(lines) > 200 else len(lines)
    is_large = len(lines) > 200 or byte_size > 256_000

    tmp = target.with_suffix(target.suffix + ".tmp")

    if is_large:
        with tmp.open("w", encoding="utf-8", newline="\n") as handle:
            for chunk_index, chunk in enumerate(iter_line_chunks(lines, chunk_lines=chunk_lines)):
                if len(chunk) > 400:
                    raise RuntimeError("chunk exceeds the absolute 400-line limit")
                handle.write("\n".join(chunk))
                is_last_chunk = chunk_index == ((len(lines) - 1) // chunk_lines)
                if not is_last_chunk or ends_with_newline:
                    handle.write("\n")
    else:
        tmp.write_text(text, encoding="utf-8", newline="\n")

    completed = tmp.read_text(encoding="utf-8")
    if completed != text:
        raise RuntimeError("temp file validation failed")
    if len(completed.splitlines()) != len(lines):
        raise RuntimeError("line count validation failed")
    if tmp.stat().st_size != byte_size:
        raise RuntimeError("byte size validation failed")

    tmp.replace(target)
```

## Windows notes

- Keep temp files on the same volume as the target so `Path.replace()` stays atomic.
- Prefer explicit `encoding="utf-8"` and `newline="\n"` for text generation.
- If a tool or editor has trouble with a very large file, route through a script instead of the interactive editor.
- If the repo has a safer existing helper for batch writes, use that helper instead of inventing a second path.

## Decision test

If you would be tempted to say "write first, check size later", stop and branch to the large-write path before any write starts.

If you would be tempted to compose a large document inline in the main session context, stop and route to a clean-context worker/subagent or section-by-section append path before composition starts.

## Scratch folder for large temporary outputs

For large temporary outputs that don't need to be committed, consider using the centralized scratch folder instead of bounded composition.

### When to use scratch folder vs. bounded composition

Use the scratch folder (`../_agent-scratch/<repo-name>/<branch-name>`) when:

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
- **Auto-cleanup**: Agents must clean up scratch folder when cleaning up worktree
- **Not for durable work**: Use the repo for persistent changes

### Scratch folder structure

- **Scratch root**: `../_agent-scratch/`
- **Per-repo**: `../_agent-scratch/<repo-name>/`
- **Per-branch**: `../_agent-scratch/<repo-name>/<branch-name>/`

### Usage pattern

1. Create scratch folder when needed: `../_agent-scratch/<repo-name>/<branch-name>/`
2. Write large temporary outputs to scratch folder
3. Use the temporary outputs as needed during the session
4. Clean up scratch folder when work is complete (when cleaning up worktree)

### Cleanup guidance

Agents must clean up their scratch folder when cleaning up their worktree. This ensures the scratch space remains clean and does not accumulate orphaned temporary files across sessions.
