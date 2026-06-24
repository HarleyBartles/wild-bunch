import { useEffect, useRef, type ReactNode } from "react";

interface CockpitOverlayFrameProps {
  open: boolean;
  eyebrow: string;
  title: string;
  description?: string;
  onClose: () => void;
  children: ReactNode;
}

export function CockpitOverlayFrame({ open, eyebrow, title, description, onClose, children }: CockpitOverlayFrameProps) {
  const closeButtonRef = useRef<HTMLButtonElement | null>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    previousFocusRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
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
    <div
      className="case-modal__backdrop"
      onClick={(event) => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
    >
      <section
        className="case-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="cockpit-surface-title"
        aria-describedby={description ? "cockpit-surface-description" : undefined}
      >
        <header className="case-modal__header">
          <div>
            <p className="eyebrow">{eyebrow}</p>
            <h2 id="cockpit-surface-title">{title}</h2>
            {description ? (
              <p id="cockpit-surface-description" className="panel-subtitle">
                {description}
              </p>
            ) : null}
          </div>
          <button ref={closeButtonRef} type="button" className="button button--ghost" onClick={onClose}>
            Close
          </button>
        </header>

        <div className="case-modal__body">{children}</div>
      </section>
    </div>
  );
}
