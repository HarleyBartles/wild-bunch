import styled from "styled-components";

interface SeedCodeEditorProps {
  seedDraft: string;
  seedDirty: boolean;
  decodeError: string | null;
  onSeedDraftChange: (value: string) => void;
}

export function SeedCodeEditor({ seedDraft, seedDirty, decodeError, onSeedDraftChange }: SeedCodeEditorProps) {
  return (
    <Field>
      <Label htmlFor="setup-seed">Setup seed</Label>
      <MonospaceInput
        id="setup-seed"
        type="text"
        value={seedDraft}
        onChange={(event) => onSeedDraftChange(event.target.value)}
        placeholder="00000000-0000-0000-0000-000000000000"
        spellCheck={false}
        autoCapitalize="off"
        autoComplete="off"
      />
      <Hint>
        Paste a UUID-shaped replay key, then click Apply to validate it. Randomize creates a fresh UUID.
      </Hint>
      {seedDirty ? <DraftNotice>Seed changes are staged until you apply them.</DraftNotice> : null}
      {decodeError ? <InlineError>{decodeError}</InlineError> : null}
    </Field>
  );
}

const Field = styled.div`
  display: grid;
  gap: 6px;
  grid-column: 1 / -1;
`;

const Label = styled.label`
  color: color-mix(in srgb, var(--text) 62%, transparent);
  font-size: 0.92rem;
`;

const baseControl = `
  width: 100%;
  border-radius: 14px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.04);
  color: var(--text);
  padding: 12px 14px;
  outline: none;

  &:focus {
    border-color: color-mix(in srgb, var(--accent) 55%, transparent);
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--accent) 18%, transparent);
  }
`;

const MonospaceInput = styled.input`
  ${baseControl}
  font-family: "SFMono-Regular", Consolas, "Liberation Mono", Menlo, monospace;
  letter-spacing: 0.03em;
`;

const Hint = styled.p`
  margin: 0;
  color: color-mix(in srgb, var(--text) 55%, transparent);
  font-size: 0.86rem;
`;

const DraftNotice = styled.p`
  margin: 0;
  color: color-mix(in srgb, var(--accent-strong) 90%, transparent);
  font-size: 0.84rem;
`;

const InlineError = styled.div`
  padding: 12px 14px;
  border-radius: 16px;
  background: color-mix(in srgb, var(--danger) 12%, transparent);
  border: 1px solid color-mix(in srgb, var(--danger) 24%, transparent);
  color: var(--danger-text);
`;
