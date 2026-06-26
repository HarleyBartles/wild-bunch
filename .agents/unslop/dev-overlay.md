# Wild Bunch Dev Overlay Unslop Profile

Repo-specific anti-slop profile for Wild Bunch dev overlay work.

Use this profile before designing, implementing, reviewing, or dispatching work on dev overlay panels, dev-only playtest controls, `/api/dev/` inspection, hidden-truth debug surfaces, contextual panel defaults, browser proof, or generated agent evidence.

This profile does not replace the backend architecture unslop profile or the web play-surface unslop profile. Use those as the authority for backend boundaries and player-facing UI. This profile only covers the dev overlay's own failure modes.

For durable dev overlay doctrine, read the repo's dev-overlay doctrine surface. This profile is the slop filter used to catch common AI shortcuts and review failures against that doctrine.

## Purpose

The dev overlay exists to help Harley playtest real game states and hard-to-reach branches without corrupting the game model.

Dev overlay slop is usually not "bad styling" or "missing architecture." It is AI taking shortcuts:

- making a universal debug cockpit instead of contextual playtest controls;
- typing raw IDs instead of selecting domain candidates;
- creating fake categories because they sound useful;
- forcing final outcomes instead of setting up normal gameplay;
- exposing hidden truth in unclear places;
- using frontend state as if it were game truth;
- committing screenshots or generated proof into the repo;
- claiming GREEN from tests without proving the actual playtest loop.

A control that merely exposes internal state is not enough unless it helps choose, reproduce, verify, or falsify a meaningful game state.

## Source profiles to apply with this one

Always apply the repo-resident backend architecture unslop profile for backend/domain/API/persistence/event questions.

Always apply the repo-resident web play-surface unslop profile when a player-facing surface, HUD/shell behavior, overlay behavior, React state ownership, copy, accessibility, or responsive behavior is touched.

For dev overlay work, apply this profile in addition to those. Do not duplicate them inside this document.

## Quick scan

Before touching dev overlay code, answer:

- What playtest problem does this control solve?
- What current game surface or domain node owns this panel?
- Is this deep control of the owned node, or light context from a related node?
- Does the control set up normal gameplay, or wrongly fabricate the outcome?
- Is the data player-known, hidden truth, developer-only audit, or ordinary UI state?
- Is the UI showing domain labels, or asking the worker to type raw IDs?
- What proves the whole loop: dev setup, normal gameplay consumption, visible result, replay/persistence if relevant?
- Where will browser evidence be stored, and is it git-ignored?

## Core rule

A dev overlay panel is a contextual playtest workbench for the current game surface.

It is not:

- a global debug cockpit;
- a universal editor;
- a raw aggregate dump;
- a shortcut around normal gameplay;
- a place to invent missing domain categories;
- a substitute for backend authority;
- a reason to commit screenshots or generated evidence.

## Golden shape

A good dev overlay slice usually looks like this:

1. The game surface determines the contextual panel set.
2. The strongest surface owner defaults first.
3. The panel shows enough state to understand the current playtest branch.
4. Dev controls use domain-facing choices, not raw IDs.
5. Mutating controls call `/api/dev/` command endpoints.
6. The backend records dev setup as aggregate-owned state/events where required.
7. The next normal player action consumes that setup.
8. The overlay refreshes from backend state after command success.
9. Browser evidence proves the UI behavior.
10. Evidence is saved under the repo's ignored `.agents/superpowers/output/...` area, not committed.

## Avoid patterns

### 1. Global cockpit gravity

Avoid turning the dev overlay into a giant always-available dashboard just because the data is useful.

Bad:

- Session Audit defaults everywhere.
- Every panel appears on every surface.
- Saloon work is hidden behind a generic debug tab.
- Dev overlay opens into a screen-dominating workbench by default.

Prefer:

- The current surface owns the default panel.
- Saloon surface defaults to Saloon dev.
- Trail surface defaults to Travel dev.
- Session/audit panels stay available where useful but do not steal focus.
- Compact mode opens first; expanded mode is opt-in.

Acceptance check:

- On a game surface with a specific owner, that owner wins the default panel.

### 2. Universal editor panels

Avoid one panel that edits everything.

Bad:

- `Session Editor`
- `Entity Inspector`
- `Game State Manager`
- one JSON editor for all aggregate state
- controls for suspect, casefile, saloon, inventory, travel, and wallet in one panel

Prefer:

- Saloon dev owns saloon encounter/playtest state.
- Travel dev owns journey/trail-day setup and inspection.
- Casefile dev owns player-known case state.
- Suspect dev owns suspect truth/configuration once that panel exists.
- Player/inventory dev owns wallet/inventory/player condition once that panel exists.

A panel may show light related context, but deep mutation belongs with the owning node.

Acceptance check:

- Every control can answer: "Which domain node owns this?"

### 3. Placeholder panel sprawl

Avoid creating shallow panels only to satisfy a doctrine phrase.

Bad:

- Empty `SuspectDevPanel`, `CasefileDevPanel`, and `PlayerDevPanel` with generic cards.
- Related panels that expose raw DTOs because the real owner model is not shaped.
- Placeholder controls that do not help a current playtest branch.

Prefer:

- Implement the panel owned by the slice.
- Show light related context where useful.
- Record follow-up panels explicitly when they need real design.

Acceptance check:

- Every added panel has real controls or inspection value for a current playtest branch.

### 4. Raw ID forcing

Avoid making the worker type IDs when the backend can provide candidates.

Bad:

```tsx
<input placeholder="suspect-2" />
<button>Force suspect</button>
```

Prefer:

```tsx
<select>
  <option value="">Any eligible suspect (normal selection)</option>
  <option value="suspect-2">Sundance Kid - $500 - Has no eyebrows</option>
</select>
```

Raw IDs may appear as fallback debug detail, but they should not be the primary control when domain labels are available.

Acceptance check:

- A playtester can choose the intended target by name/domain facts, not by memorizing IDs.

### 5. Fake control categories

Avoid preserving UI choices that do not correspond to distinct backend behavior.

Bad:

```csharp
enum DevSaloonPoiKind
{
    Suspect,
    Citizen,
    FalseLead
}
```

If `FalseLead` just produces a citizen, it is not a distinct force kind. It is a later gameplay outcome from a wrong declaration against a citizen POI.

Prefer:

```csharp
enum DevSaloonPoiKind
{
    Suspect,
    Citizen
}
```

Then test false lead by forcing Citizen and making the wrong normal gameplay declaration.

Acceptance check:

- Every visible option has distinct domain behavior.
- No option exists only because it sounds useful for testing.

### 6. Control-label dishonesty

Avoid labels that imply a broader or different action than the control performs.

Bad:

- `Force suspect`
- `Release culprit`
- `Make false lead`
- `Set active POI`

Prefer:

- `Force next saloon look-around POI`
- `Any eligible suspect (normal selection)`
- `Clear pending override`

Acceptance check:

- The label tells the reviewer what normal gameplay action will consume or observe the setup.

### 7. Final-outcome forcing

Avoid dev controls that skip the normal game action being tested.

Bad:

- Force the saloon confrontation result directly.
- Mark the suspect taken in from the dev panel.
- Fabricate the journal entry as if gameplay happened.
- Flip the read model without running the normal action.

Prefer:

- Force the next saloon look-around POI.
- Let `LookAroundSaloon` consume the override.
- Let the normal confrontation/take-in flow produce its own outcome.
- Inspect the resulting state afterward.

Acceptance check:

- The dev control sets up the branch; normal gameplay still exercises the branch.

### 8. Hidden truth ambiguity

Dev overlay may show hidden truth, but it must be explicit and contained.

Bad:

- hidden culprit facts appear without a "dev only" frame;
- normal player API fields are reused for hidden-truth display;
- UI copy says temporary gate behavior is permanent lore;
- the dev panel mixes player-known facts and hidden truth without labels.

Prefer:

- section labelled `Hidden truth (dev only)`;
- diagnostic copy that says what the backend currently does;
- separate player-known/casefile state from culprit truth;
- gate-aware language instead of permanent "never" claims.

Example:

Bad:

```text
The true culprit can never appear as a saloon POI.
```

Better:

```text
Gated out - killer trail is locked.
```

Acceptance check:

- A reviewer can tell which facts are player-known and which are dev-only hidden truth.

### 9. Stale doctrine fossilization

Avoid turning a temporary current-state mechanic into permanent design doctrine.

Bad:

- "The true culprit must never appear."
- "Killer release is clue-based" presented as desired design when it is only current backend behavior.
- "FalseLead is a POI kind" because an earlier implementation said so.

Prefer:

- Current-state diagnostics are honest.
- Intended-design mismatches are named as follow-up, not silently fixed or enshrined.
- Tests assert the intended rule for this slice, not stale copy from a previous design.

Acceptance check:

- UI copy, tests, and comments do not preserve a rule Harley has explicitly corrected.

### 10. Context mismatch laundering

Avoid making the dev panel look coherent when the UI surface and aggregate state disagree.

Bad:

- Saloon panel silently renders while aggregate context is `SheriffOffice`.
- Source spent/current POI state contradicts the visible action surface.
- Dev UI hides stale transition bugs by inventing a friendly state.

Prefer:

- Show a clear mismatch warning.
- Fix the transition if the mismatch is caused by this slice.
- Return AMBER if the mismatch is outside scope but affects proof.

Acceptance check:

- Dev overlay helps reveal stale state contradictions; it does not smooth them over.

### 11. Compact/expanded visual inversion

Avoid technically correct state labels that are visually contradicted by layout.

Bad:

```tsx
height: expanded ? "60dvh" : "auto";
```

This can make compact mode grow taller than expanded mode.

Prefer:

```tsx
height: expanded ? "80dvh" : "40dvh";
```

Compact/expanded is about available height, not whether columns are allowed.

Bad:

```tsx
grid-template-columns: expanded ? "1fr 1fr" : "1fr";
```

Prefer:

```tsx
grid-template-columns: 1fr 1fr;

@media (max-width: 700px) {
  grid-template-columns: 1fr;
}
```

Acceptance check:

- Compact opens by default and says `Expand`.
- Expanded says `Shrink`.
- Both modes use two columns when width allows.
- Narrow width collapses to one column.

### 12. Frontend-authoritative dev state

Avoid making the panel look correct before the backend has actually changed.

Bad:

```tsx
await forceOverride(request);
setPendingOverride(request);
setActivePoi(fakePoi);
```

Prefer:

```tsx
await forceOverride(request);
queryClient.invalidateQueries({ queryKey: ["dev-saloon-context", gameId] });
```

Acceptance check:

- After a dev command, the panel refreshes from backend truth.

### 13. Screenshot and generated-evidence repo pollution

Browser evidence is required for visual dev overlay work, but screenshots are generated output.

Bad:

- commit `docs/superpowers/screenshots/*.png`;
- create root `.work/screenshots`;
- add screenshots to PR as durable docs;
- leave stale screenshot folder indexes after deleting images.

Prefer:

- store generated evidence under `.agents/superpowers/output/screenshots/`;
- ignore generated contents with a local `.gitignore`;
- keep only navigational/control files tracked if needed;
- summarize or attach screenshots through review tooling, not as repo files.

Preferred local ignore pattern:

```gitignore
*
!.gitignore
!INDEX.md
```

This applies to generated evidence generally: screenshots, traces, logs, temporary exports, Playwright captures, and similar agent output.

Acceptance check:

- PR changed-file list contains no screenshot/image/generated evidence files.
- Agents mesh tells future workers where evidence goes.
- Index mesh remains honest.

### 14. PR-body proof drift

Avoid stale PR descriptions that point to deleted files or describe earlier behavior.

Bad:

- PR says screenshots are in `docs/superpowers/screenshots/` after they were moved to ignored local output.
- PR says compact/expanded screenshots prove one thing while filenames or implementation prove another.
- PR body claims "FalseLead removed" while API still accepts `FalseLead`.

Prefer:

- PR body describes current implementation only.
- Evidence locations are local/ignored paths or review attachments.
- Claims can be checked against changed files.

Acceptance check:

- PR body does not contradict the changed-file list.

### 15. GREEN from test volume

Avoid treating a broad test run as proof of the dev overlay goal.

Bad:

```text
All tests pass. GREEN.
```

Prefer:

```text
Behavior proof:
- Compact opens by default with Expand.
- Expanded after click shows Shrink.
- Two-column layout is width-based.
- Force Suspect sets pending override.
- Normal saloon look-around consumes it once.
- Hidden truth appears only in /api/dev context.
- No screenshots are committed.

Validation:
- dotnet build...
- domain tests...
- application tests...
- integration tests...
- vitest...
- tsc...
```

Acceptance check:

- Worker return proves the playtest loop, not just the validation suite.

## Positive examples from BUNCH-90

### Good: candidate dropdowns

A useful saloon dev panel replaced raw suspect ID entry with a domain-facing candidate dropdown. It showed suspect names and useful facts while submitting stable IDs internally.

Why it is good:

- Faster playtesting.
- Fewer typo paths.
- Less raw backend leakage.
- More honest domain UI.

### Good: gate-aware true culprit copy

The stale "true culprit can never appear" rule was replaced by gate-aware eligibility. Locked gate means ineligible now; released gate means the culprit can become eligible if domain rules allow it.

Why it is good:

- It avoids fossilizing a temporary constraint.
- It keeps dev tools aligned with intended game progression.
- It lets tests prove both sides of the gate.

### Good: false lead removed as force kind

`FalseLead` was removed as a dev override kind once source inspection showed it was semantically just Citizen. False lead is tested by forcing Citizen and then making a wrong normal declaration.

Why it is good:

- The UI no longer presents fake domain categories.
- The model stays smaller and clearer.
- The normal game loop remains the source of confrontation outcomes.

### Good: width-based two-column layout

The saloon dev panel uses two columns when width allows, regardless of compact/expanded state. Compact controls height; width controls columns.

Why it is good:

- Compact remains useful.
- Expanded is not required just to use available horizontal space.
- The overlay behaves like a pull-down workbench instead of a one-column debug drawer.

## Negative examples from BUNCH-90

### Bad: compact mode with `height: auto`

Compact mode grew tall enough to look expanded. The button label was technically correct, but the visual result contradicted it.

Stop this because:

- reviewers cannot trust the state;
- the overlay dominates the play surface;
- screenshots become misleading.

### Bad: committed screenshots under `docs/`

Screenshots were added as repo files under `docs/superpowers/screenshots/`.

Stop this because:

- screenshot proof is generated evidence, not durable source;
- binary artifacts pollute repo history;
- future workers will copy the pattern.

### Bad: hiding a fake option only in frontend

A visible UI option can be removed while the API/domain still accepts it. That is not cleanup; it is hidden slop.

Stop this because:

- future agents may build on the hidden API shape;
- tests may still enshrine the fake category;
- source truth remains inconsistent.

## Review questions

Ask these before accepting dev overlay work:

1. What playtest branch does this make easier to reach or inspect?
2. Which game surface owns the panel?
3. Which domain node owns each deep control?
4. Does the control set up normal gameplay rather than fabricate the final outcome?
5. Does the UI avoid raw ID typing where domain candidates exist?
6. Are visible options real backend/domain distinctions?
7. Do labels accurately describe what the controls do and what normal gameplay action consumes or observes the setup?
8. Is hidden truth clearly dev-only and absent from normal player APIs?
9. Does compact mode open by default and remain visually compact?
10. Does layout use width when available, independent of expanded state?
11. Does the panel refresh from backend state after commands?
12. Does the panel reveal context/state mismatches instead of laundering them?
13. Does browser evidence exist for layout/interaction changes?
14. Is generated evidence stored under ignored `.agents/superpowers/output/...`?
15. Are screenshots and generated evidence absent from the PR changed-file list?
16. Does the PR body match the actual implementation and evidence handling?
17. Did the worker prove the playtest loop, not just tests passing?

## Automatic AMBER triggers

Return AMBER if any are true:

- A panel has no clear owned game surface or domain node.
- Session Audit/global debug defaults over a more specific current-surface panel.
- The UI asks for raw IDs when backend candidates are available.
- A fake force kind exists only because it sounds useful.
- A control label implies a broader or different action than the control performs.
- Dev commands directly fabricate normal gameplay outcomes.
- Hidden truth appears without dev-only framing.
- Current-state diagnostics are written as permanent design doctrine.
- The overlay hides stale UI-surface versus aggregate-state contradictions.
- Compact opens as a tall screen-dominating panel.
- Two-column layout is available only in expanded mode despite enough width.
- Frontend state pretends a backend mutation happened.
- Placeholder panels are added without real inspection/control value.
- Screenshot/image/generated evidence is committed to the repo.
- Agent output is created outside `.agents/`.
- PR body cites stale screenshot paths or deleted evidence.
- Worker claims GREEN from validation commands without proving the actual dev overlay loop.

## What this profile intentionally does not duplicate

Backend architecture rules remain in the backend unslop profile:

- aggregate authority;
- CQRS separation;
- event sourcing/replay;
- persistence shape;
- projection safety;
- hidden truth leakage through player APIs;
- test confidence theater.

Web play-surface rules remain in the web unslop profile:

- player-facing game-native surfaces;
- dashboard drift;
- product chrome;
- state ownership;
- accessibility/focus/mobile behavior;
- frontend-invented player truth.

This profile only adds the dev-overlay-specific review filter between those two.
