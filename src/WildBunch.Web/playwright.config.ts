import { defineConfig } from "@playwright/test";

const baseURL = "http://127.0.0.1:4173";
const productionBaseURL = "http://127.0.0.1:4174";

export default defineConfig({
  testDir: "./browser-tests",
  testMatch: "**/*.spec.ts",
  outputDir: "./test-results/browser",
  fullyParallel: false,
  retries: process.env.CI ? 1 : 0,
  failOnFlakyTests: Boolean(process.env.CI),
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? "line" : "list",
  use: {
    baseURL,
    browserName: "chromium",
    trace: "retain-on-failure",
  },
  webServer: [
    {
      command: "npm run dev -- --host 127.0.0.1 --port 4173 --strictPort",
      url: `${baseURL}/browser-tests/modal-stack.html`,
      reuseExistingServer: false,
      timeout: 60_000,
    },
    {
      command: "npm run preview -- --host 127.0.0.1 --port 4174 --strictPort",
      url: productionBaseURL,
      reuseExistingServer: false,
      timeout: 60_000,
    },
  ],
});
