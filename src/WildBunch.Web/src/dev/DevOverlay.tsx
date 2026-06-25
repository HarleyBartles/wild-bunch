import { useEffect, useRef, useState } from "react";
import styled, { keyframes } from "styled-components";
import { devPanels } from "./DevPanelRegistry";

interface DevOverlayProps {
  open: boolean;
  onClose: () => void;
}

export function DevOverlay({ open, onClose }: DevOverlayProps) {
  const [activePanelId, setActivePanelId] = useState(devPanels[0]?.id ?? null);
  const [expanded, setExpanded] = useState(false);
  const closeButtonRef = useRef<HTMLButtonElement | null>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);

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

  const activePanel = devPanels.find((p) => p.id === activePanelId) ?? devPanels[0];

  return (
    <Drawer $expanded={expanded} role="region" aria-label="Developer overlay">
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
          {devPanels.map((panel) => (
            <Tab
              key={panel.id}
              type="button"
              $active={panel.id === activePanel?.id}
              onClick={() => setActivePanelId(panel.id)}
            >
              {panel.label}
            </Tab>
          ))}
        </Sidebar>
        <Content>
          {activePanel ? activePanel.render() : <MutedText>No panels registered.</MutedText>}
        </Content>
      </DrawerBody>
    </Drawer>
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

const Drawer = styled.div<{ $expanded: boolean }>`
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: ${(props) => (props.$expanded ? "85vh" : "42vh")};
  z-index: 1000;
  display: flex;
  flex-direction: column;
  background: var(--bg-elevated);
  backdrop-filter: blur(18px);
  color: var(--text);
  border-bottom: 1px solid var(--border-strong);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.28);
  animation: ${slideDown} 0.16s ease-out;
  transition-property: height;
  transition-duration: 160ms;
  transition-timing-function: ease-out;

  @media (max-width: 640px) {
    height: ${(props) => (props.$expanded ? "90vh" : "48vh")};
  }
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
