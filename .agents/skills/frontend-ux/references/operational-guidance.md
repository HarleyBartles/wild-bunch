# Frontend UX operational guidance

## When to apply

Use when the frontend-ux skill loaded and the question needs more than the SKILL.md summary:
- choosing component or layout patterns,
- applying accessibility criteria,
- reviewing interaction design,
- comparing platform conventions.

## Component and layout patterns

- Prefer semantic HTML elements (`<header>`, `<nav>`, `<main>`, `<button>`) over generic `<div>` and `<span>` wrappers.
- Use CSS Grid for two-dimensional layouts and Flexbox for one-dimensional alignment.
- Keep component APIs small and state explicit; lift state only when needed.
- Maintain visual hierarchy through consistent spacing and type scale.

## Accessibility

- Target WCAG 2.2 Level AA as the minimum for new work.
- Check color contrast with the WCAG relative luminance formula (4.5:1 for normal text, 3:1 for large text).
- Ensure all interactive elements are keyboard reachable and have visible focus.
- Use ARIA roles and properties only when native semantics are insufficient.
- Test with real screen readers and automated tools.

## Interaction design

- Make affordances obvious: buttons look clickable, links look linkable.
- Provide immediate feedback for user actions (hover, focus, active, loading, success, error).
- Keep task flows short; confirm destructive actions and allow undo when possible.
- Respect platform conventions while keeping the experience consistent.

## UX review

- Review the full task flow, not isolated screens.
- Watch for ambiguous labels, hidden dependencies, and error messages that blame the user.
- Verify responsive behavior across viewport sizes and input modes.

## Related references

- WCAG 2.2: https://www.w3.org/TR/WCAG22/
- W3C HTML: https://www.w3.org/TR/html/
- W3C CSS: https://www.w3.org/TR/css/
- MDN: https://developer.mozilla.org/en-US/
- Material Design 3: https://m3.material.io/
- Apple Human Interface Guidelines: https://developer.apple.com/design/human-interface-guidelines
