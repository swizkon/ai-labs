import { defineConfig } from "vite";
import { svelte } from "@sveltejs/vite-plugin-svelte";

const ingestTarget = process.env.INGEST_URL ?? "http://127.0.0.1:3000";

// https://vite.dev/config/
export default defineConfig({
  plugins: [svelte()],
  server: {
    proxy: {
      "/ws": {
        target: ingestTarget,
        ws: true,
        changeOrigin: true,
      },
      "/ingest": {
        target: ingestTarget,
        changeOrigin: true,
      },
      "/health": {
        target: ingestTarget,
        changeOrigin: true,
      },
    },
  },
});
