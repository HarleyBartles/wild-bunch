---
name: web-styling
description: Use when choosing or refactoring CSS approaches across CSS Modules, Sass, Less, and styled-components. Do not use when the work is design system governance or framework-specific component libraries.
metadata:
  source-id: web-styling
  source-path: sources/first_party/skills/web-styling/SKILL.md
  provenance-name: Web Styling first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Cross-framework CSS approach guidance
  use_when:
    - Use when choosing or refactoring CSS approaches across CSS Modules, Sass, Less, and styled-components
    - Use when comparing CSS-in-JS with preprocessed CSS
  do_not_use_when:
    - Do not use when the work is design system governance or framework-specific component libraries
license: MIT
---

# Web Styling

## Overview
Pick the styling approach that matches team size, build pipeline, and runtime constraints.

## When to Use
- A project needs scoped, composable class names → CSS Modules.
- A codebase benefits from variables, nesting, and mixins → Sass or Less.
- Styles must be co-located with components and driven by props/state → styled-components.
- The team is weighing preprocessed CSS against CSS-in-JS.

## Core Pattern
1. Default to plain CSS or CSS Modules for static, build-time scoped styles.
2. Add a preprocessor when shared variables, mixins, or nested syntax reduce duplication.
3. Reach for CSS-in-JS only when dynamic theming or runtime prop-based styles justify the bundle cost.

## Common Mistakes
- Mixing global naming conventions with CSS Modules. Use `.camelCase` or `.kebab-case` and `compose` for shared rules.
- Deep nesting in Sass/Less. Keep nesting to 3 levels and prefer explicit selectors.
- Overusing props in styled-components. Extract static rules to avoid render-time overhead.
- Preprocessing CSS-in-JS. Choose one layer; do not run Sass inside styled-components unless required.
