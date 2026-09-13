# node-next-node

## Purpose
Validate the proposed graph node against the current state before dispatching a subagent.

## Inputs
- `<scratch_dir>/review-state.json`
- `<scratch_dir>/review-metrics.json` (read-only discovery fallback)
- Proposed `<node>` name

## Recipe
1. Call `next_node.py` without `--propose` at the start of each turn to discover the single allowed next node. Discovery is read-only and does not modify `review-state.json` or `review-metrics.json`.
2. Validate the discovered `<node>` and advance the router before running that node's recipe:
   ```
   py -3 .agents/skills/iterative-review/scripts/next_node.py --state <scratch_dir>/review-state.json --propose <node>
   ```
3. If exit 0, the node is authorized; `next_node.py` advances `current_node` and `previous_node` to the dispatched node.
4. If exit 1, do not run the node recipe; route to the allowed node printed in the output.
5. `--propose ready` is always refused: version-1 review state cannot produce a trustworthy-green seal. `--metrics` cannot be combined with `--propose`; metrics-mode calls are read-only discovery only.

## Outputs
- Console routing decision
- The discovery call (no `--propose`) is read-only
- The commit call (`--propose`) advances `current_node` and `previous_node` in `review-state.json`

## Next check
py -3 .agents/skills/iterative-review/scripts/next_node.py --metrics <scratch_dir>/review-metrics.json
