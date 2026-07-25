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
