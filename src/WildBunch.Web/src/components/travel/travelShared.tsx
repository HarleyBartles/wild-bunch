import styled from "styled-components";

export function formatSignedNumber(value: number, digits = 0) {
  const formatted = value.toFixed(digits);
  return value > 0 ? `+${formatted}` : formatted;
}

export function getErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message;
  }

  if (typeof error === "string" && error.trim()) {
    return error;
  }

  return "";
}

export const Card = styled.section`
  display: grid;
  gap: 14px;
  padding: 18px;
  border-radius: 22px;
  background: rgba(255, 255, 255, 0.035);
  border: 1px solid rgba(255, 255, 255, 0.08);
`;

export const SectionHeader = styled.div`
  display: flex;
  justify-content: space-between;
  gap: 14px;
  align-items: baseline;

  strong {
    font-size: 1rem;
  }

  span {
    color: color-mix(in srgb, var(--text) 62%, transparent);
    font-size: 0.9rem;
  }
`;

export const ButtonBase = styled.button`
  border-radius: 999px;
  padding: 10px 16px;
  font-weight: 700;
  border: 1px solid transparent;
`;
