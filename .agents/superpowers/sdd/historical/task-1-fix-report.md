# Task 1 Fix Report

## Issues Fixed

### Critical Issue: `ClAnchors` Typo (Line 153)
- **Status**: Already fixed in prior commit
- **Finding**: The review mentioned a typo where `public ClAnchors ToDomain()` should be `public ClueAnchors ToDomain()`
- **Current State**: The current file at line 137 correctly shows `public ClueAnchors ToDomain()` - the typo was already corrected in a subsequent commit after the original Task 1 implementation
- **Action**: No action required

### Important Issue: `SuspectIdentityFact.IsPrimary` Data Loss (Line 101)
- **Status**: Fixed
- **Finding**: `SuspectIdentityFactSnapshot.ToDomain()` was hardcoding `IsPrimary = true`, losing the original fact's `IsPrimary` value
- **Root Cause**: The snapshot record didn't include the `IsPrimary` field
- **Fix Applied**:
  - Added `bool IsPrimary` field to `SuspectIdentityFactSnapshot` record (line 79)
  - Updated `FromDomain` to serialize `fact.IsPrimary` (line 82)
  - Updated `ToDomain` to pass through `IsPrimary` instead of hardcoding `true` (line 85)
- **File Changed**: `src/WildBunch.Domain/Cases/CaseFileSnapshot.cs`

## Test Results

### Build
- **Status**: Success
- **Warnings**: 2 (pre-existing, unrelated to changes)
- **Errors**: 0

### Unit Tests
- **WildBunch.GameContent.Tests**: 139 passed, 0 failed
- **WildBunch.Domain.Tests**: 515 passed, 0 failed
- **WildBunch.Application.Tests**: 204 passed, 0 failed
- **Total**: 858 unit tests passed

### Integration Tests
- **Status**: Failures due to missing PostgreSQL connection strings
- **Note**: These are test infrastructure issues, not related to the CaseFileSnapshot changes
- **Error**: `System.InvalidOperationException : Set ConnectionStrings__WildBunchPostgresDb to run the PostgreSQL test lane.`

## Summary

Successfully fixed the `IsPrimary` data loss issue in `SuspectIdentityFactSnapshot`. The critical typo issue was already resolved in a prior commit. All relevant unit tests pass with the fix applied.

## Files Changed

- `src/WildBunch.Domain/Cases/CaseFileSnapshot.cs` (3 insertions, 3 deletions)

## Commit

- **Commit Hash**: 4a4337b
- **Branch**: bunch-134-geometry-first-map-generation-v2
- **Message**: "Fix CaseFileSnapshot: preserve SuspectIdentityFact.IsPrimary in snapshot"
