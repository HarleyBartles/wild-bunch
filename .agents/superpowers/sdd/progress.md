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
- [ ] Task 2e: Verify snapshot shape break + dev reset
- [ ] Task 3: Disambiguate duplicate Horse feed display names by vendor

## Completion Log

Task 1: complete (commits b07bc9c..8b9de5f, review clean — Approved, no Critical/Important)
Task 2a: complete (commits 8b9de5f..c2e0580, review clean — Approved, 1 Minor: untested Raw fallback inherited from plan)
Task 2b: complete (commits c2e0580..1ad16a2, review clean — Approved, 1 Minor: SeedCaseBuilder WithForm placeholder deferred to 2c)
Task 2c: complete (commits 1ad16a2..5f9110a, review clean — Approved, no issues)
Task 2d: complete (commits 5f9110a..<this commit>, review pending — NormalizeFeatureDescriptor eliminated, WithForm used directly, 4 new Domain tests + parity tests green, full suite 842 pass)
