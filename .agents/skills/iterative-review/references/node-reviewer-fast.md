# node-reviewer-fast

## Purpose
Run the cheap `reviewer-fast` pre-lens before any deep lens is dispatched. Catch mechanical, surface-level issues that the deep reviewers should not waste effort on, and fix them before `lens-dispatch`.

## Inputs
- `reviewer-fast` profile from the Devin Desktop agents search path
- Full branch `<diff_path>`
- `<pr_description>`
- Off-repo `<scratch_dir>`

## Recipe
1. Verify the graph is at `reviewer-fast`:
   ```
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose reviewer-fast
   ```
2. `run_subagent` `reviewer-fast` with `<diff_path>`, `<pr_description>`, and `output_path` `<scratch_dir>/review-log-reviewer-fast.md`.
3. Extract the terminal line from `review-log-reviewer-fast.md`. If it is `reviewer-fast: clean`, authorize `lens-dispatch`:
   ```
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose lens-dispatch
   ```
4. If `reviewer-fast` reported `N issue(s)`, record each finding, then run `compile_metrics.py` and route to `lens-triage`:
   ```
   py -3 .agents/skills/iterative-review/scripts/record_finding.py \
       --state <scratch_dir>/review-state.json \
       --data '{"finding_id": "<id>", "lens": "reviewer-fast", "discovered_at_node": "reviewer-fast", "discovered_at_round": <round>, "severity": "<severity>", "contested": <true|false>}'
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose lens-triage
   ```

## Outputs
- `review-log-reviewer-fast.md` written by the `reviewer-fast` subagent
- `findings.jsonl` updated when `reviewer-fast` reports issues

## Next check
```
py -3 .agents/skills/iterative-review/scripts/next_node.py --state <scratch_dir>/review-state.json
```
