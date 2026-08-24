# node-fast-fix

## Purpose
Fix deterministic preflight findings and return to the `preflight` node.

## Inputs
- `<scan_findings>`
- Branch working tree
- Consumer preflight command

## Recipe
1. Read the deterministic findings from `<scan_findings>`.
2. Choose the cheapest fix for the top finding.
3. If the top finding has an existing test, run that test and confirm it fails (RED). If there is no test, write the minimal test that reproduces the finding.
4. Apply the minimal fix and re-run the test until it passes (GREEN).
5. Return to `preflight` to re-run the consumer's canonical preflight.

## Outputs
- Edited working tree and/or new commit
- Updated `<scan_findings>`

## Next check
py -3 .agents/skills/iterative-review/scripts/next_node.py --metrics <scratch_dir>/review-metrics.json
