# Cleanup Custody Profile

Portable anti-slop profile for repository/workspace cleanup decisions.

## Purpose

Catch repository/workspace cleanup slop. Stop agents from laundering clutter as provenance, laundering deletion as cleanup, or recreating source control history inside the live tree.

## Where to Use

Before cleanup, deletion, archival, deprecation, generated-output pruning, source-custody repair, tombstone handling, old-file removal, or repo hygiene work.

## Slop Patterns to Avoid

- Keeping obsolete files in the live tree `for provenance` when provenance belongs in Git history, source maps, attribution files, release artifacts, or cold store
- Tombstone theatre: leaving empty wrappers or deprecated files and calling the surface cleaned
- Replicating version control in `main` through `old`, `backup`, `archive`, `deprecated`, `final`, or `do-not-use` copies
- Deletion laundering: deleting/moving files without checking indexes, manifests, references, generated projections, docs, package registries, or sentinels
- Anti-bloat as a deletion-first hammer that risks protected evidence, publication proof, receipts, active doctrine, or source-law material
- Fake custody surfaces that have no owner, retention rule, removal condition, or discoverability purpose

## Required Avoid Rules

Prohibit `for provenance`, `tombstone`, `for context`, `backup`, `old`, `deprecated`, `archive`, `final`, `cleanup complete`, and `regenerated later` when those words hide an actual custody decision.

Catch cleanup reports that do not classify each surface as live source, cold store, governed trash, delete-now residue, or block-and-route protected material.

## Required Prefer-Instead Rules

Use a custody ladder:
- **keep live**: active source, current doctrine, required index, publication proof, protected evidence, or authoritative operating material
- **move to cold store**: inactive but lawfully retained material with retrieval value
- **move to governed trash**: reversible deletion staging with a repo-visible index/sentinel where the repo uses that pattern
- **delete now**: exact disposable residue with no provenance, authority, or future retrieval value
- **block and route**: protected, ambiguous, actor-owned, source-law-sensitive, or authority-sensitive material

Before claiming cleanup, check falsification surfaces: links, indexes, manifests, generated outputs, package registries, docs, sentinels, and tests/validators.

## False Positives / Do Not Overapply

Do not turn anti-bloat into deletion-first behavior. Protected evidence, source provenance, publication proof, and active doctrine may need to remain live. The profile should force classification, not automatic removal.

## Examples

### Before (Avoid)
> Moved old files into deprecated/ so the history remains visible.

### After (Prefer)
- `old_implementation.py`: deleted - obsolete implementation, Git history preserves the code
- `deprecated/` directory: deleted - tombstone theatre, use Git history for reference
- `README_OLD.md`: deleted - superseded by current README, Git history preserves the content

### Before (Avoid)
> Kept the obsolete wrapper as a tombstone for provenance.

### After (Prefer)
- `legacy_wrapper.py`: deleted - no active references, Git history preserves the code for provenance
- No tombstone needed - Git commit history serves as provenance

### Before (Avoid)
> Deleted the duplicate files, cleanup complete.

### After (Prefer)
- Checked falsification surfaces: plugin registry (no references), manifest (no entries), docs (no links)
- `duplicate_file.py`: deleted - exact duplicate with no unique provenance
- Cleanup complete: no stale references remain

## Acceptance Checks

This profile is acceptable only if it would stop an agent from calling cleanup done while stale references remain or while obsolete material has merely been renamed into a live-tree hiding place.
