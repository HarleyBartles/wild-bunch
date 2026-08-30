# Format and markup

Use this reference for human-facing Markdown, documentation structure, UI
copy, error messages, help text, comments, commit messages, and pull request
descriptions.

## Working rules

- Make headings describe the reader's subject or task. Do not use headings as
  decoration or as a substitute for a missing explanation.
- Put the useful answer or action near the beginning of UI copy, errors, help
  text, and short technical messages.
- Make error messages say what happened, what it means, and what the reader can
  do next. Offer retry only when the product behavior confirms that retry can
  succeed; otherwise give the accurate recovery action. Do not expose internal
  speculation as user guidance.
- Use bullets for parallel items and numbered steps for ordered actions. Keep
  the grammar and punctuation of list items consistent.
- Keep links descriptive. The link text should tell the reader what opens
  without requiring the surrounding sentence to supply the missing noun.
- Use code formatting for literal commands, paths, identifiers, and values, not
  for emphasis.
- Match the surrounding product or repository's Markdown conventions. This
  skill does not override local templates, accessibility requirements, or
  release-note style.

## Human-facing technical messages

Prefer:

`Backup restored. Your files from 14:30 are available in the project folder.`

Over:

`Operation completed successfully.`

The first tells the reader what happened and what to do next. Keep the extra
detail only when it is true and useful.

**Source basis:** The historical source, Chapters IV and VI, supplemented by
first-party guidance for modern Markdown and product copy. The historical
source is context for form, not default operational guidance.
