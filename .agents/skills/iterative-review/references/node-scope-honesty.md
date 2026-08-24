# node-scope-honesty

## Purpose
Compare the branch diff to the plan, spec, PR body, and linked issues and reconcile any scope drift.

## Inputs
- `review-state.json`
- Full branch `<diff_path>` (from `review-state.json`)
- `<pr_description>`
- Plan, spec, and roadmap files (optional; pass as `--plan`, `--spec`, `--roadmap`)
- Linked issues (optional; add to `pr_description` or pass as extra `--plan` text)

## Recipe
1. Run the concrete scope-honesty check:
   ```
   py -3 .agents/skills/iterative-review/scripts/check_scope_honesty.py \
       --state <scratch_dir>/review-state.json \
       --apply
   ```
   If you have governing documents, pass them:
   ```
   py -3 .agents/skills/iterative-review/scripts/check_scope_honesty.py \
       --state <scratch_dir>/review-state.json \
       --plan <plan_path> \
       --spec <spec_path> \
       --apply
   ```
2. The script writes `review-log-scope-honesty.md` and exits `0` for `scope-honesty: clean`.
3. If it exits `1` with `scope-honesty: drift`, read `review-log-scope-honesty.md`, fix the diff to match the declared scope (or update the PR body/plan/spec), and rerun the script.
4. Do not advance to `lens-dispatch` until `scope-honesty: clean`.

## Outputs
- `review-log-scope-honesty.md` with the comparison result
- `scope-honesty: clean` (exit `0`) when every changed file, or a parent directory/surface containing it, is mentioned in the PR body or governing documents
- `scope-honesty: drift` (exit `1`) when one or more changed files have no matching path or parent-directory mention, which requires the orchestrator to reconcile the diff, PR body, plan, spec, or roadmap before the graph may advance to `lens-dispatch`

## Next check
py -3 .agents/skills/iterative-review/scripts/next_node.py --state <scratch_dir>/review-state.json
