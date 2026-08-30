# Writing task routing

Classify the human-facing artifact and the dominant writing problem before
opening a detailed reference. Choose one primary route and add one secondary
route only when the task genuinely crosses boundaries.

| Artifact or problem | Primary reference | Optional secondary |
| --- | --- | --- |
| README, guide, report, explanation, or proposal | `composition-and-flow.md` | `clarity-and-concision.md` |
| Paragraph order, topic sentences, transitions, or argument flow | `composition-and-flow.md` | `clarity-and-concision.md` |
| Punctuation, fragments, run-ons, modifiers, or sentence joins | `sentence-mechanics.md` | `clarity-and-concision.md` |
| Passive, vague, abstract, padded, or indirect prose | `clarity-and-concision.md` | `composition-and-flow.md` |
| Ambiguous terms, jargon, clichés, or commonly misused words | `usage-and-word-choice.md` | `clarity-and-concision.md` |
| UI copy, errors, help text, headings, bullets, or Markdown | `format-and-markup.md` | `clarity-and-concision.md` |
| Commit message or pull request description | `clarity-and-concision.md` | `format-and-markup.md` |

An explicit artifact route takes precedence over the generic writing-problem
fallback. If no artifact route applies, use `clarity-and-concision.md` for the
first pass. If a project or product style guide exists, inspect it before
applying generic guidance.

The final-edit pass is separate from primary and secondary topical routing. Use
the reference named by the observed defect first, then run `final-edit.md` as
the bounded pass before returning the prose.

The original 1918 text is not a routing target for ordinary writing. It is a
bounded fallback only when the selected short reference cannot answer a
specific historical or interpretive question.
