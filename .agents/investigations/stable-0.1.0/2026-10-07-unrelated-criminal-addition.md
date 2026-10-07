# PG-009-A: Add integrated unrelated-criminal bounty gameplay later

**Work class:** Deferred player feature addition. **State:** Future story and integration obligations recorded; no release assignment, complete design or implementation authorization. This record preserves the feature being removed by [PG-009-R](2026-10-07-unrelated-criminal-removal.md). The [PG-009 assessment](2026-10-07-unrelated-criminal-feature-boundary.md) documents what the partial code did and where it fell short. Reintroduction must be designed against the future live repository, not restore retired files wholesale.

## Player story and outcome

As a player, I can discover a warrant for a wanted person outside the Wild Bunch, encounter and identify that person, lawfully secure them under the warrant's terms, and receive the sheriff's permitted bounty once. Their settlement does not establish who committed the central crime. Saloon encounters may include ordinary citizens, gang members, unrelated wanted criminals or nobody.

Gang members already have bounties. The addition concerns independent non-gang targets, not a new name for the existing gang confrontation loop. Legal wanted status, gang membership, case involvement and murder guilt remain distinct. Players receive supported public knowledge and identifying evidence, not hidden truth masquerading as a category label.

## Integration that must actually exist

| Responsibility | Required behavior | Why restoring the partial code is insufficient |
| --- | --- | --- |
| Person/warrant content and identity | Generate supported non-gang targets with stable person/warrant relationships and legal terms. Record authoritative generation inputs/facts so version changes do not rewrite past hunts. | The old code generated warrants without saloon encounter candidates; name matching is insufficient identity. Do not add outsiders to the gang suspect roster merely to reuse SuspectId. |
| Discovery and player knowledge | Posters/records reveal lawful identifying facts and warrants; discovery is recorded and correctly reflected in player reads. Define what knowledge is required to identify or declare a target. | An unrelated poster did not establish that the corresponding person could be encountered or captured. |
| Availability and encounters | Normal saloon selection can produce an eligible non-gang person. Presence, exhausted sources, visits and selected population rules are coherent and deterministic. Developer preparation, if needed, arranges legal candidates rather than final outcomes. | The current pool has only gang suspects/citizens/nobody; the old ledger's active population was not integrated into that pool. |
| Declaration and confrontation | Resolve identity against public warrant handles and authoritative active presence. Record permitted surrender/flee/death/secured outcomes and wrongful declarations under the chosen gameplay rules. | The old direct unrelated settlement accepted a WarrantId plus isAlive without a required recorded capture. |
| Sheriff settlement and resources | A lawful secured target is assessed under alive-only/dead-or-alive terms; pay the correct bounty once, preserve costs and reject duplicate/invalid settlement. Expose the selected application/API/player path. | The dormant method had no production caller; the existing application handler settled gang suspects. A sheriff visit versus immediate confrontation settlement remains a product decision. |
| Case separation and public display | Bounty completion and murder-case progress have distinct effects. Posters, case/reference surfaces and journal show supplied identity/provenance/outcome facts using stable links. | Existing unrelated content shared generic case/warrant displays, and some display logic joined by name or guessed provenance. |
| Events, CQRS and persistence | Commands decide legality; events establish all authoritative lifecycle facts; queries/projectors report them without mutation. Save/resume and independent event replay preserve identity, availability, capture, settlement and wallet/history. Caches remain reconstructible. | Snapshot ledger storage and end-of-replay rebuilding omitted unrelated settlement history; agreement between two wrong paths is not proof. |
| Population retirement/replacement, if selected | Explicitly define whether caught targets are replaced, how a pool exhausts and whether gang progress affects availability. Record selected transitions and expose honest exhausted states. | The old parity rule and 3x roster are implementation policy, not requirements implied by the user story. Their reinstatement needs a separate design decision. |
| Compatibility and developer boundary | Introduce a versioned supported transition from the future baseline and preserve historical facts under its policy. Keep internal preparation/diagnostics isolated from public play. | The future model may differ from old event/ledger shapes; migration is a requirement, not a reason to restore incomplete code. |

No new NPC platform, independent aggregate, broker or service is required by this story. Domain boundaries and the minimal lifecycle representation must be chosen during its future design, using the established strict event-authority/CQRS posture.

## Cross-feature dependencies

- **PG-001:** creates the supported authoritative target/warrant data for a hunt.
- **PG-005:** supplies public warrant/identity knowledge and discovery lifecycle.
- **PG-006:** supplies the retained encounter/declaration/confrontation conventions; extend them for non-gang targets without weakening citizen mistakes or gang/culprit rules.
- **PG-008:** consumes target/warrant links, encounter/settlement history and truthful public outcomes.
- **PG-002 and supporting replay/persistence:** preserve the entire lifecycle across resume/reconstruction.
- **PG-004/resource mechanics:** consume and display lawful payment/cost changes where relevant; buying supplies is not automatically a hard prerequisite for every target.
- **PG-009-R:** preserves the removed concept and source evidence for this addition. Retirement is not a runtime feature dependency and must not create a permanently required legacy ledger.

Discovery, encounter and settlement are separate outcomes. Whether encounter eligibility requires prior warrant discovery, and whether capture includes transport to a sheriff, are future product decisions rather than assumptions inferred from dependencies.

## Behavioral acceptance to design later

Prove a normal discover/encounter/identify/secure/settle lifecycle through production entry points, then resume/replay from independent durable history with the same correct payout and state. Preserve wrong-identity/citizen consequences and reject unknown/inactive targets, absent or unsecured presence, dead targets under alive-only warrants and duplicate settlement. Same-name people must remain distinct. An unrelated bounty must not falsely solve the central crime.

If replacement/retirement is selected, prove its actual recorded availability changes and exhaustion, not only ledger counts. Keep browser public displays and backend hidden-truth projection boundaries distinct. Use cohesive behavioral scenarios and genuine negatives at their owners, rather than restoring old snapshot/property/mapping tests as the specification.

## Open product decisions and non-goals

Encounter distribution, population size, replacement policy, gang-parity interaction, prior-discovery requirements, available confrontation choices and the sheriff handoff UX remain open. Balance and content volume are not specified. No requirement to implement these during stable 0.1.0 cleanup follows from recording them. Reassess the future codebase and settle the story before making a detailed plan.

## Later Linear shaping

Create a separate future feature item during the requested Linear cleanup, linked to PG-009-R and the source assessment. Preserve the user story, directed dependencies, incomplete prior integration and open decisions. It can later be decomposed into implementation work after an accepted feature design. No Linear item has been created or changed here.
