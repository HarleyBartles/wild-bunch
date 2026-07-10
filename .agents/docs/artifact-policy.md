# Artifact Policy

Use this reference when creating agent artifacts, managing screenshots/evidence, or working with unslop profiles.

## Scratch Artifacts
- **CRITICAL**: Scratch files (code reviews, temporary notes, draft documents, session artifacts) must be placed in `Z:\_agent-scratch\wild-bunch\<branch-name>` where `<branch-name>` matches the worktree/branch name
- This scratch space is disposable and not persistent beyond the agent's session
- Agents must clean up their scratch folder when cleaning up their worktree
- **Never commit scratch artifacts to the repo root** - files like `*-review*.md`, `*-scratch*.md`, `COMMIT_MSG.txt`, `PR_BODY.md` are scratch artifacts that pollute the tree
- If a worker finds scratch artifacts committed to the repo, remove them as part of self-healing
- Examples of scratch artifacts: code review notes, temporary analysis documents, draft ADRs, session busters, worker reports

## Agent-Generated Outputs
- All agent-generated non-work outputs (plans, evidence, screenshots, doctrine notes, unslop profiles, session artifacts) must live under the `.agents/` subtree — never at repo root, under `docs/`, or in product source folders.
- Do not create loose files at repo root for agent use (no `COMMIT_MSG.txt`, `PR_BODY.md`, scratch notes, etc.). These are worker artifacts that pollute the tree and the generated index mesh.
- Superpowers plan records live under `.agents/superpowers/plans/`. Agent-facing superpowers material is consolidated under `.agents/superpowers/`, not at root `.superpowers/` or `docs/superpowers/`.
- Browser screenshots and other agent-generated evidence artifacts must be written under `.agents/superpowers/output/screenshots/` (or a coherent `.agents/superpowers/output/...` subfolder).
- Generated screenshot/image artifacts must NOT be committed to the repo. The `.agents/superpowers/output/screenshots/` folder is git-ignored via its local `.gitignore` (`*` with `!.gitignore` and `!INDEX.md` exceptions).
- PR/return notes may cite local evidence filenames/paths or attach screenshots through the review system if needed, but must not add them as repo files.
- If a worker finds screenshots or generated evidence committed elsewhere in the repo (e.g. under `docs/`), they should remove/move them to the git-ignored `.agents/superpowers/output/` area as part of self-healing.
- If a worker finds loose agent artifact files at repo root or in product folders, remove them as part of self-healing.

## Unslop Profiles
- Repo-wide unslop profiles live under `.agents/unslop/`.
- Project-local unslop profiles live under `{project}/.agents/unslop/`.
- Profile filenames are short lowercase kebab-case scope names. Do not include `unslop`, `profile`, or `unslop-profile` in the filename; the folder already says what it is.
- Human docs may point to these profiles, but profiles themselves are agent-facing review/filter material.
- Dev-overlay work should apply `.agents/unslop/dev-overlay.md` together with the backend and web unslop profiles where relevant.
- Unslop profiles are living documents. When a worker applies an unslop profile and slop still lands, the worker must postmortem whether the profile was effective. If the profile should have caught the drift but did not, the worker must strengthen the profile in the same PR when in scope, or return a precise deferred patch. "I read the unslop profile" is not enough; closeout must state what checks the profile forced and whether any gaps were found.
- When strengthening an unslop profile, the edit must name a reusable class of drift, not the one incident. Sharpen or replace existing guidance where possible instead of appending duplicates. Keep additions short enough to remain readable. Create a clear review failure condition (a test, a check, or a concrete reviewable assertion that would fail if the drift recurs). Do not turn profiles into a dumping ground for transient failures. Include a brief closeout note in the PR or return explaining why the profile change is durable — i.e. what class of future drift it now catches that it did not before.
