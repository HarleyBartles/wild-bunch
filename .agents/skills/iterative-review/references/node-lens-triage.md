# node-lens-triage

## Purpose
Normalize lens reports and classify every finding into a severity-based routing bucket. `reviewer-fast` findings are triaged the same as deep-lens findings, but fixing them does not re-run `reviewer-fast`; the downstream fix loop verifies through the consumer preflight (`re-preflight`) and the affected lens's checklist (`reviewer-fixes`).

## Inputs
- Off-repo `<scratch_dir>` containing `review-log-<lens>.md` files
- `## Checklist` severity language from each lens profile

## Recipe
1. Run `py -3 .agents/skills/iterative-review/scripts/normalize_review_inputs.py --apply <scratch_dir>` to ensure all lens reports are plain UTF-8.
2. Classify every finding from the lens reports. For each finding, call:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/record_finding.py \
       --state <scratch_dir>/review-state.json \
       --data '{"finding_id": "<finding_id>", "lens": "<lens>", "discovered_at_node": "lens-triage", "discovered_at_round": <round>, "severity": "<severity>", "contested": <true|false>}'
   ```
   Then regenerate the metrics file:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   ```
3. Classify each finding. If a finding is determined to be a false positive or otherwise requires no fix, record that resolution at `lens-triage`:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/record_resolution.py \
       --state <scratch_dir>/review-state.json \
       --data '{"finding_id": "<finding_id>", "resolved_at_node": "lens-triage", "resolved_at_round": <round>}'
   ```
   A `lens-triage` resolution marks the finding as resolved without entering the fix loop.
4. Route:
   - Any `contested`/`load-bearing` finding -> `blocked`
   - Any unresolved `blocking/important` finding -> `metrics-track` then `finding-fix`
   - Only `trivial/deferred` findings remaining, or all `blocking/important` findings resolved at triage -> `final-strong` (the `resolved-ledger` node is skipped because no fixes were applied)

## Outputs
- Routing decision
- `<scratch_dir>/review-metrics.json` regenerated from `<scratch_dir>/review-state.json` and the recorded finding logs

## Next check
```bash
py -3 .agents/skills/iterative-review/scripts/next_node.py \
    --state <scratch_dir>/review-state.json \
    --propose <next-node>
```
