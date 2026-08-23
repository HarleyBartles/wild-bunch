# node-blocked

## Purpose
Record an unresolvable blocker and hand the review to a human.

## Inputs
- Contested or load-bearing finding
- `review-metrics.json`

## Recipe
1. Record the blocker, then regenerate the metrics file and commit the `blocked` state:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/record_blocker.py \
       --state <scratch_dir>/review-state.json \
       --data '{"finding_id": "<finding_id>", "blocker_class": "<contested|tool-blocked>"}'
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose blocked
   ```
   Then hand to a human.
2. If the human says "carry on", resume from `metrics-track`.
3. If `next_node.py` or `resolved_ledger.py` returns a `BLOCKED` result, treat it as a graph error: do not override it, do not dispatch `final-strong` out of order, and resume from the allowed node.

## Outputs
- `<scratch_dir>/review-metrics.json` regenerated from `<scratch_dir>/review-state.json` and the recorded logs

## Next check
```bash
py -3 .agents/skills/iterative-review/scripts/next_node.py \
    --state <scratch_dir>/review-state.json \
    --propose <next-node>
```
