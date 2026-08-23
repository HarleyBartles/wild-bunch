# node-regression-scan

## Purpose
Widen review to the touched area for non-trivial or cross-cutting fixes to catch regressions.

## Inputs
- Fix diff
- Full branch diff blast radius
- `reviewer-strong` profile
- `review-metrics.json`

## Recipe
1. Dispatch `reviewer-strong` on the touched area with `<log_path>` set to `$scratch/review-log-strong.md`.
2. If the scan is clean, regenerate the metrics file and go to `resolved-ledger`:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose resolved-ledger
   ```
3. If it finds a new issue, classify it:
   - `same-lens-blast-radius` if in the same lens and blast radius
   - `cross-lens-blast-radius` if in a different lens and blast radius
   - `outside-blast-radius` if outside the blast radius
4. Record the new finding and its regression relationship:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/record_finding.py \
       --state <scratch_dir>/review-state.json \
       --data '{"finding_id": "<new_finding_id>", "lens": "<lens>", "discovered_at_node": "regression-scan", "discovered_at_round": <round>, "severity": "<severity>"}'
   py -3 .agents/skills/iterative-review/scripts/record_regression.py \
       --state <scratch_dir>/review-state.json \
       --data '{"fix_for": "<original_finding_id>", "new_finding": "<new_finding_id>", "discovered_at_node": "regression-scan", "discovered_at_round": <round>, "regression_class": "<regression_class>", "severity": "<severity>"}'
   ```
5. Regenerate the metrics file and return to `metrics-track`:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose metrics-track
   ```

## Outputs
- Write `review-log-strong.md`
- `<scratch_dir>/review-metrics.json` regenerated from `<scratch_dir>/review-state.json` and the recorded logs

## Next check
```bash
py -3 .agents/skills/iterative-review/scripts/next_node.py \
    --state <scratch_dir>/review-state.json \
    --propose <next-node>
```
