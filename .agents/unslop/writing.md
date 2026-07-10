# Writing Anti-Slop Profile

Use this profile when writing documents, plans, specs, or any other text artifacts. This profile enforces standards for artifact placement and writing quality.

## Scratch Artifact Check
- **CRITICAL**: Before creating any scratch files (draft documents, temporary notes, session artifacts), check `.agents/docs/artifact-policy.md` for placement guidance
- Scratch files must be placed in `Z:\_agent-scratch\wild-bunch\<branch-name>`, never in the repo root
- Files like `*-review*.md`, `*-scratch*.md`, `*-draft*.md`, `COMMIT_MSG.txt`, `PR_BODY.md` are scratch artifacts that pollute the tree
- If you find scratch artifacts committed to the repo, remove them as part of self-healing

## Artifact Placement
- Agent-generated non-work outputs (plans, evidence, screenshots, doctrine notes, unslop profiles, session artifacts) must live under the `.agents/` subtree
- Do not create loose files at repo root for agent use
- Superpowers plan records live under `.agents/superpowers/plans/`
- Browser screenshots and evidence must be written under `.agents/superpowers/output/screenshots/` (git-ignored)
- Generated screenshot/image artifacts must NOT be committed to the repo

## Writing Quality
- Be concise and direct
- Use clear, unambiguous language
- Structure documents with clear headings and sections
- Provide concrete examples when helpful
- Avoid jargon unless it's standard terminology
- Keep sentences short and readable
- Use active voice
- Verify that the document serves its intended purpose

## Writing Checklist
- [ ] Scratch artifacts are placed in `Z:\_agent-scratch\wild-bunch\<branch-name>`
- [ ] Agent-generated outputs are under `.agents/` subtree
- [ ] No loose files at repo root
- [ ] Screenshots/evidence are in `.agents/superpowers/output/screenshots/` (git-ignored)
- [ ] Document is concise and direct
- [ ] Language is clear and unambiguous
- [ ] Document is properly structured
- [ ] Examples are concrete and helpful
