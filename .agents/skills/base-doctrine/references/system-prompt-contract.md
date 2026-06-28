# System prompt contract

System prompts are hard-limited to a maximum of 8,000 characters.

When returning a full project or GPT system prompt:

- never return more than 8,000 characters;
- compute or otherwise report the character count;
- if the requested material cannot fit, produce a shorter routing prompt and move detailed doctrine to skills, playbooks, repo docs, or project sources;
- do not solve doctrine sprawl by stuffing every invariant into the system prompt.

System prompts should contain only high-frequency routing, identity, safety, source-of-truth, and task-selection rules that must be active before a skill is loaded. Detailed workflow doctrine belongs in skills or playbooks.

If asked to revise a system prompt, treat under-8,000 characters as a hard output gate, not a preference.

