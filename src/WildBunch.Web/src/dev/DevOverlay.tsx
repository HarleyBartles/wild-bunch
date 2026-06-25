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
    transform: translateY(-100%);
  }
  to {
    transform: translateY(0);
  }
`;

const Drawer = styled.div<{ $expanded: boolean }>`
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: ${(props) => (props.$expanded ? "85vh" : "45vh")};
  z-index: 1000;
  display: flex;
  flex-direction: column;
  background: rgba(12, 10, 8, 0.97);
  color: rgba(242, 239, 232, 0.92);
  border-bottom: 1px solid rgba(228, 186, 126, 0.3);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.5);
  animation: ${slideDown} 0.18s ease-out;
  transition: height 0.18s ease-out;

  @media (max-width: 640px) {
    height: ${(props) => (props.$expanded ? "90vh" : "50vh")};
  }
`;

const DrawerHeader = styled.header`
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  flex-shrink: 0;
`;

const TitleGroup = styled.div`
  display: flex;
  flex-direction: column;
`;

const Eyebrow = styled.p`
  margin: 0;
  color: #efc37e;
  text-transform: uppercase;
  letter-spacing: 0.18em;
  font-size: 0.72rem;
`;

const DrawerTitle = styled.h2`
  margin: 2px 0 0;
  font-size: 1.05rem;
`;

const HeaderActions = styled.div`
  display: flex;
  gap: 8px;
  align-items: center;
`;

const ToggleButton = styled.button`
  padding: 5px 12px;
  border-radius: 999px;
  border: 1px solid rgba(228, 186, 126, 0.3);
  background: transparent;
  color: rgba(242, 239, 232, 0.92);
  cursor: pointer;
  font-size: 0.82rem;

  &:hover {
    background: rgba(255, 255, 255, 0.06);
  }
`;

const CloseButton = styled.button`
  padding: 5px 12px;
  border-radius: 999px;
  border: 1px solid rgba(228, 186, 126, 0.3);
  background: transparent;
  color: rgba(242, 239, 232, 0.92);
  cursor: pointer;
  font-size: 0.82rem;

  &:hover {
    background: rgba(255, 255, 255, 0.06);
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
  gap: 4px;
  padding: 12px 10px;
  border-right: 1px solid rgba(255, 255, 255, 0.08);
  min-width: 160px;
  flex-shrink: 0;

  @media (max-width: 640px) {
    flex-direction: row;
    flex-wrap: wrap;
    border-right: none;
    border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    min-width: 0;
  }
`;

const Tab = styled.button<{ $active: boolean }>`
  padding: 7px 10px;
  border-radius: 8px;
  border: 1px solid ${(props) => (props.$active ? "rgba(228, 186, 126, 0.3)" : "transparent")};
  background: ${(props) => (props.$active ? "rgba(255, 255, 255, 0.06)" : "transparent")};
  color: ${(props) => (props.$active ? "rgba(242, 239, 232, 0.92)" : "rgba(242, 239, 232, 0.5)")};
  text-align: left;
  cursor: pointer;
  font-size: 0.84rem;
`;

const Content = styled.div`
  flex: 1;
  overflow: auto;
  padding: 16px;
`;

const MutedText = styled.p`
  color: rgba(242, 239, 232, 0.5);
`;
