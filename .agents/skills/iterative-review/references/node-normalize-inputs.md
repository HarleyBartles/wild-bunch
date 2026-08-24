# node-normalize-inputs

## Purpose
Run `normalize_review_inputs.py --apply` on the scratch directory so every
downstream file is plain UTF-8.

## Inputs
- Off-repo `<scratch_dir>`

## Recipe
1. Collect the raw review inputs in `<scratch_dir>`.
2. Run:
   ```
   py -3 .agents/skills/iterative-review/scripts/normalize_review_inputs.py --apply <scratch_dir>
   ```
3. Verify every generated file is plain UTF-8 with no BOM.

## Outputs
- UTF-8 normalized review inputs in `<scratch_dir>`

## Next check
py -3 .agents/skills/iterative-review/scripts/next_node.py --metrics <scratch_dir>/review-metrics.json
