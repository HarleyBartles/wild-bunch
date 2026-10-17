import { useId, useLayoutEffect, useRef, type ReactNode } from "react";
import styled from "styled-components";
import { Eyebrow, PanelSubtitle, Button } from "./ui/sharedStyled";
import { wrapDialogTabNavigation } from "./dialogTabBoundary";

const ModalBackdrop = styled.dialog`
  position: fixed;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  width: 100vw;
  height: 100vh;
  max-width: none;
  max-height: none;
  margin: 0;
  overflow: hidden;
  border: 0;
  background: transparent;
  color: inherit;

  &:not([open]) {
    display: none;
  }

  &::backdrop {
    background: rgba(7, 8, 11, 0.78);
    backdrop-filter: blur(8px);
  }
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
  const id = useId();
  const closeButtonRef = useRef<HTMLButtonElement | null>(null);
  const dialogRef = useRef<HTMLDialogElement | null>(null);

  useLayoutEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) {
      return;
    }

    if (!open) {
      if (dialog.open) {
        if (typeof dialog.close === "function") {
          dialog.close();
        } else {
          dialog.removeAttribute("open");
        }
      }
      return;
    }

    if (dialog.open) {
      return;
    }

    if (typeof dialog.showModal === "function") {
      dialog.showModal();
    } else {
      dialog.setAttribute("open", "");
    }
    closeButtonRef.current?.focus();

    return () => {
      if (!dialog.open) {
        return;
      }
      if (typeof dialog.close === "function") {
        dialog.close();
      } else {
        dialog.removeAttribute("open");
      }
    };
  }, [open]);

  return (
    <ModalBackdrop
      ref={dialogRef}
      aria-modal="true"
      aria-labelledby={`${id}-title`}
      aria-describedby={description ? `${id}-description` : undefined}
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
      onKeyDown={wrapDialogTabNavigation}
      onClick={(event) => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
    >
      <Modal>
        <ModalHeader>
          <div>
            <Eyebrow>{eyebrow}</Eyebrow>
            <h2 id={`${id}-title`}>{title}</h2>
            {description ? (
              <PanelSubtitle id={`${id}-description`} as="p">
                {description}
              </PanelSubtitle>
            ) : null}
          </div>
          <Button ref={closeButtonRef} type="button" $variant="ghost" onClick={onClose}>
            Close
          </Button>
        </ModalHeader>

        <ModalBody>{children}</ModalBody>
      </Modal>
    </ModalBackdrop>
  );
}
