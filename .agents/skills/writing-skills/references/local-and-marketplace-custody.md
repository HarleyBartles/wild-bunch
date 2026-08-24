# Local and marketplace custody

Use `--custody local` only for names beginning `mark-`; it creates tracked
repository-local skill custody under `.agents/skills/`. Local skills are always
`first_party` and have no authority directory.

Use `--custody marketplace` for source custody under
`codex-marketplace/plugins/<lane>/skills/`. Choose the lane before writing: marketplace
`first_party` scaffolds only `SKILL.md` and `references/`; `skills-with-source`
and `skills-with-citation` add the authority records needed by their
source-backed workflow. Follow `source-grounded-authoring.md` for decomposition,
legal approval, citations, reconciliation, and manual freshness review.

Do not create registries, `agents/openai.yaml`, marketplace bundle files,
third-party modifications, or generated indexes from this scaffolder.
