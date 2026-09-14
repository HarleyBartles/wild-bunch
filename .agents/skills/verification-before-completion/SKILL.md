---
name: verification-before-completion
description: Use when a claim that work is complete, fixed, passing, or ready needs
  current evidence before a commit, pull request, or handoff.
metadata:
  source-id: verification-before-completion
  source-path: codex-marketplace/plugins/superpowers-plus/skills/verification-before-completion/SKILL.md
  provenance-name: Verification Before Completion first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  use_when:
  - about to claim work is complete, fixed, or passing, before committing
    or creating PRs.
  - a verification command can prove the claim.
  - a completion claim should be backed by fresh evidence.
  do_not_use_when:
  - no verification command exists for the claim.
  - to override fresh evidence with confidence.
  - a substitute for running the actual verification.
  related_skills:
  - executing-plans
  - subagent-driven-development
  - requesting-code-review
  - finishing-a-development-branch
  - test-driven-development
license: MIT
---
## Provenance

This marketplace-maintained derivative is based on `obra/superpowers` v6.3.0 commit `b36e0829c6d0140e93cfef2ca599b1b07d4a7797` under the MIT License. Upstream source is not vendored; this directory contains the maintained Superpowers+ implementation.

# Verification Before Completion

## Overview

**Core principle:** Evidence before claims, always.

**Violating the letter of this rule is violating the spirit of this rule.**

## The Iron Law

```
NO COMPLETION CLAIMS WITHOUT STATE-BOUND VERIFICATION EVIDENCE
```

Evidence is fresh when it proves the same tested state (tree/head/staged
state),
command and scope, relevant environment, and claim. A conversation turn is
not a state identifier. Reuse unchanged proof; repeat only for a changed
state, failure, unresolved concern, nondeterminism, environment drift, or a
different proof claim.

## The Gate Function

```
BEFORE claiming any status or expressing satisfaction:

1. IDENTIFY: What command proves this claim?
2. CHECK STATE: Record the tested tree/head/staged state, relevant environment,
   command scope, and whether existing evidence still covers the claim
3. RUN OR REUSE: Execute the named command when state changed or evidence is
   stale; otherwise reuse the unchanged proof
4. READ: Full output, check exit code, count failures
5. VERIFY: Does output confirm the claim?
   - If NO: State actual status with evidence
   - If YES: State claim WITH evidence
6. ONLY THEN: Make the claim

Skip any step = lying, not verifying
```

## Common Failures

| Claim | Requires | Not Sufficient |
|-------|----------|----------------|
| Tests pass | Test command output: 0 failures | Previous run, "should pass" |
| Linter clean | Linter output: 0 errors | Partial check, extrapolation |
| Build succeeds | Build command: exit 0 | Linter passing, logs look good |
| Bug fixed | Test original symptom: passes | Code changed, assumed fixed |
| Regression test works | Red-green cycle verified | Test passes once |
| Agent completed | VCS diff shows changes | Agent reports "success" |
| Requirements met | Line-by-line checklist | Tests passing |

## Red Flags - STOP

- Using "should", "probably", "seems to"
- Expressing satisfaction before verification ("Great!", "Perfect!", "Done!", etc.)
- About to commit/push/PR without verification
- Trusting agent success reports
- Relying on partial verification
- Thinking "just this once"
- Tired and wanting work over
- **ANY wording implying success without having run verification**

## Rationalization Prevention

| Excuse | Reality |
|--------|---------|
| "Should work now" | RUN the verification |
| "I'm confident" | Confidence ≠ evidence |
| "Just this once" | No exceptions |
| "Linter passed" | Linter ≠ compiler |
| "Agent said success" | Verify independently |
| "I'm tired" | Exhaustion ≠ excuse |
| "Partial check is enough" | Partial proves nothing |
| "Different words so rule doesn't apply" | Spirit over letter |

## Key Patterns

**Tests:**
```
✅ [Run test command] [See: 34/34 pass] "All tests pass"
❌ "Should pass now" / "Looks correct"
```

**Regression tests (TDD Red-Green):**
```
✅ Write → Run (pass) → Revert fix → Run (MUST FAIL) → Restore → Run (pass)
❌ "I've written a regression test" (without red-green verification)
```

**Build:**
```
✅ [Run build] [See: exit 0] "Build passes"
❌ "Linter passed" (linter doesn't check compilation)
```

**Requirements:**
```
✅ Re-read plan → Create checklist → Verify each → Report gaps or completion
❌ "Tests pass, phase complete"
```

**Agent delegation:**
```
✅ Agent reports success → Check VCS diff → Verify changes → Report actual state
❌ Trust agent report
```

## When To Apply

**ALWAYS before:**
- ANY variation of success/completion claims
- ANY expression of satisfaction
- ANY positive statement about work state
- Committing, PR creation, task completion
- Moving to next task
- Delegating to agents

**Rule applies to:**
- Exact phrases
- Paraphrases and synonyms
- Implications of success
- ANY communication suggesting completion/correctness
