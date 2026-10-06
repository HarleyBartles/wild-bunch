# Versioned Release Foundation Implementation Plan

**Status:** Provisional follow-on draft. The [interactive stable-0.1.0 investigation](2026-10-06-stable-0.1.0-investigation.md) comes first; its agreed baseline and remediation scope must be reconciled into this draft before execution. This plan is not currently approved for implementation.

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give Wild Bunch a reproducible `0.1.0` development baseline with one version authority, matching build identity and Gitflow-aware validation, ready for a first versioned release.

**Architecture:** A root MSBuild properties file owns the game version; Python release tooling and the Vite configuration consume that value rather than maintaining their own versions. Existing canonical validation remains the CI gate. Gitflow and baseline release instructions replace the current main-only contribution assumptions.

**Tech Stack:** Existing .NET 10, React/Vite/TypeScript, Python 3.12 with pytest, Git and GitHub Actions. No new runtime service or third-party versioning package.

**Spec:** [Public playability and versioned releases](../specs/2026-10-06-cloud-playability-and-releases.md). Parent: [cloud playability roadmap](../roadmaps/2026-10-06-cloud-playability.md).

**Execution Strategy:** `executing-plans`, because version consumption, CI inputs and contribution guidance share one small release contract and benefit from continuous implementation context.

## Global Constraints

- Preserve DDD, CQRS, event sourcing, and the existing migration strategy.
- A release has one declared version shared across the application, artifacts, tag, and release metadata.
- A published version is immutable.
- The first releases establish a reproducible, validated base and the actual release process.
- Public access requires effective user isolation and absence of developer capabilities, regardless of the game's alpha status.
- No server configuration, paid services, DNS changes, Google client registration, account behavior, containerization or deployment workflows in this slice. Later roadmap slices own those outcomes.
- Do not cut a tag, publish a release, create remote `develop`, modify branch rules or merge as part of this code plan. The deliverable is a reviewable implementation and an exact operational procedure; human-owned release actions are outside its checklist.
- No dependency-upgrade sweep or new gameplay feature. Existing dependency findings remain separate work unless they block the actual release gate.
- Use `.agents/runbooks/implementing.md`, the focused lanes below and the normal staged-snapshot hook. No bypass, duplicated full gate around a successful commit, committed test-result receipts, emojis, em dashes or wrapped Markdown prose.

## Review Focus

- An invalid, missing or ambiguous authored version fails the release command and production web build, rather than silently becoming `0.0.0`.
- A tag with the wrong version or a tag commit outside main history fails baseline verification before a release is published.
- Distinct revisions of the same version retain distinct source identity; merge commits with the same source tree can be distinguished without inventing a new game version.
- Failed canonical CI does not upload a qualifying baseline build manifest.
- Hosted staged-snapshot validation leaves HEAD at the parent commit; manifest generation restores the tested commit and refuses to record that temporary parent state.
- Draft PRs retain the existing skip behavior; integration and stabilization branch pushes run the canonical gate.

## Files and responsibilities

| Path | Responsibility |
|---|---|
| `Directory.Build.props` (new) | Sole authored game `VersionPrefix`, initially `0.1.0`, inherited by all .NET projects. |
| `tools/release.py` (new) | Read and validate the version, emit source identity, verify a local baseline release tag. |
| `scripts/tests/test_release.py` (new) | Executable version, manifest and Git-history rejection behavior in temporary repositories. |
| `src/WildBunch.Web/vite.config.ts` | Read the authored version and emit `version.json` with the built web assets. |
| `src/WildBunch.Web/build/releaseVersion.ts` (new) | Focused XML value extraction and validation for the web build, separate from the game UI. |
| `src/WildBunch.Web/build/releaseVersion.test.ts` (new) | Missing, ambiguous and invalid version input behavior. |
| `src/WildBunch.Web/package.json`, `package-lock.json` | Remove the private npm package's placeholder version so it cannot be mistaken for the game release authority. |
| `.github/workflows/ci.yml` | Extend canonical branch validation and retain a successful build identity manifest. |
| `CONTRIBUTING.md`, `.agents/runbooks/pr.md` | Describe actual Gitflow routing and the one-time bootstrap exception. |
| `.agents/runbooks/releasing.md` (new) | Exact baseline finalization, version/tag checks, publication and merge-back procedure. |

No API endpoint is needed to establish build identity in this slice: .NET assembly informational version and the built web `version.json` are observable outputs. The deployed-version endpoint and immutable container digest set belong to the container/release-deployment slices.

## Task 1: One authored version and matching build identity

**Files:** Create `Directory.Build.props`, `tools/release.py`, `scripts/tests/test_release.py`, `src/WildBunch.Web/build/releaseVersion.ts`, `src/WildBunch.Web/build/releaseVersion.test.ts`; modify `src/WildBunch.Web/vite.config.ts`, `src/WildBunch.Web/package.json`, `src/WildBunch.Web/package-lock.json`.

**Interfaces:** Consumes the current project and web build entrypoints. Produces Python `parse_version(xml: str) -> str`, `read_version(path: Path) -> str`, `build_manifest(root: Path) -> dict[str, str | int]`, `verify_tag(root: Path, tag: str, main_ref: str) -> dict[str, str | int]`; TypeScript `readReleaseVersion(xml: string): string`; CLI `version`, `manifest --output PATH`, `verify-tag --tag TAG --main-ref REF`. Manifest schema 1 contains only `schema`, `version`, `sourceCommit`, `sourceTree`; no credentials, test results or deployment claims. Keep CLI argument parsing under `if __name__ == "__main__"` so tests can import the pure functions.

- [ ] **Step 1:** Write behavior tests for exactly one valid `VersionPrefix`; reject missing value, duplicate definitions, leading-zero components, malformed suffixes and empty input. Implement the finite initial contract as normal `MAJOR.MINOR.PATCH` versions only; candidate uniqueness comes from full source commit/tree identity, not mutable version strings. Test `0.1.0` and `0.10.12` as valid. Use the same input cases in Python and TypeScript, without asserting source strings or file existence.

```python
def test_ambiguous_version_is_rejected(tmp_path):
    props = tmp_path / "Directory.Build.props"
    props.write_text("<Project><PropertyGroup><VersionPrefix>0.1.0</VersionPrefix><VersionPrefix>0.2.0</VersionPrefix></PropertyGroup></Project>", encoding="utf-8")
    with pytest.raises(ValueError):
        read_version(props)
```

```typescript
it("rejects ambiguous version authority", () => {
  expect(() => readReleaseVersion("<Project><VersionPrefix>0.1.0</VersionPrefix><VersionPrefix>0.2.0</VersionPrefix></Project>" )).toThrow();
});
```

- [ ] **Step 2:** Run the focused tests before implementation: `py -3 -m pytest scripts/tests/test_release.py -q` and `npm --prefix src/WildBunch.Web run test -- build/releaseVersion.test.ts`. Confirm failure is caused by the missing release functions, then implement a single-property XML reader in Python and a narrow reader for that same literal XML property in TypeScript. Neither reader supports computed MSBuild values, substitutions or a fallback version. Keep the authored file deliberately simple:

```xml
<Project>
  <PropertyGroup>
    <VersionPrefix>0.1.0</VersionPrefix>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3:** Add temporary Git-repository tests that create two commits with one version and different trees, then a merge/history shape with matching trees. Assert the manifest distinguishes commits and accurately preserves tree equality. Implement subprocess calls with argument arrays, root-relative `cwd`, `check=True`, and captured output for `git rev-parse HEAD` and `git rev-parse HEAD^{tree}`. Refuse release manifests when `git status --porcelain --untracked-files=all` reports modified, staged or untracked non-ignored source; allow ignored build outputs. Test both dirty tracked source and untracked source as failures. JSON output is deterministic apart from the explicit source identity, with no timestamps needed.

```python
manifest = build_manifest(repo)
assert manifest["schema"] == 1
assert manifest["version"] == "0.1.0"
assert manifest["sourceCommit"] == expected_commit
assert manifest["sourceTree"] == expected_tree
```

- [ ] **Step 4:** Configure a small Vite plugin that calls `readReleaseVersion` on root `Directory.Build.props` when loading the config and emits the following asset during the production build. Use `import.meta.url` to resolve paths so caller working directory is irrelevant. Do not add version display or dev-menu changes to `AppShell`.

```json
{"version":"0.1.0"}
```

- [ ] **Step 5:** Remove the optional `version` from the private npm package and its root lockfile metadata, using the installed npm tooling to refresh the lockfile without upgrading dependencies. Verify .NET SDK inheritance with `dotnet msbuild src/WildBunch.Api/WildBunch.Api.csproj -getProperty:Version`; it must report `0.1.0`. Run focused Python/web tests, web typecheck and production build. Inspect `src/WildBunch.Web/dist/version.json` and the Release API assembly informational version after `dotnet build src/WildBunch.Api/WildBunch.Api.csproj -c Release`. SDK-added revision metadata is acceptable; the base version must match. Change the version to `0.1.1` temporarily, rebuild both, verify both outputs change, and restore `0.1.0` before staging. This manual consumption proof avoids a costly nested-build test in every gate.

- [ ] **Step 6:** Stage only this task's source and commit with the normal hook: `git commit -m "build: establish one game release version"`. Confirm the hook passes and inspect the committed diff. Do not run the whole canonical gate again immediately around this commit.

**Exit:** Both build systems consume the same authored version; malformed version inputs fail; source identity is explicit and no game behavior has changed.

## Task 2: Gitflow branch validation and baseline release checks

**Files:** Modify `.github/workflows/ci.yml`, `tools/release.py`, `scripts/tests/test_release.py`. Keep `scripts/tests/test_ci_workflow.py`'s existing hook-parity coverage; do not expand it into assertions on YAML spelling.

**Interfaces:** Consumes task 1's manifest and version reader. Produces the same canonical `validation` check on Gitflow branches, successful CI artifact `baseline-<sourceCommit>-<runAttempt>`, and a `verify-tag` result bound to the tagged source tree. This is baseline build identity, not a deployment qualification or container manifest.

- [ ] **Step 1:** In temporary Git repositories, exercise `verify_tag`: a valid `v0.1.0` tag reachable from the supplied main ref succeeds; `v0.2.0` fails version equality; a matching tag on an unrelated branch fails main ancestry; a missing tag/ref fails cleanly. Read `Directory.Build.props` from the tag's tree with `git show TAG:Directory.Build.props`, never from the caller's working tree. Resolve the peeled commit (`TAG^{commit}`) so annotated tags work. Return tag commit/tree plus the parsed version in the manifest schema. Confirm tests fail before implementing.

```python
with pytest.raises(ValueError):
    verify_tag(repo, "v0.2.0", "main")
```

- [ ] **Step 2:** Implement the checker using `git merge-base --is-ancestor` and exact `v<version>` equality. Nonzero CLI exit on failed checks; bounded error messages without environment dumps. Keep version publication immutability and GitHub Release permissions as operational checks in task 3; a local Git test cannot prove a remote release is absent or immutable.

- [ ] **Step 3:** Extend push triggers to the following branches, retaining current PR event types, draft skip, per-ref concurrency, PostgreSQL service, tool setup and `REPO_STANDARDS_HOSTED_COMMIT: HEAD` tracked-hook entry. Do not replace the hook with a lighter build command. Add `workflow_dispatch` for observing the same gate on an explicitly selected branch.

```yaml
on:
  push:
    branches: [main, develop, 'release/*', 'hotfix/*']
  pull_request:
    types: [opened, synchronize, reopened, ready_for_review]
  workflow_dispatch:
```

- [ ] **Step 4:** Account for the hosted hook's actual Git state: it soft-resets HEAD to the parent and leaves the tested commit staged. After successful validation, restore HEAD to the exact event commit with `git reset --soft "$GITHUB_SHA"`, then require both staged and unstaged diffs to be empty before generating identity. Use the following separate commands in a default-success step; do not hard-reset or bypass the gate:

```bash
git reset --soft "$GITHUB_SHA"
git diff --exit-code
git diff --cached --exit-code
python tools/release.py manifest --output artifacts/baseline/manifest.json
```

Add a temporary-repository test that recreates this soft-reset state, verifies manifest generation rejects it, restores the candidate with the same soft-reset command and verifies the manifest identifies the candidate rather than its parent. Upload that one file using the current official GitHub artifact action, with artifact name `baseline-${{ github.sha }}-${{ github.run_attempt }}` and a finite 30-day retention. Checkout remains depth 2 because manifest generation needs only HEAD/tree; local tag verification explicitly fetches history in the release runbook. Upload uses the default success condition and read-only repository permissions. Add no deployment credentials or package-write permission. A failed hook must never reach restoration, manifest generation or upload.

- [ ] **Step 5:** Run the focused release tests and existing workflow-parity tests: `py -3 -m pytest scripts/tests/test_release.py scripts/tests/test_ci_workflow.py -q`. Review the event/job conditions against successful and failed gate execution. Hosted event execution is verified after authorized publication; do not fabricate remote branch-push evidence locally.

- [ ] **Step 6:** Stage and commit through the normal hook with `git commit -m "ci: validate Gitflow branches and retain baseline identity"`. Inspect the committed workflow and hook result.

**Exit:** Canonical validation covers integration and stabilization branches; successful runs retain their version/source identity; local release verification rejects mismatched or unrelated tags.

## Task 3: Executable contribution and baseline release procedure

**Files:** Modify `CONTRIBUTING.md`, `.agents/runbooks/pr.md`; create `.agents/runbooks/releasing.md`.

**Interfaces:** Consumes task 2's `verify-tag` command and canonical CI job. Produces one consistent contributor routing policy and a baseline release runbook for an authorized human/operator. No gameplay or environment deployment contract is added.

- [ ] **Step 1:** Update contribution and PR routing: ordinary feature/task branches start from and target `develop`; stabilization `release/<version>` targets `main`, then merges back to `develop`; `hotfix/<version>` starts at released main, returns to main and develop and any active affected release branch. Preserve dedicated worktrees, Draft default, hosted-check expectations and prohibition on unapproved direct main pushes. Explain the one-time bootstrap: this foundation PR targets existing `main`; after its merge, create `develop` at that verified merged main commit before starting new feature work. No existing main-only publication instruction may silently remain authoritative.

- [ ] **Step 2:** Write the baseline runbook around explicit source/ref inputs. First release proposal is `0.1.0`, cut as `release/0.1.0`; version changes are committed on the candidate before validation. Settle source/version changes before merge, check canonical CI on the release and main trees, and verify equality of tested release tree and finalized main tree. Use these commands with explicit fetched refs, never guessed stale local branches:

```powershell
git fetch origin --tags
git rev-parse 'origin/release/0.1.0^{tree}'
git rev-parse 'origin/main^{tree}'
py -3 tools/release.py version
py -3 tools/release.py verify-tag --tag v0.1.0 --main-ref origin/main
```

Explain PowerShell quoting for brace-bearing revision arguments if needed: pass `'origin/main^{tree}'` and `'origin/release/0.1.0^{tree}'` as literal arguments. Before creating the tag, independently inspect remote tags and GitHub Releases for `v0.1.0`; existing publication is never overwritten. Create the annotated tag on the verified main commit, push it and create a GitHub Release only under explicit release authorization. Read back tag target, release state and metadata. Release notes state the foundation scope, incomplete pre-alpha status and absence of public deployment. Baseline manifests identify builds; they are not proof that preprod was tested. Later roadmap slice 6 supersedes this baseline-only procedure with artifact qualification and promotion.

- [ ] **Step 3:** Document merge-back verification and branch policy expectations: `develop` becomes the repository's integration/default PR base, main and develop require the canonical validation check, and release merges preserve tested tree identity. Remote default-branch/rule changes are separately authorized operational actions. Read back their actual settings when performed. Do not claim local prose has activated Gitflow remotely.

- [ ] **Step 4:** Review the three guidance files together for contradictory base branches, nonexistent commands and missing bootstrap sequencing. Confirm the roadmap still defers public deployment until account/dev boundaries and runtime work ship. No source-string tests for the guidance are warranted. Stage and commit through the normal hook: `git commit -m "docs: define Gitflow and baseline release procedure"`.

- [ ] **Step 5:** Perform whole-branch review against this plan and approved spec, correct material defects and verify changed behavior with focused checks plus any subsequent hooked commit. Prepare a fully reviewable Draft PR against `main` when publication is authorized, with exact source head and validation evidence. Do not change it to Ready, merge, publish a release or configure the server as part of this plan. Preserve live parent roadmap/spec through this slice; follow completed-artifact custody once the plan's entire implementation scope is delivered.

**Exit:** An engineer can follow one coherent bootstrap, feature and baseline-release procedure. Source changes are reviewable with local gate evidence; hosted validation and remote branch/release actions are reported only when independently proven.

## Acceptance and handoff evidence

The execution handoff identifies the committed head, matching .NET/web version outputs, meaningful rejection tests, normal-hook result and any unresolved hosted evidence. Do not commit test counts or readiness ratings. Local plan completion establishes a release-ready foundation, not cloud playability, deployed environments or a published version. Human approval, remote merge/rules and first release publication are subsequent actions rather than unfinished code checkboxes.

## References

- [Semantic Versioning](https://semver.org/), including initial development versions and immutable publication.
- [MSBuild SDK properties](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props), for `VersionPrefix` inheritance and informational version.
- Local implementation owner: [implementing runbook](../runbooks/implementing.md); verification owner: [validation doctrine](../doctrine/validation-policy.md).
