# Nonnegative Money Invariants Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Enforce nonnegative wallet balances and bounty amounts across construction, persisted-event deserialization, settlement state, and projection inputs while preserving zero balances and the existing capped-fine behavior.

**Architecture:** `Wallet` owns the balance invariant, `WarrantTerms` owns bounty terms, and both `SheriffTurnInSettled` and `SheriffTurnInSettlementState` own the recorded settlement amount. A negative settlement fact is rejected when constructed or deserialized, before aggregate replay or independent event projectors can interpret it as a debit. Aggregate application also validates settlement state before changing cash. No event shape, command outcome, or persistence schema changes.

**Tech Stack:** C#/.NET 10, xUnit, Wild Bunch Domain, repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; row 16 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; DN-15 in `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`.

**Execution Strategy:** `executing-plans` inline. Constructor validation, event replay rejection, and the owning domain tests share one money-invariant boundary; splitting them would publish an incomplete contract, while per-task implementer handoffs would add overhead without independent workstreams. Use one fresh whole-branch review after the coupled slice is green.

## Global Constraints

- Keep `Directory.Build.props` as the sole authored application-version authority and advance exactly once to `0.1.0-dev.114` in the first substantive implementation commit.
- A wallet balance and bounty/settlement amount may equal zero but may not be negative.
- A negative `SheriffTurnInSettled.BountyAmount` is invalid history; it is not a fine or a wallet debit.
- Preserve the existing $10 mistaken-take-in fine, capped at available cash, and keep penalties separate from bounty credits.
- Keep event payloads and persistence schemas unchanged; do not add migrations or upcasters for this constructor/replay validation.
- Preserve the public `Wallet.Deconstruct(out decimal Cash)` API when making the constructor explicit.
- Preserve existing command, settlement, replay, and snapshot behavior for valid nonnegative values; do not expand into general price or decimal validation.
- Preserve the branch-collision preflight and paired worktree/local/upstream-ref closeout rules in the spec and roadmap.

## Review Focus

- Negative wallet construction, warrant bounty construction, and settlement-state construction must each fail at the owning domain boundary; zero remains a valid value for all three.
- Constructing or deserializing a negative bounty settlement must fail before aggregate replay or independent event projectors can apply it; existing positive bounty payout and capped-fine behavior remain unchanged.
- Snapshot restoration reaches the validated settlement and wallet constructors, and event payload shape remains unchanged; do not add a parallel persistence-only invariant.

---

### Task 1: Enforce nonnegative wallet and bounty facts

**Files:**
- Create: `tests/WildBunch.Domain.Tests/MoneyInvariantTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/GameSessionSheriffTurnInTests.cs`
- Create: `tests/WildBunch.Integration.Tests/SheriffTurnInSettledSerializerTests.cs`
- Modify: `src/WildBunch.Domain/Economy/Wallet.cs`
- Modify: `src/WildBunch.Domain/Cases/CaseWarrants.cs`
- Modify: `src/WildBunch.Domain/Cases/SheriffTurnInResult.cs`
- Modify: `src/WildBunch.Domain/Events/SheriffTurnInSettled.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `.agents/doctrine/gameplay-invariants.md`
- Modify: `docs/decisions/ADR-0026-turn-in-outcome-semantics-for-bounty-and-murder-case-separation.md`
- Modify: `docs/features.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-dn15-citizencast-hash-overflow.md`

**Interfaces:**
- Consumes: `Wallet(decimal cash)`, `WarrantTerms(decimal bountyAmount, ...)`, `SheriffTurnInSettlementState(..., decimal BountyAmount, ...)`, and `GameSession.RehydrateFromEvents(...)`.
- Produces: the same valid public construction and replay APIs, with negative balances and bounty facts rejected as `ArgumentOutOfRangeException` before aggregate replay or event projection can apply them.

- [x] Add `Wallet_RejectsNegativeCash`, `WarrantTerms_RejectsNegativeBounty`, and `SheriffTurnInSettlementState_RejectsNegativeBounty` in `MoneyInvariantTests.cs`. Each directly invokes the public constructor with `-1m` and asserts `ArgumentOutOfRangeException`.
- [x] Add `ZeroWalletAndBountyValuesRemainValid` in `MoneyInvariantTests.cs`. Construct a zero-cash `Wallet`, zero-bounty `WarrantTerms`, and zero-bounty `SheriffTurnInSettlementState`; assert each retains zero.
- [x] The initial implementation added and observed `RehydrateFromEvents_RejectsNegativeTurnInBounty`; the review correction below moved primary rejection to supported event construction/deserialization, then removed the now-unreachable invalid typed-event setup.
- [x] Add `Wallet_CanBeDeconstructed` and `SheriffTurnInSettled_RejectsNegativeBounty` to `MoneyInvariantTests.cs`; verify the first test fails because conversion removed `Deconstruct`, and the second fails because event construction accepts a negative bounty.
- [x] Add `DeserializeEvent_RejectsNegativeBountySettlement` in `SheriffTurnInSettledSerializerTests.cs`. Serialize a valid settlement, change only its persisted bounty field to a negative amount, and assert the production event serializer rejects it before projection rebuild.

```csharp
Assert.Throws<ArgumentOutOfRangeException>(() => new Wallet(-1m));
Assert.Throws<ArgumentOutOfRangeException>(() => new WarrantTerms(
    WarrantDisposition.DeadOrAlive,
    -1m,
    Array.Empty<string>(),
    Array.Empty<string>(),
    "Sheriff",
    InvestigationTargetKind.GangMember,
    Array.Empty<OutlawGangId>(),
    null));
Assert.Throws<ArgumentOutOfRangeException>(() => new SheriffTurnInSettlementState(
    new SuspectId("suspect-1"),
    "Mira Cline",
    WarrantDisposition.DeadOrAlive,
    IsAlive: true,
    BountyAmount: -1m,
    Day: 1,
    Turn: 1));

Assert.Throws<ArgumentOutOfRangeException>(() => new SheriffTurnInSettled
{
    TargetSuspectId = new SuspectId("suspect-1"),
    TargetName = "Mira Cline",
    Disposition = WarrantDisposition.DeadOrAlive,
    IsAlive = true,
    BountyAmount = -1m,
    Message = "Invalid negative bounty",
    Day = 1,
    Turn = 1
});
```

- [x] Run the three negative-constructor tests individually with `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~Wallet_RejectsNegativeCash"`, `--filter "FullyQualifiedName~WarrantTerms_RejectsNegativeBounty"`, and `--filter "FullyQualifiedName~SheriffTurnInSettlementState_RejectsNegativeBounty"`; each must fail because no exception is thrown.
- [x] Run the replay test alone with `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~RehydrateFromEvents_RejectsNegativeTurnInBounty"`; it must fail because the current replay accepts the negative fact as a debit.
- [x] Run the new event, Wallet deconstruction and persisted-event tests before implementation; each failed for its intended reason (no exception, missing public method, or no exception on deserialization).
- [x] Convert `Wallet` to an explicit readonly record struct constructor that throws `ArgumentOutOfRangeException(nameof(cash))` when cash is below zero and assigns the immutable `Cash` property otherwise. Keep `Starting` delegating to the same validated constructor and leave the default struct value at zero.
- [x] Add `bountyAmount < 0` validation to `WarrantTerms` before assigning `BountyAmount`, throwing `ArgumentOutOfRangeException(nameof(bountyAmount))` while preserving zero.
- [x] Validate `SheriffTurnInSettlementState.BountyAmount` at construction while preserving its existing positional property names, record equality, and call sites. In `GameSession.Apply(SheriffTurnInSettled)`, construct this validated state before calling `Player.AdjustCash`, so malformed event history fails before any wallet change.
- [x] Validate `SheriffTurnInSettled.BountyAmount` in its init accessor so object construction and the production JSON event deserializer reject the invalid fact before any direct event projection can consume it. Preserve the serialized property and payload shape.
- [x] Restore `Wallet.Deconstruct(out decimal Cash)` and verify it returns the validated balance.

```csharp
if (cash < 0)
{
    throw new ArgumentOutOfRangeException(nameof(cash));
}
```

- [x] Run the four negative tests and `ZeroWalletAndBountyValuesRemainValid`; confirm the targeted behaviors pass and valid zero values remain accepted.
- [x] Run `Wallet_CanBeDeconstructed` and the production serializer rejection test; confirm the API and persisted-event behaviors pass.
- [x] Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`; confirm the complete Domain suite passes, including existing replay, payout, and fine-cap tests.
- [x] Add a dated baseline clarification and gameplay invariant for nonnegative wallet balances and bounty/settlement amounts, with zero legal and fines remaining separate capped penalties.
- [x] Add a dated editorial clarification to ADR-0026 recording that a bounty is a nonnegative credit and a negative settlement fact is invalid history; preserve the original decision and catalogue summary because the existing distinction remains in force.
- [x] Update PG-006 evidence to report constructor and replay rejection tests without claiming ordinary content previously produced negative values or changing the feature promise.
- [x] Add a dated partial DN-15 disposition for Wallet, WarrantTerms, and settlement-state input validation; keep the unrelated PathSegment/layout input observations open and do not claim they are ordinary-player failures.
- [x] Retire the `.113` CitizenCast plan and update row 16 to this `.114` plan. Record PR #298 reviewed head `2e77687682b91d54937f99750049fa7884245284`, squash merge `d066338f76be31053a6573850c9b0d9a70d36d35`, fresh whole-branch approval, exact-head hosted gate run `38250586345`, and verified clean worktree/local/upstream-ref removal. State that the branch head was not an ancestor after squash and that the merge result is present in develop.
- [x] Keep the branch-collision preflight and paired worktree/branch cleanup additions in the spec and roadmap in this same delivery.
- [x] Change `Directory.Build.props` exactly once from `0.1.0-dev.113` to `0.1.0-dev.114`; do not update it in the plan-only commit.
- [x] Stage the implementation, behavior tests, dated dispositions, ADR/spec/doctrine/feature/roadmap updates, `.114` version and `.113` plan retirement together; inspect the staged diff, commit once, and let the check-only pre-commit hook run the canonical fail-fast gate. Do not use `--no-verify` or run the complete gate redundantly immediately before or after a successful hooked commit.

**Review correction, 2026-10-12:** Whole-branch review showed that HUD and travel-diary projectors consume event lists independently of aggregate replay. The initial replay test exercised aggregate validation but did not protect these direct projection inputs. Reject the invalid fact at its event init boundary and prove persisted JSON rejection through the production serializer; this makes invalid typed events unavailable to either projector. The review also caught that an explicit non-positional `Wallet` removed the generated deconstruction API, so preserve and test it. The earlier direct negative-event replay test was retired because supported construction and persisted deserialization now reject the event before replay; aggregate settlement validation remains defense in depth.

**Acceptance:** Negative wallet, bounty-term, settlement-event, settlement-state and persisted-event facts are rejected before aggregate replay or independent projection can apply them; zero and all existing lawful positive flows remain valid. Wallet deconstruction compatibility is preserved. No event payload/schema migration is introduced, DN-15 is updated with bounded evidence, PR #298 is reconciled in the roadmap, the completed `.113` plan is retired, and the merged implementation PR targets `develop` at `0.1.0-dev.114`.

**Validation:** The focused xUnit tests and full Domain suite must pass before the implementation commit. The staged pre-commit hook must pass the complete repository gate. A fresh independent whole-branch review and exact-head hosted CI are required before merge.

**Closeout:** Verify the reviewed PR head is the branch tip, exact-head hosted CI is green, and its squash merge is contained in `develop`. Fast-forward the primary checkout, remove the clean worktree first, then delete and verify the plan's local and upstream refs; confirm all three are absent before starting another plan.
