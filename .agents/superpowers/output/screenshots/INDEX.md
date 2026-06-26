# screenshots

Agent-generated browser screenshot evidence for UI/playtest verification.

## What belongs here

- Browser screenshots captured during playtest, UI verification, or dev overlay visual proof.
- Generated image artifacts produced by agents during validation.

## What does NOT belong here

- Tracked documentation images or diagrams (those belong in `docs/` if ever needed).
- Any file that should be version-controlled.

## Git policy

Generated screenshot/image artifacts are git-ignored via the local `.gitignore`.

The folder itself is represented by tracked navigation files (`INDEX.md`, `.gitignore`).
Actual image files are never committed to the repo.

PR/return notes may cite local evidence filenames/paths or attach screenshots through
the review system if needed, but must not add them as repo files.

If a worker finds screenshots or generated evidence committed elsewhere in the repo,
they should remove/move them here as part of self-healing.

Back to [output/](../INDEX.md)
