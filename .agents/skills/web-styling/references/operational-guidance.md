# Operational guidance for web-styling

## CSS Modules
- Scope class names locally by default so they are hashed to the component.
- Compose shared rules with `composes: foo from './foo.css';` to avoid duplication.
- Name classes with `camelCase` or `kebab-case` and keep files next to their components.

## Sass
- Store variables, mixins, and functions in partials prefixed with `_` and import them with `@use` or `@import`.
- Limit nesting to three levels; excessive nesting increases specificity and bundle size.
- Use `@mixin` for repeated patterns and `@include` to apply them.

## Less
- Define variables with `@` and reuse them for colors, spacing, and typography.
- Build mixins with `.mixin-name()` and call them like standard class references.
- Use guards (`when`) and imports to conditionally load theme files.

## styled-components
- Write styles as tagged template literals next to the component.
- Theme through a `ThemeProvider` and read values from props.
- Avoid passing many dynamic props; compute styles outside render where possible.

## CSS-in-JS vs preprocessed CSS
- Preprocessed CSS (Sass/Less/CSS Modules) keeps styles in build artifacts and is easier to cache.
- CSS-in-JS (styled-components) is best when theme or props drive styles at runtime.
- Do not combine a preprocessor with a CSS-in-JS runtime unless a migration is in progress.
