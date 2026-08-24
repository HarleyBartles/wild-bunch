# node-ready

## Purpose
Flip the PR from draft to ready after the review loop is complete.

## Inputs
- Branch working tree
- `<pr_number>`
- `review-metrics.json`

## Recipe
1. Run `py -3 tools/run.py ci --check` (or the consumer's equivalent); do not proceed if it fails.
2. Flip the PR from draft to ready with `gh pr ready <pr_number>`.
3. Wait for remote CI to pass using `gh pr checks <pr_number> --watch` or the equivalent consumer command.
4. Do not merge until the PR is green.
5. Regenerate the final metrics file and commit the `ready` state:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose ready
   ```

## Outputs
- `<scratch_dir>/review-metrics.json` regenerated from `<scratch_dir>/review-state.json` and the recorded logs
- PR flipped to ready

## Next check
```bash
py -3 .agents/skills/iterative-review/scripts/next_node.py \
    --state <scratch_dir>/review-state.json \
    --propose <next-node>
```
