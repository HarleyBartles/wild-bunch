# BUNCH-72 Bounty Loop Aggregate Candidate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce a single internal bounty-loop candidate inside `GameSession` that owns the saloon -> confrontation -> sheriff-turn-in decision slice without changing public behavior or persistence shape.

**Architecture:** Keep `GameSession` as the only externally loaded and persisted aggregate root. Extract the smallest coherent internal coordinator for bounty-loop decisions and result shaping, keep `CaseFile`, `TownVisitState`, `WantedSuspectPresenceLedger`, `Player`, `BountyDeclarationMatchPolicy`, and `BountySettlementPolicy` as explicit owners of their current state and policy concerns, and preserve all public messages and DTO shapes.

**Tech Stack:** C# / .NET, xUnit, existing Wild Bunch domain tests.

---

### Task 1: Characterize the current bounty-loop seam

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/GameSessionSheriffTurnInTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/GameSessionSaloonWantedSuspectLoopTests.cs`

- [ ] **Step 1: Add one focused regression around the saloon-to-sheriff handoff**

Add or tighten a test that proves the armed wanted path still performs the payout, clears the saloon target, and rejects a duplicate sheriff payout.

- [ ] **Step 2: Add one focused regression around the sheriff-turn-in guard**

Ensure `AssessSheriffTurnIn` still rejects unsecured, wrong-target, and duplicate-settlement cases exactly as the live API expects.

- [ ] **Step 3: Run the touched domain tests and confirm the current behavior baseline**

Run:
`dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~GameSessionSaloonPersonOfInterestTests|FullyQualifiedName~GameSessionSaloonWantedSuspectLoopTests|FullyQualifiedName~GameSessionSheriffTurnInTests"`

Expected: PASS on current `main`.

### Task 2: Extract the internal bounty-loop candidate

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`

- [ ] **Step 1: Move the saloon/confrontation/sheriff decision branch into one internal helper**

Add a small private nested coordinator or helper inside `GameSession` that accepts the current session state and returns the same public result objects `GameSession` already returns.

- [ ] **Step 2: Keep ownership boundaries explicit**

Leave hidden case truth in `CaseFile`, active town state in `TownVisitState` / `TownVisitTownState`, presence state in `WantedSuspectPresenceLedger`, player cash/state in `Player`, and policy decisions in `BountyDeclarationMatchPolicy` and `BountySettlementPolicy`.

- [ ] **Step 3: Re-run the focused tests**

Run the same filtered `dotnet test` command and confirm the public behavior did not change.

### Task 3: Validate and capture return evidence

**Files:**
- No source changes expected

- [ ] **Step 1: Run repository validation**

Run `dotnet build` and `dotnet test`.

- [ ] **Step 2: Run PostgreSQL-backed validation if needed**

If the persistence or integration lane is touched, run `.\scripts\postgres-dev.ps1 validate`.

- [ ] **Step 3: Record branch and publication evidence**

Capture branch name, head commit hash, remote head hash, PR URL, changed files, and cleanup status for the final handoff.

## Self-Review

**Spec coverage:** The plan covers the requested internal bounty-loop candidate extraction, keeps `GameSession` as the root aggregate, and preserves the existing saloon/confrontation/sheriff behavior.

**Placeholder scan:** No TBDs or vague follow-up steps remain.

**Type consistency:** The plan keeps the current public API names and only introduces an internal helper inside `GameSession`.
