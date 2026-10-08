# Repository command bus

`tools/run.py` is the agent-facing command bus for supported build, test, and repository-validation operations. It delegates to the existing toolchains; it does not replace them with a second build system.

Run `py -3 tools/run.py` to list available targets, or `py -3 tools/run.py <target> --help` for a target's modes, prerequisites, side effects, and arguments. A selected target requires an explicit mode; target work never starts because a mode was silently assumed.

## Targets

| Target | Modes | Purpose |
| --- | --- | --- |
| `ci` | `--check` | Run the complete fail-fast check gate used by pre-commit and hosted CI. |
| `dotnet-build` | `--check` | Build the .NET solution; arguments after `--` go to `dotnet build`. |
| `dotnet-test` | `--check` | Run .NET tests, including PostgreSQL integration tests; arguments after `--` go to `dotnet test`. |
| `web` | `--check` | Install locked npm dependencies, typecheck, test, and build the web app. |
| `python-tests` | `--check` | Run repository script and tooling behavior tests; arguments after `--` go to both pytest suites. |
| `setup-hooks` | `--check`, `--apply` | Verify or set this checkout's local `core.hooksPath` to `githooks`. |

The canonical gate stops at the first failure and orders checks from cheapest to most expensive. Fast formatting, lint, static, and repository-contract checks run before test suites or expensive builds when their prerequisites allow. `ci --check --diagnostics` is a separate manual troubleshooting mode that reports independent failures; it is never used by the commit hook or hosted CI.

Check targets may create ignored build and test outputs, but do not repair maintained files or stage changes. `setup-hooks --apply` changes local Git configuration and uses the shared-checkout guard; pass `--allow-shared-checkout` only when applying it in a shared `main` checkout.

The Python bus and repository-checker behavior tests live in `tools/tests/`. Tracked-hook and standalone script behavior tests live in `scripts/tests/`. Application tests remain with their .NET or web owners.

The [AOM command-bus subscription](../.agents/contracts/operating-standards.json) and [self-certification](../.agents/contracts/standards-certification.md) record the adopted interface and current conformance evidence. Maintained Python checkers and shared helpers live alongside the bus; `tools/postgres-dev.ps1` remains a native shared-service lifecycle tool.
