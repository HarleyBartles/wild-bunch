# node-closeout

## Purpose
Archive completed planning artifacts before flipping the PR to ready.

## Inputs
- PR body, linked issues
- `.agents/plans/<plan-name>.md`
- `.agents/specs/<spec-name>.md`
- Any related roadmaps or research files
- Branch working tree

## Recipe
1. Identify the plan and spec named in the PR body, linked issues, or `.agents/plans/` and `.agents/specs/`.
2. Confirm the plan is complete: every top-level checkbox is checked or the plan records the implementation PR.
3. `git mv .agents/plans/<plan-name>.md .agents/plans/completed/`
4. If the plan lists a spec: `git mv .agents/specs/<spec-name>.md .agents/specs/completed/`
5. Move any related roadmaps or research files referenced by the plan.
6. Run `py -3 tools/heal_archive_links.py --apply` and `py -3 tools/check_archive_links.py`.
7. Run `py -3 tools/run.py mesh --apply` and `py -3 tools/run.py marketplace --apply`.
8. Run `py -3 tools/run.py ci --check`; do not proceed if it fails.
9. Commit the archive with `git commit -m "archive: complete <plan-name>"`.

## Outputs
- Moved archived plan/spec/roadmap files
- Commit recording the archive

## Next check
py -3 .agents/skills/iterative-review/scripts/next_node.py --metrics <scratch_dir>/review-metrics.json
