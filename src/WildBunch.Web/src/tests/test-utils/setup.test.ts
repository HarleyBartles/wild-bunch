import { expect, it, vi } from "vitest";

it("provides a silent scrollTo implementation for browser tests", () => {
  const virtualConsole = (window as typeof window & {
    _virtualConsole: { once(eventName: string, listener: () => void): void };
  })._virtualConsole;
  const onJSDOMError = vi.fn();
  virtualConsole.once("jsdomError", onJSDOMError);

  window.scrollTo(0, 0);

  expect(onJSDOMError).not.toHaveBeenCalled();
});
