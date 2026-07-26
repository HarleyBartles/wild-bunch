import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import { viteStaticCopy } from 'vite-plugin-static-copy';

export default defineConfig({
  plugins: [
    react(),
    viteStaticCopy({
      targets: [
        {
          src: '../WildBunch.Assets/production/sprites/town-hub-buildings/**/*',
          dest: 'assets/town-hub-buildings'
        },
        {
          src: '../WildBunch.Assets/production/tiles/town-hub-roads/**/*',
          dest: 'assets/town-hub-roads'
        },
        {
          src: '../WildBunch.Assets/production/tiles/town-hub-ground/**/*',
          dest: 'assets/town-hub-ground'
        },
        {
          src: '../WildBunch.Assets/production/sprites/town-hub-ground/props/**/*',
          dest: 'assets/town-hub-ground/props'
        }
      ]
    })
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
