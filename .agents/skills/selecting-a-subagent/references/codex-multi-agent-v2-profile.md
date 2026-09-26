# Codex Desktop MultiAgentV2 Profile

Use this profile only when the live schema exposes `spawn_agent` with `fork_turns`.

## Live contract

Read the live runtime inventory before each route choice. The current V2 inventory exposes `gpt-6-luna`, `gpt-6-sol`, and `gpt-6-astra`; the live runtime inventory outranks this profile when it changes. Do not select an unexposed model. Older model families still exposed by the runtime have no normal route here.

`fork_turns: "all"` is the full-history mode: children inherit the parent model and reasoning. It cannot take a model or reasoning override. Use `fork_turns: "none"` for a self-contained fresh-context brief, or a positive turn count for bounded recent context; those modes allow a supported model and reasoning override.

Check supported reasoning values and capacity in the live schema rather than treating a snapshot as portable. The schema does not expose pricing or entitlement.

## Route policy

- `gpt-6-luna` at the lowest adequate effort: ordinary bounded work and focused repair.
- `gpt-6-sol` at the lowest adequate effort: normal implementation, planning, and review, with proportionate reasoning for consequential or complex work.
- `gpt-6-astra` is exceptional, for a clearly described highly complex integration problem that Sol cannot adequately handle. Astra reasoning must never exceed `low`.

Review difficulty, branch size, the word “strong”, or a desire for extra confidence does not justify Astra. A failed Sol attempt does not automatically justify Astra; identify the capability need the failure demonstrates before considering it. Do not use Astra merely because it is exposed.

Choose full history only when the inherited parent model and reasoning are adequate. When the task needs another model or a reasoning override, send a bounded brief with `fork_turns: "none"` or a positive count; do not silently inherit the parent route.

Fresh context is not model-family diversity. State whether review value comes from fresh context, a different selected model, or deterministic verification.

## Mapping to shared custom-profile roles

These are semantic roles in Codex. The `.md` custom profiles are installed for Devin Desktop and do not pin Codex models. Use the live inventory and route policy above before dispatch.

| Shared role          | Normal Codex V2 route                                |
| -------------------- | ---------------------------------------------------- |
| `reviewer`           | `gpt-6-sol` at medium or the lowest adequate effort  |
| `reviewer-strong`    | `gpt-6-sol` at high or the lowest adequate effort    |
| `reviewer-fixes`     | `gpt-6-luna` at medium or the lowest adequate effort |
| `implementer`        | `gpt-6-sol` at medium or the lowest adequate effort  |
| `implementer-strong` | `gpt-6-sol` at high or the lowest adequate effort    |

No role name alone authorizes Astra. If the live runtime no longer exposes these routes, select the least escalated adequate route it does expose and record the deviation.

## Vendor and third-party profiles

Marketplace packs can ship third-party subagent `.md` profile assets under `assets/profiles/`. Codex MultiAgentV2 does not consume `.md` profile files directly; map a vendor profile name through the live runtime and shared role table. A repo-local override or a vendor profile that ships a Codex adapter note takes precedence over the default mapping. See `vendor-profile-packaging.md` for the packaging contract and the consumer search-path order.
