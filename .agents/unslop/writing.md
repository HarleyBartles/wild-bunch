# Writing Anti-Slop Profile

Read this profile in full when selected through the [unslop playbook](../playbooks/unslop.md) and maintain encountered patterns through its [observation loop](../playbooks/unslop.md#record-and-improve). Concision must preserve meaning; domain terminology is appropriate when it helps its intended reader. Active plans, specifications and durable slop observations are governed artifacts, not scratch merely because they describe an investigation.

Use this profile when writing documents, plans, specs, or any other text artifacts. This profile enforces standards for artifact placement and writing quality.

## Keep Durable Guidance Scope-Stable

Recognize a runbook or topical playbook that embeds a named epic, temporary plan link, dated campaign sequence, or one-release version/worktree rule as if it were permanent operating procedure. Move campaign-specific requirements to the active roadmap or execution plan. Keep only reusable procedure in durable guidance; stable repository policy such as the default branch and release boundary may remain. Re-read the edited guidance for scope and meaning. Do not add a structural checker for this distinction. See [observation U-011](observations.md#u-011-campaign-instructions-leaked-into-durable-guidance).

## Remove Empty Sections

Recognize headings with no useful content or sections whose only content is a placeholder such as `None.` or `N/A`. Remove the section instead of preserving template boilerplate. Retain an empty field only when a validator or external schema requires that exact field. Do not add a heading-existence or empty-section test; judge whether each section gives its reader an actual instruction or decision. See [observation U-010](observations.md#u-010-placeholder-only-sections-padded-agent-guidance).

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
