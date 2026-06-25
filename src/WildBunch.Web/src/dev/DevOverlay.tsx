import { useEffect, useRef, useState } from "react";
import { devPanels } from "./DevPanelRegistry";

interface DevOverlayProps {
  open: boolean;
  onClose: () => void;
}

export function DevOverlay({ open, onClose }: DevOverlayProps) {
  const [activePanelId, setActivePanelId] = useState(devPanels[0]?.id ?? null);
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

  const activePanel = devPanels.find((p) => p.id === activePanelId) ?? devPanels[0];

  return (
    <div className="dev-overlay" role="dialog" aria-modal="true" aria-label="Developer overlay">
      <header className="dev-overlay__header">
        <div className="dev-overlay__title-group">
          <p className="dev-overlay__eyebrow">Dev</p>
          <h2 className="dev-overlay__title">Developer overlay</h2>
        </div>
        <button ref={closeButtonRef} type="button" className="dev-overlay__close" onClick={onClose}>
          Close
        </button>
      </header>
      <div className="dev-overlay__body">
        <nav className="dev-overlay__sidebar" aria-label="Dev panels">
          {devPanels.map((panel) => (
            <button
              key={panel.id}
              type="button"
              className={`dev-overlay__tab${panel.id === activePanel?.id ? " dev-overlay__tab--active" : ""}`}
              onClick={() => setActivePanelId(panel.id)}
            >
              {panel.label}
            </button>
          ))}
        </nav>
        <div className="dev-overlay__content">
          {activePanel ? activePanel.render() : <p className="dev-panel-muted">No panels registered.</p>}
        </div>
      </div>
    </div>
  );
}
