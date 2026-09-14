# Skill authoring policy

Status: active policy
Owner: Wild Bunch repository
Scope: repository-local `wild-bunch-*` skill custody

## Local delta

- Generic skill design, TDD, and review method belongs to `writing-skills`.
- A repository-local skill must own recurring Wild Bunch-specific judgment that
  is not already owned by a portable skill, doctrine, runbook, contract, or
  deterministic script.
- Author repo-local skills under `.agents/skills/wild-bunch-<name>/` and list
  the exact directory/frontmatter name in `.agents/plugins/marketplace.json`
  under `repo.local_skills`.
- Do not add marketplace provenance fields or `agents/openai.yaml` to a
  repo-local skill unless a separate task is preparing marketplace publication.
- Generic capabilities belong in the marketplace source, not in a Wild Bunch
  overlay.

## Local validation

After an authored change, run:

```powershell
py -3 scripts\validate_local_skills_extra.py --check .agents\skills wild-bunch-
py -3 tools\run.py ci --apply
```

The normal hooked commit verifies projection freshness, script contracts, mesh,
and the complete repository check lane.
