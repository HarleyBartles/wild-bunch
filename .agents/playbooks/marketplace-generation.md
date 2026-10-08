# Plugin subscriptions and authored skills

## When

Changing repository plugin declarations, authored skill custody, or AOM
subscription and certification records.

## Unslop before work

Before changing subscriptions, profiles or agent routes, read [routing](../unslop/routing.md) and [writing guards](../unslop/writing.md) in full. Verify reachability from the actual agent work entrypoints and maintain [observations](../unslop/README.md#record-and-improve) plus the affected certification.

## Composition

1. Declare repository plugins in `.agents/plugins/marketplace.json` and bind
   the Codex catalog and selected identities in `.codex/config.toml`.
2. Keep plugin payloads in Codex's cache and repository-authored skills under
   `.agents/skills/`. Do not add a parallel skill registry or plugin copies.
3. Pin each adopted AOM standard in
   `.agents/contracts/operating-standards.json` and maintain its semantic
   assessment in `.agents/contracts/standards-certification.md`.
4. Run `py -3 tools/run.py ci --check`; the check validates local declarations
   and certification references but does not fetch or install dependencies.

## Doctrine and contracts

- [Repository skill policy](../doctrine/repo-skills-policy.md)
- [Operating-standard subscriptions](../contracts/operating-standards.json)
- [Standards certification](../contracts/standards-certification.md)
- [Repository check commands](../contracts/repo-standards-commands.json)

## Evidence contract

- [ ] Codex plugin entries, marketplace binding, and local activations agree.
- [ ] Authored skills remain in repository custody and are not plugin payloads.
- [ ] Changed standard surfaces have an accurate certification assessment.
- [ ] Runtime, trust, authentication, and remote access claims have direct evidence.

## Runbook routing

- [Implementing](../runbooks/implementing.md)
- [Decision records](decision-records.md) - when standards adoption or agent-surface ownership changes a durable repository decision.
