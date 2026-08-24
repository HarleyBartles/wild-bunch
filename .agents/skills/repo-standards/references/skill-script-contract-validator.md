# Skill-bundled script CLI contract validator

## What it checks

`repo-standards/scripts/validate_skill_scripts.py` walks every installed skill Python script under `.agents/skills/*/scripts/*.py` and verifies the contract from `.agents/specs/completed/2026-08-04-skill-script-cli-contract-design.md`:

- `--help` exits `0` and contains a `usage:` line.
- `--help` declares the script classification: `read-only`, `mutating`, or `mixed`.
- `--check` does not exit `2` (which means the script rejects the argument and is not contract-aware).

## When to run it

Run it as part of `tools/run repo-standards --check` or `tools/run ci --check`. CI will fail if any non-deferred script fails.

## How to fix a failure

1. Run the failing script with `--help` and `--check` locally:
   ```bash
   py -3 codex-marketplace/plugins/<plugin-pack>/skills/<skill-name>/scripts/<script>.py --help
   py -3 codex-marketplace/plugins/<plugin-pack>/skills/<skill-name>/scripts/<script>.py --check
   ```
2. Add `argparse` with `--help`, `--check`, and (for mixed scripts) `--apply`.
3. Keep `--check` as the default mode and document the classification in the help text.
4. Re-run the validator:
   ```bash
   py -3 codex-marketplace/plugins/repo-worker-pack/skills/repo-standards/scripts/validate_skill_scripts.py
   ```

## Deferred scripts

A small set of scripts are in `DEFERRED` while they are migrated. They are reported but do not fail validation. Remove entries from `DEFERRED` as scripts are migrated.
