# Path Segment Bounds Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every public `PathSegment` construction and copy-update enforce the documented inclusive 0–100 logical coordinate bounds while preserving its positional record API.

**Architecture:** `PathSegment` is a Domain value object whose four coordinates describe positions on the town-hub logical surface. Validate through its public init properties so direct construction and `with` updates cannot bypass the existing bounds contract; keep `Create` as the same supported convenience entry point. No API DTO, persistence shape, gameplay rule, or generated town layout changes.

**Tech Stack:** C#/.NET 10, xUnit, Wild Bunch Domain, repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; DN-15 in `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`; row 16 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`.

**Execution Strategy:** `executing-plans` inline. One Domain value contract and its behavior tests form a tightly coupled change; a fresh whole-branch review is the useful independent boundary, while per-task handoffs add context cost without independent work.

## Global Constraints

- Keep `Directory.Build.props` as the sole authored application-version authority and advance exactly once to `0.1.0-dev.115` in the first substantive implementation commit; do not change it in the plan-only commit.
- Every `PathSegment` coordinate remains within the inclusive logical range 0–100, as already documented by the value type and TownLayout DTO.
- Preserve `PathSegment.Create`, positional construction, record equality, generated `Deconstruct`, and JSON-visible property names and values.
- Reject invalid values through both the public constructor and `with` copy updates; keep all four boundary values 0 and 100 legal.
- Do not expand this slice to `BuildingPlacement`, `TownLayout`, mutable grids, or other conditional DN-15 observations without a supported contract and concrete owner evidence.
- Preserve the current branch-collision preflight and paired worktree/local/upstream-ref cleanup order in the roadmap and spec.
- The first substantive PR commit retires the completed `.114` plan, updates the row-16 roadmap with PR #299 evidence, updates DN-15 with the bounded PathSegment disposition, and advances the version together with the implementation and tests.

## Review Focus

- Each coordinate rejects both an underflow and overflow through direct construction, while every edge boundary remains legal.
- A `with` update cannot create an invalid PathSegment, and valid record equality, deconstruction, `Create`, and serialized property shape remain intact.
- Existing layouts and consumers remain unchanged because generated production paths are empty and DTO shape does not change; do not claim this closes the separate `BuildingPlacement` or `TownLayout` observations.

---

### Task 1: Enforce PathSegment bounds at the value boundary

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/World/PathSegmentTests.cs`
- Modify: `src/WildBunch.Domain/World/PathSegment.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-dn15-nonnegative-money.md`

**Interfaces:**
- Consumes: `PathSegment(int StartX, int StartY, int EndX, int EndY)`, `PathSegment.Create(...)`, and the existing TownLayout DTO contract.
- Produces: the same positional record API with all four coordinates validated at construction and on init assignment.

- [ ] Add a theory named `PathSegment_DirectConstructionRejectsCoordinatesOutsideLogicalSurface`, with eight cases covering `-1` and `101` for each of `StartX`, `StartY`, `EndX`, and `EndY`; construct through `new PathSegment(...)` and assert `ArgumentOutOfRangeException`.
- [ ] Run only that new theory and confirm it fails because direct construction currently accepts invalid coordinates.
- [ ] Add `PathSegment_WithUpdateRejectsCoordinateOutsideLogicalSurface`, using a valid segment and a `with` update setting one coordinate to `101`; assert `ArgumentOutOfRangeException` so the positional record's init surface cannot bypass validation.
- [ ] Run the new `with` test and confirm it fails because the current generated init setter accepts the invalid value.
- [ ] Implement validation on the four positional record properties' `init` accessors with one shared helper, throwing `ArgumentOutOfRangeException` outside the inclusive 0–100 range. Keep the primary positional record parameters and generated deconstruction/equality; retain `Create` as a delegating constructor call without duplicate validation logic. The property form is:

```csharp
private int _startX;

public int StartX
{
    get => _startX;
    init => _startX = ValidateCoordinate(value, nameof(StartX));
}

private static int ValidateCoordinate(int value, string parameterName)
{
    if (value is < MinCoordinate or > MaxCoordinate)
    {
        throw new ArgumentOutOfRangeException(parameterName);
    }

    return value;
}
```

- [ ] Keep the same validated-property form for `StartY`, `EndX`, and `EndY`, each with its own backing field and property name in the exception.
- [ ] Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~PathSegmentTests`; confirm direct construction, `with`, `Create`, valid boundaries, and existing factory rejection tests pass.
- [ ] Add `PathSegment_RoundTripsThroughWebJson` to the focused test, serializing and deserializing a valid segment with `new JsonSerializerOptions(JsonSerializerDefaults.Web)` and asserting record equality. In the same test, deconstruct a constructed segment and assert its four coordinates; this proves the public positional contract remains usable while the DTO/persistence property names continue to serialize through the supported web defaults.
- [ ] Update DN-15 with a dated disposition stating that direct construction and `with` updates now enforce the existing documented coordinate bounds. Explicitly leave the separate `BuildingPlacement`, `TownLayout`, and grid observations open because this slice does not establish their complete valid-input contract.
- [ ] Replace the current row-16 active-plan link with this `.115` plan, record PR #299 reviewed head `2ce4f3873a737ee4d454cac1b52f2d27ef96bdd3`, squash merge `5ded1f96f2d2556be5f3c08345749a58fdd1182a`, fresh whole-branch and follow-up review outcomes, and exact-head hosted gate run `38258396963` success. Record that primary develop fast-forwarded to the merge, worktree `codex/row16-dn15-nonnegative-money` was removed before local and upstream refs, and the worktree plus both refs were verified absent.
- [ ] Preserve the existing paired stale-branch/worktree cleanup rule and branch-collision preflight in the spec and roadmap; do not introduce an unrelated branch-maintenance project.
- [ ] Change `Directory.Build.props` exactly once from `0.1.0-dev.114` to `0.1.0-dev.115` in this first substantive implementation commit.
- [ ] Stage the source, behavioral tests, DN-15/roadmap changes, `.115` version, and `.114` plan retirement together; inspect the staged diff and commit once. Let the normal check-only pre-commit hook run the canonical fail-fast gate; do not use `--no-verify` or run the complete gate redundantly immediately before or after a successful hooked commit.

**Acceptance:** Direct construction and record copy-update reject every coordinate outside 0–100, boundary values remain legal, and the positional record contract remains compatible. Existing generated layouts and DTO consumers remain unchanged; the DN-15 evidence and roadmap record only the bounded work actually delivered, the `.114` plan is retired, and the branch cleanup policy remains explicit.

**Validation:** Observe red for the new direct-constructor and copy-update behaviors, then green for the focused Domain suite. The staged pre-commit hook, fresh whole-branch review, and exact-head hosted CI must pass before merge to `develop`.

**Closeout:** Verify the reviewed PR head is the branch tip, exact-head hosted CI is green, and the squash merge is contained in `develop`. Fast-forward the primary checkout, remove this clean worktree first, then delete and verify the local and upstream branch refs; confirm the worktree and both refs are absent before starting another plan.

The direct-construction theory uses these boundary failures:

```csharp
[Theory]
[InlineData(-1, 50, 50, 50)]
[InlineData(101, 50, 50, 50)]
[InlineData(50, -1, 50, 50)]
[InlineData(50, 101, 50, 50)]
[InlineData(50, 50, -1, 50)]
[InlineData(50, 50, 101, 50)]
[InlineData(50, 50, 50, -1)]
[InlineData(50, 50, 50, 101)]
public void PathSegment_DirectConstructionRejectsCoordinatesOutsideLogicalSurface(
    int startX, int startY, int endX, int endY)
{
    Assert.Throws<ArgumentOutOfRangeException>(() => new PathSegment(startX, startY, endX, endY));
}

[Fact]
public void PathSegment_WithUpdateRejectsCoordinateOutsideLogicalSurface()
{
    var segment = new PathSegment(10, 20, 30, 40);

    Assert.Throws<ArgumentOutOfRangeException>(() => segment with { StartX = 101 });
}
```
