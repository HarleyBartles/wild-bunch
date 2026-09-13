---
name: unslop-engine
description: Use when observed AI output defaults in a domain are repetitive and
  you need a durable anti-slop profile to counter them.
metadata:
  source-id: unslop-engine
  source-path: codex-marketplace/plugins/unslop-plus/skills/unslop-engine/SKILL.md
  provenance-name: Unslop Engine first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  use_when:
  - generating a domain-specific anti-slop profile from samples or observed
    defaults.
  do_not_use_when:
  - applying an existing anti-slop profile to a task.
  related_skills:
  - unslop-profiles
license: MIT
---

# Unslop Engine

## When to Use

Use this skill when you need to empirically detect repetitive AI output patterns in a domain and generate a reusable anti-slop profile.

Do not use this skill when applying an existing anti-slop profile; use `unslop-profiles` instead.

## Core Pattern

1. Identify the domain and whether you are analyzing text or visual samples.
2. Collect representative samples (inline, fixture files, or a sample directory).
3. Run the engine:
   ```bash
   py -3 scripts/unslop.py --apply --domain "..." [--type visual --count N]
   ```
4. Review the generated artifacts in `unslop-output/`:
   - `analysis.md` — counted repeated patterns
   - `skill.md` — generated anti-slop profile
5. Return the profile name, the dominant repeated patterns, and how to use the profile.
