# node-preflight

## Purpose
Run the consumer's canonical preflight on the branch and gate on a clean result.

## Inputs
- Branch working tree
- Consumer's canonical preflight command from `AGENTS.md` or `.devin/rules`
- `<scan_findings>` file path

## Recipe
1. Run the consumer's canonical preflight on the branch; for this repo use `py -3 tools/run.py ci --check`.
2. For each deterministic finding, record it:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/record_finding.py \
       --state <scratch_dir>/review-state.json \
       --data '{"finding_id": "<finding_id>", "lens": "preflight", "discovered_at_node": "preflight", "discovered_at_round": <round>, "severity": "<severity>"}'
   ```
   If the preflight is clean, no new finding is recorded.
3. Regenerate the metrics file and authorize the next node:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose <next-node>
   ```
4. Do not proceed until the preflight is clean or its findings are converted to a `fast-fix` and re-checked.

## Outputs
- Updated `<scan_findings>` file
- `<scratch_dir>/review-metrics.json` regenerated from `<scratch_dir>/review-state.json` and the recorded logs

## Next check
```bash
py -3 .agents/skills/iterative-review/scripts/next_node.py \
    --state <scratch_dir>/review-state.json \
    --propose <next-node>
```
