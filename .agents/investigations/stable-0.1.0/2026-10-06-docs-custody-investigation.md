# Documentation custody investigation for stable 0.1.0

**Status:** Findings and proposed dispositions for discussion. No source document has been moved, deleted or rewritten. Part of the [stable 0.1.0 investigation](2026-10-06-stable-0.1.0-investigation.md), alongside the [ADR investigation](2026-10-06-adr-truth-investigation.md).

**Scope:** All six files under `docs/` outside `docs/decisions/`, read in full. Classification uses their actual content, current inbound references, relevant agent guidance and source evidence. Historical authorship and removal intent have not been reconstructed.

## Classification principle

Being readable by humans does not justify a second copy of repository law in `docs/`. Doctrine and playbooks are also readable by humans. A repo doc earns its home by explaining the product, design, architecture or operation for an identifiable reader, with a distinct purpose from instructions agents must obey. Classify rules as doctrine, independently consumed agreements as contracts, reusable decision-making methods as skills, topical procedures as playbooks and lifecycle procedures as runbooks. A document can contain mixed roles and need splitting rather than a mechanical rename.

Explanatory docs may link to operational authority. Binding guidance may link to useful explanations. Neither should require maintaining competing copies of the same rule. None of these six files defines an independently consumed agreement that warrants a contract, and none establishes a distinct reusable method that requires a new skill.

## Disposition of every file

| File | What it actually contains | Justification for ordinary repo-doc custody | Proposed disposition |
|---|---|---|---|
| [unslop-style-guide.md](../../../docs/unslop-style-guide.md) | Prescriptive player copy, visual language, implementation naming, document prose and review rules. | Weak as written. A focused creative voice brief could be a product doc, but this file mixes contributor instructions and review guards already owned elsewhere. | Consolidate useful player-copy/visual guards into [play-surface UI](../../unslop/play-surface-ui.md), prose guards into [writing](../../unslop/writing.md), and genuine coding policy into its existing owner. Retire the duplicate after preserving any unique intent. |
| [testing-posture.md](../../../docs/testing-posture.md) | Lane definitions, minimum coverage requirements, evidence selection and local server/database commands. | Weak as written. Its central job is telling contributors which proof is required and how to obtain it. | Consolidate accepted test policy into [validation doctrine](../../doctrine/validation-policy.md), selection/execution into [testing](../../playbooks/testing.md), and browser procedure into [UI browser checks](../../playbooks/ui-browser-check.md). Retire the duplicate. |
| [testing-lanes.md](../../../docs/testing-lanes.md) | Binding test-category definitions, default provider/identity assumptions, test placement and PostgreSQL lifecycle instructions. | Weak as written. This is validation policy plus procedure, not a separate explanation with a different audience or task. | Reconcile the policy against source, then consolidate definitions/placement into validation doctrine and executable steps into the testing playbook. Retire competing lane authority. |
| [product-roadmap.md](../../../docs/product-roadmap.md) | Label/milestone taxonomy and rules for assigning issues to horizons. It contains no actual feature roadmap. | Weak as written. The title suggests product explanation, but the content governs work planning and issue classification. | Put accepted issue-planning procedure in a topical planning/issue-shaping playbook; keep stable taxonomy rules with their policy owner if needed. Preserve actual future product ideas separately rather than inventing a roadmap from this process document. |
| [local-postgresql.md](../../../docs/local-postgresql.md) | Windows local service setup, commands, connection parameters, launch/test examples and shared-service ownership safeguards. | Strong enough for a focused human local-development guide. README and scripts/README already direct a developer here for a real setup task. | Retain a concise human how-to, with an explicit link to the canonical service-ownership/testing procedure. Consolidate authoritative safeguards and duplicated execution policy into their agent owner. If no distinct human guide is wanted, route humans to the same playbook instead; relocation is not mandatory merely because agents also use it. |
| [frontend-styling.md](../../../docs/frontend-styling.md) | Mandatory styling technology, forbidden classes, design-token rules, primitive extraction threshold and claimed test enforcement. | Weak as written. It largely duplicates binding [frontend standards](../../doctrine/frontend-standards.md), which even points back to this file as durable guidance. | Merge any accepted unique rule into frontend doctrine, route styling work through the code-style playbook and retire the parallel rule source. Keep technology rationale in ADRs rather than another current implementation inventory. |

## Findings requiring reconciliation before consolidation

### D-01: Two test policy docs describe different/currently stale validation assumptions

`testing-posture.md` describes unit/acceptance/integration/manual lanes across backend and frontend and imposes minimum coverage. `testing-lanes.md` narrows acceptance to exactly one public call and says acceptance/integration default to in-memory storage. Current acceptance classes use [PostgreSqlApiFactory](../../../tests/WildBunch.Integration.Tests/TestInfrastructure/PostgreSqlApiFactory.cs); current [validation doctrine](../../doctrine/validation-policy.md) explicitly identifies real PostgreSQL HTTP/persistence flows. Reconcile intended behavioral categories with actual provider usage instead of copying both conflicting definitions into doctrine.

`testing-lanes.md` qualifies its authenticated-client claim, which correctly avoids claiming production auth exists. However, [AcceptanceTestHarness](../../../tests/WildBunch.Integration.Tests/TestInfrastructure/AcceptanceTestHarness.cs) only adds an Authorization header; a header alone is not a server-established identity or an authorization test. Future account work must supply actual authenticated principals and ownership proof. Preserve the explicit absence of production auth rather than convert this test convenience into a security guarantee.

`testing-posture.md` contains a broken ADR link through `adr/ADR-0022...` rather than the current `decisions/` home. Replace it with the agreed stable decision catalogue route when this document's disposition is implemented. This is part of the same stale evidence/routing problem identified by ADR findings A-14/A-15.

### D-02: Styling doctrine and the docs guide are competing copies

The docs guide and frontend doctrine repeat the same styling stack, class prohibition, tokens and primitive names. The docs guide adds a three-unrelated-surfaces extraction threshold absent from doctrine; that threshold needs an explicit keep/revise decision rather than silently becoming binding by relocation.

The claimed [styling enforcement test](../../../src/WildBunch.Web/src/tests/stylingEnforcement.test.ts) checks the absence of a legacy stylesheet, stylesheet imports, selected legacy literal classes and inline style syntax. It does not establish the complete stated design-token discipline or reject every possible className usage. Its helper behavior tests and its source-shape assertions are different concerns; evaluate them in the later test-slop investigation rather than calling all of them behavior proof or deleting the suite in this custody pass. The current SASS entry imports variables/reset/base, so the basic stack description is supported.

### D-03: A supposed roadmap is issue-governance guidance with weak discovery

`product-roadmap.md` uses proposed horizons and labels such as must/should/could, now/next/later and boring. These are statements from the document, not verified current external tracker configuration. There are no inbound references to this document in the inspected root entrypoints, agent guidance, docs, scripts or source/test Markdown. Do not treat its old proposed taxonomy as the current planning system or automatically replace it with semver release versions. Product horizons and release versions serve different purposes. Record accepted/replaced planning conventions truthfully if consolidation changes them.

### D-04: The anti-slop style guide has no work-point route and mixes audiences

No inbound reference to `unslop-style-guide.md` was found in the same inspected surfaces. Its useful concrete player-copy rules overlap the routed play-surface profile, while its architecture/naming/prose instructions cover other scopes. Terms such as surface and workflow are poor player copy in some contexts but legitimate technical vocabulary in contributor documentation. Preserve that scope distinction when adopting useful guards; do not promote the file's broad word list into a universal ban.

### D-05: PostgreSQL setup is a legitimate explanation with embedded shared-state policy

The guide's `Z:/_postgres-cluster`, port 5435, database wildbunch_dev and command behavior match [tools/postgres-dev.ps1](../../../tools/postgres-dev.ps1). This is source verification, not an inspection or lifecycle operation on the running service. Its instructions not to stop shared PostgreSQL during ordinary cleanup and not to drop the persistent app database are operational constraints, and agents must encounter them through the testing/browser workflow even if a human guide remains in docs.

Avoid copying full command recipes across three documents. Choose one procedure owner, then make the human guide explain prerequisites and the developer's task while linking to that owner. A machine-specific local guide should remain clearly local, not become the public cloud deployment contract.

## Custody and historical follow-through

Five files lack a convincing separate explanatory job in their present form. One has a credible human operational purpose but needs authority separation. Classification does not authorize bulk relocation, automatic creation of six new playbooks or deletion of unique content.

Before removing a document, settle any conflicting rule, preserve its accepted unique knowledge at an existing owner, repair inbound routes, and record material policy changes/removals in the decision history where relevant. An accidental loss remains a deviation, not an invented successor decision. Prefer consolidation into existing doctrine/playbooks/profiles over multiplying files. The earlier retirement of the generated index mesh does not prevent a useful authored catalogue with a concrete discovery job.
