# Writing Anti-Slop Profile

Read this profile in full before authored prose and maintain encountered patterns through the [observation loop](README.md#record-and-improve). Concision must preserve meaning; domain terminology is appropriate when it helps its intended reader. Active plans, specifications and durable slop observations are governed artifacts, not scratch merely because they describe an investigation.

Use this profile when writing documents, plans, specs, or any other text artifacts. This profile enforces standards for artifact placement and writing quality.

## Scratch Artifact Check
- **CRITICAL**: Before creating any scratch files (draft documents, temporary notes, session artifacts), check `.agents/doctrine/artifact-custody.md` for placement guidance
- Scratch files must be placed in `Z:\_agent-scratch\wild-bunch\<branch-name>`, never in the repo root
- Files like `*-review*.md`, `*-scratch*.md`, `*-draft*.md`, `COMMIT_MSG.txt`, `PR_BODY.md` are scratch artifacts that pollute the tree
- If a committed artifact appears to be scratch, apply the completed-artifact policy before removing it; preserve live, ambiguous and enduring material.

## Artifact Placement
- Active plans, specifications, and roadmaps live under `.agents/plans/`,
  `.agents/specs/`, and `.agents/roadmaps/`.
- Durable doctrine and binding repo-local profiles live under
  `.agents/doctrine/` and `.agents/unslop/`.
- Temporary notes, review packages, worker reports, screenshots, and other
  evidence live in `Z:\_agent-scratch\wild-bunch\<branch-name>`.
- Do not create loose agent files at repo root or commit generated evidence.

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
- [ ] Tracked artifacts use their current `.agents/` source home
- [ ] No loose files at repo root
- [ ] Screenshots/evidence are in the branch-scoped `_agent-scratch` workspace
- [ ] Document is concise and direct
- [ ] Language is clear and unambiguous
- [ ] Document is properly structured
- [ ] Examples are concrete and helpful
