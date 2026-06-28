# Writing Profile

Portable anti-slop profile for everyday explanatory, persuasive, reflective, summary, and narrative writing across repos and domains.

## Purpose

Catch generic AI prose before it becomes user-facing text. Apply before drafting or revising prose where the output should sound grounded, specific, and useful rather than inflated.

## Where to Use

Before drafting or revising: chat answers, documents, summaries, memos, release notes, issue descriptions, and explanatory copy.

## Slop Patterns to Avoid

- Cliche openings and broad scene-setting before the actual subject
- Inflated adjectives: `robust`, `seamless`, `transformative`, `holistic`, `game-changing`
- Empty universals about landscapes, ecosystems, complexity, empowerment, and outcomes
- Fake nuance that gestures at balance without naming real tradeoffs
- Conclusion padding that restates the obvious in grander language
- List bloat where bullets exist because the model defaults to bullets

## Required Avoid Rules

Do not use stock phrase families as default moves:
- `in today's`, `let's dive`, `at its core`, `it is worth noting`, `the bottom line`
- `unlock`, `landscape`, `ecosystem`, `robust`, `seamless`, `holistic`, `empower`

Do not use generic stakes-setting unless the prompt actually asks for broad context.

## Required Prefer-Instead Rules

- Start with the actual subject, actor, claim, or requested action
- Replace broad adjectives with concrete constraints, examples, tradeoffs, or evidence
- Keep conclusions only when they add a decision, caveat, or next move
- Use bullets only when they improve scanability

## False Positives / Do Not Overapply

Do not ban legitimate domain terms when they are precise. For example, `ecosystem` may be valid in ecology, package management, or platform strategy. Catch default filler use, not all appearances of the word.

## Examples

### Before (Avoid)
> In today's fast-paced digital landscape, it is more important than ever to unlock robust and seamless outcomes.

### After (Prefer)
> This system reduces deployment time by 40% through automated caching and parallel processing.

### Before (Avoid)
> The bottom line is that success requires a holistic approach that balances people, process, and technology.

### After (Prefer)
> To ship this feature, we need: (1) backend API changes, (2) frontend component updates, and (3) migration scripts for existing data.

## Acceptance Checks

This profile is acceptable only if it would catch all negative golden examples while preserving precise technical, scientific, or product-domain language when used intentionally.
