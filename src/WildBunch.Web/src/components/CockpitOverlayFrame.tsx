import { useEffect, useRef, type ReactNode } from "react";
import styled from "styled-components";
import { Eyebrow, PanelSubtitle, Button } from "./ui/sharedStyled";

const ModalBackdrop = styled.div`
  position: fixed;
  inset: 0;
  z-index: 50;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  background: rgba(7, 8, 11, 0.78);
  backdrop-filter: blur(8px);
`;

const Modal = styled.section`
  display: flex;
  flex-direction: column;
  width: min(1180px, 100%);
  max-height: min(92vh, 980px);
  background: var(--bg-panel);
  border: 1px solid var(--border-strong);
  border-radius: 26px;
  box-shadow: var(--shadow);
  overflow: hidden;
`;

const ModalHeader = styled.header`
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding: 22px 24px;
  border-bottom: 1px solid var(--border);

  h2 {
    margin: 0 0 8px;
    font-size: clamp(1.6rem, 3vw, 2.4rem);
    line-height: 1;
    text-wrap: balance;
  }
`;

const ModalBody = styled.div`
  overflow: auto;
  padding: 22px 24px 24px;
  flex: 1;
`;

interface CockpitOverlayFrameProps {
  open: boolean;
  eyebrow: string;
  title: string;
  description?: string;
  onClose: () => void;
  children: ReactNode;
}

export function CockpitOverlayFrame({
  open,
  eyebrow,
  title,
  description,
  onClose,
  children,
}: CockpitOverlayFrameProps) {
  const closeButtonRef = useRef<HTMLButtonElement | null>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    previousFocusRef.current =
      document.activeElement instanceof HTMLElement ? document.activeElement : null;
    closeButtonRef.current?.focus();

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      previousFocusRef.current?.focus();
    };
  }, [open, onClose]);

  if (!open) {
    return null;
  }

  return (
    <ModalBackdrop
      onClick={(event) => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
    >
      <Modal
        role="dialog"
        aria-modal="true"
        aria-labelledby="cockpit-surface-title"
        aria-describedby={description ? "cockpit-surface-description" : undefined}
      >
        <ModalHeader>
          <div>
            <Eyebrow>{eyebrow}</Eyebrow>
            <h2 id="cockpit-surface-title">{title}</h2>
            {description ? (
              <PanelSubtitle id="cockpit-surface-description" as="p">
                {description}
              </PanelSubtitle>
            ) : null}
          </div>
          <Button
            ref={closeButtonRef}
            type="button"
            $variant="ghost"
            onClick={onClose}
          >
            Close
          </Button>
        </ModalHeader>

        <ModalBody>{children}</ModalBody>
      </Modal>
    </ModalBackdrop>
  );
}
