# BUNCH-118 SDD Progress Ledger

Plan: `.agents/superpowers/plans/2026-07-01-bunch-118-fix-infrastructure-and-minor-issues.md`
Branch: `harleydbartles/bunch-118-fix-infrastructure-and-minor-issues`
Merge base: `9ed4a69` (origin/main)

## Tasks

- [x] Task 1: Broaden dev CORS to allow any localhost port
- [x] Task 2a: Add FeatureLanguage, FeatureDescriptor, FeatureLanguageService
- [x] Task 2b: Migrate SuspectIdentityFact to carry FeatureLanguage
- [x] Task 2c: Update CaseSuspectFeaturePool to store FeatureLanguage
- [x] Task 2d: Eliminate NormalizeFeatureDescriptor from SaloonPersonOfInterestDescriptor
- [x] Task 2e: Verify snapshot shape break + dev reset
- [x] Task 3: Disambiguate duplicate Horse feed display names by vendor

## Completion Log

Task 1: complete (commits b07bc9c..8b9de5f, review clean — Approved, no Critical/Important)
Task 2a: complete (commits 8b9de5f..c2e0580, review clean — Approved, 1 Minor: untested Raw fallback inherited from plan)
Task 2b: complete (commits c2e0580..1ad16a2, review clean — Approved, 1 Minor: SeedCaseBuilder WithForm placeholder deferred to 2c)
Task 2c: complete (commits 1ad16a2..5f9110a, review clean — Approved, no issues)
Task 2d: complete (commits 5f9110a..ec7dfe4, review clean — Approved, no issues, bug fixed)
Task 2e: complete (no commit — pure verification: break real, no shim, no schema drift, 169 integration tests pass)
Task 3: complete (commits ec7dfe4..b2bac6c, review clean — Approved, no issues)

## Final whole-branch review
Branch quality: Approved. No Critical/Important. 4 Minor findings:
1. FIXED (commit 10446d7): Stale doc comment on CitizenCast.ResolveDescriptor referencing old normalization shared with SaloonPersonOfInterestDescriptor.
2. Deferred: FeatureLanguage.Raw default whoForm includes trailing punctuation (latent — all 26 callers pass whoForm explicitly).
3. Deferred: NodFeature FamilyKey hardcoded bodyPart→key map with silent fallthrough to "brow" (only 4 categories today).
4. Deferred: no-eyebrows has FeatureCategory.Absence but retains MissingPart tag (semantic, no functional impact).
