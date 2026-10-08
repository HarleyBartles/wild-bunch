# Hunt creation contract: PG-001

**Status:** Working design record for the stable 0.1.0 feature discussion. The user has established the setup inputs and semantic startup progression; implementation details remain under discussion. No application changes are made by this record.

## Player story and required facts

As a player starting a new playthrough, I can enter my character's name and start with the default game settings. I can optionally change difficulty, randomness and the world seed before starting.

The signed-in account identifies the user who owns playthroughs. The character name identifies the persona the user inhabits in this playthrough, such as Clint Eastwood. It is not a requirement for a profile system or a replacement for account ownership.

| Fact | Purpose | Required player input |
| --- | --- | --- |
| Character name | Names the identity the player inhabits and supports immersion. | Enter a name. |
| Difficulty | A gameplay variability and replay lever. | Optional; resolve a default. |
| Randomness, currently represented as entropy | A separate gameplay variability and replay lever. | Optional; resolve a default. |
| Seed UUID | Represents the starting world and provides another replay lever. | Optional; generate a fresh UUID for each new visit to the starting page, while allowing editing. |

The game needs all four resolved facts; the player only needs to supply the name. The current defaults are Standard difficulty and Classic entropy. No change to those particular defaults has been requested in this discussion.

## Acceptance direction

A fresh setup visit offers a freshly generated seed, so repeatedly taking the default quick-start route does not always select the same starting world. Editing settings and rendering the same draft must not silently replace its seed. An explicitly edited seed must be validated and used by the setup command; invalid input must not silently fall back to another seed.

The server owns the resulting playthrough facts through its events. Resuming an existing playthrough uses those facts rather than regenerating a world or substituting default difficulty/randomness. The seed describes world generation; this contract does not assert that the seed alone determines every later outcome or replay compatibility across versions.

Behavioral coverage should establish that name-only input submits resolved defaults and a fresh seed; an edited valid seed is used exactly; invalid input does not create a playthrough; resumed setup uses recorded facts; and pending/retry behavior cannot create unintended duplicate playthroughs. Tests must use lawful startup phases rather than a fixture that jumps directly to GameStarted for every operation.

## Agreed startup progression

The semantic flow is setup -> Go -> prologue -> starting-town selection -> arrival in the selected town. Once the player submits setup successfully, they are playing the game. Go creates the game and settles its world, gang, identifying characteristics, culprit and case facts on the server. At the prologue, these starting facts are already authoritative; the player's location remains unresolved until their first town choice. Future lawman location is also unresolved, but that feature is not implemented and is not added to this slice. The prologue supplies the backstory and contains the initial case lead in its text. Ride on advances the opening flow; it does not reveal a separate clue or generate another case. Selecting the starting town is the player's first free travel, with no journey time. Preserve this sequence because it follows the original game being tributed. Do not skip the prologue or automatically select the starting town as a quick-start shortcut.

Starting-town selection uses the same world/travel map as later travel. It is a different travel context with an initial free selection rule, not a separate starting map or a separately generated world. This product meaning does not require routing first travel through every later journey mechanic; the implementation must preserve the same world, explicit player town choice, lawful arrival and zero initial journey time.

The current implementation separates setup, prologue acknowledgement, starting-town selection and completion of game start. In the domain, SelectStartingTown requires PrologueViewed, and CompleteGameStart requires StartingTownSelected. GameStarted and other technical phase names must not redefine the product boundary: prologue and initial town selection are already gameplay. Reconcile naming and phase ownership during implementation design without collapsing the sequence.

Behavioral coverage must prove that Go enters the prologue, continuing it offers town selection on the same world's travel map, and selecting a legal starting town establishes arrival without journey time. Resume must restore the appropriate phase and its recorded world/backstory. Negative coverage must protect phase order and reject invalid town choices; avoid tests that require particular component names, separate map implementations or incidental HTTP choreography.

Known implementation gaps are tracked as WB-05/06/07 in the [Web investigation](2026-10-07-web-layer-investigation.md), with related coverage in the [Web test follow-up](2026-10-07-web-test-followup.md). The [feature matrix](../../../docs/features.md) owns the current capability grouping and disposition.

## Proposed code changes against today's source

### Setup draft and submission

Change useStartGameSeed to initialize/reset each new setup draft with a random UUID instead of createCanonicalSeedState's all-zero UUID. Keep Standard/Classic defaults. Consolidate seedDraft and seedState so the value visibly edited is the value validated and submitted; the current change handler only updates seedDraft, while PreSessionSurface submits seedState. Remove redundant dirty state and unused encoding/request helpers where consolidation makes them unnecessary. SetupHuntStep should make name-only start obvious and describe the remaining settings as optional. Validate required name, UUID and supported difficulty/entropy at the API boundary as well as providing useful form feedback.

### Prologue belongs to the created playthrough

Replace the live seed/options-based prologue request with a session-scoped query. StorySoFarStep should supply session identity, not browser setup values. GetPrologueHandler currently generates a fresh case from supplied parameters; ViewPrologueHandler also regenerates the descriptor from session seed/options. The prologue query must use a player-safe projection of the playthrough facts settled by Go. Continuing the prologue records progression only; remove its responsibility for regenerating or newly revealing the initial lead. The initial lead is part of the established backstory, already displayed in the prologue. Keep CQRS strict; do not solve the query by loading a command aggregate or expose hidden culprit identifiers to the browser. Reading the prologue must not emit acknowledgement events; continuing it does. Reconcile the existing PrologueViewed payload and historical consumers through the migration strategy rather than treating its RevealedSuspectIdentifier field as a product requirement. Exact narrative preservation across content versions is a separate compatibility design question, not permission to invent an event schema here.

### One phase authority, explicit pending and failure

PreSessionSurface derives a server phase and also calls useStartFlow.advance/goToStep. Remove the second persisted-progress authority; derive progression from successful command responses/read state. Retain only transient form/selection state locally. Cache the setup response immediately, as later commands already do, and track each command's pending state. Prevent repeated Go/Continue/town clicks while pending, display recoverable command/query errors, and remain in the lawful phase on failure. StartingTownStep currently treats query errors and empty results as endless loading; distinguish those states and provide recovery. Lost-response creation retries need a server-level idempotency decision; a disabled button alone cannot prove exactly-once creation.

### One map, two selection contexts

StartingTownStep and TravelPrepSurface already use PhaserMapHost. Both API map routes already invoke GetStartingTownMapHandler against the saved world. Consolidate these aliases/read DTO names/query keys into a world-map capability, using IGameSessionReadRepository rather than the current command repository in the query. Move the renderer out of components/start-flow into the world-map slice. Opening selection and later travel supply their own lawful selectable destinations and command callbacks. Share the world/map contract without merging first arrival into timed journey processing.

### Preserve event-backed initial arrival

Keep the existing setup/world -> prologue acknowledgement -> StartingTownSelected -> GameStarted event sequence for now. CompleteGameStartHandler already selects the town and commits arrival together; Apply(GameStarted) places the player and initializes the town without advancing the clock or creating a journey. Correct phase/retry guards where needed, including ViewPrologue accepting a StartingTownSelected phase and SelectStartingTown silently ignoring a different repeated choice. Clarify current comments/UI terminology: GameStarted is the historical technical event name for completing initial arrival, while gameplay begins at successful setup. Event renaming or payload changes require an explicit migration decision, not cosmetic replacement.

### Behavioral proof and dependency boundary

Replace StartFlow's always-started fixtures with lawful phase transitions. Cover fresh/default and edited-seed starts, resume at prologue and town choice, actual-case prologue consistency, command failures without false advance, invalid phase/town rejection, and replay of initial arrival with unchanged clock and no journey. Preserve useful existing negative tests and extend only genuine gaps. Map tests should establish the same world's identities/layout across opening and later travel and meaningful selection rules, rather than preserving starting-specific class names or duplicate endpoint shapes.

This slice does not provide account isolation. CompletePlayerSetupHandler currently archives every active session returned by an unscoped status query. Account ownership, per-user active-playthrough selection and creation idempotency must be resolved before claiming safe public multiplayer; record that dependency alongside PG-001/PG-002 rather than treating a browser form correction as delivery of sign-in.
