# node-resolved-ledger

## Purpose
Mark findings resolved and generate the resolved-ledger evidence gate.

## Inputs
- `review-metrics.json`
- Off-repo `<scratch_dir>`

## Recipe
1. When `reviewer-fixes` or `regression-scan` is clean, `resolved-ledger` is the single authority that records the resolution. For each fixed finding, call `record_resolution.py` once with `resolved_at_node` set to `reviewer-fixes` or `regression-scan` and the current round:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/record_resolution.py \
       --state <scratch_dir>/review-state.json \
       --data '{"finding_id": "<finding_id>", "resolved_at_node": "<reviewer-fixes|regression-scan>", "resolved_at_round": <round>}'
   ```
2. Regenerate the metrics file:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   ```
3. When the queue is empty, run:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/resolved_ledger.py --apply --metrics <scratch_dir>/review-metrics.json
   ```
4. If the command exits 1, do not proceed to `final-strong`; return to `finding-fix` or `regression-scan`.
5. If more findings remain in the queue, choose the next one, then authorize `finding-fix`:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose finding-fix
   ```
6. If the queue is empty and `resolved_ledger.py` succeeded, authorize `final-strong`:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose final-strong
   ```

## Outputs
- Write `review-log-resolved-ledger.md` when every `important`/`blocking` finding has a `resolved_at_node` and `regressions` is empty
- `<scratch_dir>/review-metrics.json` regenerated from `<scratch_dir>/review-state.json` and the recorded logs

## Next check
```bash
py -3 .agents/skills/iterative-review/scripts/next_node.py \
    --state <scratch_dir>/review-state.json \
    --propose <next-node>
```
