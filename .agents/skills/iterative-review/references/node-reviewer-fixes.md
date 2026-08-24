# node-reviewer-fixes

## Purpose
Verify a fix against the originating lens's checklist, tightly scoped to the blast radius.

## Inputs
- `findings.jsonl` - to know the `lens` and finding being fixed
- `review-log-<lens>.md` - to extract the originating lens's `## Checklist` section
- `review-log-implementer-report.md` (if an `implementer` fixed the finding) or the inline fix diff
- The affected file(s) only - do not re-review the whole branch

## Recipe

1. From `findings.jsonl` and `resolutions.jsonl`, determine the finding being re-reviewed and its originating `lens`.
2. Read the originating `review-log-<lens>.md` and extract the `## Checklist` section into a temporary `<scratch_dir>/lens-<lens>-checklist.txt` file.
3. Prepare a fix diff scoped to the blast radius of the fix. Do not include the whole branch.
4. Dispatch the `reviewer-fixes` subagent (do not re-dispatch the original lens directly). Provide:
   - `<diff_path>` - the scoped fix diff
   - `<log_path>` - `<scratch_dir>/review-log-reviewer-fixes.md`
   - `<pr_description>` - the PR title/body
   - `<original_finding>` - the finding text or reference
   - `<fix_diff_path>` - the same scoped fix diff
   - `<full_diff_slice_path>` - the relevant slice of the full branch diff
   - `<lens>` - the originating lens, e.g. `reviewer-plans`
   - `<lens_checklist>` - the prepared `lens-<lens>-checklist.txt` file
5. Wait for the subagent. Its final response must be exactly one line:
   - `reviewer-fixes: clean`
   - `reviewer-fixes: N issue(s)`
6. Read `review-log-reviewer-fixes.md`.
7. On `reviewer-fixes: clean`:
   - Do not record the resolution here; `resolved-ledger` is the single authority that records resolutions. Regenerate the metrics file:
     ```bash
     py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
         --state <scratch_dir>/review-state.json \
         --metrics <scratch_dir>/review-metrics.json
     ```
   - If the fix should trigger `regression-scan`, pass `--non-trivial` to `next_node.py --propose` and route to `regression-scan`:
     ```bash
     py -3 .agents/skills/iterative-review/scripts/next_node.py \
         --state <scratch_dir>/review-state.json \
         --propose regression-scan \
         --non-trivial
     ```
   - Otherwise, route to `resolved-ledger`:
     ```bash
     py -3 .agents/skills/iterative-review/scripts/next_node.py \
         --state <scratch_dir>/review-state.json \
         --propose resolved-ledger
     ```
8. On `reviewer-fixes: N issue(s)`:
   - Do **not** increment `fix_round` (`finding-fix` owns that on the next pass).
   - If the report shows the original finding is still unresolved, no new record is needed. Regenerate the metrics file and route back to `finding-fix`:
     ```bash
     py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
         --state <scratch_dir>/review-state.json \
         --metrics <scratch_dir>/review-metrics.json
     py -3 .agents/skills/iterative-review/scripts/next_node.py \
         --state <scratch_dir>/review-state.json \
         --propose finding-fix
     ```
   - If a new same-lens issue was found in the blast radius, record it and its relationship to the original finding:
     ```bash
     py -3 .agents/skills/iterative-review/scripts/record_finding.py \
         --state <scratch_dir>/review-state.json \
         --data '{"finding_id": "<new_finding_id>", "lens": "<lens>", "discovered_at_node": "reviewer-fixes", "discovered_at_round": <round>, "severity": "<severity>"}'
     py -3 .agents/skills/iterative-review/scripts/record_regression.py \
         --state <scratch_dir>/review-state.json \
         --data '{"fix_for": "<original_finding_id>", "new_finding": "<new_finding_id>", "discovered_at_node": "reviewer-fixes", "discovered_at_round": <round>, "regression_class": "same-lens-blast-radius", "severity": "<severity>"}'
     ```
     Then regenerate the metrics file and route to `metrics-track`:
     ```bash
     py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
         --state <scratch_dir>/review-state.json \
         --metrics <scratch_dir>/review-metrics.json
     py -3 .agents/skills/iterative-review/scripts/next_node.py \
         --state <scratch_dir>/review-state.json \
         --propose metrics-track
     ```

## Outputs
- `review-log-reviewer-fixes.md` ending with exactly one of:
  - `reviewer-fixes: clean`
  - `reviewer-fixes: N issue(s)`
- `<scratch_dir>/review-metrics.json` regenerated from `<scratch_dir>/review-state.json` and the recorded logs

## Next check
```bash
py -3 .agents/skills/iterative-review/scripts/next_node.py \
    --state <scratch_dir>/review-state.json \
    --propose <next-node>
```
