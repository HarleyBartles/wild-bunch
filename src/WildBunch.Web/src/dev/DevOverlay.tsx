import { useEffect, useMemo, useRef, useState } from "react";
import styled, { keyframes } from "styled-components";
import { getAvailablePanels, getDefaultPanelId } from "./DevPanelRegistry";
import { useDevSurface } from "./DevSurfaceContext";
import type { DevSurface } from "./DevSurfaceContext";

interface DevOverlayProps {
  open: boolean;
  onClose: () => void;
  top: number;
}

export function DevOverlay({ open, onClose, top }: DevOverlayProps) {
  const surface = useDevSurface();
  const availablePanels = useMemo(() => getAvailablePanels(surface), [surface]);
  const defaultPanelId = useMemo(() => getDefaultPanelId(surface), [surface]);
  const [activePanelId, setActivePanelId] = useState<string | null>(null);
  const [expanded, setExpanded] = useState(false);
  const closeButtonRef = useRef<HTMLButtonElement | null>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);
  const previousSurfaceRef = useRef<DevSurface | null>(null);
  const userSelectedRef = useRef(false);

  // When the surface changes, reset to the surface-owner default.
  // When the current panel becomes unavailable, switch to the default.
  // User manual selection is respected until the surface changes.
  useEffect(() => {
    if (availablePanels.length === 0) {
      setActivePanelId(null);
      return;
    }

    // Surface changed — reset to default per dev-overlay doctrine
    if (previousSurfaceRef.current !== surface) {
      previousSurfaceRef.current = surface;
      userSelectedRef.current = false;
      setActivePanelId(defaultPanelId);
      return;
    }

    // Current panel became unavailable — switch to default
    const stillAvailable = availablePanels.some((p) => p.id === activePanelId);
    if (!stillAvailable) {
      setActivePanelId(defaultPanelId);
    }
  }, [availablePanels, activePanelId, defaultPanelId, surface]);

  useEffect(() => {
    if (!open) {
      setExpanded(false);
      return;
    }

    previousFocusRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    closeButtonRef.current?.focus();

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        if (expanded) {
          setExpanded(false);
        } else {
          onClose();
        }
      }
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      previousFocusRef.current?.focus();
    };
  }, [open, onClose, expanded]);

  if (!open) {
    return null;
  }

  const activePanel = availablePanels.find((p) => p.id === activePanelId) ?? availablePanels[0];

  return (
    <>
      <ClickAway $top={top} onClick={onClose} aria-hidden="true" data-testid="dev-click-away" />
      <Drawer $expanded={expanded} $top={top} role="region" aria-label="Developer overlay" data-testid="dev-drawer">
        <DrawerHeader>
          <TitleGroup>
            <Eyebrow>Dev</Eyebrow>
            <DrawerTitle>Developer overlay</DrawerTitle>
          </TitleGroup>
          <HeaderActions>
            <ToggleButton type="button" onClick={() => setExpanded((prev) => !prev)}>
              {expanded ? "Shrink" : "Expand"}
            </ToggleButton>
            <CloseButton ref={closeButtonRef} type="button" onClick={onClose}>
              Close
            </CloseButton>
          </HeaderActions>
        </DrawerHeader>
        <DrawerBody>
          <Sidebar aria-label="Dev panels">
            {availablePanels.length > 0 ? (
              availablePanels.map((panel) => (
                <Tab
                  key={panel.id}
                  type="button"
                  $active={panel.id === activePanel?.id}
                  aria-pressed={panel.id === activePanel?.id}
                  onClick={() => {
                    userSelectedRef.current = true;
                    setActivePanelId(panel.id);
                  }}
                >
                  {panel.label}
                </Tab>
              ))
            ) : (
              <MutedText>No contextual dev panel for this surface.</MutedText>
            )}
          </Sidebar>
          <Content data-testid="dev-overlay-content">
            {activePanel ? activePanel.render({ expanded }) : <MutedText>No contextual dev panel for this surface.</MutedText>}
          </Content>
        </DrawerBody>
      </Drawer>
    </>
  );
}

const slideDown = keyframes`
  from {
    opacity: 0;
    transform: translateY(-8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
`;

const ClickAway = styled.div<{ $top: number }>`
  position: fixed;
  top: ${(props) => props.$top}px;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 999;
  background: transparent;
`;

const Drawer = styled.div<{ $expanded: boolean; $top: number }>`
  position: fixed;
  top: ${(props) => props.$top}px;
  left: 0;
  right: 0;
  height: ${(props) => (props.$expanded ? `calc(80dvh - ${props.$top}px)` : `calc(40dvh - ${props.$top}px)`)};
  max-height: calc(100dvh - ${(props) => props.$top}px);
  z-index: 1000;
  display: flex;
  flex-direction: column;
  background: var(--bg-elevated);
  backdrop-filter: blur(18px);
  color: var(--text);
  border-bottom: 1px solid var(--border-strong);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.28);
  animation: ${slideDown} 0.16s ease-out;
`;

const DrawerHeader = styled.header`
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 20px;
  border-bottom: 1px solid var(--border);
  flex-shrink: 0;
`;

const TitleGroup = styled.div`
  display: flex;
  flex-direction: column;
`;

const Eyebrow = styled.p`
  margin: 0;
  color: var(--accent);
  text-transform: uppercase;
  letter-spacing: 0.18em;
  font-size: 0.68rem;
  font-weight: 600;
`;

const DrawerTitle = styled.h2`
  margin: 1px 0 0;
  font-size: 0.98rem;
  text-wrap: balance;
`;

const HeaderActions = styled.div`
  display: flex;
  gap: 6px;
  align-items: center;
`;

const ToggleButton = styled.button`
  padding: 6px 14px;
  border-radius: 999px;
  border: 1px solid var(--border-strong);
  background: transparent;
  color: var(--text);
  cursor: pointer;
  font-size: 0.8rem;
  font-weight: 600;
  min-height: 32px;
  transition-property: background-color, border-color;
  transition-duration: 120ms;
  transition-timing-function: ease-out;

  &:hover {
    background: rgba(255, 255, 255, 0.06);
    border-color: var(--accent);
  }

  &:active {
    transform: scale(0.97);
  }
`;

const CloseButton = styled.button`
  padding: 6px 14px;
  border-radius: 999px;
  border: 1px solid var(--border-strong);
  background: transparent;
  color: var(--text);
  cursor: pointer;
  font-size: 0.8rem;
  font-weight: 600;
  min-height: 32px;
  transition-property: background-color, border-color;
  transition-duration: 120ms;
  transition-timing-function: ease-out;

  &:hover {
    background: rgba(255, 255, 255, 0.06);
    border-color: var(--accent);
  }

  &:active {
    transform: scale(0.97);
  }
`;

const DrawerBody = styled.div`
  flex: 1;
  display: flex;
  overflow: hidden;
  min-height: 0;

  @media (max-width: 640px) {
    flex-direction: column;
  }
`;

const Sidebar = styled.nav`
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 10px 8px;
  border-right: 1px solid var(--border);
  min-width: 156px;
  flex-shrink: 0;

  @media (max-width: 640px) {
    flex-direction: row;
    flex-wrap: wrap;
    border-right: none;
    border-bottom: 1px solid var(--border);
    min-width: 0;
  }
`;

const Tab = styled.button<{ $active: boolean }>`
  padding: 7px 10px;
  border-radius: 8px;
  border: 1px solid ${(props) => (props.$active ? "var(--border-strong)" : "transparent")};
  background: ${(props) => (props.$active ? "rgba(223, 159, 79, 0.1)" : "transparent")};
  color: ${(props) => (props.$active ? "var(--text)" : "var(--muted)")};
  text-align: left;
  cursor: pointer;
  font-size: 0.82rem;
  font-weight: ${(props) => (props.$active ? 600 : 400)};
  min-height: 32px;
  transition-property: background-color, color, border-color;
  transition-duration: 100ms;
  transition-timing-function: ease-out;

  &:hover {
    color: var(--text);
    background: rgba(255, 255, 255, 0.04);
  }
`;

const Content = styled.div`
  flex: 1;
  overflow: auto;
  padding: 14px 16px;
`;

const MutedText = styled.p`
  color: var(--muted);
  margin: 0;
`;
