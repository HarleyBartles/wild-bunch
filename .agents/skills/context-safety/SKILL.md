---
name: context-safety
description: Use when large or context-heavy text writes need bounded composition,
  deliberate compaction boundaries, safe staging, and atomic replacement. Use when
  a write may exceed the safe threshold or when inline composition risks exhausting
  context.
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

- the output is likely to exceed about 300 lines;
- the session has already accumulated significant subagent output, research, or file reads in context.

When context-risky:

1. Do not compose the whole document as one inline string in the main session.
2. Prefer a clean-context worker/subagent write with only the required inputs.
3. Or generate the document in bounded sections with sequential append calls, keeping each section below the existing large-write threshold.
4. Still apply the existing chunked/temp-file write mechanics inside the chosen path.

## Large-write threshold

Treat a write as large when either of these is true:

- more than 300 lines;
- more than 256 KB of UTF-8 text.

The exact threshold can be adjusted for a repository, but the decision must happen before the write starts.

## Safe sequence

1. Estimate line count and byte size from the content in memory.
2. Choose the write path before opening the temp file.
3. For small payloads, write the whole content to a temp file in one shot.
4. For large payloads, write the temp file in chunks or append loops.
5. Re-open and validate the completed temp file.
6. Atomically replace the target only after validation passes.

## Python pattern

```python
from pathlib import Path


def iter_text_chunks(text: str, chunk_size: int = 8192):
    for start in range(0, len(text), chunk_size):
        yield text[start:start + chunk_size]


def write_large_text(target: Path, text: str) -> None:
    lines = text.splitlines()
    byte_size = len(text.encode("utf-8"))
    is_large = len(lines) > 300 or byte_size > 256_000

    tmp = target.with_suffix(target.suffix + ".tmp")

    if is_large:
        with tmp.open("w", encoding="utf-8", newline="\n") as handle:
            for chunk in iter_text_chunks(text):
                handle.write(chunk)
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
