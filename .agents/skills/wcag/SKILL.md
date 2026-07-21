---
name: wcag
description: Use when auditing web content accessibility against WCAG 2.2 or mapping
  success criteria to a verification plan. Do not use when the work is general UX
  design or automated tooling setup only.
metadata:
  source-id: wcag
  source-path: sources/first_party/skills/wcag/SKILL.md
  provenance-name: Wcag first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when auditing web content accessibility against WCAG 2.2 or mapping success
    criteria to a verification plan.
  use_when:
  - Use when auditing web content accessibility against WCAG 2.2.
  - Use when mapping success criteria to a verification plan.
  do_not_use_when:
  - Do not use when the work is general UX design.
  - Do not use when the work is automated tooling setup only.
license: MIT
---

# WCAG

Use this skill when auditing web content accessibility against WCAG 2.2 or mapping success criteria to a verification plan.

## When to Use

- Use when evaluating whether web content meets WCAG 2.1 or 2.2 success criteria.
- Use when creating or reviewing an accessibility audit, remediation plan, or conformance claim.
- Use when mapping features, components, or user flows to specific WCAG success criteria.
- Do not use for general visual or interaction design decisions that are not framed as accessibility conformance.
- Do not use when the task is limited to installing, configuring, or running automated accessibility tools without interpreting results.

## Core Pattern

1. **Scope the audit**. Identify the target pages, components, user tasks, and the conformance level to evaluate (A, AA, or AAA).
2. **Apply the POUR principles**. For each Perceivable, Operable, Understandable, and Robust principle, identify the relevant success criteria.
3. **Select success criteria**. Reference the common WCAG 2.2 success criteria for the scoped content, such as text alternatives, keyboard access, contrast, resizing, focus indication, and error prevention.
4. **Choose testing methods**. Combine automated scanning with manual checks such as keyboard navigation, screen-reader interaction, color-contrast measurement, zoom and reflow tests, and code inspection.
5. **Record findings**. Note the success criterion, level, observed result, evidence, and remediation needed.
6. **Build a verification plan**. Group findings by principle and priority; assign fixes and re-test steps.

## Common Mistakes

- Treating automated tool output as a complete audit.
- Evaluating AAA criteria when the target only claims A or AA.
- Testing only one viewport or assistive technology.
- Mapping a single criterion without considering the surrounding user task.
- Confusing accessibility best practices with mandatory WCAG success criteria.
