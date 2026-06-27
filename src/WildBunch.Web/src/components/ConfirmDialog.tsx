import { useEffect, useRef } from "react";
import styled, { keyframes } from "styled-components";
import { Button } from "./ui/sharedStyled";

const fadeIn = keyframes`
  from { opacity: 0; }
  to { opacity: 1; }
`;

const scaleIn = keyframes`
  from { opacity: 0; transform: translateY(8px) scale(0.98); }
  to { opacity: 1; transform: translateY(0) scale(1); }
`;

const Backdrop = styled.div`
  position: fixed;
  inset: 0;
  z-index: 60;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  background: rgba(7, 8, 11, 0.78);
  backdrop-filter: blur(8px);
  animation: ${fadeIn} 120ms ease-out;
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
    to { transform: rotate(360deg); }
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
}: ConfirmDialogProps) {
  const dialogRef = useRef<HTMLElement | null>(null);
  const confirmButtonRef = useRef<HTMLButtonElement | null>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    previousFocusRef.current =
      document.activeElement instanceof HTMLElement ? document.activeElement : null;
    // Focus the confirm button on open — the primary action.
    confirmButtonRef.current?.focus();

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        if (!busy) {
          onCancel();
        }
        return;
      }

      // Focus trap: Tab cycles within the dialog.
      if (event.key === "Tab" && dialogRef.current) {
        const focusable = dialogRef.current.querySelectorAll<HTMLElement>(
          'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
        );
        if (focusable.length === 0) {
          event.preventDefault();
          return;
        }
        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        const active = document.activeElement;

        if (event.shiftKey) {
          if (active === first || !dialogRef.current?.contains(active)) {
            event.preventDefault();
            last.focus();
          }
        } else {
          if (active === last || !dialogRef.current?.contains(active)) {
            event.preventDefault();
            first.focus();
          }
        }
      }
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      previousFocusRef.current?.focus();
    };
  }, [open, onCancel, busy]);

  if (!open) {
    return null;
  }

  return (
    <Backdrop
      onClick={(event) => {
        if (event.target === event.currentTarget && !busy) {
          onCancel();
        }
      }}
    >
      <Dialog
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-dialog-title"
        aria-describedby="confirm-dialog-body"
      >
        <DialogHeader>
          <h2 id="confirm-dialog-title">{title}</h2>
        </DialogHeader>
        <DialogBody id="confirm-dialog-body">{body}</DialogBody>
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
