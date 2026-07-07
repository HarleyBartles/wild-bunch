# Task 5 Report: Final Promotion, Mesh Refresh, and Verification

## What I implemented

- Ran the final building promotion pass exactly against `src/WildBunch.Assets/staging/town-hub-buildings` into `src/WildBunch.Assets/sprites/town-hub-buildings`.
- Regenerated the full repo index mesh and validated the generated mesh.
- Verified the final asset-tree contract from live repo state:
  - `src/WildBunch.Assets/source/town-hub-buildings/`, `src/WildBunch.Assets/staging/town-hub-buildings/`, and `src/WildBunch.Assets/sprites/town-hub-buildings/` remain the building-family homes.
  - `src/WildBunch.Assets/sprites/town-hub-roads/` and `src/WildBunch.Assets/sprites/town-hub-ground/` remained in place without running sprite cutting over them.
  - The legacy `src/WildBunch.Assets/town-buildings/` custody tree is absent.
- Confirmed there were no remaining repo edits required from the promotion or mesh refresh pass.

## Tested

1. `python scripts/image_asset_pipeline.py promote-sprites --input-root src/WildBunch.Assets/staging/town-hub-buildings --out-root src/WildBunch.Assets/sprites/town-hub-buildings`
   - Result: success
   - Evidence: `Promoted 120 PNG files from src\WildBunch.Assets\staging\town-hub-buildings to src\WildBunch.Assets\sprites\town-hub-buildings`
2. `python scripts/generate_index_mesh.py`
   - Result: success
   - Evidence: `Wrote index mesh: 235 files`
3. `python scripts/generate_index_mesh.py --check`
   - Result: success
   - Evidence: `OK index mesh: 235 indexes current`
4. `git status --short`
   - Result before report write: clean working tree after promotion + mesh pass
5. `Test-Path src/WildBunch.Assets\town-buildings`
   - Result: `False`
6. `py -3` validation pass over `src/WildBunch.Assets/{source,staging,sprites}/town-hub-roads` and `src/WildBunch.Assets/{source,staging,sprites}/town-hub-ground`
   - Result: success
   - Evidence:
     - every road and ground PNG is exactly `60x50` in source, staging, and sprites
     - road mirror pairs match as expected:
       - `main-road/flat-edge-left.png` <-> `main-road/flat-edge-right.png`
       - `main-road/path-edge-left.png` <-> `main-road/path-edge-right.png`
       - `main-road/spur-cross-left.png` <-> `main-road/spur-cross-right.png`
       - `main-road/end-top.png` <-> `main-road/end-bottom.png`
       - `spur-road/end-left.png` <-> `spur-road/end-right.png`
     - road and ground bytes are identical across source, staging, and sprites, so the tiles were copy-promoted rather than sprite-cut
7. Manual visual read pass of representative filler-building, road, and dirt outputs against the town-building style contract
   - Result: pass
   - Evidence: the final assets read consistently with the existing western town families in the live repo views

## TDD evidence

- Not applicable. Task 5 was a promotion, mesh-refresh, and verification pass with no feature or bugfix implementation.

## Changed files

- `.agents/superpowers/sdd/2026-07-06-bunch-142-town-hub-filler-assets-implementation/task-5-report.md`

## Self-review findings

- No findings on the narrow Task 5 scope.
- The final promotion command completed successfully after installing `Pillow` into the active `python` 3.12 environment used by the repo-local script call.
- The mesh generator both wrote and validated successfully, and it did not leave tracked file diffs behind.
- Road and ground validation now has explicit 60x50, mirror-pair, and byte-for-byte copy-promotion evidence in addition to the mesh evidence.
- No additional doc-link edits were necessary in this pass because the repo already had no remaining references to the old asset custody root under `src/WildBunch.Assets/town-buildings/`.

## Concerns

- The active `python` interpreter in this worktree initially lacked `Pillow`, so the first promotion attempt failed until that local interpreter dependency was installed. This did not change repo files, but it is an environment dependency worth noting for future reruns.
