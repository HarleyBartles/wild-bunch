---
name: frontend-ux
description: Use when designing, reviewing, or debugging frontend user interfaces
  and the task calls for accessibility, layout, interaction, or UX guidance.
metadata:
  source-id: frontend-ux
  source-path: codex-marketplace/plugins/frontend-pack/skills/frontend-ux/SKILL.md
  provenance-name: Frontend UX first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when designing, reviewing, or debugging frontend user interfaces and
    the task calls for accessibility, layout, interaction, or UX guidance.
  use_when:
  - Use when designing or reviewing a frontend layout, component, or interaction.
  - Use when checking accessibility, color, typography, or responsive behavior.
  - Use when refining user flows, feedback, and platform-appropriate conventions.
  do_not_use_when:
  - Do not use when another more specific skill owns the task.
  related_skills:
  - playwright-testing
  - wcag
  - react
  - web-styling
license: MIT
---

# Frontend UX

Use this skill for frontend user-interface guidance covering component and layout patterns, accessibility, interaction design, and UX review.

## When to Use

- Designing or reviewing a frontend layout, component, or interaction.
- Checking accessibility, color, typography, or responsive behavior.
- Refining user flows, feedback, and platform-appropriate conventions.

## Core Pattern

1. Start with semantic HTML and clear information architecture; avoid unnecessary wrapper elements.
2. Ensure keyboard navigability and screen-reader support before styling polish.
3. Use consistent spacing, color, and typography aligned to the design system.
4. Provide visible focus states, loading feedback, and clear error messaging.
5. Validate contrast, motion preferences, and touch targets against WCAG and platform guidelines.
6. Review the whole flow, not just a single screen; load operational guidance for deep patterns.

## Common Mistakes

- Adding motion or modals without reduced-motion alternatives. → Respect `prefers-reduced-motion` and keep modals focus-trapped.
- Hiding focus indicators for visual cleanliness. → Provide visible, high-contrast focus states.
- Reusing a desktop layout on mobile without touch-target or readability adjustments. → Design mobile-first with adequate tap targets and readable type.

Load `references/operational-guidance.md` for deeper coverage of accessibility, component patterns, and cross-platform design systems.
