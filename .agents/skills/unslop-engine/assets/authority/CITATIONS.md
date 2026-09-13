# Authority record for unslop-engine

## Scholarly citation

- Upstream repository: https://github.com/mshumer/unslop
- Pinned commit: edcb62386d129c65e4395f0cfcc9168eb1ba2148
- License: MIT
- Upstream README SHA-256: 5c5e317d341aa63d73f73ca0b50309ca712acaebf660c6057b4ee376736643bd

## Derivation boundary

- The unslop-engine `SKILL.md` is a first-party operational synthesis of the upstream engine concept (sample collection, pattern detection, profile generation) adapted for Codex/GPT skill use.
- The bundled `scripts/unslop.py` is an Asset Marketplace adaptation that replaces the upstream `claude` CLI dependency with Python standard library text analysis and optional Playwright visual evidence.
- The upstream MIT license and copyright are preserved in `LICENSE.upstream`.

## Attribution

- Original idea and upstream implementation: Matt Shumer (mshumer/unslop, MIT license, Copyright (c) 2026 Matt Shumer).
- Asset Marketplace adaptation and first-party synthesis: Harley Bartles.

## Human review

- Reviewer: Harley Bartles
- Date: 2026-07-22
- Decision: Approved. Operational `SKILL.md` body contains no inline citations; source-grounded claims are recorded in `assets/authority/`.
