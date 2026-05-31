# Wild Bunch Testing Lanes

Wild Bunch keeps tests grouped by behavioral scope, not by storage provider.

## Unit

Unit tests prove one object, rule, aggregate method, domain service, handler, or
small collaborator under controlled construction.

Use unit tests when the subject owns the mechanics being exercised. These tests
do not enter through HTTP and should stay fast and surgical.

## Acceptance

Acceptance tests prove one public product contract.

Acceptance tests use an authenticated test client, an in-memory store by
default, and exactly one public API call as the behavior under test. They may
seed a known aggregate/session state directly or via named scenario seeds, but
the action under test must enter through the real public API endpoint. The test
should assert the public result and the aggregate/session state transition that
the call caused.

Current-state note: the production API does not yet enforce authentication or
authorization middleware. In this repo, "authenticated test client" means the
test host carries a fixed test identity, represented today by a test
`Authorization: Test acceptance-user` header, and is ready for future auth
plumbing, not that the production API is already security-gated.

## Integration

Integration tests prove product-flow composition across multiple API calls.

Use this lane for workflows such as start game -> preview travel -> start
journey -> advance day -> resolve encounter -> read journal. These tests may
also use in-memory storage, but they should stay focused on multi-endpoint
composition rather than a single contract.

## Provider And Storage

Provider/storage tests are exceptional, not the default confidence lane.

Use them when the behavior under test is EF mapping, migrations, SQL
translation, snapshot persistence, concurrency, or provider-specific behavior.
When this lane is intentional, name it clearly so it is obvious that the test is
about provider fidelity rather than ordinary gameplay behavior.

## Repo Placement

- Unit tests live in the domain/application/game-content test projects.
- Acceptance tests live under `tests/WildBunch.Integration.Tests/Acceptance`.
- Workflow integration tests remain under `tests/WildBunch.Integration.Tests`.
- Provider/storage checks remain explicitly named inside the integration test
  project.
