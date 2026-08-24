---
name: brainstorming
description: Use when starting any creative work such as creating features, building
  components, adding functionality, or modifying behavior.
metadata:
  source-id: brainstorming
  source-path: codex-marketplace/plugins/superpowers-plus/skills/brainstorming/SKILL.md
  provenance-name: Brainstorming first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when starting any creative work such as creating features, building components,
    adding functionality, or modifying behavior.
  use_when:
  - Use when starting any new feature, component, or modification.
  - Use when the human frames a creative or build goal and no approved spec exists.
  - Use when a project needs intent, constraints, and approach clarified before implementation.
  do_not_use_when:
  - Do not use when an approved spec or plan already exists and is ready for execution.
  - Do not use as a substitute for writing-plans or executing-plans.
  - Do not use when the task is pure execution without design decisions.
  related_skills:
  - using-superpowers-plus
  - handoff-gates
  - writing-plans
  - writing-roadmaps
license: MIT
---
## Provenance

This skill is a first-party authored derivation of `obra/superpowers` v6.2.0, released under the MIT License. The original upstream snapshot is retained in `codex-marketplace/plugins/superpowers-plus/skills/brainstorming/` for reference.

# Brainstorming Ideas Into Designs

Help turn ideas into fully formed designs and specs through natural collaborative dialogue.

Start by understanding the current project context, then ask questions one at a time to refine the idea. Once you understand what you're building, present the design and get user approval.

<HARD-GATE>
Do NOT invoke any implementation skill, write any code, scaffold any project, or take any implementation action until you have presented a design and the user has approved it. This applies to EVERY project regardless of perceived simplicity.
</HARD-GATE>

## Anti-Pattern: "This Is Too Simple To Need A Design"

Every project goes through this process. A todo list, a single-function utility, a config change — all of them. "Simple" projects are where unexamined assumptions cause the most wasted work. The design can be short (a few sentences for truly simple projects), but you MUST present it and get approval.

## Checklist

You MUST create a task for each of these items and complete them in order:

1. **Load baseline and local guide** — read this skill's baseline (`references/design-baseline.md`) and the repo's `.agents/runbooks/design.md` before executing the stage checklist.
2. **Explore project context** — check files, docs, recent commits
3. **Ask clarifying questions** — one at a time, understand purpose/constraints/success criteria
4. **Propose 2-3 approaches** — with trade-offs and your recommendation
5. **Present design** — in sections scaled to their complexity, get user approval after each section
6. **Write design doc** — save to `.agents/specs/YYYY-MM-DD-<topic>-design.md` and commit
7. **Spec self-review & readiness gate** — quick inline check for placeholders, contradictions, ambiguity, scope; then use a reviewer subagent or `handoff-gates` spec-readiness lane. Rate the spec (8/10 floor, 9/10 target). Report the final rating.
8. **User reviews written spec** — ask user to review the spec file and the final rating before proceeding.
9. **Transition to implementation** — invoke writing-plans skill to create implementation plan

## Process Flow

```dot
digraph brainstorming {
    "Explore project context" [shape=box];
    "Scope too large for one spec?" [shape=diamond];
    "Invoke writing-roadmaps" [shape=doublecircle];
    "Ask clarifying questions" [shape=box];
    "Propose 2-3 approaches" [shape=box];
    "Present design sections" [shape=box];
    "User approves design?" [shape=diamond];
    "Write design doc" [shape=box];
    "Spec self-review &\nreadiness gate" [shape=box];
    "User reviews spec?" [shape=diamond];
    "Invoke writing-plans skill" [shape=doublecircle];

    "Explore project context" -> "Scope too large for one spec?";
    "Scope too large for one spec?" -> "Invoke writing-roadmaps" [label="yes"];
    "Scope too large for one spec?" -> "Ask clarifying questions" [label="no"];
    "Ask clarifying questions" -> "Propose 2-3 approaches";
    "Propose 2-3 approaches" -> "Present design sections";
    "Present design sections" -> "User approves design?";
    "User approves design?" -> "Present design sections" [label="no, revise"];
    "User approves design?" -> "Write design doc" [label="yes"];
    "Write design doc" -> "Spec self-review &\nreadiness gate";
    "Spec self-review &\nreadiness gate" -> "Spec self-review &\nreadiness gate" [label="fix inline"];
    "Spec self-review &\nreadiness gate" -> "User reviews spec?" [label="meets floor"];
    "User reviews spec?" -> "Write design doc" [label="changes requested"];
    "User reviews spec?" -> "Invoke writing-plans skill" [label="approved"];
}
```

**The normal terminal state is invoking writing-plans.** If the project is too large for a single spec, invoke `writing-roadmaps` instead. Do NOT invoke frontend-design, mcp-builder, or any other implementation skill directly from brainstorming.

## The Process

**Understanding the idea:**

- Check out the current project state first (files, docs, recent commits)
- Before asking detailed questions, assess scope: if the request describes multiple independent subsystems (e.g., "build a platform with chat, file storage, billing, and analytics"), flag this immediately. Don't spend questions refining details of a project that needs to be decomposed first.
- If the project is too large for a single spec, stop and invoke `writing-roadmaps` to build a sequenced roadmap and write Plan 1. Do not brainstorm the whole epic in one pass or continue with detailed design questions until Plan 1 is approved.
- For appropriately-scoped projects, ask questions one at a time to refine the idea
- Prefer multiple choice questions when possible, but open-ended is fine too
- Only one question per message - if a topic needs more exploration, break it into multiple questions
- If a single missing fact blocks the next step, invoke `/asking-clarifying-questions` before guessing.
- Focus on understanding: purpose, constraints, success criteria

**Exploring approaches:**

- Propose 2-3 different approaches with trade-offs
- Present options conversationally with your recommendation and reasoning
- Lead with your recommended option and explain why
- YAGNI ruthlessly - remove unnecessary features from every approach and design

**Presenting the design:**

- Once you believe you understand what you're building, present the design
- Scale each section to its complexity: a few sentences if straightforward, up to 200-300 words if nuanced
- Ask after each section whether it looks right so far
- Cover: architecture, components, data flow, error handling, testing
- Be ready to go back and clarify if something doesn't make sense

**Design for isolation and clarity:**

- Break the system into smaller units that each have one clear purpose, communicate through well-defined interfaces, and can be understood and tested independently
- For each unit, you should be able to answer: what does it do, how do you use it, and what does it depend on?
- Can someone understand what a unit does without reading its internals? Can you change the internals without breaking consumers? If not, the boundaries need work.
- Smaller, well-bounded units are also easier for you to work with - you reason better about code you can hold in context at once, and your edits are more reliable when files are focused. When a file grows large, that's often a signal that it's doing too much.

**Working in existing codebases:**

- Explore the current structure before proposing changes. Follow existing patterns.
- Where existing code has problems that affect the work (e.g., a file that's grown too large, unclear boundaries, tangled responsibilities), include targeted improvements as part of the design - the way a good developer improves code they're working in.
- Don't propose unrelated refactoring. Stay focused on what serves the current goal.

## After the Design

**Documentation:**

- Write the validated design (spec) to `.agents/specs/YYYY-MM-DD-<topic>-design.md`
  - (User preferences for spec location override this default)
- Use elements-of-style:writing-clearly-and-concisely skill if available
- Commit the design document to git

**Spec Self-Review:**
After writing the spec document, look at it with fresh eyes:

1. **Placeholder scan:** Any "TBD", "TODO", incomplete sections, or vague requirements? Fix them.
2. **Internal consistency:** Do any sections contradict each other? Does the architecture match the feature descriptions?
3. **Scope check:** Is this focused enough for a single implementation plan, or does it need decomposition?
4. **Ambiguity check:** Could any requirement be interpreted two different ways? If so, pick one and make it explicit.

Fix any issues inline. No need to re-review — just fix and move on.

**User Review Gate:**
After the spec review loop passes, ask the user to review the written spec before proceeding:

> "Spec written and committed to `<path>`. Please review it and let me know if you want to make any changes before we start writing out the implementation plan."

Wait for the user's response. If they request changes, make them and re-run the spec review loop. Only proceed once the user approves.

**Implementation:**

- Invoke the writing-plans skill to create a detailed implementation plan
- Do NOT invoke any other skill. writing-plans is the next step.


