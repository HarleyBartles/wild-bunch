### Normal parent posture

The intended persistent on-disk parent is GPT-5.6 Terra at High reasoning, configured outside this skill. The skill may describe that posture but must not claim authority to switch the parent.

### Approved routes

* `gpt-5.4-mini`
* `gpt-5.5`
* `gpt-5.6-luna`
* `gpt-5.6-terra`
* `gpt-5.6-sol`

Treat exact runtime slugs—including a slug shown under a Custom picker entry—as the model actually used.

### Routing

**GPT-5.4 mini**

* Medium: mechanical edits, exact repetitive transformations, routine cleanup, straightforward tests, low-judgment migrations.
* High: default bounded implementation after strong Superpowers/SDD planning.
* Prefer it over Luna for ordinary well-specified coding when context is sufficient.
* Do not use it as the sole high-consequence architecture, security, migration, or concurrency reviewer.

Fallback when unavailable:

* Luna for tightly bounded work or where large context is the main need;
* Terra when implementation judgment matters.

**GPT-5.6 Luna**

* Low: exact lookups and fast discovery.
* Medium: repository inventories, broad scans, source summaries, large-context read-heavy work.
* High only when 5.6 behaviour or its larger context materially helps a bounded task.
* Do not make Luna the routine implementation default while GPT-5.4 mini remains adequate.

**GPT-5.6 Terra**

* Medium: normal multi-file implementation, established integration work, ordinary engineering judgment.
* High: difficult debugging, cross-boundary reasoning, planning, synthesis, or meaningful local design decisions.

**GPT-5.6 Sol**

* High: architecture, domain modelling, security, concurrency, transactional correctness, difficult migrations, consequential review, or conflicting-findings adjudication.
* Extra High: exceptional architecture/security/migration/debugging or unresolved high-consequence disagreement.
* Extra High is the absolute ceiling. Ultra is forbidden.
* Do not spend Sol on routine navigation, scans, mechanical edits, or ordinary bounded implementation.

**GPT-5.5**

Use only for:

* deliberate regression comparison against previously trusted 5.5 behaviour;
* intentionally diverse second opinion;
* continuation of a workflow calibrated on 5.5;
* checking whether a 5.6 route introduced behavioural regression.

Use High. Fall back to Sol High when unavailable. Do not present 5.5 as a cheaper Sol substitute.

### Codex review gradient

* GPT-5.4 mini implementation: Terra High parent review for ordinary work; independent Terra High or Sol High for consequential work.
* Terra Medium implementation: Terra High or Sol High review as risk requires.
* Terra High implementation: Sol High review when independent stronger verification matters.
* Sol High work: fresh-context Sol High review; Sol Extra High only for exceptional consequence or unresolved disagreement.
