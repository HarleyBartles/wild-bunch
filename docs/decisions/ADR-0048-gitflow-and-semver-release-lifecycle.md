# ADR-0048 Gitflow and SemVer Release Lifecycle

## Status

`live`

## Dated History

- `2026-10-15` - Adopt the pinned AOM Gitflow and SemVer standards for the Wild Bunch browser-game product and record the repository's pre-1.0 compatibility and release policy.
- `2026-10-15` - Clarify that pre-1.0 adoption makes no promise about a future `1.0.0` contract; define that stable compatibility contract only when preparing that release.

## Decision Type

`operations`, `process`

## Related ADRs

- `related to`: ADR-0043
- `related to`: ADR-0018

## Context

The repository needs a repeatable route from integration work to a qualified
release without confusing an ordinary development checkpoint with a public
release. Its initial `0.1.0` baseline is still being established, and old
development playthroughs do not carry a blanket compatibility guarantee.

## Decision

The Wild Bunch browser-game product follows the pinned AOM `gitflow` and
`semver` standards. `develop` is the integration and ordinary PR target;
`main` is the stable release line. Ordinary task branches start from current
`develop` and return by PR, with squash permitted. Release and hotfix
promotions and reconciliation use merge commits. Release branches start from
`develop`; hotfix branches start from released `main`. Reconcile promoted
changes to `develop` and each affected active release line without discarding
continuing development.

During `0.y.z`, the product makes no public API or gameplay-compatibility
promise, and breaking changes are allowed. `0.1.0` identifies the first
qualified baseline, not a guarantee that every pre-release playthrough remains
usable. The existing migration strategy remains required for data the
repository declares supported. Do not decide a future `1.0.0` compatibility
contract in advance.

`Directory.Build.props` is the one authored application version. The build
propagates it to runtime product identity, and validation rejects malformed,
missing, stale, or conflicting product-version outputs. Dependencies and
event/schema versions remain under their own version authorities. Each
ordinary merge into `develop`, including documentation and tooling changes,
establishes one unique `MAJOR.MINOR.PATCH-dev.N` checkpoint per PR. A changed
and requalified release candidate uses `MAJOR.MINOR.PATCH-rc.N`; unchanged
verification does not allocate another candidate number. Promote a qualified
source to its stable core version and bind the published identity and
artifacts to the verified immutable source. A published version's contents
are immutable.

If release or hotfix reconciliation leaves `develop` at the released
baseline, retain the stable version until new development begins. If a later
release line is already in progress, preserve that core and allocate the next
unique development checkpoint for reconciliation.

## Rationale and Alternatives

The two standards jointly provide branch promotion and version-checkpoint
rules for the repository's chosen integration and release lines. A simpler
single-branch delivery model was considered in the release design and was not
selected because release stabilization and preprod qualification need a
separate integration path. No future stable compatibility promise is set
before the product is ready to define one.

## Consequences

Contributors and reviewers use the release lifecycle runbook and verify
current hosting facts at publication time. Certification distinguishes
documented procedure and locally verified build identity from release flows
and artifact/deployment behavior that have not yet been exercised. The
decision does not claim that GitHub branch protection enforces the written
routes.
