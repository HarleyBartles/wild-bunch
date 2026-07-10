# Upstream Provenance

- Upstream repository: `mshumer/unslop`
- Pinned commit: `edcb62386d129c65e4395f0cfcc9168eb1ba2148`
- Commit date: 2026-03-18
- Upstream author shown by git: Matt Shumer
- License: MIT (Copyright (c) 2026 Matt Shumer)
- Source custody path: `sources/third_party/unslop/upstream/`
- Upstream license notice: `LICENSE.upstream` (in this directory's parent)

## Engine Adaptation

The upstream `unslop.py` is a Claude Code CLI tool that requires the `claude` binary, spawns `claude -p` subprocesses for sample generation and analysis, and includes an interactive terminal UI with spinners and progress bars. These runtime assumptions are not appropriate for a Codex/GPT skill package.

The projected script in `scripts/unslop.py` is adapted from the upstream idea: it uses Python standard library text analysis on local sample files, removes the Claude Code CLI dependency, removes the interactive terminal UI, and only uses Playwright for visual dependencies when already present. The upstream MIT license and copyright are preserved in `LICENSE.upstream`.

The upstream source remains retained verbatim in `sources/third_party/unslop/upstream/unslop.py` for provenance and comparison.

## First-Party Profiles

The thirteen profiles in `profiles/` are first-party portable profiles authored by Harley Bartles (Asset Marketplace) under MIT license. They are projected verbatim from `sources/first_party/skills/unslop-plus/profiles/` and are not derived from upstream content.
