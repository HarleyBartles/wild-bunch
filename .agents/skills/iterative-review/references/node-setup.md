# node-setup

## Purpose

Collect and normalize the off-repo review inputs for the draft PR.

## Inputs

- `<pr_number>`
- Branch worktree (current directory)
- `gh` CLI with access to the repository

## Recipe

Run the bootstrap script from the branch worktree:

```
py -3 .agents/skills/iterative-review/scripts/start_review.py --pr <pr_number> --apply
```

`start_review.py` does the following and then advances the graph to `normalize-inputs`:

1. Resolves the off-repo scratch workspace under `_agent-scratch/<repo>/<branch>/iterative-review-<pr_number>/`.
2. Fetches the PR title, body, base/head refs, and head SHA from `gh`.
3. Writes `pr_description.txt` into the scratch directory.
4. Generates the full branch diff as `review-<base7>..<head7>.diff`.
5. Creates `review-state.json` with the PR and scratch metadata.
6. Proposes `normalize-inputs` to the graph and runs `normalize_review_inputs.py --apply` on the scratch.
7. Prints the allowed next node, its recipe file, and the `next_node.py` command to authorize it.

## Outputs

- `<scratch_dir>/review-state.json`
- `<scratch_dir>/pr_description.txt`
- `<scratch_dir>/review-<base7>..<head7>.diff`
- Console line pointing to the next node recipe

## Next check

Run the `next_node.py` command printed by `start_review.py`, or:

```
py -3 .agents/skills/iterative-review/scripts/next_node.py --state <scratch_dir>/review-state.json
```
