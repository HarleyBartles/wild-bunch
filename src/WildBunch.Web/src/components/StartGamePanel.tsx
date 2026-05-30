import { createContext, useContext, useEffect, useMemo, useState, type FormEvent } from "react";
import styled from "styled-components";
import type { GameSessionDto, StartGameRequest, TravelDifficulty } from "../api/types";
import { formatLoadoutProfile, formatTravelDifficulty } from "../ui/formatters";
import {
  createCanonicalSeedState,
  decodeGameSetupSeed,
  encodeGameSetupSeed,
  withDifficulty,
  withLoadoutProfile,
  withRandomEntropy,
  withStartWithHorse,
  type GameSetupLoadoutProfile,
  type GameSetupSeedState,
} from "../ui/gameSetupSeedCodec";

interface StartGamePanelProps {
  session: GameSessionDto | null;
  busy: boolean;
  gameId: string | null;
  resetToken: number;
  onStartGame: (request: StartGameRequest) => Promise<void>;
  onRefresh: () => Promise<void>;
}

interface StartGameContextValue {
  playerName: string;
  seedState: GameSetupSeedState;
  seedDraft: string;
  seedDirty: boolean;
  decodeError: string | null;
  submitError: string | null;
  session: GameSessionDto | null;
  busy: boolean;
  gameId: string | null;
  setPlayerName: (value: string) => void;
  setSeedDraft: (value: string) => void;
  setDifficulty: (difficulty: TravelDifficulty) => void;
  setStartWithHorse: (value: boolean) => void;
  setLoadoutProfile: (profile: GameSetupLoadoutProfile) => void;
  applySeed: () => Promise<void>;
  randomizeSeed: () => void;
  onRefresh: () => Promise<void>;
}

const StartGameContext = createContext<StartGameContextValue | null>(null);

function useStartGame() {
  const context = useContext(StartGameContext);
  if (!context) {
    throw new Error("Start game context is unavailable.");
  }

  return context;
}

function getErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message;
  }

  if (typeof error === "string" && error.trim()) {
    return error;
  }

  return "Unable to update the setup seed.";
}

function StartGameHeader() {
  const { busy, gameId, seedDirty, session } = useStartGame();

  return (
    <StartGameHeaderCard>
      <div>
        <Eyebrow>Seeded setup</Eyebrow>
        <Title>{gameId ? "Refine the next hunt" : "Start a new hunt"}</Title>
        <Lead>
          The setup seed stays visible and editable, so the player can paste a code, tune the v1 options, or let the game roll a fresh variant.
        </Lead>
      </div>
      <HeaderMeta>
        <MetaCard>
          <span>Session</span>
          <strong>{session ? session.player.name : "No active hunt"}</strong>
        </MetaCard>
        <MetaCard>
          <span>Status</span>
          <strong>{busy ? "Working" : seedDirty ? "Seed pending" : gameId ? "Ready to refresh" : "Ready to start"}</strong>
        </MetaCard>
      </HeaderMeta>
    </StartGameHeaderCard>
  );
}

function PlayerNameField() {
  const { playerName, setPlayerName } = useStartGame();

  return (
    <Field>
      <Label htmlFor="player-name">Player name</Label>
      <Input
        id="player-name"
        type="text"
        value={playerName}
        onChange={(event) => setPlayerName(event.target.value)}
        placeholder="Enter a rider name"
        autoComplete="off"
      />
    </Field>
  );
}

function SeedCodeField() {
  const { decodeError, seedDraft, seedDirty, setSeedDraft } = useStartGame();

  return (
    <Field>
      <Label htmlFor="setup-seed">Setup seed</Label>
      <MonospaceInput
        id="setup-seed"
        type="text"
        value={seedDraft}
        onChange={(event) => setSeedDraft(event.target.value)}
        placeholder="WB1-N-03-000000000000-0000"
        spellCheck={false}
        autoCapitalize="characters"
        autoComplete="off"
      />
      <Hint>
        Paste a code, then click Apply to decode it. Editing the options rewrites the applied seed.
      </Hint>
      {seedDirty ? <DraftNotice>Seed changes are staged until you apply them.</DraftNotice> : null}
      {decodeError ? <InlineError>{decodeError}</InlineError> : null}
    </Field>
  );
}

function DifficultyField() {
  const { seedState, setDifficulty } = useStartGame();

  return (
    <Field>
      <Label htmlFor="difficulty">Difficulty</Label>
      <Select id="difficulty" value={seedState.difficulty} onChange={(event) => setDifficulty(Number(event.target.value) as TravelDifficulty)}>
        <option value={0}>Normal</option>
        <option value={1}>Easy</option>
        <option value={2}>Hard</option>
      </Select>
    </Field>
  );
}

function OptionRow() {
  const { seedState, setLoadoutProfile, setStartWithHorse } = useStartGame();

  return (
    <OptionsRow>
      <Field>
        <Label htmlFor="start-with-horse">Start with horse</Label>
        <ToggleRow>
          <ToggleInput
            id="start-with-horse"
            type="checkbox"
            checked={seedState.startWithHorse}
            onChange={(event) => setStartWithHorse(event.target.checked)}
          />
          <ToggleLabel htmlFor="start-with-horse">
            {seedState.startWithHorse ? "Enabled" : "Disabled"}
          </ToggleLabel>
        </ToggleRow>
      </Field>

      <Field>
        <Label htmlFor="loadout-profile">Loadout profile</Label>
        <Select
          id="loadout-profile"
          value={seedState.loadoutProfile}
          onChange={(event) => setLoadoutProfile(Number(event.target.value) as GameSetupLoadoutProfile)}
        >
          <option value={0}>Standard</option>
          <option value={1}>Light</option>
          <option value={2}>Stocked</option>
        </Select>
      </Field>
    </OptionsRow>
  );
}

function SeedSummary() {
  const { seedState } = useStartGame();

  return (
    <SummaryCard>
      <SummaryItem>
        <dt>Difficulty</dt>
        <dd>{formatTravelDifficulty(seedState.difficulty)}</dd>
      </SummaryItem>
      <SummaryItem>
        <dt>Horse</dt>
        <dd>{seedState.startWithHorse ? "Enabled" : "Disabled"}</dd>
      </SummaryItem>
      <SummaryItem>
        <dt>Loadout</dt>
        <dd>{formatLoadoutProfile(seedState.loadoutProfile)}</dd>
      </SummaryItem>
      <SummaryItem>
        <dt>Entropy</dt>
        <dd>{seedState.entropy.toString(16).toUpperCase().padStart(12, "0")}</dd>
      </SummaryItem>
    </SummaryCard>
  );
}

function ActionBar() {
  const { applySeed, busy, gameId, onRefresh, randomizeSeed, seedDirty } = useStartGame();

  return (
    <ActionBarRow>
      <PrimaryButton type="submit" disabled={busy}>
        {busy ? "Starting..." : "Start new game"}
      </PrimaryButton>
      <GhostButton type="button" onClick={() => void applySeed()} disabled={!seedDirty || busy}>
        Apply seed
      </GhostButton>
      <GhostButton type="button" onClick={() => void randomizeSeed()} disabled={busy}>
        Randomize seed
      </GhostButton>
      <GhostButton type="button" onClick={() => void onRefresh()} disabled={!gameId || busy}>
        Refresh session
      </GhostButton>
    </ActionBarRow>
  );
}

export function StartGamePanel({ session, busy, gameId, resetToken, onStartGame, onRefresh }: StartGamePanelProps) {
  const [playerName, setPlayerName] = useState("");
  const [seedState, setSeedState] = useState(createCanonicalSeedState());
  const [seedDraft, setSeedDraft] = useState("");
  const [seedDirty, setSeedDirty] = useState(false);
  const [decodeError, setDecodeError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    setPlayerName(session?.player.name ?? "");
  }, [session?.id, session?.player.name, resetToken]);

  useEffect(() => {
    if (resetToken === 0) {
      return;
    }

    const resetSeed = createCanonicalSeedState();
    setSeedState(resetSeed);
    setSeedDirty(false);
    setDecodeError(null);
  }, [resetToken]);

  useEffect(() => {
    let cancelled = false;

    void encodeGameSetupSeed(seedState)
      .then((seedCode) => {
        if (!cancelled) {
          setSeedDraft(seedCode);
        }
      })
      .catch((error) => {
        if (!cancelled) {
          setDecodeError(getErrorMessage(error));
        }
      });

    return () => {
      cancelled = true;
    };
  }, [seedState]);

  async function applySeed() {
    if (!seedDirty) {
      return;
    }

    try {
      const decoded = await decodeGameSetupSeed(seedDraft);
      setDecodeError(null);
      setSeedDirty(false);
      setSeedState({
        difficulty: decoded.difficulty,
        startWithHorse: decoded.startWithHorse,
        loadoutProfile: decoded.loadoutProfile,
        entropy: decoded.entropy,
      });
    } catch (error) {
      setDecodeError(getErrorMessage(error));
    }
  }

  const contextValue = useMemo<StartGameContextValue>(
    () => ({
      playerName,
      seedState,
      seedDraft,
      seedDirty,
      decodeError,
      submitError,
      session,
      busy,
      gameId,
      setPlayerName,
      setSeedDraft: (value) => {
        setSubmitError(null);
        setDecodeError(null);
        setSeedDraft(value);
        setSeedDirty(true);
      },
      setDifficulty: (difficulty) => {
        setSubmitError(null);
        setDecodeError(null);
        setSeedDirty(false);
        setSeedState((current) => withDifficulty(current, difficulty));
      },
      setStartWithHorse: (value) => {
        setSubmitError(null);
        setDecodeError(null);
        setSeedDirty(false);
        setSeedState((current) => withStartWithHorse(current, value));
      },
      setLoadoutProfile: (profile) => {
        setSubmitError(null);
        setDecodeError(null);
        setSeedDirty(false);
        setSeedState((current) => withLoadoutProfile(current, profile));
      },
      randomizeSeed: () => {
        setSubmitError(null);
        setDecodeError(null);
        setSeedDirty(false);
        setSeedState((current) => withRandomEntropy(current));
      },
      applySeed,
      onRefresh,
    }),
    [applySeed, busy, decodeError, gameId, onRefresh, playerName, resetToken, seedDraft, seedDirty, seedState, session, submitError],
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmedName = playerName.trim();
    if (!trimmedName) {
      setSubmitError("Enter a player name to start.");
      return;
    }

    setSubmitError(null);
    const seedCode = await encodeGameSetupSeed(seedState);
    await onStartGame({
      playerName: trimmedName,
      travelDifficulty: seedState.difficulty,
      seedCode,
    });
  }

  return (
    <StartGameContext.Provider value={contextValue}>
      <StartGameStage>
        <StartGameHeader />

        {submitError ? <PanelError>{submitError}</PanelError> : null}

        <StartGameForm onSubmit={(event: FormEvent<HTMLFormElement>) => void handleSubmit(event)}>
          <DraftGrid>
            <PlayerNameField />
            <SeedCodeField />
            <DifficultyField />
            <OptionRow />
          </DraftGrid>

          <SeedSummary />
          <ActionBar />
        </StartGameForm>
      </StartGameStage>
    </StartGameContext.Provider>
  );
}

const StartGameStage = styled.article`
  display: grid;
  gap: 18px;
  padding: 22px;
  border-radius: 28px;
  border: 1px solid rgba(228, 186, 126, 0.2);
  background:
    radial-gradient(circle at top left, rgba(236, 203, 146, 0.14), transparent 28%),
    linear-gradient(180deg, rgba(29, 23, 16, 0.98), rgba(16, 12, 8, 0.98));
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.34);
`;

const StartGameHeaderCard = styled.header`
  display: flex;
  justify-content: space-between;
  gap: 18px;
  align-items: end;
`;

const Eyebrow = styled.p`
  margin: 0 0 6px;
  color: #efc37e;
  text-transform: uppercase;
  letter-spacing: 0.22em;
  font-size: 0.74rem;
`;

const Title = styled.h3`
  margin: 0 0 8px;
  font-family: "Iowan Old Style", Georgia, serif;
  font-size: clamp(2rem, 3.8vw, 3rem);
  line-height: 0.98;
`;

const Lead = styled.p`
  max-width: 72ch;
  margin: 0;
  color: rgba(242, 239, 232, 0.75);
`;

const HeaderMeta = styled.div`
  display: grid;
  gap: 10px;
  width: min(100%, 280px);
`;

const MetaCard = styled.div`
  display: grid;
  gap: 4px;
  padding: 12px 14px;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);

  span {
    color: rgba(242, 239, 232, 0.62);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    font-size: 0.75rem;
  }
`;

const StartGameForm = styled.form`
  display: grid;
  gap: 16px;
`;

const DraftGrid = styled.div`
  display: grid;
  gap: 14px;
  grid-template-columns: repeat(2, minmax(0, 1fr));
`;

const Field = styled.div`
  display: grid;
  gap: 6px;
`;

const Label = styled.label`
  color: rgba(242, 239, 232, 0.62);
  font-size: 0.92rem;
`;

const baseControl = `
  width: 100%;
  border-radius: 14px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.04);
  color: #f2efe8;
  padding: 12px 14px;
  outline: none;

  &:focus {
    border-color: rgba(223, 159, 79, 0.55);
    box-shadow: 0 0 0 3px rgba(223, 159, 79, 0.18);
  }
`;

const Input = styled.input`
  ${baseControl}
`;

const MonospaceInput = styled.input`
  ${baseControl}
  font-family: "SFMono-Regular", Consolas, "Liberation Mono", Menlo, monospace;
  letter-spacing: 0.03em;
`;

const Select = styled.select`
  ${baseControl}
`;

const Hint = styled.p`
  margin: 0;
  color: rgba(242, 239, 232, 0.55);
  font-size: 0.86rem;
`;

const DraftNotice = styled.p`
  margin: 0;
  color: rgba(239, 195, 126, 0.9);
  font-size: 0.84rem;
`;

const InlineError = styled.div`
  padding: 12px 14px;
  border-radius: 16px;
  background: rgba(240, 126, 110, 0.12);
  border: 1px solid rgba(240, 126, 110, 0.24);
  color: #ffe8e3;
`;

const PanelError = styled.div`
  padding: 12px 14px;
  border-radius: 16px;
  background: rgba(240, 126, 110, 0.14);
  border: 1px solid rgba(240, 126, 110, 0.26);
  color: #ffe4de;
`;

const OptionsRow = styled.div`
  display: grid;
  gap: 14px;
  grid-template-columns: repeat(2, minmax(0, 1fr));
`;

const ToggleRow = styled.div`
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 48px;
  padding: 0 2px;
`;

const ToggleInput = styled.input`
  width: 18px;
  height: 18px;
  accent-color: #df9f4f;
`;

const ToggleLabel = styled.label`
  color: rgba(242, 239, 232, 0.82);
`;

const SummaryCard = styled.dl`
  display: grid;
  gap: 10px;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin: 0;
  padding: 16px;
  border-radius: 20px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(255, 255, 255, 0.03);
`;

const SummaryItem = styled.div`
  dt {
    color: rgba(242, 239, 232, 0.58);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    font-size: 0.74rem;
  }

  dd {
    margin: 4px 0 0;
    font-weight: 600;
  }
`;

const ActionBarRow = styled.div`
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
`;

const ButtonBase = styled.button`
  border-radius: 999px;
  padding: 10px 16px;
  font-weight: 700;
  border: 1px solid transparent;

  &:disabled {
    cursor: not-allowed;
    opacity: 0.55;
  }
`;

const PrimaryButton = styled(ButtonBase)`
  background: linear-gradient(180deg, #efc37e, #bf7a35);
  color: #1b1308;
  border-color: rgba(239, 195, 126, 0.55);
`;

const GhostButton = styled(ButtonBase)`
  background: transparent;
  color: #f2efe8;
  border-color: rgba(255, 255, 255, 0.16);
`;
