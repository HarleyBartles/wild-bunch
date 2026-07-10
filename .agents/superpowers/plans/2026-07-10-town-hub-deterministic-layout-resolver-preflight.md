# Town Hub Deterministic Layout Resolver and Salt Controls - Preflight Plan

> **For agentic workers:** This is a preflight investigation plan. Do not proceed to full implementation without explicit approval.

**Goal:** Investigate current seams, confirm contract soundness, and propose salt split and versioning approach before full implementation.

**Architecture:** Confirm that the current `TownLayoutGenerator` → `TownLayout` → `TownLayoutDto` → `TownHubScene` flow is the right place to introduce versioned resolution and split salts.

**Tech Stack:** C#/.NET backend, Phaser/TypeScript frontend, existing deterministic seed system

## Global Constraints

- Keep change narrow to town-hub layout resolution and dev controls
- Do not broaden into unrelated world generation or UI polish
- Do not change approved asset custody or regenerate assets
- Assume prop sprites exist via prop-sprite asset ticket before this lands
- Preserve existing seed semantics: same seed means same world structure and services
- Preserve playthrough semantics: different salts may change town look but not functional identity

---

## Preflight Investigation Tasks

### Task 1: Confirm Current Layout Resolution Flow

**Files:**
- Read: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- Read: `src/WildBunch.Domain/World/TownLayout.cs`
- Read: `src/WildBunch.Application/Games/Models/TownLayoutDto.cs`
- Read: `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts`

**Questions to answer:**
1. Is the current `TownLayoutGenerator.GenerateLayout()` the single source of truth for layout resolution?
2. Does the frontend (`TownHubScene`) consume the resolved layout directly, or does it re-decide tile/sprite placement?
3. Are there any other code paths that generate or modify town layouts?

**Deliverable:** Write findings to scratch file `Z:\_agent-scratch\wild-bunch\bunch-147-town-hub-deterministic-layout-resolver\preflight-findings.md` with:
- Confirmation of single source of truth
- List of all layout generation/modification code paths
- Assessment of whether frontend re-decides anything

- [ ] **Step 1: Read current layout generation code**

Review `TownLayoutGenerator.cs` to understand the current resolution logic.

- [ ] **Step 2: Read domain and DTO contracts**

Review `TownLayout.cs` and `TownLayoutDto.cs` to understand the data contract.

- [ ] **Step 3: Read frontend consumption**

Review `TownHubScene.ts` to confirm it renders resolved layout directly.

- [ ] **Step 4: Search for other layout generation paths**

Search for any other code that generates or modifies `TownLayout` or `TownLayoutDto`.

- [ ] **Step 5: Document findings**

Write findings to scratch file with answers to the three questions.

---

### Task 2: Investigate Salt Usage and Split Requirements

**Files:**
- Read: `src/WildBunch.Domain/Game/SaltSource.cs`
- Read: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs` (salt usage)
- Read: `src/WildBunch.Application/Dev/Commands/ForceDevSaltSourceCommand.cs`
- Read: `src/WildBunch.Application/Dev/Commands/ClearDevSaltSourceCommand.cs`

**Questions to answer:**
1. How is the current `SaltSource` used in `TownLayoutGenerator`?
2. What specific layout concerns need separate salts (buildings, roads, dirt, props)?
3. Should salts be stored per-town or per-playthrough?
4. What is the current dev overlay surface for salt inspection?

**Deliverable:** Append to scratch file with:
- Current salt usage in layout generation
- Proposed salt split structure (what salts for what concerns)
- Storage location recommendation (per-town vs per-playthrough)
- Current dev overlay salt surface assessment

- [ ] **Step 1: Read current SaltSource implementation**

Review `SaltSource.cs` to understand the current single-salt structure.

- [ ] **Step 2: Analyze salt usage in layout generation**

Review `TownLayoutGenerator.cs` to see how salt is currently used.

- [ ] **Step 3: Review dev salt commands**

Review dev commands to understand current salt mutation surface.

- [ ] **Step 4: Propose salt split structure**

Design a structure for split salts (buildings, roads, dirt, props) that supports:
- Deterministic layout from `seed + split salts + resolver version`
- Dev overlay inspection and copying
- Per-playthrough storage

- [ ] **Step 5: Document findings**

Append to scratch file with salt split proposal and storage recommendation.

---

### Task 3: Design Versioned Resolver Approach

**Files:**
- Read: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- Read: `src/WildBunch.Domain/World/TownLayout.cs`

**Questions to answer:**
1. Should versioning be a field on `TownLayout` or a separate resolver version parameter?
2. Where should the resolver version be stored (in GameSession, in town data, or elsewhere)?
3. How does versioning interact with the existing seed system?
4. What is the migration path when resolver version changes?

**Deliverable:** Append to scratch file with:
- Versioning field placement recommendation
- Resolver version storage location
- Migration strategy for version changes
- Interaction with existing seed system

- [ ] **Step 1: Analyze current resolver signature**

Review `TownLayoutGenerator.GenerateLayout()` signature and parameters.

- [ ] **Step 2: Design versioning approach**

Design where and how to store resolver version, considering:
- Backend persistence location
- Frontend consumption
- Migration path

- [ ] **Step 3: Design resolver contract**

Design the new versioned resolver contract that takes `seed + split salts + resolver version`.

- [ ] **Step 4: Document findings**

Append to scratch file with versioning approach and resolver contract design.

---

### Task 4: Design Dev Overlay Surface for Grouped Salts

**Files:**
- Read: `.agents/docs/dev-overlay-doctrine.md`
- Read: `src/WildBunch.Web/src/dev/DevOverlay.tsx`
- Find and read: `src/WildBunch.Web/src/dev/DevPanelRegistry.ts` (if exists)

**Questions to answer:**
1. Should town layout salts be a new dev panel or part of an existing panel?
2. What is the minimal UI shape for inspecting and copying grouped salts?
3. Which panel should own town layout salts per dev-overlay doctrine?
4. How should salts be displayed (compact vs expanded)?

**Deliverable:** Append to scratch file with:
- Panel ownership recommendation (new panel vs existing)
- Minimal UI shape for salt inspection/copying
- Panel placement in dev overlay hierarchy
- Display format recommendation

- [ ] **Step 1: Review dev overlay doctrine**

Review dev-overlay-doctrine.md to understand panel ownership model.

- [ ] **Step 2: Review current dev overlay structure**

Review `DevOverlay.tsx` and panel registry to understand current panel structure.

- [ ] **Step 3: Design salt inspection UI**

Design minimal UI for:
- Displaying grouped layout salts
- Copying salt bundle
- Freezing salts for testing

- [ ] **Step 4: Determine panel ownership**

Apply dev-overlay doctrine to determine which panel should own town layout salts.

- [ ] **Step 5: Document findings**

Append to scratch file with dev overlay design recommendation.

---

### Task 5: Identify Missing Model Fields and DTO Changes

**Files:**
- Read: `src/WildBunch.Domain/World/TownLayout.cs`
- Read: `src/WildBunch.Application/Games/Models/TownLayoutDto.cs`
- Read: `src/WildBunch.Domain/Game/SaltSource.cs`

**Questions to answer:**
1. What new fields are needed on `TownLayout` for versioning?
2. What new fields are needed on `TownLayoutDto` for frontend consumption?
3. Does `SaltSource` need to be replaced with a split-salt structure?
4. Are there any missing fields for prop sprite support?

**Deliverable:** Append to scratch file with:
- List of new domain fields needed
- List of new DTO fields needed
- Proposed `SaltSource` replacement structure
- Any missing prop sprite fields

- [ ] **Step 1: Compare domain vs DTO**

Compare `TownLayout` and `TownLayoutDto` to identify gaps.

- [ ] **Step 2: Identify versioning fields**

Identify what versioning fields are needed on both domain and DTO.

- [ ] **Step 3: Design split-salt structure**

Design the replacement for `SaltSource` that supports split salts.

- [ ] **Step 4: Check prop sprite support**

Verify if any fields are needed for prop sprite support (per issue assumption).

- [ ] **Step 5: Document findings**

Append to scratch file with complete list of model changes needed.

---

### Task 6: Assess Test Coverage Requirements

**Files:**
- Read: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`
- Read: `.agents/docs/validation-policy.md`

**Questions to answer:**
1. What tests exist for current layout generation?
2. What new tests are needed for versioned resolver?
3. What new tests are needed for split salts?
4. How to test determinism with fixed seed + fixed salts?

**Deliverable:** Append to scratch file with:
- Current test coverage assessment
- List of new tests needed for versioning
- List of new tests needed for split salts
- Test strategy for determinism verification

- [ ] **Step 1: Review current tests**

Review `TownLayoutGeneratorTests.cs` to understand current test coverage.

- [ ] **Step 2: Review validation policy**

Review validation-policy.md to understand test requirements.

- [ ] **Step 3: Design versioning tests**

Design tests for:
- Resolver version changes
- Migration between versions
- Version field propagation

- [ ] **Step 4: Design split-salt tests**

Design tests for:
- Determinism with fixed split salts
- Salt variation while keeping seed constant
- Salt bundle serialization/deserialization

- [ ] **Step 5: Document findings**

Append to scratch file with test coverage requirements and test strategy.

---

### Task 7: Preflight Summary and Go/No-Go Recommendation

**Files:**
- Read: `Z:\_agent-scratch\wild-bunch\bunch-147-town-hub-deterministic-layout-resolver\preflight-findings.md`

**Deliverable:** Create preflight summary document with:
- Confirmed current seams and their soundness
- Proposed salt split structure
- Proposed versioning approach
- Proposed dev overlay design
- List of required model changes
- Test coverage requirements
- Go/No-Go recommendation for proceeding to full implementation plan
- Any risks or concerns identified

- [ ] **Step 1: Compile findings from all tasks**

Review all findings from Tasks 1-6 and compile into a coherent summary.

- [ ] **Step 2: Assess contract soundness**

Assess whether the current contract (layout generator → domain → DTO → frontend) is sound for the proposed changes.

- [ ] **Step 3: Identify risks**

Identify any technical or architectural risks with the proposed approach.

- [ ] **Step 4: Make Go/No-Go recommendation**

Recommend whether to proceed to full implementation plan or if the issue needs refinement.

- [ ] **Step 5: Update Linear with preflight results**

Update the Linear issue BUNCH-147 with preflight findings and recommendation.

---

## Preflight Completion Criteria

- [ ] All 7 investigation tasks completed
- [ ] Findings documented in scratch file
- [ ] Preflight summary created with Go/No-Go recommendation
- [ ] Linear issue updated with preflight results
- [ ] Decision made on whether to proceed to full implementation plan

## Next Steps After Preflight

If Go:
- Create full implementation plan based on preflight findings
- Design exact data structures for split salts
- Design exact resolver versioning scheme
- Design exact dev overlay panel implementation
- Proceed to implementation

If No-Go:
- Update Linear issue with blockers
- Recommend issue refinement or architectural changes
- Stop and await further guidance
