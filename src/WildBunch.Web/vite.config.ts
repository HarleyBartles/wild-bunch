import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    setupFiles: ["./src/tests/test-utils/setup.ts"],
    css: true,
    globals: false,
  },
  server: {
    host: "0.0.0.0",
    port: 5173,
  },
});
