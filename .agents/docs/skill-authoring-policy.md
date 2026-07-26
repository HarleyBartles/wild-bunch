# Skill Authoring Policy

Status: active policy
Owner: Wild Bunch repository
Scope: repo-local Wild Bunch skill classification, authoring, review, custody, and refresh-boundary checks
Nearest router: `.agents/docs/repo-skills-policy.md`

This policy governs repository-local skills before any `wild-bunch-*` skill is
created, renamed, migrated, or retired. It adapts marketplace skill standards
to Wild Bunch local custody without turning local skills into marketplace
source.

## Authority and custody

- Generic skill-writing technique comes from the installed `writing-skills`
  skill.
- Marketplace frontmatter, provenance, plugin membership, and projection rules
  are upstream references, not local source custody.
- Repo-local Wild Bunch skills are authored under `.agents/skills/wild-bunch-*/`.
- Marketplace-derived skills are refreshed from the pinned
  `.agents/plugins/marketplace-source` submodule and remain separate from local
  Wild Bunch custody.
- A plugin may be vendored locally under `.agents/plugins/<name>/` and declared
  in `.agents/plugins/marketplace.json` with `"source": "local"`. Its skills are
  installed into `.agents/skills/<name>/` under their original names and are not
  `wild-bunch-*` repo-local skills.
- The deterministic installer and validator must preserve repo-local
  `wild-bunch-*` skills and must not classify them as marketplace-derived.
- Do not add `agents/openai.yaml` to a repo-local skill unless a future task
  explicitly prepares that skill for marketplace projection.

The `wild-bunch-` prefix is reserved for Wild Bunch repository-local skills.
The canonical inventory is the matching directories under `.agents/skills/`.
Do not maintain a parallel list of their names.

## Surface classification

Use the smallest surface that owns the decision.

| Surface | Use it for | Do not use it for |
| --- | --- | --- |
| Skill | A triggerable, reusable technique, pattern, tool capability, or reference guide that addresses recurring agent judgment | Project law, one-off decisions, or a deterministic procedure only |
| Doctrine or policy | Durable invariants, authority, source truth, protected boundaries, and must or must-not rules | A triggerable capability |
| Stage guide | Repo-specific design, planning, implementation, or review overlays | Durable law or capability routing |
| Runbook | A repeatable procedure with known inputs, commands, and stop conditions | Judgment that should compose as a skill |
| Reference or contract | Schemas, factual lookup, acceptance criteria, or operating details read on demand | Broad workflow control |
| Script or tool | Mechanical checking, transformation, generation, or enforcement | Instructions that require agent judgment |
| README | Human orientation and usage context | Agent law or skill routing |
| `INDEX.md` | Generated navigation and coverage | Operative law or manual inventory |

Project-specific conventions belong in doctrine, guides, or references unless
they also provide a recurring, triggerable capability with an owned decision.
The `wild-bunch-` prefix signals local custody; it does not make a document a
skill.

## Skill qualification test

Retain a candidate as a skill only when all of these are true:

1. A human request or observable symptom reliably triggers it.
2. It owns a decision or capability with a clear boundary.
3. The same judgment recurs across multiple Wild Bunch tasks.
4. The behavior is not already owned by a generic skill, local guide,
   doctrine, runbook, reference, or script.
5. A pressure scenario demonstrates a meaningful failure without it.
6. The guidance can stay compact, with heavier detail moved to `references/`.

If these tests fail, retire the candidate, merge it into the owning surface, or
reclassify it as doctrine, a guide, a reference, a runbook, or a script.

## Required skill shape

Every repo-local Wild Bunch skill must have a directory matching its `name` and
a UTF-8 `SKILL.md` with:

- `name`, matching the directory and using lowercase letters, numbers, and
  hyphens;
- a concise `description` beginning with `Use when...`, describing triggers
  rather than summarizing the workflow;
- local `metadata.status`, `metadata.scope`, `metadata.use_when`, and
  `metadata.do_not_use_when` fields;
- no marketplace identity fields such as `source-id`, `source-path`,
  `provenance-name`, `source-category`, or `owner`;
- an overview and owned decision;
- hard boundaries and stop conditions;
- a minimal workflow or decision pattern;
- progressive references for detailed operating facts;
- no `agents/openai.yaml` unless a future task explicitly prepares the skill
  for marketplace projection.

Keep the control plane compact. Move detailed facts, examples, and operating
contracts into `references/` instead of bloating the entrypoint.

## Authoring gate

- A candidate that merely repeats a project rule is reclassified as doctrine or a reference.
- A `wild-bunch-*` skill with `source-path: sources/first_party/...` fails local validation.
- A local skill that disappears from a marketplace plugin remains after a refresh.
- Renaming or adding a `wild-bunch-*` directory requires no inventory edit; discovery follows the prefix.

Skill authoring is documentation TDD:

1. Write the pressure scenario without the candidate skill.
2. Run it and record the baseline failure or ambiguity.
3. Retire or reclassify the candidate if no meaningful failure appears.
4. Write the smallest skill that addresses the observed failure.
5. Run the same scenario with the skill and verify compliance.
6. Check frontmatter, links, word count, mesh discoverability, and refresh
   preservation before moving to another skill.

Do not bulk-copy marketplace skills, author several untested skills together,
or treat a possible upstream home as evidence of local usefulness.

## Review and disposition record

Every assessed candidate must record:

- source and revision;
- local use case;
- disposition: retain, adapt, merge, reclassify, or retire;
- overlapping owner;
- stale claims;
- pressure scenario and result;
- final local path or retirement reason.

Marketplace retirement is not capability retirement. A capability is retained
only when the local evidence and qualification test support it.
