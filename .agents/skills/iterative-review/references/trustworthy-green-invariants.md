# Trustworthy-green invariants

The version-2 evidence kernel exists to make one claim defensible: when
`reviewctl` reports a green candidate, every gate behind that claim was
witnessed, bound to the exact reviewed snapshot, and closed in a lawful order.
This document is the normative contract. `reviewctl.py --help` owns the command
surface; this file owns what the green claim means and what it never means.

The kernel enforces **process completeness**. It cannot enforce omniscience.
A green candidate says the required evidence exists, is current, and is
mutually consistent for the reviewed head. It does not say no defect exists
anywhere, that reviewers were honest beyond what their witness records attest,
or that remote state cannot drift after sealing. Frontier review remains the
calibration check the local loop predicts, and `present` (Plan 6) re-verifies
remote identity at emission time rather than trusting a stored verdict.

## The nine green predicates

`evaluate_green` re-derives every predicate over validated state plus witness
records; a stored `green_seal` value is output, never input. The design spec's
nine predicates map to the kernel's finer-grained checks as follows:

1. **authorities_complete** - the expected authority manifest is loaded, and
   every required authority is either bound by content hash or explicitly
   recorded unavailable under its class.
2. **impact_maps_current** - both the semantic and contract impact maps exist
   for the current snapshot epoch.
3. **coverage_complete** - the challenged coverage inventory spans the union of
   both maps, and every obligation is assigned and closed or lawfully
   exempted.
4. **challenge_current** - the independent scope challenge for the epoch
   completed clean; an incomplete challenge blocks rather than degrades.
5. **checks_current** - required local checks are witnessed successful on the
   epoch; hosted checks are remotely verified on the exact reviewed SHA.
6. **reviews_current** - every required review carries launch and completion
   witnesses matching the dispatch's `agent_id` and scope, with
   `audit_result: clean` and a known verdict. Plan 1 accepts the `audit_result`
   the completing attestation carries; independently deriving contamination
   from the ingested `tool-transcript` evidence is deferred to a later plan,
   so the audit claim today is attested, not transcript-verified. A reviewer
   that touched out-of-scope paths could therefore still self-attest clean;
   transcript-derived verification is tracked in the roadmap's later plans
   and until it lands this predicate trusts reviewer self-attestation.
7. **findings_clear** - every finding, all severities including `minor`, sits
   in a closed disposition; nothing remains `open`, `fixing`,
   `review-repairing`, `contested`, `deferred`, or `unassessed`.
8. **final_current** - the blind final review and the closure audit are
   witnessed clean on the current epoch.
9. **remote_verified** - a fresh remote observation matches repository, PR,
   head SHA, authority, and feedback identity at evaluation time.

All nine must hold for one snapshot epoch at once. `accepted-risk` findings
contribute only to `reviewed-with-exceptions`, never to green.

## Version-1 versus version-2 authority

`reviewctl.py` is the only mutation authority for `schema_version: 2` state.
The legacy tools that read review state directly (`next_node.py`,
`compile_metrics.py`) refuse version-2 state outright; the remaining legacy
helpers (`resolved_ledger.py` and friends) operate on compiled metrics and
logs, never on review state, so no legacy path can create a version-2 green
seal. A version-1 `ready` verdict is review
assistance, not proof of reviewed green: version-1 state has no snapshot
binding, no witness records, and no epoch-scoped evidence, so nothing in it
can satisfy predicate evaluation.

## Current-snapshot evidence rules

Every record in version-2 state binds `{snapshot_epoch,
snapshot_fingerprint}`. Evidence ingested mid-transaction binds the epoch that
will exist when the transaction commits; evidence registered during a
snapshot-installing action binds the candidate snapshot, not the one being
replaced. A record whose epoch no longer matches the installed snapshot is
historical, not current: it stays in the record for audit but cannot satisfy a
predicate. `freeze-review-input`, `enter-fixing`, and `refresh-review-input`
are the only snapshot-installing transitions; drift is never repaired in
place.

## Snapshot acquisition and the witness chain

A snapshot is installed only from a transcript-witnessed enumeration.
`reviewctl enumerate` runs the acquisition under the hooks pack so the exec
is recorded in the harness transcript; `reviewctl complete --action
<freeze|refresh>-review-input --acquired <dir>` then binds the produced
acquisition directory. The `authority-discovery` witness record carries
`transcript_range` plus `record_positions` and `chain_head_at_record`
against the chained witness log; `TranscriptWitnessVerifier` re-verifies
chain integrity and the subject digest at evaluation time, so a snapshot
claim that lacks the witnessed enumeration segment cannot satisfy the
authority predicates.

Refresh advances exactly one epoch and requires non-empty drift reasons
drawn from the snapshot subject fields; a byte-identical subject (epoch
excluded) is refused as `no-drift`. Refresh invalidates downstream proof -
coverage inventory, ready transition, CI candidate, and green seal reset -
while findings and witness history stay durable.

Two digests keep provider feedback honest: `feedback_history_sha256` covers
the complete actionable thread and change-request history, including items
resolved before freeze, and `unresolved_feedback_sha256` covers the subset
still open at enumeration time. Each actionable item also enters through
the freeze/refresh payload as a feedback-sourced finding bound to the
installed snapshot; finding identity is content-derived, so a re-enumerated
item never overwrites an existing record and a provider-side Resolve cannot
reopen or reset its lifecycle.

Honest residual: the witness chain proves the enumeration ran under the
hooks pack, but hook-stream integrity depends on the harness emitting a
record for every exec. A harness that drops a tool record produces a gap
the verifier cannot distinguish from omission. That limitation is documented
rather than claimed away.

## All-severity finding closure

Green requires every finding closed, not every finding fixed. The lawful
terminal dispositions are `fixed` (verification and fix review on record),
`review-repaired` (independent repair verification after a same-snapshot
invalidation cut), `false-positive` (adjudicator attestation plus
counter-evidence), and `accepted-risk` (durable human-decision witness, which
yields `reviewed-with-exceptions` and never green). `minor` findings follow
the same lifecycle; no severity is exempt from closure.

## Blocked and resume semantics

A blocker is a durable record, not a status flag. `block` requires an
installed snapshot because blocker records must bind a live epoch; there is
one active blocker at a time; and `resume` must present resolution evidence
current at the resume epoch. Resuming recomputes the derived stage rather
than trusting the stage stored before the block. An `incomplete` witness
outcome routes to `blocked` instead of degrading to partial credit.

## Derived reports are not authority

Metrics, ledgers, Markdown summaries, and any other rendered output are
projections of state. They may be stale, may be regenerated, and carry no
decision weight. The state file plus its witness chain is the only authority;
a report that disagrees with state is a bug in the report, not evidence about
the review.
