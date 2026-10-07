# Stable 0.1.0 investigation

These documents are source assessments, test dispositions and working product decisions from the interactive cleanup investigation. They are not execution plans. Earlier unresolved proposals are qualified by later user decisions; the approved [baseline specification](../../specs/2026-10-07-stable-0.1.0-baseline.md) owns the programme boundary and the [cleanup roadmap](../../roadmaps/2026-10-07-stable-0.1.0-cleanup.md) allocates delivery. Implementation requires a subsequently accepted bounded plan, refreshed against live source.

## Repository and testing assessment

- [Investigation tracker and F findings](2026-10-06-stable-0.1.0-investigation.md).
- [ADR truth and historical dispositions](2026-10-06-adr-truth-investigation.md).
- [Documentation custody](2026-10-06-docs-custody-investigation.md).
- [Whole-test quality assessment](2026-10-06-test-quality-investigation.md) and [.NET file dispositions](2026-10-06-dotnet-test-dispositions.md).

## Layer findings and related test owners

- [Application findings](2026-10-07-application-layer-investigation.md) and [test follow-up](2026-10-07-application-test-followup.md).
- [Persistence findings](2026-10-07-persistence-layer-investigation.md) and [test follow-up](2026-10-07-persistence-test-followup.md).
- [Domain findings](2026-10-07-domain-layer-investigation.md) and [test follow-up](2026-10-07-domain-test-followup.md).
- [Web research criteria](2026-10-07-web-architecture-spike.md), [source findings](2026-10-07-web-layer-investigation.md) and [test follow-up](2026-10-07-web-test-followup.md).

## Feature discovery and working decisions

- [Working feature inventory and directed dependencies](2026-10-07-feature-inventory.md), awaiting reconciliation into the durable product matrix.
- [Hunt creation](2026-10-07-hunt-creation-contract.md) and [confirmed start-over lifecycle](2026-10-07-playthrough-lifecycle-contract.md).
- [Store and inventory boundaries](2026-10-07-store-and-inventory-boundaries.md).
- [Culprit identity and release](2026-10-07-culprit-identity-and-release-contract.md).
- [Saloon challenge and current versus future outcomes](2026-10-07-saloon-challenge-contract.md).
- [Unrelated-criminal dependency assessment](2026-10-07-unrelated-criminal-feature-boundary.md), [retirement job](2026-10-07-unrelated-criminal-removal.md) and [separate future addition](2026-10-07-unrelated-criminal-addition.md).

## Use and custody

Read the relevant complete findings and test dispositions when shaping a bounded slice; do not paste every audit into every plan. Preserve finding IDs, corrections and historical uncertainty. These documents are live inputs while the programme is being specified and delivered, not generated test receipts. Consolidate lasting product decisions into the feature matrix, durable architecture decisions into ADRs, and reusable engineering obligations into their owning guidance as the accepted work is completed. Follow completed-artifact custody before retiring any record.
