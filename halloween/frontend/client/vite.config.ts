import { readdirSync } from "node:fs";
import { resolve } from "node:path";
import { defineConfig, type Plugin } from "vite";
import { svelte } from "@sveltejs/vite-plugin-svelte";

const ingestTarget = process.env.INGEST_URL ?? "http://127.0.0.1:3000";

// Exposes the mp3 files in public/soundbank as `virtual:soundbank` (an array of URLs).
function soundbank(): Plugin {
  const id = "virtual:soundbank";
  const resolvedId = "\0" + id;
  const dir = resolve(__dirname, "public/soundbank");
  return {
    name: "soundbank",
    resolveId(source) {
      return source === id ? resolvedId : null;
    },
    load(source) {
      if (source !== resolvedId) return null;
      const files = readdirSync(dir)
        .filter((f) => f.toLowerCase().endsWith(".mp3"))
        .map((f) => `/soundbank/${encodeURIComponent(f)}`);
      return `export default ${JSON.stringify(files)};`;
    },
  };
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [svelte(), soundbank()],
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
