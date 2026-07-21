# WCAG Operational Guidance

## POUR principles

WCAG organizes accessibility under four principles: Perceivable, Operable, Understandable, and Robust.

- **Perceivable**: Information and user-interface components must be presentable to users in ways they can perceive. Examples include text alternatives for images, captions and transcripts for audio and video, sufficient color contrast, and the ability to resize or reflow content without loss of information.
- **Operable**: Interface components and navigation must be operable. Examples include full keyboard access, enough time to read and use content, controls that do not cause seizures or physical reactions, and clear focus indication.
- **Understandable**: Information and the operation of the user interface must be understandable. Examples include readable text, predictable navigation, consistent labels, and helpful error messages.
- **Robust**: Content must be robust enough to work with current and future assistive technologies. Examples include valid markup, correct use of roles and properties, and proper name and state exposure for custom controls.

## Conformance levels

WCAG 2.2 defines three conformance levels:

- **A** — the minimum level. Missing Level A success criteria make a site inaccessible for many people.
- **AA** — the standard target for most organizations and regulations. Most accessibility policies aim for Level AA.
- **AAA** — the highest level. AAA is not required as a general policy target because it is not always possible for all content.

When auditing, confirm the target level at the start and report against that level only. Do not penalize content for failing AAA criteria if the target is A or AA.

## Common success criteria and testing approach

The audit should map content to the relevant success criteria. Common criteria include:

- **1.1.1 Non-text Content (A)**: Provide text alternatives for images, icons, and non-decorative graphics.
- **1.4.3 Contrast (Minimum) (AA)**: Text and images of text must have a contrast ratio of at least 4.5:1, or 3:1 for large text.
- **1.4.10 Reflow (AA)**: Content must be usable at 400% zoom without horizontal scrolling in most cases.
- **2.1.1 Keyboard (A)**: All functionality must be available from a keyboard unless the function cannot be done with a keyboard.
- **2.4.3 Focus Order (A)**: Focus order must follow a meaningful sequence.
- **2.4.7 Focus Visible (AA)**: Keyboard focus indicators must be visible.
- **2.5.5 Target Size (Enhanced) (AAA)**: Targets must be at least 44 by 44 CSS pixels, unless an equivalent target exists or the target is inline.
- **3.3.1 Error Identification (A)**: Users must be told when an input error occurs.
- **3.3.2 Labels or Instructions (A)**: Labels or instructions must be provided when content requires user input.
- **4.1.2 Name, Role, Value (A)**: UI components must expose name, role, and value to assistive technologies.

Testing combines automated scanning, manual keyboard navigation, screen-reader inspection, color-contrast measurement, and zoom/reflow checks. Automated tools catch many programmatic issues; manual testing catches logical focus order, alternative-text quality, and real-world screen-reader behavior.

## Accessibility audit workflow

1. **Define scope and target level**. Confirm the pages, components, states, and conformance level being evaluated.
2. **Run automated scans**. Use a scanner to find machine-detectable failures, then triage false positives.
3. **Test manually with a keyboard**. Tab through the page, operate controls, and check focus order and visibility.
4. **Test with a screen reader**. Move through headings, landmarks, links, forms, and dynamic content. Verify announcements and non-visual alternatives.
5. **Check visual and zoom behavior**. Verify contrast, text resizing, reflow at 400% zoom, and responsive states.
6. **Inspect code and ARIA**. Validate HTML, review ARIA usage, and confirm names, roles, and states for custom controls.
7. **Record findings**. For each issue, list the success criterion, level, observation, evidence, and recommended remediation.
8. **Prioritize and plan**. Group issues by principle, impact, and effort; map fixes to success criteria and schedule re-testing.
