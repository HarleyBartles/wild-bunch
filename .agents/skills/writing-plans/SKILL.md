---
name: writing-plans
description: Use when an approved specification or settled requirements need to become
  an executable multi-step implementation plan.
metadata:
  source-id: writing-plans
  source-path: codex-marketplace/plugins/superpowers-plus/skills/writing-plans/SKILL.md
  provenance-name: Writing Plans first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  use_when:
  - an approved spec exists for a multi-step task.
  - the goal fits a single tight implementation plan.
  - implementation code has not yet been changed.
  do_not_use_when:
  - the spec covers multiple independent subsystems; invoke writing-roadmaps
    to create a roadmap before writing plans.
  - implementation has already started.
  - a substitute for brainstorming.
  related_skills:
  - brainstorming
  - handoff-gates
  - executing-plans
  - subagent-driven-development
  - writing-roadmaps
license: MIT
---
## Provenance

This marketplace-maintained derivative is based on `obra/superpowers` v6.3.0 commit `b36e0829c6d0140e93cfef2ca599b1b07d4a7797` under the MIT License. Upstream source is not vendored; this directory contains the maintained Superpowers+ implementation.

# Writing Plans

## Overview

Write comprehensive implementation plans assuming the engineer has zero context for our codebase and questionable taste. Document everything they need to know: which files to touch for each task, code, testing, docs they might need to check, how to test it. Give them the whole plan as bite-sized tasks. DRY. YAGNI. TDD. Frequent commits.

Assume they are a skilled developer, but know almost nothing about our toolset or problem domain. Assume they don't know good test design very well.

**Announce at start:** "I'm using the writing-plans skill to create the implementation plan."

**First step:** If you were not already routed here by `using-superpowers-plus`, invoke `using-superpowers-plus` first. Then read this skill's baseline (`references/planning-baseline.md`) and the repo's `.agents/runbooks/planning.md` before executing the stage checklist.

**Context:** If working in an isolated worktree, it should have been created via the `using-git-worktrees` skill at execution time.

**Save plans to:** `.agents/plans/YYYY-MM-DD-<feature-name>.md`
- (User preferences for plan location override this default)

## Scope Check

If the spec covers multiple independent subsystems, invoke `writing-roadmaps` to create a sequenced roadmap before writing any plan. If brainstorming already produced a roadmap, write Plan 1 from the roadmap and leave remaining subsystems as pending future plans. Each plan should produce working, testable software on its own.

## When to stop and ask

Before drafting a task, decide what to do when a plan item is missing scope or detail. Use this decision table:

| Situation | Use |
|---|---|
| Plan item has no acceptance criteria and the answer is not in durable source or the spec | `asking-clarifying-questions` |
| The whole shape of the solution is unknown | `brainstorming` to update the spec first |
| Plan item has acceptance criteria but is large | Write the plan as a high-level draft and iterate |
| Scope is in the spec but not yet broken into tasks | Write the plan, then review |

If a single missing fact blocks the next step, invoke `asking-clarifying-questions` before guessing.

## Plan Lifecycle

Plans are durable, tracked files. The in-flight plan is the source of truth for the work, not a transient scratch note.

- **In-flight home:** `.agents/plans/YYYY-MM-DD-<feature-name>.md` (or `.agents/plans/<epic-name>/YYYY-MM-DD-<feature-name>.md` for epic plans). Off-repo scratch is for transient session artifacts only; the plan itself always lives in the in-flight plan home.
- **Commit before handoff:** A plan must exist and be committed before it can be handed to `executing-plans` or `subagent-driven-development`. Execution skills read the saved, committed file, not unsaved editor state.
- **Completion:** When the work is complete, promote enduring decisions to ADRs or current doctrine, then remove the finished plan and associated planning artifacts from Git. A convenience copy may live in the consumer's central completed-artifact scratch store but is disposable and not evidence.
- **Roadmap and index links:** Remove links to completed artifacts rather than maintaining a completed-artifact index. See the consumer's completion runbook for its removal sequence.

## File Structure

Before defining tasks, map out which files will be created or modified and what each one is responsible for. This is where decomposition decisions get locked in.

- Design units with clear boundaries and well-defined interfaces. Each file should have one clear responsibility.
- You reason best about code you can hold in context at once, and your edits are more reliable when files are focused. Prefer smaller, focused files over large ones that do too much.
- Files that change together should live together. Split by responsibility, not by technical layer.
- In existing codebases, follow established patterns. If the codebase uses large files, don't unilaterally restructure - but if a file you're modifying has grown unwieldy, including a split in the plan is reasonable.

This structure informs the task decomposition. Each task should produce self-contained changes that make sense independently.

## Task Right-Sizing

A task is the smallest unit that carries its own test cycle and is worth a
fresh reviewer's gate. When drawing task boundaries: fold setup,
configuration, scaffolding, and documentation steps into the task whose
deliverable needs them; split only where a reviewer could meaningfully
reject one task while approving its neighbor. Each task ends with an
independently testable deliverable.

## Right-Sizing and Escape Hatches

If you think "this is a lot" or "the plan is huge" while writing, you are at a scope sizing decision point. **MUST READ:** `references/plan-scope-sizing.md` and follow one of the three escape hatches before continuing.

A long plan with well-sliced, independently testable tasks is not a problem. The `subagent-driven-development` execution lane is designed for that shape. A plan is too big only when it crosses independent concerns or one of its tasks cannot fit in one review cycle.

## Bite-Sized Task Granularity

**Each step is one action (2-5 minutes):**
- "Write the failing test" - step
- "Run it to make sure it fails" - step
- "Implement the minimal code to make the test pass" - step
- "Run the tests and make sure they pass" - step
- "Commit" - step

## Recipient-relative planning

Write each plan for the executor and stage that will receive it. Always make
the observable goal, exclusions, seams, invariants, interfaces, authority,
acceptance evidence, and task exits explicit. For Luna or lower-capability
executors, pre-resolve consequential alternatives, name exact evidence homes
and commands when known, and use finite decision tables where a choice would
otherwise be rediscovered during execution. Exact implementation code is
optional unless the code shape itself is the contract; specify behavior,
interfaces, and evidence rather than pretending pseudocode is a guarantee.

## Plan Document Header

**Every plan MUST start with this header:**

```markdown
# [Feature Name] Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** [One sentence describing what this builds]

**Architecture:** [2-3 sentences about approach]

**Tech Stack:** [Key technologies/libraries]

**Spec:** [path to the spec/design doc this plan implements — the plan
argues from the spec, so the spec travels with it; executors read both]

**Execution Strategy:** `subagent-driven-development` (default for independent tasks) — `executing-plans` (for tightly coupled/sequential tasks), `dispatching-parallel-agents` (for 2+ independent parallel tracks), or `manual` (for human-driven work). The planner picks the recommended lane.

## Global Constraints

[The spec's project-wide requirements — version floors, dependency limits,
naming and copy rules, platform requirements — one line each, with exact
values copied verbatim from the spec. Every task's requirements implicitly
include this section.]

---
```

## Task Structure

````markdown
### Task N: [Component Name]

**Files:**
- Create: `exact/path/to/file.py`
- Modify: `exact/path/to/existing.py:123-145`
- Test: `tests/exact/path/to/test.py`

**Interfaces:**
- Consumes: [what this task uses from earlier tasks — exact signatures]
- Produces: [what later tasks rely on — exact function names, parameter
  and return types. A task's implementer sees only their own task; this
  block is how they learn the names and types neighboring tasks use.]

- [ ] **Step 1: Write the failing test**

```python
def test_specific_behavior():
    result = function(input)
    assert result == expected
```

- [ ] **Step 2: Run test to verify it fails**

Run: `pytest tests/path/test.py::test_name -v`
Expected: FAIL with "function not defined"

- [ ] **Step 3: Write minimal implementation**

```python
def function(input):
    return expected
```

- [ ] **Step 4: Run test to verify it passes**

Run: `pytest tests/path/test.py::test_name -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add tests/path/test.py src/path/file.py
git commit -m "feat: add specific feature"
```
````

## No Placeholders

Every step must contain the actual content an engineer needs. These are **plan failures** — never write them:
- "TBD", "TODO", "implement later", "fill in details"
- "Add appropriate error handling" / "add validation" / "handle edge cases"
- "Write tests for the above" (without actual test code)
- "Similar to Task N" (repeat the code — the engineer may be reading tasks out of order)
- Steps that describe what to do without showing how (code blocks required for code steps)
- References to types, functions, or methods not defined in any task

## Self-review & plan-readiness gate

After writing the complete plan, look at the spec with fresh eyes and check the plan against it. Then use `handoff-gates` `plan-readiness` lane as the plan-readiness gate. These are the same step: the self-review produces the plan, and the readiness gate rates it.

**1. Spec coverage:** Skim each section/requirement in the spec. Can you point to a task that implements it? List any gaps.

**2. Placeholder scan:** Search your plan for red flags — any of the patterns from the "No Placeholders" section above. Fix them.

**3. Type consistency:** Do the types, method signatures, and property names you used in later tasks match what you defined in earlier tasks? A function called `clearLayers()` in Task 3 but `clearFullLayers()` in Task 7 is a bug.

**5. Plan Size Check:** Did you think the plan was too large while writing? If yes, did you apply one of the escape hatches in `references/plan-scope-sizing.md`? Is the `Execution Strategy` field filled with an allowed value and a clear rationale?

**4. Plan-readiness rating:** Use the `handoff-gates` `plan-readiness` lane.
Rate the plan for execution confidence (8/10 floor, 9/10 target), report the
rating in the current handoff, and do not persist it or execute below 8/10.

If you find issues during the self-review, fix them inline and re-run the plan-readiness gate. If you find a spec requirement with no task, add the task.

## Execution Handoff

After the saved plan meets the readiness floor, read the `Execution Strategy` and present it to the user:

> "Plan complete and saved to `.agents/plans/<filename>.md`. The `Execution Strategy` is `<strategy>`. The plan-readiness rating is `<X>/10`.
> Do you want to proceed with the recommended strategy, or switch to another lane?"

If the user chooses a different lane, note it in the handoff and let the executing skill handle the override. Do not re-derive the whole plan from scratch.
