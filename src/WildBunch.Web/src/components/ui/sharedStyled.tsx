import styled from "styled-components";

// Shared styled primitives — genuinely cross-surface (3+ unrelated surfaces).
// Feature-specific styling families stay local in the owning component.
// Design tokens are referenced via var(--token) from src/styles/_variables.scss.
// Variants use transient props ($variant, $wide) — not stringly class-style blobs.

export const Panel = styled.section<{ $wide?: boolean }>`
  grid-column: ${({ $wide }) => ($wide ? "1 / -1" : "auto")};
  background: var(--bg-elevated);
  border: 1px solid var(--border);
  box-shadow: var(--shadow);
  backdrop-filter: blur(18px);
  border-radius: var(--radius);
  padding: 22px;

  @media (max-width: 640px) {
    padding: 18px;
    border-radius: 22px;
  }
`;

export const PanelHead = styled.header`
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;

  h2 {
    margin-bottom: 0;
    font-size: 1.1rem;
  }
`;

export const PanelActions = styled.div`
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
`;

export const PanelSubtitle = styled.p`
  margin: 0;
  color: var(--muted);
`;

export const Eyebrow = styled.p`
  margin: 0 0 6px;
  color: var(--accent-strong);
  text-transform: uppercase;
  letter-spacing: 0.24em;
  font-size: 0.76rem;
`;

export const Muted = styled.p`
  margin: 0;
  color: var(--muted);
`;

type ButtonVariant = "primary" | "secondary" | "ghost";

export const Button = styled.button<{ $variant?: ButtonVariant }>`
  border: 1px solid
    ${({ $variant }) =>
      $variant === "ghost" || $variant === "secondary"
        ? "var(--border-strong)"
        : "color-mix(in srgb, var(--accent) 35%, transparent)"};
  background: ${({ $variant }) =>
    $variant === "primary"
      ? "linear-gradient(180deg, var(--accent-strong), var(--accent))"
      : $variant === "secondary" || $variant === "ghost"
        ? "transparent"
        : "linear-gradient(180deg, color-mix(in srgb, var(--accent) 96%, transparent), color-mix(in srgb, var(--accent-strong-dark) 96%, transparent))"};
  color: ${({ $variant }) =>
    $variant === "ghost" || $variant === "secondary" ? "var(--text)" : "var(--accent-ink)"};
  border-radius: 999px;
  padding: 10px 16px;
  font-weight: 700;
  transition-property: transform, background-color, border-color, box-shadow, color, opacity;
  transition-duration: 150ms;
  transition-timing-function: ease-out;

  ${({ $variant }) =>
    $variant === "primary"
      ? "border-color: color-mix(in srgb, var(--accent-strong) 55%, transparent);"
      : ""}

  &:hover:not(:disabled),
  &:focus-visible:not(:disabled) {
    box-shadow: 0 10px 24px rgba(0, 0, 0, 0.18);
  }

  &:active:not(:disabled) {
    transform: translateY(1px);
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
`;

export const Notice = styled.div`
  margin-bottom: 12px;
  padding: 12px 14px;
  border-radius: 14px;
  background: color-mix(in srgb, var(--success) 16%, transparent);
  border: 1px solid color-mix(in srgb, var(--success) 22%, transparent);
`;

export const Error = styled.div`
  margin-bottom: 12px;
  padding: 12px 14px;
  border-radius: 14px;
  background: color-mix(in srgb, var(--danger) 14%, transparent);
  border: 1px solid color-mix(in srgb, var(--danger) 26%, transparent);
`;

export const Stack = styled.div`
  display: grid;
  gap: 12px;
`;

export const StatusCard = styled.section`
  padding: 18px;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border);

  h3 {
    margin-bottom: 12px;
    font-size: 1rem;
  }
`;

export const StatList = styled.dl`
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  margin: 0;

  @media (max-width: 640px) {
    grid-template-columns: 1fr;
  }

  dt {
    color: var(--muted);
    font-size: 0.8rem;
    text-transform: uppercase;
    letter-spacing: 0.08em;
  }

  dd {
    margin: 3px 0 0;
    font-weight: 600;
  }
`;

export const Field = styled.div`
  display: grid;
  gap: 4px;

  label {
    font-size: 0.82rem;
    color: var(--muted);
    text-transform: uppercase;
    letter-spacing: 0.06em;
  }

  select,
  input {
    padding: 8px 10px;
    border-radius: 10px;
    border: 1px solid var(--border-strong);
    background: rgba(0, 0, 0, 0.25);
    color: var(--text);
    font-size: 0.94rem;
  }
`;

type FlowSurfaceVariant = "pre-session" | "town-hub" | "place" | "travel-prep" | "trail" | "arrival";

export const FlowSurface = styled.div<{ $variant?: FlowSurfaceVariant }>`
  display: grid;
  gap: 20px;
  max-width: ${({ $variant }) => ($variant === "pre-session" ? "720px" : "1100px")};
  margin: 0 auto;

  ${({ $variant }) =>
    $variant
      ? `padding: 8px 0 24px; align-content: start;`
      : ""}

  @media (max-width: 1366px) {
    max-width: ${({ $variant }) => ($variant === "pre-session" ? "720px" : "960px")};
  }

  @media (max-width: 960px) {
    max-width: 100%;
  }
`;

export const FlowNotice = styled.p`
  margin: 0;
  padding: 12px 14px;
  border-radius: 14px;
  background: color-mix(in srgb, var(--success) 16%, transparent);
  border: 1px solid color-mix(in srgb, var(--success) 22%, transparent);
  color: var(--success-text);
`;

export const FlowError = styled.p`
  margin: 0;
  padding: 12px 14px;
  border-radius: 14px;
  background: color-mix(in srgb, var(--danger) 14%, transparent);
  border: 1px solid color-mix(in srgb, var(--danger) 26%, transparent);
  color: var(--danger-text);
`;

export const BackButton = styled.button`
  border: 1px solid var(--border-strong);
  background: transparent;
  color: var(--text);
  border-radius: 999px;
  padding: 6px 14px;
  font-size: 0.84rem;
  font-weight: 600;
  cursor: pointer;
  transition: border-color 0.15s;

  &:hover {
    border-color: var(--accent);
  }
`;
