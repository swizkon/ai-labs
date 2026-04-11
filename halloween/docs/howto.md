
Here’s what was implemented and how to use it.

### Layout ([`halloween/frontend/`](c:\dev\github\swizkon\ai-labs\halloween\frontend))
- **npm workspaces**: `client` (Vite + Svelte 5) and `server` (Node + TypeScript).
- **Server** ([`frontend/server/src/index.ts`](c:\dev\github\swizkon\ai-labs\halloween\frontend\server\src\index.ts)): Fastify + `@fastify/cors` + `ws`
  - `GET /health` → `{ ok: true }`
  - `POST /ingest` → JSON or `text/plain`; broadcasts `{ ts, source: "http", payload }` to UI sockets
  - `WS /ws/device` → JSON or plain text per message; `source: "ws-device"`
  - `WS /ws/ui` → browser clients receive the same envelope
  - Optional **`INGEST_TOKEN`**: `Authorization: Bearer …` or `X-Ingest-Token` on HTTP; `?token=` on device WebSocket URL
- **Client** ([`frontend/client/src/App.svelte`](c:\dev\github\swizkon\ai-labs\halloween\frontend\client\src\App.svelte)): connects to `/ws/ui`, shows the last 100 messages, **Enable audio** inline section (no modal), chime from [`public/notify.wav`](c:\dev\github\swizkon\ai-labs\halloween\frontend\client\public\notify.wav) (regenerate with `npm run gen:notify --workspace=client`).
- **Dev proxy** ([`frontend/client/vite.config.ts`](c:\dev\github\swizkon\ai-labs\halloween\frontend\client\vite.config.ts)): proxies `/ws`, `/ingest`, `/health` to `INGEST_URL` (default `http://127.0.0.1:3000`).

### Scripts ([`frontend/package.json`](c:\dev\github\swizkon\ai-labs\halloween\frontend\package.json))
- `npm run dev:server` — ingest on port 3000  
- `npm run dev` — Vite client  
- `npm run dev:all` — both (via `concurrently`)  
- `npm run build` — build client + server  

### Docker ([`halloween/docker-compose.yaml`](c:\dev\github\swizkon\ai-labs\halloween\docker-compose.yaml))
- **`ingest`**: build [`frontend/server/Dockerfile`](c:\dev\github\swizkon\ai-labs\halloween\frontend\server\Dockerfile), host **`3000`**, healthcheck on `/health`
- **`iot-frontend`**: build [`frontend/client/Dockerfile`](c:\dev\github\swizkon\ai-labs\halloween\frontend\client\Dockerfile) + nginx [`nginx.conf`](c:\dev\github\swizkon\ai-labs\halloween\frontend\client\nginx.conf) proxying `/ws`, `/ingest`, `/health` → `ingest:3000`, UI on host **`8080`**
- **`nodered`** unchanged on **1880**

### Try it
1. Local: from `halloween/frontend`, run `npm run dev:all`, open the Vite URL, enable audio, then e.g. `curl -X POST http://127.0.0.1:3000/ingest -H "Content-Type: application/json" -d "{\"test\":1}"`.
2. Docker: from `halloween`, `docker compose up -d ingest iot-frontend`, open `http://localhost:8080`, same curl to `http://localhost:3000/ingest`.

Override WebSocket URL in the client with env **`VITE_WS_URL`** if needed.