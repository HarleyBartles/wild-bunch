# Frontend UI Profile

Portable anti-slop profile for generic product UI and visual-layout defaults.

## Purpose

Catch generic product UI and visual-layout defaults. Push agents toward task-specific interfaces instead of decorative landing-page patterns.

## Where to Use

Before designing or reviewing frontend UI copy, layouts, dashboards, forms, onboarding flows, empty states, design prompts, or browser-game/game-adjacent HUD surfaces.

## Slop Patterns to Avoid

- Hero-section defaults with gradients, cards, fake dashboards, and generic CTAs
- Fake social proof: `trusted by`, testimonials, logos, and invented metrics
- Visual polish language that avoids interaction details
- Decorative dashboards that do not support a user task
- `Premium`, `beautiful`, `modern`, or `intuitive` as substitutes for concrete affordances
- Layouts that protect aesthetics but not accessibility, responsiveness, focus order, or error states

## Required Avoid Rules

Prohibit generic landing-page furniture, fake social proof, invented metrics, and polish-first UI language unless the task explicitly asks for marketing layout.

Catch UI descriptions that do not name the user task, information hierarchy, interaction states, or constraints.

## Required Prefer-Instead Rules

- Start with the user's task and the decision the surface must support
- Name primary, secondary, disabled, loading, empty, and error states where relevant
- Preserve accessibility, keyboard/focus behavior, responsive behavior, and content hierarchy
- Use visual style only to support the task
- Avoid fake data unless clearly marked as fixture/mock data

## False Positives / Do Not Overapply

Do not ban marketing pages or polished visuals when they are the requested output. The profile should catch unrequested marketing defaults and decorative filler.

## Examples

### Before (Avoid)
> A modern hero section with a gradient background, rounded cards, and a prominent Get Started CTA.

### After (Prefer)
> A sign-up form with email and password fields. Primary action: "Create account". Loading state: spinner on button. Error state: inline error message below each field. Empty state: not applicable.

### Before (Avoid)
> Add trusted-by logos, testimonials, and a clean dashboard preview.

### After (Prefer)
> A dashboard showing user statistics: total users, active users, and revenue. Each metric is a card with the number and a label. Data is real from the backend API. No fake data or testimonials.

## Acceptance Checks

This profile is acceptable only if it would stop an agent from producing a generic SaaS landing page when the requested work is a specific product surface or workflow.
