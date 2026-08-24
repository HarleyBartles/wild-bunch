# Codex Desktop MultiAgentV2 Profile

Use this profile only when the live schema exposes `spawn_agent` with
`fork_turns`.

## Live contract

The current V2 inventory exposes `gpt-5.6-terra` and `gpt-5.6-sol`. Use the
runtime schema as authority if that inventory changes.

`fork_turns: "all"` is the full-history mode: children inherit the parent model and reasoning. It cannot take a model or reasoning override. Use
`fork_turns: "none"` for a self-contained fresh-context brief, or a positive
turn count for bounded recent context; those modes allow a supported model and
reasoning override.

The current V2 schema accepts `low`, `medium`, `high`, `xhigh`, `max`, and
`ultra` reasoning values. Four total agent slots are currently available,
including the parent; recheck live inventory rather than treating that limit as
portable. The schema does not expose pricing or entitlement.

## Route policy

* Terra at `medium`: ordinary bounded implementation, integration, or discovery.
* Terra at `high`: debugging, planning, and cross-boundary engineering judgment.
* Sol at `high`: consequential architecture, security, correctness, or adjudication.
* Sol at `xhigh`: exceptional consequence or unresolved high-consequence disagreement.

Choose full history only when the inherited parent model and reasoning are
adequate. When the task needs Sol or a reasoning override, send a bounded brief
with `fork_turns: "none"` or a positive count; do not silently inherit the
parent route. `ultra` is exposed but forbidden by shared policy.

Fresh context is not model-family diversity. State whether review value comes
from fresh context, a different selected model, or deterministic verification.

## Mapping to shared custom-profile roles

These are the Codex MultiAgentV2 equivalents of the Devin Desktop custom
profiles. Use them when a sibling skill says "use `/selecting-a-subagent`" for a
role.

| Shared role | Codex V2 route |
|---|---|
| `reviewer` | `gpt-5.6-terra` at `high` with `fork_turns: "none"` |
| `reviewer-strong` | `gpt-5.6-sol` at `high` or `xhigh` with `fork_turns: "none"` |
| `reviewer-fixes` | `gpt-5.6-terra` at `medium` with `fork_turns: "none"` |
| `implementer` | `gpt-5.6-terra` at `medium` |
| `implementer-strong` | `gpt-5.6-terra` at `high` or `gpt-5.6-sol` at `high` |

## Vendor and third-party profiles

Marketplace packs can ship third-party subagent `.md` profile assets under
`assets/profiles/`. Codex MultiAgentV2 does not consume `.md` profile files
directly; map a vendor profile name to the V2 route above using the shared
role table. A repo-local override or a vendor profile that ships a Codex
adapter note takes precedence over the default mapping. See
`vendor-profile-packaging.md` for the packaging contract and the consumer
search-path order.
