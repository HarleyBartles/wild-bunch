# node-final-strong

## Purpose
Run one whole-branch `reviewer-strong` pass after all `blocking/important` findings are resolved.

## Inputs
- Full branch diff
- `<pr_description>`
- All lens logs
- `review-metrics.json`
- `review-log-resolved-ledger.md` (only when `resolved-ledger` was visited)
- `reviewer-strong` profile
- `<log_path>`

## Recipe
1. Validate the dispatch:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose final-strong
   ```
2. Build the input package and `run_subagent` `reviewer-strong` to the `<log_path>`.
3. If `reviewer-strong: clean` and the preflight is clean, authorize `closeout`:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose closeout
   ```
4. If findings are reported, record each new finding, then regenerate the metrics file and go to `metrics-track` to start a new fix loop:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/record_finding.py \
       --state <scratch_dir>/review-state.json \
       --data '{"finding_id": "<finding_id>", "lens": "reviewer-strong", "discovered_at_node": "final-strong", "discovered_at_round": <round>, "severity": "<severity>"}'
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose metrics-track
   ```
5. If a `contested` or `load-bearing` finding is reported, record the finding and the blocker, then regenerate the metrics file and go to `blocked`:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/record_finding.py \
       --state <scratch_dir>/review-state.json \
       --data '{"finding_id": "<finding_id>", "lens": "reviewer-strong", "discovered_at_node": "final-strong", "discovered_at_round": <round>, "severity": "<severity>"}'
   py -3 .agents/skills/iterative-review/scripts/record_blocker.py \
       --state <scratch_dir>/review-state.json \
       --data '{"finding_id": "<finding_id>", "blocker_class": "contested"}'
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose blocked
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
