import { useState } from "react";
import styled from "styled-components";
import { DevOverlay } from "../dev/DevOverlay";
import { DevSurfaceProvider } from "../dev/DevSurfaceContext";
import { useDevSurfaceSync } from "./useDevSurfaceSync";
import { ShellChrome } from "./AppShell";

export function PlaytestAppShell() {
  return (
    <DevSurfaceProvider>
      <PlaytestChrome />
    </DevSurfaceProvider>
  );
}

function PlaytestChrome() {
  const [devOverlayOpen, setDevOverlayOpen] = useState(false);
  useDevSurfaceSync();

  return (
    <ShellChrome
      toolbar={
        <DevNav aria-label="Developer tools">
          <DevToggleButton
            type="button"
            $active={devOverlayOpen}
            onClick={() => setDevOverlayOpen((prev) => !prev)}
            aria-expanded={devOverlayOpen}
          >
            {devOverlayOpen ? "Hide dev" : "Dev"}
          </DevToggleButton>
        </DevNav>
      }
      renderOverlay={(top) => (
        <DevOverlay open={devOverlayOpen} onClose={() => setDevOverlayOpen(false)} top={top} />
      )}
    />
  );
}

const DevNav = styled.nav`
  display: flex;
  gap: 6px;
`;

const DevToggleButton = styled.button<{ $active: boolean }>`
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 14px;
  border-radius: 999px;
  border: 1px solid ${(props) => (props.$active ? "var(--accent)" : "var(--border-strong)")};
  background: ${(props) => (props.$active ? "rgba(223, 159, 79, 0.14)" : "rgba(255, 255, 255, 0.03)")};
  color: ${(props) => (props.$active ? "var(--accent-strong)" : "var(--text)")};
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  min-height: 32px;
  cursor: pointer;
  transition-property: background-color, border-color, color;
  transition-duration: 120ms;
  transition-timing-function: ease-out;

  &::before {
    content: "";
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background: ${(props) => (props.$active ? "var(--accent)" : "var(--muted)")};
    flex-shrink: 0;
  }

  &:hover {
    border-color: var(--accent);
    background: ${(props) => (props.$active ? "rgba(223, 159, 79, 0.2)" : "rgba(223, 159, 79, 0.08)")};
    color: ${(props) => (props.$active ? "var(--accent-strong)" : "var(--accent-strong)")};
  }

  &:active {
    transform: scale(0.97);
  }
`;
