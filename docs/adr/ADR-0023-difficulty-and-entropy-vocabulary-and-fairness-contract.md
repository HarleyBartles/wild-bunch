# ADR-0023 Difficulty and Entropy Vocabulary and Fairness Contract

## Status

live

## Dated Status History

- 2026-06-09 - live: accepted the locked difficulty and entropy vocabulary and the fairness contract for future setup and validation work.

## Decision Type

architecture, gameplay, process

## Related ADRs

- `depends on`: ADR-0007, ADR-0010
- `informs`: ADR-0021
- `related to`: ADR-0009, ADR-0013, ADR-0024

## Context

BUNCH-6 separated difficulty from entropy as two different game-system axes. This ADR records the durable vocabulary for those axes so later implementation slices do not have to rediscover the terms or blur the distinction.

The repo already has an ADR log in `docs/adr/`, so this decision extends the existing convention instead of creating a parallel documentation home.

## Decision Drivers

- Keep challenge pressure separate from world volatility.
- Preserve a fair mystery contract where identity truth stays fixed once setup is complete.
- Give future setup, source, and replay work a boring shared vocabulary.
- Avoid premature runtime enums, types, or config in this child issue.

## Decision Summary

Difficulty controls ordinary challenge pressure.

Entropy controls world divergence, volatility, churn, and surprise.

The locked difficulty ladder is `easy`, `normal`, `hard`, `brutal`.

The locked entropy ladder is `boring`, `classic`, `adventurous`, `wild`.

Neither axis may rewrite established culprit identity/truth after challenge setup.

## Detailed Decision Breakdown

Difficulty is the normal challenge envelope. It may shape evidence burden, assistance strength, source reliability, and other ordinary pressure points that make a case easier or harder to solve.

The locked difficulty ladder is `easy`, `normal`, `hard`, `brutal`. Those names are the decision, not placeholders and not examples.

Entropy is world divergence, volatility, churn, and surprise. It may shape setup variation, noise, churn, and other world-state changes that make the case feel stable or lively without turning entropy into another difficulty axis.

The locked entropy ladder is `boring`, `classic`, `adventurous`, `wild`. Those names are the decision, not placeholders and not examples.

The fairness rule is fixed:

- Difficulty and entropy may shape setup, evidence, source reliability, and world churn.
- Neither axis may rewrite established culprit identity/truth after challenge setup.
- Higher difficulty may increase evidence burden or reduce assistance, but must not make the case unknowable.
- Higher entropy may add volatility, noise, and churn, but must preserve fair solvability.

This keeps the mystery readable even when the case is demanding or noisy. The world may mislead through incomplete evidence, stale information, or churn, but it must not perform a bait-and-switch on already-established truth.

## Options Considered and Rejected

- Collapse entropy into difficulty.
- Treat the ladder names as provisional working labels.
- Allow challenge-time randomness to rewrite culprit identity.
- Defer the vocabulary until implementation work starts.

## When a Rejected Option Would Have Been Better

Collapsing the axes would only help if the game were intentionally built around one blended pressure knob. That is not the current design.

Provisional ladder names would only help if the naming itself were still under active exploration. This issue is past that point.

## Benefits

- Future workers get one stable vocabulary for challenge and volatility.
- Fairness rules are explicit before runtime seams arrive.
- Follow-on work can focus on mechanics instead of terminology.

## Accepted Tradeoffs

- The vocabulary is now locked earlier than the runtime seam.
- Future implementation slices must conform to the decision instead of renegotiating the names.
- This ADR does not add behavior, persistence, UI, or source-taxonomy mechanics.

## Risks

- Later implementation could accidentally blur the two axes if it ignores this record.
- New setup code could try to invent alternate ladder names if it does not consult the ADR log.
- Fairness could drift if future work treats entropy as license to rewrite truth.

## Consequences for Future Work

BUNCH-26 owns the typed setup/config seam that will carry these ladders into runtime code.

BUNCH-27 owns fairness and identity-marker characterization tests.

BUNCH-28 owns source-taxonomy implications for the two-axis model.

BUNCH-3 and BUNCH-29 own replayable persistence architecture, which must respect the same fixed truth boundary.

## Implementation Status or Plan

Live as repository doctrine and vocabulary. No runtime enums, types, config, or gameplay effects are introduced in this issue.

## Related Stable Source Surfaces

- `docs/adr/README.md`
- `docs/INDEX.md`
- `docs/adr/TEMPLATE.md`

## Proof of Implementation or Explicit Non-Implementation

This issue adds the ADR log entry and the vocabulary record only. It does not implement gameplay difficulty effects, entropy events, decoys, case-file theory UI, lawman behavior, source-noise behavior, persistence changes, or runtime type/config seams.

## Review Triggers

- If a later slice starts using different ladder names.
- If a future implementation tries to make entropy another difficulty setting.
- If setup, evidence, or source work starts rewriting established culprit truth after setup.
- If runtime seams need to be added before BUNCH-26.
