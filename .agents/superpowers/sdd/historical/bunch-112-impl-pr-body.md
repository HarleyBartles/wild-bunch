## Summary
- Decomposes the god GameSession aggregate by extracting a real BountyLoop child domain component that owns bounty-loop state and behavior through narrow context-record inputs and explicit results+events.
- BountyLoop owns: WantedSuspectPresenceLedger, UnrelatedCriminalLedger, DevSaloonOverride state. It does NOT reference GameSession, produce events, enter action context, adjust cash, or mutate CaseFile/TownVisitState/Player.
- GameSession retains: public command entry points (guards + orchestration), EnterActionContext, ProduceEvent, Apply dispatch (calling BountyLoop.Apply for owned state + applying cross-owner mutations itself), and the persistence boundary.
- No public API, DTO, event payload, message string, or snapshot shape changes. JSON snapshot persistence preserved.

## Falsification checks
- BountyLoop does NOT reference GameSession: confirmed zero code matches (all references are in doc-comments only)
- BountyLoop does NOT call ProduceEvent/EnterActionContext/Player.AdjustCash/CaseFile.Record*/RecordCaseUpdate/CurrentTownVisit.Set*: confirmed zero code matches (all references are in comments only)
- GameSession no longer directly owns bounty-loop decision rules: confirmed — public method bodies are guard + context-build + _bountyLoop call + event production
- GameSession still controls guards, EnterActionContext, ProduceEvent, Apply dispatch, _version++: confirmed
- All bounty-loop tests pass with same counts as baseline: domain 475 passed/0 failed/0 skipped (same as baseline), integration 169 passed/0 failed/0 skipped

## Validation
- dotnet build WildBunch.sln — PASS (warnings: 9, all pre-existing)
- dotnet test WildBunch.Domain.Tests — PASS, passed: 475, failed: 0, skipped: 0
- dotnet test WildBunch.Integration.Tests (via postgres-dev.ps1 test) — PASS, passed: 169, failed: 0, skipped: 0
- GameSession.cs line count: 3556 (was 3588)
- BountyLoop.cs line count: 910
- BountyLoopCoordinator.cs: deleted
- Index mesh regenerated (changed: yes, Game/INDEX.md updated after file deletion)

#### Test plan
- [x] Full domain test suite green
- [x] Full integration test suite green (PostgreSQL lane)
- [x] Saloon confrontation acceptance tests green
- [x] Event-sourcing/replay equality tests green (proves Apply semantics unchanged)
- [x] Dev saloon override tests green
- [x] Unrelated criminal ledger persistence tests green

Generated with [Devin](https://devin.ai)
