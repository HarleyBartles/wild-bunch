import { execFileSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";
import type { Plugin } from "vite";
import react from "@vitejs/plugin-react";
import { viteStaticCopy } from "vite-plugin-static-copy";

const webRoot = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(webRoot, "../..");
const apiProject = resolve(repositoryRoot, "src/WildBunch.Api/WildBunch.Api.csproj");

function buildIdentityPlugin(): Plugin {
  return {
    name: "wild-bunch-build-identity",
    apply: "build",
    generateBundle() {
      let version: string;
      try {
        version = execFileSync("dotnet", ["msbuild", apiProject, "-getProperty:Version"], {
          cwd: repositoryRoot,
          encoding: "utf8",
        }).trim();
      } catch (error) {
        throw Object.assign(
          new Error(`Could not evaluate application version from ${apiProject}: ${String(error)}`),
          { cause: error },
        );
      }

      if (!/^0\.1\.0-dev\.[1-9][0-9]*$/.test(version)) {
        throw new Error(
          `MSBuild returned an unsupported development version: ${JSON.stringify(version)}`,
        );
      }

      this.emitFile({
        type: "asset",
        fileName: "version.json",
        source: JSON.stringify({ version }),
      });
    },
  };
}

export default defineConfig({
  plugins: [
    react(),
    buildIdentityPlugin(),
    viteStaticCopy({
      targets: [
        {
          src: "../WildBunch.Assets/production/sprites/town-hub-buildings/**/*",
          dest: "assets/town-hub-buildings",
        },
        {
          src: "../WildBunch.Assets/production/tiles/town-hub-roads/**/*",
          dest: "assets/town-hub-roads",
        },
        {
          src: "../WildBunch.Assets/production/tiles/town-hub-ground/**/*",
          dest: "assets/town-hub-ground",
        },
        {
          src: "../WildBunch.Assets/production/sprites/town-hub-ground/props/**/*",
          dest: "assets/town-hub-ground/props",
        },
      ],
    }),
  ],
  test: {
    environment: "jsdom",
    setupFiles: ["./src/tests/test-utils/setup.ts"],
    css: true,
    globals: false,
    testTimeout: 30000,
    hookTimeout: 30000,
  },
  server: {
    host: "0.0.0.0",
    port: 5173,
  },
  build: {
    chunkSizeWarningLimit: 1500, // Phaser lazy chunk ~1.5 MB; only loaded on town-selection/trailhead
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (id.includes("node_modules")) {
            if (id.includes("react-dom") || id.includes("react/") || id.includes("scheduler/")) {
              return "vendor";
            }
            if (id.includes("@tanstack/react-router") || id.includes("@tanstack/react-query")) {
              return "router";
            }
            if (id.includes("styled-components")) {
              return "styled";
            }
          }
        },
      },
    },
  },
});
