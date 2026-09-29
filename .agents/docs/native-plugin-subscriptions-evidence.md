# Native Codex plugin subscription field test

Tested on 29 September 2026 with Codex CLI `0.159.0`, based on Wild Bunch `c51181a` and Marketplace PR #343 (`5fcfb473948fd0f32998ffce31d8e528f2261572`). [The extracted JSON](native-plugin-subscriptions-evidence.json) records native plugin metadata, successful cached-skill reads, session transcript hashes, invocation results and the Upgrade response.

## Catalog and payload refs

The marketplace name is **wild-bunch**. The repo config registers its Git catalog from `https://github.com/HarleyBartles/wild-bunch.git` at `main` and enables four `plugin-name@wild-bunch` identities. Every plugin source independently tracks its upstream `main`.

A candidate catalog must be published before Codex can fetch it. Draft field tests selected catalog commit `6d65c957a58e1a7274f0d54ed3d5340f23da9d94` on the published branch using this invocation-only override:

```powershell
codex -c 'marketplaces.wild-bunch.ref="codex/native-plugin-subscriptions"' plugin list --marketplace wild-bunch --json
codex -c 'marketplaces.wild-bunch.ref="codex/native-plugin-subscriptions"' plugin marketplace upgrade wild-bunch --json
```

The override selects the pre-merge catalog; plugin payload refs remain `main`. After merge the checked-in catalog ref suffices. Activation alone did not discover an unregistered catalog. A local catalog could install plugins but native Upgrade rejected it as not Git-backed; the final consumer configuration registers a Git-backed catalog.

## Discovery and invocation

| Plugin identity | Version | Representative invoked skill |
|---|---|---|
| game-studio@wild-bunch | 0.1.2 | /game-studio:game-studio |
| architecture-pack@wild-bunch | 1.0.0 | /architecture-pack:hexagonal-architecture |
| dotnet-pack@wild-bunch | 1.0.0 | /dotnet-pack:dotnet |
| frontend-pack@wild-bunch | 0.1.0 | /frontend-pack:react |

Native `plugin list --json` returned all four as installed and enabled in both `Z:/wild-bunch` and `Z:/_agent-worktrees/wild-bunch/codex/native-plugin-subscriptions`. Fresh `codex exec --ephemeral --json -s read-only` sessions invoked the four skills, read their catalog-resolved `SKILL.md` files with exit code 0, and applied one rule from each to a React/C# stack. Paths resolved under `C:/Users/hbart/.codex/plugins/cache/wild-bunch/<plugin>/<version>/skills/`. The JSON records the actual read commands and responses.

The primary checkout test temporarily overlaid the candidate manifest and Codex config, restoring both original paths in a `finally` block. Its final `git status --porcelain` was empty. The linked worktree used its own candidate configuration. Native install commands populated the cache; their user-config writes were restored after installation, preserving existing user policy and making repo activation the tested owner.

In unrelated `Z:/portfolio`, `codex plugin list --marketplace wild-bunch --json` returned `installed: []`, `available: []`. The unrelated prompt catalog contained zero occurrences of all four representative plugin-qualified skills. No Portfolio file or configuration was changed.

## Real native refresh

Codex first installed the actual historical Architecture Pack from Marketplace commit `ae148c5c7f7b86d759b1d0db6e837abf23cbcb9f`, using an external scratch fixture. The checked-in plugin declaration remained `ref: main` during Upgrade. No upstream branch or cache bytes were edited to manufacture a change.

The Git-backed Upgrade selected `wild-bunch`, refreshed `C:/Users/hbart/.codex/.tmp/marketplaces/wild-bunch`, and returned `errors: []`.

| Architecture Pack cache | Files | SHA-256 of sorted path-to-file-hash map |
|---|---|---|
| Historical install | 57 | 5021535cdda1cf18847f2d1b853d64be1dec526c6a3cee1feb0792ec949070b1 |
| After native Upgrade | 58 | bc3b7c108ba382abc92b59d9c69971d265d63ec4e1964bd78f955a303ff1eef9 |

Upgrade added portable `plugin.json`; every resulting cached file path and SHA-256 matched published `dist/plugins/architecture-pack` at `5fcfb473948fd0f32998ffce31d8e528f2261572`. The refresh target was the managed `wild-bunch` catalog and installed Git-subdirectory payloads, separate from ambient `agent-asset-marketplace` and the pinned tooling submodule. This replays a historical-to-current update; upstream `main` did not advance during the test.

## Custody decisions

- Preserve all seven previous operating standards and add `repo-plugin-subscriptions`. Deploy resources through refreshed AOM at the new pinned revision, use its preserving scaffold, and validate locally.
- Remove the Game Studio bundle, all 23 plugin-skill projections, projection provenance, refresh adapter and obsolete projection tests. The runner no longer refreshes or rolls plugin sources; authored skills are no longer generated hook output.
- Preserve six exact local-skill registrations. `git diff origin/main -- <six registered skill directories>` is empty.
- Retain the Marketplace submodule for the Markdown safe-link-label wheel and independent skill-script validator. Selected standards are deployed with provenance. The deliberate tooling pin update to PR #343 is independent of native plugin refresh; hosted CI still initializes this tooling submodule.
- Defer Devin activation. The scaffold's `.devin/config.json` has three empty plugin arrays and enables nothing.

## Validation

Passed: `deploy_operating_standards.py --check`; `repo_standards.py --check --standard repo-plugin-subscriptions`; the full eight-standard check; and `py -3 -m pytest scripts/tests -q` (22 passed). The normal tracked hook ran canonical apply/check: .NET build, 1,190 passing .NET tests with 10 existing skips, npm installation, TypeScript checking, 309 passing Vitest tests across 42 files, production build and whitespace checks. Skips are not counted as passes.

Draft PRs intentionally skip the repository's hosted validation job. Publication and fresh independent review results belong in the PR.

## Independent review limitations

A fresh whole-branch review of `c51181a..b29d125` found two upstream AOM validator gaps. Both were reproduced using disposable fixture copies: removing the native marketplace registration still returns no findings, and paths `.//plugins/game-studio` or `./C:/plugins/game-studio` also return no findings on this Windows host. The current Wild Bunch configuration has an explicit Git-backed marketplace and the exact safe relative paths required by the handover; native discovery, invocation and Upgrade prove those configured inputs.

Disposition: retain the deployed AOM resources and their verified provenance, and defer generic validator hardening to the Marketplace source owner. No malformed input is configured here. The passing standard check is not proof that every invalid registration or path is rejected. No Marketplace source was edited and no upstream issue or message was sent as part of this migration.
