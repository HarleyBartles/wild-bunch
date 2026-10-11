import type { KeyboardEvent } from "react";

const FOCUSABLE_SELECTOR =
  'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

export function wrapDialogTabNavigation(event: KeyboardEvent<HTMLDialogElement>): void {
  if (event.key !== "Tab") {
    return;
  }

  const dialog = event.currentTarget;
  const activeElement = document.activeElement;
  if (!(activeElement instanceof HTMLElement) || activeElement.closest("dialog") !== dialog) {
    return;
  }

  const focusable = Array.from(dialog.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)).filter(
    (element) => element.tabIndex >= 0,
  );
  if (focusable.length === 0) {
    return;
  }

  const first = focusable[0];
  const last = focusable[focusable.length - 1];

  if (event.shiftKey && activeElement === first) {
    event.preventDefault();
    last.focus();
  } else if (!event.shiftKey && activeElement === last) {
    event.preventDefault();
    first.focus();
  }
}
