# Codex Desktop MultiAgentV1 Profile

Use this profile only when the live schema exposes
`multi_agent_v1__spawn_agent` and Boolean `fork_context`.

## Live contract

Use only the exact model slugs and reasoning values shown by the active V1
schema. The current V1 inventory is:

| Model | Exposed reasoning values |
|---|---|
| `gpt-5.4` | `low`, `medium`, `high`, `xhigh` |
| `gpt-5.5` | `low`, `medium`, `high`, `xhigh` |
| `gpt-5.6-luna` | `low`, `medium`, `high`, `xhigh`, `max` |
| `gpt-5.6-terra` | `low`, `medium`, `high`, `xhigh`, `max`, `ultra` |
| `gpt-5.6-sol` | `low`, `medium`, `high`, `xhigh`, `max`, `ultra` |

Omitting `model` or `reasoning_effort` inherits the parent value. Do not infer
the parent model or reasoning when the runtime does not expose it.

Use `fork_context: true` when the child needs current-thread history. Use
`fork_context: false` or omit it for a fresh-context brief. V1 exposes no
documented model/fork incompatibility; do not claim whether backend enforcement
is hard or advisory without dispatch evidence.

The current schema may expose a priority service tier, but it does not expose
pricing, entitlement, or numeric concurrency. A desired absent route is “not
exposed by this dispatch surface,” not proof that it is unavailable in the
backend.

## Route policy

* `gpt-5.4`: bounded, well-specified implementation and mechanical work.
* `gpt-5.6-luna`: discovery, inventories, and read-heavy work.
* `gpt-5.6-terra`: ordinary integration, debugging, planning, and engineering judgment.
* `gpt-5.5`: deliberate regression comparison or a diverse second opinion.
* `gpt-5.6-sol`: consequential architecture, security, correctness, or adjudication.

Start at the lowest adequate exposed reasoning value. For consequential Sol
work, escalate from `high` to `xhigh` and then `max` only with concrete
justification. `ultra` is exposed but forbidden by shared policy.

Fresh context is not model-family diversity. Name the review property honestly:
fresh context, a different selected model, or deterministic verification.

## Mapping to shared custom-profile roles

These are the Codex MultiAgentV1 equivalents of the Devin Desktop custom
profiles. Use them when a sibling skill says "use `/selecting-a-subagent`" for a
role.

| Shared role | Codex V1 route |
|---|---|
| `reviewer` | `gpt-5.6-terra` at `high` |
| `reviewer-strong` | `gpt-5.6-sol` at `high` |
| `reviewer-fixes` | `gpt-5.4` at `low` or `medium` |
| `implementer` | `gpt-5.4` at `medium` |
| `implementer-strong` | `gpt-5.6-terra` at `high` |

## Vendor and third-party profiles

Marketplace packs can ship third-party subagent `.md` profile assets under
`assets/profiles/`. Codex MultiAgentV1 does not consume `.md` profile files
directly; map a vendor profile name to the V1 route above using the shared
role table. A repo-local override or a vendor profile that ships a Codex
adapter note takes precedence over the default mapping. See
`vendor-profile-packaging.md` for the packaging contract and the consumer
search-path order.
