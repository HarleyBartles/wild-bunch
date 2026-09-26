---
name: command-bus
description: Use when creating, reviewing, or changing a repository command bus, named command targets, standard modes, orchestration, or exit behaviour.
metadata:
  source-id: command-bus
  source-path: codex-marketplace/plugins/agent-operating-model/skills/command-bus/SKILL.md
  provenance-name: Command Bus first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
license: MIT
---

# Command Bus

A repository command bus gives agents one discoverable entrypoint over named, deterministic targets. The repository owns its target inventory; this skill owns the portable interface.

## Contract

- Keep the entrypoint thin: parse, resolve, dispatch, propagate.
- Every target appears in help and has one owning implementation.
- Support `--check` for read-only drift detection and `--apply` for mutation when the target has both modes.
- Reject contradictory or unsupported modes before running child commands.
- Preserve child output and propagate non-zero exit status.
- Run multi-target requests in declared order and stop on the first failure.
- Keep diagnostics explicit; success output must not hide warnings or skipped material checks.
- Test help discovery, target selection, mode forwarding, order, and failure propagation.

Use `python` when the command bus is implemented in Python. Use `repository-validation` for what the targets must prove.
