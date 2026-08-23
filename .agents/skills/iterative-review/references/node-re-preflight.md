# node-re-preflight

## Purpose
Re-run the consumer's canonical preflight after a fix to catch newly introduced deterministic issues. This is the consumer's `ci`/`preflight` command (e.g. `py -3 tools/run.py ci --check`), not the `reviewer-fast` pre-lens. Do not re-run `reviewer-fast` in this node.

## Inputs
- Post-fix branch working tree
- `<scan_findings>`
- Consumer preflight command

## Recipe
1. Re-run the consumer's canonical preflight over the post-fix range.
2. For each new deterministic finding, record it:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/record_finding.py \
       --state <scratch_dir>/review-state.json \
       --data '{"finding_id": "<finding_id>", "lens": "preflight", "discovered_at_node": "re-preflight", "discovered_at_round": <round>, "severity": "<severity>"}'
   ```
   If the re-preflight is clean, no new finding is recorded.
3. Regenerate the metrics file and authorize the next node:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose <next-node>
   ```
4. If it reports new deterministic issues, go to `fast-fix`.
5. If it is clean, go to `reviewer-fixes`.

## Outputs
- Updated `<scan_findings>`
- `<scratch_dir>/review-metrics.json` regenerated from `<scratch_dir>/review-state.json` and the recorded logs

## Next check
```bash
py -3 .agents/skills/iterative-review/scripts/next_node.py \
    --state <scratch_dir>/review-state.json \
    --propose <next-node>
```
