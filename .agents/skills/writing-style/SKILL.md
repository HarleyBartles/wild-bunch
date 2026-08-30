---
name: writing-style
description: Use when revising or reviewing human-facing prose against an authorised voice card or a contextual reader-fatigue profile.
metadata:
  source-id: writing-style
  source-path: codex-marketplace/plugins/writing-pack/skills/writing-style/SKILL.md
  provenance-name: Writing Style first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Apply bounded voice and reader-fatigue guidance without overriding facts or clarity.
  use_when:
  - Use when a supplied draft needs an authorised voice review.
  - Use when repeated or mismatched prose patterns may tire a known audience.
  - Use when a style concern must be separated from an authorship or detector claim.
  do_not_use_when:
  - Do not use for ordinary drafting when $writing owns the composed workflow.
  - Do not use to infer authorship, evade detection, or build a private author corpus.
  related_skills:
  - writing
  - writing-with-clarity
  - writing-profile-engine
license: MIT
---

# Writing Style

## Overview

Challenge repeated reader-fatigue patterns while preserving authorised voice.
A signal is not a defect: context, audience, density, evidence freshness, and
higher-authority writing requirements decide whether guidance is warranted.

## Authority order

Preserve verified facts, safety, legal requirements, accessibility, explicit
user intent, and project style before applying a voice card or fatigue profile.
Clarity and meaning outrank fatigue heuristics. When a lower-authority style
finding conflicts, report it and leave the higher-authority text unchanged.

## Bounded workflow

1. Establish the draft, audience, purpose, context, and hard constraints.
2. If the user supplied current-task text or explicit preferences, load
   `references/voice-card.md`; otherwise do not derive a personal voice card.
3. Load only the relevant profile. For general prose-fatigue review, use
   `references/profiles/fatigue/ai-prose-fatigue/profile.md`.
4. Check pattern clusters against their contextual thresholds and preserve
   conditions. A phrase occurrence alone never licenses repair.
5. Return the smallest supported guidance, with one of these finding types:
   `observed`, `candidate`, `preserve`, `repair`, or `abstain`.
6. Recheck every proposed change through $writing-with-clarity so meaning,
   qualification, readability, and deliberate voice survive.

Use `references/profile-contract.md` when maintaining profile data or when the
finding boundary is unclear. Do not load the research package for an ordinary
review.

## Finding discipline

Every finding names the observed cluster, audience/context cost, evidence
class, scope, preserve case, limitation, and smallest repair. If `review_after`
has passed, retain the observation but downgrade automated guidance to
`candidate` or `abstain` pending review.

## Boundaries

- Do not issue detector scores, AI-authorship conclusions, or evasion advice.
- Do not ban exact tokens or remove em dashes, triads, contrast, questions,
  repetition, fragments, or `real` without a concrete reader cost.
- Do not add errors, choppiness, or random variation to simulate human prose.
- Do not retain supplied source prose in a voice card, fixture, log, or corpus.
- Abstain when context or evidence cannot support a bounded decision.

## Common mistakes

- Treating a familiar phrase as proof. -> Record at most an observation.
- Varying every sentence mechanically. -> Preserve useful parallel form.
- Flattening distinctive language. -> Apply an authorised voice card first.
- Strengthening a claim while trimming prose. -> Restore the qualification.
