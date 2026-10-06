# Distinct slop observations

Records below describe concrete incidents and guard outcomes. They are durable inputs to the feedback loop, not a log of work performed. Unknown recurrence, reading and effect remain unknown.

## U-001: Existing profiles with weak work-point routes

**Work:** Unslop adoption ahead of the interactive stable-0.1.0 audit, 2026-10-06. Treat the discovered gaps together as one incident, not multiple independent recurrence examples.

**Evidence:** At repository commit `4eef2d6fb5592b5125686c6fe8dcf42e27c2787d`, `.agents/contracts/unslop/backend-architecture.md` declared use before backend design, implementation and review, but `.agents/doctrine/architecture-guardrails.md` and the design/implementation runbooks did not link it. `.agents/runbooks/code-review.md` routed to a directory of profiles without naming the profile for review itself. `.agents/contracts/unslop/dev-overlay.md` required backend and web profiles without linking their paths. Inspect those files at that commit to distinguish this incident from later reports.

**Failure and correction:** Existing guidance was available on disk but weakly routed at the decision points that needed it. Move canonical profiles to `.agents/unslop/`, link them directly from stage and concern guidance, and require scoped full reading from the root/contributor selector. [Routing guard](routing.md) makes that correction reusable.

**Reach and effect:** The paths and missing direct routes were inspected during this adoption. Whether earlier agents discovered, read or ignored the profiles is unknown; no historical code defect or independent recurrence is inferred. The adopting agent read the profiles and revised the routes. Future audit work must assess whether those routes and guards change decisions; successful link checks alone do not establish effectiveness.

## U-002: Obsolete profile-placement detector

**Work and evidence:** The same adoption encountered a distinct validation obstacle: `scripts/tests/test_repo_guidance_contracts.py` at commit `4eef2d6fb5592b5125686c6fe8dcf42e27c2787d` required `.agents/unslop/` to contain no Markdown, required the former contracts directory and asserted a fixed web profile filename. It tested neither an agent's route nor a validator's behavior. The newly adopted immutable standard instead requires `.agents/unslop/` as canonical custody.

**Correction and guard:** Remove this obsolete location-only detector; do not replace its expected filenames with the new layout. Retain behavior fixtures for the subscription and AGENTS validators and inspect actual route reachability. [Code-review guards](code-review.md#location-only-change-detectors) distinguish a change detector from a legitimate structural-validator contract.

**Reach and effect:** The adopting agent read the review profile, encountered the incompatible test during validation and applied the existing prohibition on change-detector tests. This observation makes the corrective distinction explicit for subsequent agents. It is one near miss, not established recurrence. Whether future agents reach and follow this guard remains unproven.
