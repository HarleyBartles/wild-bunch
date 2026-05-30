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

const MonospaceInput = styled.input`
  ${baseControl}
  font-family: "SFMono-Regular", Consolas, "Liberation Mono", Menlo, monospace;
  letter-spacing: 0.03em;
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
