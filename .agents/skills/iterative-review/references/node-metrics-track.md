# node-metrics-track

## Purpose
Ensure `review-metrics.json` is generated from the recorded logs and current state before routing to the next fix.

## Inputs
- `<scratch_dir>/review-state.json`
- Recorded finding, resolution, regression, and blocker logs

## Recipe
1. Confirm the upstream node has already recorded any new finding, resolution, regression, or blocker events with the appropriate `record_*.py` scripts.
2. Regenerate the metrics file:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   ```
3. Authorize the next node:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose finding-fix
   ```

## Outputs
- `<scratch_dir>/review-metrics.json` regenerated from `<scratch_dir>/review-state.json` and the recorded logs

## Next check
```bash
py -3 .agents/skills/iterative-review/scripts/next_node.py \
    --state <scratch_dir>/review-state.json \
    --propose finding-fix
```
