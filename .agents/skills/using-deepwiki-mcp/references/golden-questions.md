# Golden questions for DeepWiki

## Good questions

These produce focused, verifiable answers:

- "What is the canonical build/test command for this repo?"
- "How are new skills added to this marketplace?"
- "What is the release process and versioning policy?"
- "How does this repo handle dependency injection?"
- "Compare the PR workflow in repo A and repo B."

## Bad questions and how to fix them

| Bad | Why it is bad | Better |
| --- | --- | --- |
| "What does this file do?" | DeepWiki is high-level; use a file read for a specific file. | "What is the role of the `src/scheduler` package?" |
| "Implement X for me." | DeepWiki answers questions; it does not generate code. | "What is the recommended pattern for adding a new scheduler task?" |
| "Is the latest version of Y compatible with Z?" | DeepWiki is not a live registry. | "What dependencies does this repo declare for Y?" |
| "Dump the full wiki." | Wastes context; use targeted questions. | "List the wiki topics for this repo first." |

## Follow-up pattern

After `ask_question`, the tool returns a `result` plus suggested wiki pages and a DeepWiki search URL. Use the suggested pages to refine the next question.
