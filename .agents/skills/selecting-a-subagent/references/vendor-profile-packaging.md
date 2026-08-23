# Portable and vendor profile packaging

## Canonical location

First-party portable subagent `.md` profile assets live in
`codex-marketplace/plugins/superpowers-plus/skills/selecting-a-subagent/assets/`.
For example:

```
codex-marketplace/plugins/superpowers-plus/skills/selecting-a-subagent/
  assets/
    reviewer.md
    reviewer-fixes.md
    reviewer-strong.md
    implementer.md
    implementer-strong.md
```

Third-party marketplace packs may also ship `.md` profile assets under their own
pack `assets/profiles/` directory.

## Installation

First-party portable profiles are installed to the Devin Desktop user-global
agents directory by `install_profiles.py`:

```bash
py -3 .agents/skills/selecting-a-subagent/scripts/install_profiles.py --apply
```

The default target is `~/.config/devin/agents/` on macOS/Linux and
`%APPDATA%\devin\agents\` on Windows. Use `--target <dir>` to install to a
different path. Do not commit the user-global directory to a repo.

## Consumer search paths

Devin Desktop searches the following locations; a later path in this list
overrides an earlier one:

1. Built-in profiles documented in `devin-desktop-profile.md`.
2. User-global: `~/.config/devin/agents/` (or `%APPDATA%\devin\agents\` on Windows).
3. `.devin/agents/` — user- or repo-local hand-authored overrides.
4. `.agents/agents/` — plugin-local or third-party vendor profiles.

No skill should create or pressure a consumer to create `.devin/agents/`.

## Packaging contract

- First-party portable profiles are source-custodied in the `selecting-a-subagent`
  pack `assets/` directory.
- Pack `assets/profiles/` is reserved for third-party vendor profiles.
- The `install_profiles.py` installer records no provenance entry for first-party
  profiles; the canonical source is the pack tree. Other marketplace tooling that
  stages vendor profiles into `.agents/agents/` may track them in the consumer's
  `.agents/skills/.provenance.json` under a `vendorProfiles` array.
