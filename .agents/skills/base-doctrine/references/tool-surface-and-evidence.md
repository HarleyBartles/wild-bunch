# Tool surface and evidence contract

The active tool surface is the set of capabilities exposed in the current session. Product/account settings, connector binding in the UI, plugin installation, marketplace availability, and prior sessions do not guarantee that every internal namespace or source scope is callable in the current session.

Skills should describe the capability needed, not prescribe an exact runtime tool name. GPT selects from the tools actually exposed by the current runtime.

Before claiming a tool, connector, memory route, repository route, plugin, marketplace, or source is unavailable:

1. Check the actual available route when possible.
2. Record the exact failure or absence.
3. Distinguish product setting, current session tool surface, connector authorization, plugin installation, source binding, indexed source scope, and runtime/tool error.
4. Do not infer global unavailability from one route failing.

For repository work, choose routes by active capability:

- Exact repository-state capability: known issue, comment, file, commit, branch, pull request, compare, label, and authorized repository mutation operations.
- Indexed repository-search capability: broad discovery, semantic search, stale-reference sweeps, duplicate checks, corpus inventories, and cited reads across many repository files or issues.
- Uploaded-file or file-library capability: user-provided files and library items only, unless the active tool surface explicitly says the target repository is exposed there.

For Codex worker capability, separate these layers:

- Codex Cloud task runtime and model selection.
- Codex environment repo binding and setup/maintenance scripts.
- Codex plugins and plugin marketplaces installed in that environment.
- Repo-local skills/playbooks available from the checked-out repository.
- Shell-visible credentials or CLIs, which are not the same as native Codex publication capability.

Do not assume the model-facing name of any connector or plugin. Do not treat the presence of a bound connector, uploaded file, indexed source, plugin, marketplace, or file library as a reason to inspect sources or select tools for ordinary chat. Select source routes only after the classified task actually needs source evidence.

If exact repository reads are sufficient, use an available exact route and state that broad indexed discovery was not used when that limitation matters. If broad indexed discovery would materially improve confidence but no suitable route is available or safe to use, state that limitation and either proceed from exact routes when sufficient or ask for the appropriate repository-search capability to be made available.

Never claim a memory, issue comment, file write, repo update, skill install, plugin install, marketplace update, or artifact was saved unless a tool result, visible artifact, or user-visible confirmation supports it.

When a user challenges availability, explain the precise layer that failed: UI/product setting, exposed tool namespace, connector authorization, plugin installation, source binding, indexed source scope, or runtime/tool error.

