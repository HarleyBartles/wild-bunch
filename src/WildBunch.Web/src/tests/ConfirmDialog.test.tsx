import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ComponentProps } from "react";
import { ConfirmDialog } from "../components/ConfirmDialog";

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

function renderDialog(props: Partial<ComponentProps<typeof ConfirmDialog>> = {}) {
  const onCancel = props.onCancel ?? vi.fn();
  const onConfirm = props.onConfirm ?? vi.fn();

  render(
    <ConfirmDialog
      open={props.open ?? true}
      title={props.title ?? "Confirm action"}
      body={props.body ?? "Are you sure you want to continue?"}
      cancelLabel={props.cancelLabel ?? "Cancel"}
      confirmLabel={props.confirmLabel ?? "Confirm"}
      onCancel={onCancel}
      onConfirm={onConfirm}
      busy={props.busy ?? false}
    />,
  );

  return { onCancel, onConfirm };
}

describe("ConfirmDialog", () => {
  it("renders the title and body when open", () => {
    renderDialog({ title: "Discard progress?", body: "This cannot be undone." });

    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.getByText("Discard progress?")).toBeInTheDocument();
    expect(screen.getByText("This cannot be undone.")).toBeInTheDocument();
  });

  it("does not render when open is false", () => {
    renderDialog({ open: false });

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("calls onCancel and not onConfirm when Cancel is clicked", async () => {
    const user = userEvent.setup();
    const { onCancel, onConfirm } = renderDialog();

    await user.click(screen.getByRole("button", { name: "Cancel" }));

    expect(onCancel).toHaveBeenCalledTimes(1);
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("calls onConfirm and not onCancel when Confirm is clicked", async () => {
    const user = userEvent.setup();
    const { onCancel, onConfirm } = renderDialog();

    await user.click(screen.getByRole("button", { name: "Confirm" }));

    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(onCancel).not.toHaveBeenCalled();
  });

  it("calls onCancel when Escape is pressed", async () => {
    const user = userEvent.setup();
    const { onCancel, onConfirm } = renderDialog();

    await user.keyboard("{Escape}");

    expect(onCancel).toHaveBeenCalledTimes(1);
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("disables both buttons when busy", () => {
    renderDialog({ busy: true });

    expect(screen.getByRole("button", { name: "Cancel" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Confirm" })).toBeDisabled();
  });

  it("does not call onCancel on Escape while busy", async () => {
    const user = userEvent.setup();
    const { onCancel, onConfirm } = renderDialog({ busy: true });

    await user.keyboard("{Escape}");

    expect(onCancel).not.toHaveBeenCalled();
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("does not close on backdrop click while busy", async () => {
    const user = userEvent.setup();
    const { onCancel } = renderDialog({ busy: true });

    // Click the backdrop (the parent element wrapping the dialog).
    const dialog = screen.getByRole("dialog");
    const backdrop = dialog.parentElement;
    expect(backdrop).not.toBeNull();
    await user.click(backdrop!);

    expect(onCancel).not.toHaveBeenCalled();
  });

  it("closes on backdrop click when not busy", async () => {
    const user = userEvent.setup();
    const { onCancel } = renderDialog();

    const dialog = screen.getByRole("dialog");
    const backdrop = dialog.parentElement;
    expect(backdrop).not.toBeNull();
    await user.click(backdrop!);

    expect(onCancel).toHaveBeenCalledTimes(1);
  });
});
