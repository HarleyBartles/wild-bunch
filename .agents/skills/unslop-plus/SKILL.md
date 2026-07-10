---
name: unslop-plus
description: Use when apply domain-specific anti-slop profiles for common software
  development workflows, with thirteen portable profiles for writing, technical-writing,
  implementation-plans, code-review, worker-returns, debugging, frontend-react, frontend-ui,
  api-design, architecture, testing, security-review, and cleanup-custody.
metadata:
  source-id: unslop-plus
  source-path: sources/first_party/skills/unslop-plus/SKILL.md
  provenance-name: Unslop Plus first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when apply domain-specific anti-slop profiles for common software development
    workflows, with thirteen portable profiles for writing, technical-writing, implementation-plans,
    code-review, worker-returns, debugging, frontend-react, frontend-ui, api-design,
    architecture, testing, security-review, and cleanup-custody.
  use_when:
  - Use when apply domain-specific anti-slop profiles for common software development
    workflows, with thirteen portable profiles for writing, technical-writing, implementation-plans,
    code-review, worker-returns, debugging, frontend-react, frontend-ui, api-design,
    architecture, testing, security-review, and cleanup-custody.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---

# Unslop+

Use this skill when you need to apply domain-specific anti-slop guidance to your work. This skill bundles the Unslop analysis engine with thirteen portable profiles for common software development workflows.

## Available Profiles

This skill includes the following portable profiles:

1. **writing** - Generic AI prose patterns for everyday explanatory, persuasive, reflective, summary, and narrative writing
2. **technical-writing** - Documentation and technical content slop
3. **implementation-plans** - Executable coding plans and worker issues
4. **code-review** - Evidence-based code review
5. **worker-returns** - Completion report validation
6. **debugging** - Systematic bug diagnosis
7. **frontend-react** - React implementation defaults
8. **frontend-ui** - Generic UI patterns
9. **api-design** - API contract design
10. **architecture** - Pattern-based architecture reasoning
11. **testing** - Behavior-focused testing
12. **security-review** - Concrete security analysis
13. **cleanup-custody** - Repository hygiene decisions

## How to Use

1. Select the appropriate profile for your current workflow
2. Read the profile's purpose, slop patterns, and required rules
3. Apply the avoid/prefer rules to your work
4. Use the examples as guidance for what to avoid and prefer instead
5. Verify your work against the acceptance checks

## Profile Selection Guide

- **Writing prose**: Use the `writing` profile
- **Writing docs/technical content**: Use the `technical-writing` profile
- **Creating implementation plans**: Use the `implementation-plans` profile
- **Reviewing code changes**: Use the `code-review` profile
- **Writing completion reports**: Use the `worker-returns` profile
- **Debugging issues**: Use the `debugging` profile
- **Working with React**: Use the `frontend-react` profile
- **Designing UI layouts**: Use the `frontend-ui` profile
- **Designing APIs**: Use the `api-design` profile
- **Architecture work**: Use the `architecture` profile
- **Writing tests**: Use the `testing` profile
- **Security review**: Use the `security-review` profile
- **Repository cleanup**: Use the `cleanup-custody` profile

## Profile Portability

All profiles are portable across repos and do not contain Asset Marketplace-specific nouns, local issue IDs, or project-only assumptions. They can be used in any software development context.

## Engine

This skill uses the Unslop analysis engine adapted from the upstream `mshumer/unslop` project. The engine provides the analysis framework, while the profiles provide domain-specific guidance.

## Deliverable

Return the profile you applied, the key changes you made based on the profile's guidance, and confirmation that your work meets the profile's acceptance checks.
