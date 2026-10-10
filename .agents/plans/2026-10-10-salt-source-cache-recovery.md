# Salt Source Cache Recovery From World Generation Facts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** A valid current-version `saltSource` component that contradicts the immutable `WorldGenerated` fact must not change the salt used by later game actions.

**Architecture:** `WorldGenerated.SaltSource` is the event-established salt source. The `saltSource` component is a rebuildable cache. On command aggregate load, compare the decoded cache with the single generated fact; a mismatch uses the existing full event replay path. The player and journal read models do not consume this component. Event decoding and replay failures remain fail-closed. Reads do not write back, and a later ordinary legal command save repairs the cache.

**Tech Stack:** .NET 10, C#, EF Core, PostgreSQL, xUnit.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, event history/cache recovery contract; row 07 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; PS-05 in `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md` and the persistence test follow-up.

**Execution Strategy:** Use `executing-plans` inline and sequentially. First create a real PostgreSQL session with an event-established SaltSource, change only its valid current-version cache value, and observe the current wrong value flow into a salted saloon action. Then add the smallest event-to-cache comparison and verify recovery plus ordinary-save repair. Do not validate or replay healthy state as part of normal operation.

## Global Constraints

- Start from `develop` merge `fb4be3b9e3f2059034ec1e98eae9f97d8901c717` in this fresh canonical worktree; target `develop`.
- Record PR #238 source `02e83d1bdd5ce5ab7c2d8354b06807418f68395f`, merge `fb4be3b9e3f2059034ec1e98eae9f97d8901c717`, exact-head gate `38018312652`, develop push gate `38018674609`, and delivered `0.1.0-dev.53`. Retire the `.53` plan, point row 07 to this plan, record PR #238 delivery and advance `Directory.Build.props` to `0.1.0-dev.54` in this planning commit.
- Compare the complete persisted SaltSource value, including mode and exact salt, with the sole `WorldGenerated.SaltSource` fact. Do not regenerate salt or draw random values during loading.
- On mismatch, recover through ordered supported events. Preserve fail-closed decoding, upcasting and replay behavior for malformed, missing, unknown or future-version history.
- Keep reads free of persistence writes. A later ordinary legal command save may repair the component from the replayed aggregate.
- Do not change event payloads, event schema version, upcasters, projection versions, database schema or migrations. Preserve ADR-0028; this is enforcement of its existing event-authority/cache-recovery decision.
- The read models do not consume SaltSource; do not add query-side loading or projection work solely to exercise this component.

## Review Focus

- A valid current-version cache can still be semantically false: the command aggregate must use the salt recorded by `WorldGenerated`.
- Prove the fixture's altered salt changes the next `SaloonPersonOfInterestSpotted` result so the PostgreSQL regression has a meaningful gameplay oracle.
- Reads preserve the altered component and event history; an ordinary legal save repairs only through the existing repository/unit-of-work path.
- Malformed authoritative WorldGenerated history must still fail before cache recovery.

---

### Task 1: Record PR #238 and commit the `.54` successor plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-10-world-cache-recovery-from-generated-facts.md`; create this plan.

- [ ] Confirm PR #238's exact source and merge SHAs and both hosted gate runs above.
- [ ] Record `.53` delivery and this selected PS-05 SaltSource-consistency slice in row 07 and the roadmap dated delivery history.
- [ ] Retire the completed `.53` plan and advance the single authored application version to `0.1.0-dev.54`.
- [ ] Commit the planning handoff before editing source or tests.

**Expected:** The roadmap points to this plan, records verified `.53` delivery, and this plan states a bounded implementation and proof contract.

### Task 2: Recover a current but event-inconsistent SaltSource cache

**Files:** Modify `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; create a focused recovery helper under `src/WildBunch.Persistence/GameSessions`; add a PostgreSQL behavior test to `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`.

- [ ] Create an event-backed session with a deterministic runtime SaltSource and persist it through the production repository.
- [ ] Demonstrate that the chosen altered but valid SaltSource leads to a different next saloon person-of-interest fact than the generated event salt.
- [ ] Change only the current-version `saltSource` component to that valid alternative; prove the current command loader uses the false salt and the new behavior assertion fails before production changes.
- [ ] Compare the decoded component with the sole `WorldGenerated.SaltSource`; classify mismatch as invalid required cache and route command loading through existing full replay.
- [ ] Assert the reloaded aggregate uses the exact recorded mode and salt, and the same saloon action produces the event-selected person; prove the read preserved component JSON/version, envelope watermarks and stored event metadata/payloads.
- [ ] Perform an ordinary legal command and unit-of-work save; confirm a fresh load retains the recorded salt and the current-version component now matches it.
- [ ] Corrupt authoritative WorldGenerated JSON while the component is mismatched and prove the event decode failure still escapes.

**Expected:** A valid but contradictory salt cache cannot affect the next salt-driven action, query behavior is unchanged, reads do not write back, and ordinary save repairs the cache from replayed state.

### Task 3: Verify, review and publish the slice

**Files:** All implementation and planning paths in Tasks 1-2.

- [ ] Run focused SaltSource cache recovery and adjacent replay/history tests with shared PostgreSQL ensured. Run `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`; no migration is expected.
- [ ] Review the complete branch against `develop`, event-sourcing doctrine, ADR-0028 and this plan; resolve all Critical and Important findings with witnessed RED/GREEN cycles. If reviewer-agent dispatch remains unavailable, disclose author self-review.
- [ ] Let the normal check-only pre-commit hook run the canonical staged-candidate gate for each implementation commit; do not rerun the full local gate immediately before or after a successful hooked commit. Hosted CI must pass on the exact PR head and on the develop merge commit.
- [ ] Push and open a PR to `develop`; verify hosted CI passes for the exact PR head before merge, merge as authorized by the goal, then verify the exact develop push gate passes.
- [ ] Record actual source/merge SHAs and CI evidence in the roadmap through the successor planning handoff, fast-forward the main checkout to merged `develop`, then remove this verified merged worktree and local/remote branch.

**Expected:** The PR merges to `develop` at `0.1.0-dev.54` with exact-head and merge hosted gates green; row 07 points to the next JIT outcome.
