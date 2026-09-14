# Skill authoring runbook

Use `/writing-skills` for generic skill design and test-first authoring.

A repo-local skill is justified only when it owns recurring Wild Bunch-specific
judgment that doctrine, a runbook, a contract, a deterministic script, or a
portable skill does not own. Give it one focused decision and a bounded return
shape. Add its exact directory/frontmatter name to `repo.local_skills`; no
prefix establishes custody.

Do not add marketplace provenance fields or `agents/openai.yaml` unless a
separate task is preparing marketplace publication. Validate through the
marketplace refresh route in [marketplace generation](marketplace-generation.md)
and the canonical gate in [testing](testing.md).
