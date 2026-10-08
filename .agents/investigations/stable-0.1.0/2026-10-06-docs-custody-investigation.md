# Documentation custody investigation for stable 0.1.0

**Status:** Historical assessment of all six non-ADR documents under `docs/`; the selected dispositions are embodied in the accompanying 2026-10-08 stable-baseline documentation change. This file preserves the original evidence and reasoning, not current contributor instructions. Part of the [stable 0.1.0 investigation](2026-10-06-stable-0.1.0-investigation.md), alongside the [ADR investigation](2026-10-06-adr-truth-investigation.md).

**Scope:** All six files under `docs/` outside `docs/decisions/`, read in full. Classification uses their actual content, current inbound references, relevant agent guidance and source evidence. Historical authorship and removal intent have not been reconstructed.

## Classification principle

Being readable by humans does not justify a second copy of repository law in `docs/`. Doctrine and playbooks are also readable by humans. A repo doc earns its home by explaining the product, design, architecture or operation for an identifiable reader, with a distinct purpose from instructions agents must obey. Classify rules as doctrine, independently consumed agreements as contracts, reusable decision-making methods as skills, topical procedures as playbooks and lifecycle procedures as runbooks. A document can contain mixed roles and need splitting rather than a mechanical rename.

Explanatory docs may link to operational authority. Binding guidance may link to useful explanations. Neither should require maintaining competing copies of the same rule. None of these six files defines an independently consumed agreement that warrants a contract, and none establishes a distinct reusable method that requires a new skill.

## Disposition of every file

| File | What it actually contains | Justification for ordinary repo-doc custody | Disposition |
|---|---|---|---|
| `docs/unslop-style-guide.md` (retired) | Prescriptive player copy, visual language, implementation naming, document prose and review rules. | Weak as written. A focused creative voice brief could be a product doc, but this file mixed contributor instructions and review guards already owned elsewhere. | Retired the duplicate. The routed [play-surface UI profile](../../unslop/play-surface-ui.md) owns player-copy/visual guards, [writing](../../unslop/writing.md) owns prose guards, and coding rules remain with their existing owner. |
| `docs/testing-posture.md` (retired) | Lane definitions, minimum coverage requirements, evidence selection and local server/database commands. | Weak as written. Its central job was telling contributors which proof is required and how to obtain it. | Retired the duplicate. Validation doctrine owns test lanes, the testing playbook owns selection/execution, and the UI browser-check playbook owns browser procedure. |
| `docs/testing-lanes.md` (retired) | Binding test-category definitions, default provider/identity assumptions, test placement and PostgreSQL lifecycle instructions. | Weak as written. This was validation policy plus procedure, not a separate explanation with a different audience or task. | Retired without adopting its contradicted in-memory assumptions. Validation doctrine identifies the real PostgreSQL-backed HTTP/persistence lane; the testing playbook owns execution. |
| `docs/product-roadmap.md` (retired) | Proposed label/milestone taxonomy and rules for assigning issues to horizons. It contained no actual feature roadmap. | Weak as written. The title suggested product explanation, but the content governed work planning and issue classification. | Retired as an unaccepted, unreferenced repository proposal. This disposition makes no claim about external Linear labels, milestones, or configuration. |
| `docs/local-postgresql.md` (retained) | Windows local service setup, commands, connection parameters, launch/test examples and shared-service ownership safeguards. | Strong enough for a focused human local-development guide. README and scripts/README route developers here for a real setup task. | Retained as the human how-to. Its service and database claims were checked against `tools/postgres-dev.ps1`; the agent testing playbook remains the owner of validation procedure. |
| `docs/frontend-styling.md` (retired) | Mandatory styling technology, forbidden classes, design-token rules, primitive extraction threshold and claimed test enforcement. | Weak as written. It duplicated binding [frontend standards](../../doctrine/frontend-standards.md) and linked back to them as durable guidance. | Retired the parallel rule source. Frontend doctrine retains the current stack, token and component-ownership rules; its whole-doctrine test enforcement claim was removed because the test covers only selected legacy forms. |

## Findings and their delivered resolutions

### D-01: Two test policy docs describe different/currently stale validation assumptions

`testing-posture.md` described unit/acceptance/integration/manual lanes across backend and frontend and imposed minimum coverage. `testing-lanes.md` narrowed acceptance to exactly one public call and said acceptance/integration default to in-memory storage. Current acceptance classes use [PostgreSqlApiFactory](../../../tests/WildBunch.Integration.Tests/TestInfrastructure/PostgreSqlApiFactory.cs); current [validation doctrine](../../doctrine/validation-policy.md) identifies real PostgreSQL HTTP/persistence flows. Both competing documents were retired; no in-memory default was carried into current guidance.

`testing-lanes.md` qualified its authenticated-client claim, which correctly avoided claiming production auth exists. However, [AcceptanceTestHarness](../../../tests/WildBunch.Integration.Tests/TestInfrastructure/AcceptanceTestHarness.cs) only adds an Authorization header; a header alone is not a server-established identity or an authorization test. The retirement preserves the absence of production auth and does not convert this test convenience into a security guarantee.

`testing-posture.md` contained a broken ADR link through `adr/ADR-0022...` rather than the current `decisions/` home. Retiring the document removes that broken route; the manual browser evidence obligation remains in the existing browser-check playbook and the ADR is discoverable through the decision-record playbook.

### D-02: Styling doctrine and the docs guide are competing copies

The docs guide and frontend doctrine repeated the same styling stack, class prohibition, tokens and primitive names. The guide's unsupported three-unrelated-surfaces extraction threshold was not promoted. Frontend doctrine now states the component-owned display contract: parents compose through supported props and surrounding layout rather than reaching into child internals.

The claimed [styling enforcement test](../../../src/WildBunch.Web/src/tests/stylingEnforcement.test.ts) checks the absence of a legacy stylesheet, stylesheet imports, selected legacy literal classes and inline style syntax. It does not establish the complete design-token discipline or reject every possible `className` usage. The test's helper behavior and source-shape assertions remain classified in the separate test-quality investigation; this documentation disposition did not edit the test suite. The current SASS entry imports variables/reset/base, so the basic stack description is supported.

### D-03: A supposed roadmap is issue-governance guidance with weak discovery

`product-roadmap.md` used proposed horizons and labels such as must/should/could, now/next/later and boring. These were statements from the document, not verified external tracker configuration. The route audit found no active repository entrypoint referring to this proposal. It was retired without claiming that external Linear settings were changed or replacing product horizons with SemVer release versions.

### D-04: The anti-slop style guide has no work-point route and mixes audiences

The original route audit found no work-point reference to `unslop-style-guide.md`. Its concrete player-copy rules overlap the routed play-surface profile, while its architecture/naming/prose instructions cover other scopes. Terms such as surface and workflow can be poor player copy but remain legitimate technical vocabulary in contributor documentation. The duplicate was retired without promoting its broad word list into a universal ban.

### D-05: PostgreSQL setup is a legitimate explanation with embedded shared-state policy

The guide's `Z:/_postgres-cluster`, port 5435, database `wildbunch_dev` and command behavior match [tools/postgres-dev.ps1](../../../tools/postgres-dev.ps1). This is source verification, not an inspection or lifecycle operation on the running service. The human guide remains linked from both README entrypoints; the testing/browser workflows continue to route agent-specific obligations separately.

The retained guide remains explicitly local and does not define a cloud deployment contract. It distinguishes stopping the shared cluster from resetting only the `wildbunch_dev` database, so the documented consequence matches the script.

## Custody and historical follow-through

Five files lacked a convincing separate explanatory job in their present form. The PostgreSQL guide had a credible human operational purpose and remains in `docs/`. No new playbooks were created and no external issue tracker configuration was changed.

The five duplicate or unaccepted documents were retired after their useful current guidance was confirmed at existing owners and active inbound routes were resolved. The historical source paths remain plain text in this assessment so they do not masquerade as live files. The earlier retirement of the generated index mesh does not prevent a useful authored ADR catalogue with a concrete discovery job.
