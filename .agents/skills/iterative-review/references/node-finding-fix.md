# node-finding-fix

## Purpose
Verify and fix a single `blocking/important` lens finding.

## Inputs
- `original_finding` with exact text and severity
- `lens` name (e.g., `reviewer-security`)
- `lens_checklist` from the originating `reviewer-*.md`
- `diff_slice` of the full branch diff that the finding touches
- `fix_constraints` (what not to break, tests, consumer `ci --check`)
- `<pre-fix-sha>` and branch working tree

## Recipe
1. Use `receiving-code-review` to verify the finding.
2. Choose the fix path using the decision table below.

| Path | Use when... |
|---|---|
| `implementer` subagent | The change spans more than one file, is non-trivial logic, touches consumer preflights or tests in a non-obvious way, or the orchestrator is not confident making the change directly. |
| Inline/orchestrator | The change is one file / one conceptual edit, is docs/markdown/spec text, or the blast radius is minimal and the orchestrator can safely apply it. |

3. If `implementer` is chosen:
   - Create `<scratch_dir>/review-log-implementer-brief.md` from `review-log-implementer-brief-template.md`.
   - Fill in the `## Finding`, `## Fix instructions`, `## Out of scope`, `## Verification`, and `## Outputs` sections.
   - Dispatch an `implementer` subagent with the brief and the consumer's preflight command.
   - Verify the resulting `review-log-implementer-report.md` and the fix commit.
4. If inline/orchestrator is chosen:
   - Apply the minimal change to the affected file(s).
   - Run the consumer's preflight (e.g., `py -3 tools/run.py ci --check`) and confirm it passes.
5. If the finding severity is `blocking` or `important`, or if `non_trivial_fix` is `true`, the fix must be proven with a failing-then-passing test:
   - **RED:** Create or identify a test that reproduces the bug. Run it and capture the failing output in the implementer report or inline log.
   - **GREEN:** Apply the minimal fix. Re-run the same test until it passes.
   - The test must be added or updated in the permanent test suite. `compile_metrics.py` and the consumer's preflight must pass.
   - For inline/orchestrator fixes, record the RED/GREEN commands and output in `review-log-finding-fix.md`.
6. After the fix is committed, do not hand-edit `review-metrics.json`. Regenerate the metrics file and authorize `re-preflight`:
   ```bash
   py -3 .agents/skills/iterative-review/scripts/compile_metrics.py \
       --state <scratch_dir>/review-state.json \
       --metrics <scratch_dir>/review-metrics.json
   py -3 .agents/skills/iterative-review/scripts/next_node.py \
       --state <scratch_dir>/review-state.json \
       --propose re-preflight
   ```
7. Move to `re-preflight`.
8. Round cap: the fix round for a finding is `round - discovered_at_round + 1` from `<scratch_dir>/review-state.json`. When that reaches `max_fix_rounds`, escalate to `implementer-strong`; if it still fails at the cap, route to `blocked`.

## Outputs
- `<scratch_dir>/review-metrics.json` regenerated from `<scratch_dir>/review-state.json` and the recorded logs
- The `fix_round` semantics are derived from `discovered_at_round` and the current `round` in `<scratch_dir>/review-state.json`; this node does not modify `review-state.json` directly
- If `implementer` was used:
  - `review-log-implementer-brief.md`
  - `review-log-implementer-report.md`
  - Commit containing the fix
- If inline was used:
  - The updated file(s)
  - Updated `scan_findings`

## Next check
```bash
py -3 .agents/skills/iterative-review/scripts/next_node.py \
    --state <scratch_dir>/review-state.json \
    --propose re-preflight
```
