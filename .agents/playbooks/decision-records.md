# Decision records playbook

## When

Use this playbook when a change makes, changes, corrects, or materially removes a durable product, domain, architecture, persistence, security, testing, or repository decision.

## Select records

1. Start at the [decision catalogue](../../docs/decisions/README.md); read records relevant to the changed behavior or boundary and follow their successor links.
2. Compare the selected decisions with the current implementation and the proposed change. The repository's code, contracts, and certification remain implementation truth; an ADR records the decision and its history.
3. Do not load every ADR by default, treat a history date as a review date, or copy implementation inventories into the log.

## Keep the history truthful

- Leave an ADR unchanged when the implementation still follows its decision and no durable choice changed.
- Create a new ADR when a different durable choice replaces an existing one; link the predecessor and state what the successor changes.
- For partial supersession, retain the original decision, add a dated history entry, name the successor, and state which scope survives. For full supersession, preserve the original decision and record the successor and date.
- Add a dated editorial clarification when the record is inaccurate or ambiguous. Preserve the original decision date and distinguish known facts from unknown provenance; never invent the reason for a historical change or removal.
- When behavior tied to an ADR is materially removed, preserve that removal in the relevant record or successor even when the removal was accidental or its reason is unknown. Do not make the removed behavior appear never to have existed.
- Keep implementation status, source/test inventories, validation receipts, task lists, and future feature backlogs in their owning documents. Decision status says whether the decision remains authoritative, not whether code is complete.

## Lifecycle obligations

- Design identifies the durable decisions that govern the design and whether it makes or changes one.
- Planning names the governing records and includes any required ADR creation, correction, or supersession in the plan.
- Implementation follows the selected decisions and records a discovered divergence or material removal in the same change.
- Review compares the diff with relevant decisions and checks that history and successor links describe the actual change.
- Pull-request publication confirms that a required record update is included, or states why the change does not alter durable decisions.
