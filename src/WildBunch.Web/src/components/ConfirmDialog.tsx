import { useId, useLayoutEffect, useRef } from "react";
import styled, { keyframes } from "styled-components";
import { Button } from "./ui/sharedStyled";
import { wrapDialogTabNavigation } from "./dialogTabBoundary";

const fadeIn = keyframes`
  from { opacity: 0; }
  to { opacity: 1; }
`;

const scaleIn = keyframes`
  from { opacity: 0; transform: translateY(8px) scale(0.98); }
  to { opacity: 1; transform: translateY(0) scale(1); }
`;

const Backdrop = styled.dialog`
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
    animation: ${fadeIn} 120ms ease-out;
  }
`;

const Dialog = styled.section`
  display: flex;
  flex-direction: column;
  width: min(440px, 100%);
  background: var(--bg-panel);
  border: 1px solid var(--border-strong);
  border-radius: 22px;
  box-shadow: var(--shadow);
  overflow: hidden;
  animation: ${scaleIn} 160ms ease-out;
`;

const DialogHeader = styled.header`
  padding: 20px 22px 8px;

  h2 {
    margin: 0;
    font-size: clamp(1.2rem, 2.5vw, 1.5rem);
    line-height: 1.2;
    text-wrap: balance;
  }
`;

const DialogBody = styled.p`
  margin: 0;
  padding: 0 22px 20px;
  color: var(--muted);
  line-height: 1.5;
  white-space: pre-wrap;
`;

const DialogError = styled.p`
  margin: 0;
  padding: 0 22px 20px;
  color: var(--danger-text);
  line-height: 1.5;
`;

const DialogFooter = styled.footer`
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 22px 22px;
  border-top: 1px solid var(--border);
`;

const Spinner = styled.span`
  display: inline-block;
  width: 14px;
  height: 14px;
  margin-right: 8px;
  border: 2px solid color-mix(in srgb, var(--accent-ink) 35%, transparent);
  border-top-color: var(--accent-ink);
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  vertical-align: middle;

  @keyframes spin {
    to {
      transform: rotate(360deg);
    }
  }
`;

export interface ConfirmDialogProps {
  open: boolean;
  title: string;
  body: string;
  cancelLabel: string;
  confirmLabel: string;
  onCancel: () => void;
  onConfirm: () => void;
  busy?: boolean;
  errorMessage?: string;
}

export function ConfirmDialog({
  open,
  title,
  body,
  cancelLabel,
  confirmLabel,
  onCancel,
  onConfirm,
  busy = false,
  errorMessage,
}: ConfirmDialogProps) {
  const id = useId();
  const dialogRef = useRef<HTMLDialogElement | null>(null);
  const confirmButtonRef = useRef<HTMLButtonElement | null>(null);

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
    confirmButtonRef.current?.focus();

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
    <Backdrop
      ref={dialogRef}
      aria-modal="true"
      aria-labelledby={`${id}-title`}
      aria-describedby={`${id}-body`}
      onCancel={(event) => {
        event.preventDefault();
        if (!busy) {
          onCancel();
        }
      }}
      onKeyDown={wrapDialogTabNavigation}
      onClick={(event) => {
        if (event.target === event.currentTarget && !busy) {
          onCancel();
        }
      }}
    >
      <Dialog>
        <DialogHeader>
          <h2 id={`${id}-title`}>{title}</h2>
        </DialogHeader>
        <DialogBody id={`${id}-body`}>{body}</DialogBody>
        {errorMessage ? <DialogError role="alert">{errorMessage}</DialogError> : null}
        <DialogFooter>
          <Button
            type="button"
            $variant="ghost"
            onClick={onCancel}
            disabled={busy}
            aria-label={cancelLabel}
          >
            {cancelLabel}
          </Button>
          <Button
            ref={confirmButtonRef}
            type="button"
            $variant="primary"
            onClick={onConfirm}
            disabled={busy}
            aria-label={confirmLabel}
          >
            {busy ? (
              <>
                <Spinner aria-hidden="true" />
                {confirmLabel}
              </>
            ) : (
              confirmLabel
            )}
          </Button>
        </DialogFooter>
      </Dialog>
    </Backdrop>
  );
}
