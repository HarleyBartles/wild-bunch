# Repository command bus

`tools/run.py` is the agent-facing command bus for supported build, test, and repository-validation operations. It delegates to the existing toolchains; it does not replace them with a second build system.

Run `py -3 tools/run.py` to list available targets, or `py -3 tools/run.py <target> --help` for a target's modes, prerequisites, side effects, and arguments. A selected target requires an explicit mode; target work never starts because a mode was silently assumed.

## Targets

| Target | Modes | Purpose |
| --- | --- | --- |
| `ci` | `--check` | Run the complete fail-fast check gate used by pre-commit and hosted CI. |
| `format` | `--check`, `--apply` | Check formatting or format explicit supported files; whole-repository apply requires `--all`. |
| `lint` | `--check` | Run check-only language diagnostics; lint checks never apply fixes. |
| `dotnet-build` | `--check` | Build the .NET solution; arguments after `--` go to `dotnet build`. |
| `dotnet-test` | `--check` | Run .NET tests, including PostgreSQL integration tests; arguments after `--` go to `dotnet test`. |
| `web` | `--check` | Check web formatting and lint, typecheck, run tests, and build the app. |
| `python-tests` | `--check` | Run repository script and tooling behavior tests; arguments after `--` go to both pytest suites. |
| `setup-hooks` | `--check`, `--apply` | Verify or set this checkout's local `core.hooksPath` to `githooks`. |

The canonical gate stops at the first failure and orders checks from cheapest to most expensive: whitespace and repository contracts, Python format/lint, web format/lint, .NET format/analyzers, .NET build and web typecheck, then behavioral tests and the web build. All format, lint, and static checks finish before behavioral suites begin, so an agent does not wait for an expensive test after a cheaper diagnostic has already failed. `ci --check --diagnostics` is a separate manual troubleshooting mode that reports independent failures; it is never used by the commit hook or hosted CI.

`py -3 tools/run.py format --check [paths...]` checks selected supported code files, or the full supported scope when no paths are supplied. `py -3 tools/run.py format --apply <path> [paths...]` formats only those files and reports what changed; pass `--all` instead of paths for an intentional whole-scope migration. Paths must resolve inside the repository. C# uses `dotnet format whitespace`, Python uses pinned Ruff formatting, and web JavaScript/TypeScript/TSX/SCSS uses pinned Prettier. Markdown, generated, dependency, vendor, and build files are excluded.

`py -3 tools/run.py lint --check [paths...]` checks selected supported code files, or the full supported scope when no paths are supplied. Ruff checks Python, ESLint applies type-aware TypeScript rules and the core Rules of Hooks/dependency checks to web JavaScript/TypeScript/TSX, and .NET SDK analyzers check C#. Lint never runs automatic fixes; correct reported diagnostics explicitly and rerun the focused target.

The style targets require the pinned Ruff package from `tools/requirements.txt`, the .NET SDK pinned by `global.json`, and locked web dependencies from `src/WildBunch.Web/package-lock.json`. If web style dependencies are missing, the check may run `npm ci` to create ignored `node_modules`; if installed tool versions do not match the lockfile, it fails with `npm ci` as the repair.

`ci --check` runs the same style policy against the staged candidate in the local pre-commit hook and the committed tree in hosted validation. Neither mode calls formatter apply or lint autofix. `ci --check --diagnostics` is available only for manually requested troubleshooting and may continue after independent failures.

Check targets may create ignored build and test outputs, but do not repair maintained files or stage changes. `setup-hooks --apply` changes local Git configuration and uses the shared-checkout guard; pass `--allow-shared-checkout` only when applying it in a shared `main` checkout.

The Python bus and repository-checker behavior tests live in `tools/tests/`. Tracked-hook and standalone script behavior tests live in `scripts/tests/`. Application tests remain with their .NET or web owners.

The [AOM command-bus subscription](../.agents/contracts/operating-standards.json) and [self-certification](../.agents/contracts/standards-certification.md) record the adopted interface and current conformance evidence. Maintained Python checkers and shared helpers live alongside the bus; `tools/postgres-dev.ps1` remains a native shared-service lifecycle tool.
