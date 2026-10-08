# Decision records playbook

## When

Use this playbook when a change makes, changes, corrects, or materially removes a durable product, domain, architecture, persistence, security, testing, or repository decision.

For ADR prose or agent-guidance changes, also follow the [unslop playbook](unslop.md) and its scoped profile selection.

## Select records

1. Start at the [decision catalogue](../../docs/decisions/README.md); read records relevant to the changed behavior or boundary and follow their successor links.
2. Compare the selected decisions with the current implementation and the proposed change. The repository's code, contracts, and certification remain implementation truth; an ADR records the decision and its history.
3. Do not load every ADR by default, treat a history date as a review date, or copy implementation inventories into the log.

## Keep the history truthful

- Leave an ADR unchanged when the implementation still follows its decision and no durable choice changed.
- Create a new ADR when a different durable choice replaces an existing one; link the predecessor and state what the successor changes.
- For partial supersession, retain the original decision, add a dated history entry, name the successor, and state which scope survives. For full supersession, preserve the original decision and record the successor and date.
- Add a dated editorial clarification when the record is inaccurate or ambiguous. Preserve the original decision date and distinguish known facts from unknown provenance; never invent the reason for a historical change or removal.
- Do not remove or contradict ADR-protected behavior as incidental cleanup. Make that a deliberate decision and include the matching amendment or successor in the same change; preserve the removal in the history even when its original cause was accidental or unknown.
- Do not introduce durable architecture without consulting relevant ADRs and recording the decision in the same change. Routine implementation that preserves existing decisions does not require a new ADR.
- Keep implementation status, source/test inventories, validation receipts, task lists, and future feature backlogs in their owning documents. Decision status says whether the decision remains authoritative, not whether code is complete.

## Lifecycle obligations

- Design identifies the durable decisions that govern the design and whether it makes or changes one.
- Planning names the governing records and includes any required ADR creation, correction, or supersession in the plan.
- Implementation follows the selected decisions and records a discovered divergence or material removal in the same change.
- Before requesting review, the PR author compares the actual proposed diff with relevant decisions and includes required ADR creation, correction, or supersession, or states why no durable decision changes.
- The reviewer independently repeats that comparison against the actual diff. An author's assertion or the presence of an ADR edit alone does not establish that the decision log remains true.
- Pull-request publication confirms the author check is resolved and the review gate has independently assessed the same decision boundary.
