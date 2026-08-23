# node-lens-dispatch

## Purpose
Dispatch the matching deep lens reviewers.

## Inputs
- All `reviewer-*.md` files in the Devin Desktop agents search path
- Full branch `<diff_path>`
- `<pr_description>`
- `<scan_findings>`
- `review-log-reviewer-fast.md` (the pre-lens report from `reviewer-fast`)
- Lens-specific inputs (`<plan_path>`, `<spec_path>`, `<roadmap_path>`)
- Off-repo `<scratch_dir>`

## Recipe

1. Run `select_lenses.py` to discover matching deep lenses. `reviewer-fast` is no longer selected here; it is dispatched by the `reviewer-fast` node.
   ```
   py -3 .agents/skills/iterative-review/scripts/select_lenses.py --state <scratch_dir>/review-state.json --apply
   ```
2. Run `diff_slicer.py` to generate a scoped diff for each selected deep lens:
   ```
   py -3 .agents/skills/iterative-review/scripts/diff_slicer.py --state <scratch_dir>/review-state.json --apply
   ```
3. Read `<scratch_dir>/lenses.jsonl`; each line now includes a `diff_path`. Build the common input package: `<pr_description>`, `<scan_findings>`, and `review-log-reviewer-fast.md` from the pre-lens pass. Use the lens's `diff_path` for the scoped diff. If the lens's `## Inputs` section calls for `<plan_path>`, `<spec_path>`, or `<roadmap_path>`, add the requested file to that lens's package.
4. `run_subagent` each deep lens from `lenses.jsonl` with its `profile_path`, `output_path`, and the lens-specific input package.
5. Wait for all `run_subagent` calls to complete. From each `review-log-<lens>.md`, extract the terminal (last) line.
6. If no deep lens matches, continue to `lens-triage` with only the `reviewer-fast` log.
7. If `run_subagent` is unavailable, route to `blocked`.

`lens-dispatch` is a one-time dispatch. After this node, the graph routes to `normalize-inputs` and then `lens-triage`. Downstream fix handling (`metrics-track` -> `finding-fix` -> `re-preflight` -> `reviewer-fixes`) re-runs only the lens associated with the finding being fixed; do not re-dispatch all lenses.

## Outputs
- Write `review-log-<lens>.md` for each dispatched lens

## Next check
py -3 .agents/skills/iterative-review/scripts/next_node.py --state <scratch_dir>/review-state.json
