# Local and marketplace custody

Use `--custody local` for any valid exact skill name. It creates tracked
repository-local skill custody under `.agents/skills/`; add that exact name to
`repo.local_skills` in `.agents/plugins/marketplace.json` to establish custody.
Prefixes are optional naming choices, not custody mechanisms. Local skills are
always `first_party` and have no authority directory.

Use `--custody marketplace` for source custody under
`codex-marketplace/plugins/<lane>/skills/`. Choose the lane before writing: marketplace
`first_party` scaffolds only `SKILL.md` and `references/`;
`skills-with-source`, `skills-with-mixed-source`, and `skills-with-citation`
add the authority records needed by their source-backed workflow. Follow
`source-grounded-authoring.md` for decomposition, legal approval, citations,
reconciliation, and manual freshness review.

Do not create registries, `agents/openai.yaml`, marketplace bundle files,
third-party modifications, or generated indexes from this scaffolder.
