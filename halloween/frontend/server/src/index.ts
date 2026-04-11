import cors from "@fastify/cors";
import Fastify, { type FastifyRequest } from "fastify";
import { WebSocketServer, WebSocket } from "ws";

const PORT = Number(process.env.PORT ?? "3000");
const CORS_ORIGIN = process.env.CORS_ORIGIN ?? "*";
const INGEST_TOKEN = process.env.INGEST_TOKEN;

function parseCorsOrigin(): boolean | string | string[] {
  if (CORS_ORIGIN === "*") {
    return true;
  }
  const list = CORS_ORIGIN.split(",").map((s) => s.trim()).filter(Boolean);
  return list.length === 1 ? list[0]! : list;
}

function ingestAuthorized(req: FastifyRequest): boolean {
  if (!INGEST_TOKEN) return true;
  const auth = req.headers.authorization;
  const headerToken = req.headers["x-ingest-token"];
  return (
    auth === `Bearer ${INGEST_TOKEN}` ||
    headerToken === INGEST_TOKEN
  );
}

function deviceTokenFromUrl(url: string | undefined): string | null {
  if (!url) return null;
  try {
    const u = new URL(url, "http://localhost");
    return u.searchParams.get("token");
  } catch {
    return null;
  }
}

function normalizePayload(raw: unknown, isText: boolean): unknown {
  if (isText && typeof raw === "string") return raw;
  return raw;
}

async function main(): Promise<void> {
  const fastify = Fastify({ logger: true });

  await fastify.register(cors, {
    origin: parseCorsOrigin(),
    methods: ["GET", "POST", "OPTIONS"],
  });

  fastify.addContentTypeParser(
    "text/plain",
    { parseAs: "string" },
    (_req, body, done) => {
      done(null, body);
    },
  );

  const uiClients = new Set<WebSocket>();

  function broadcast(source: "http" | "ws-device", payload: unknown): void {
    const envelope = JSON.stringify({
      ts: new Date().toISOString(),
      source,
      payload,
    });
    for (const client of uiClients) {
      if (client.readyState === WebSocket.OPEN) {
        client.send(envelope);
      }
    }
  }

  fastify.get("/health", async () => ({ ok: true }));

  fastify.post("/ingest", async (request, reply) => {
    if (!ingestAuthorized(request)) {
      return reply.code(401).send({ error: "Unauthorized" });
    }
    const ct = request.headers["content-type"] ?? "";
    let payload: unknown = request.body;
    if (ct.includes("text/plain")) {
      payload = normalizePayload(request.body, true);
    } else if (
      ct.length > 0 &&
      !ct.includes("application/json") &&
      !ct.includes("text/plain")
    ) {
      return reply.code(415).send({ error: "Unsupported Media Type" });
    }
    broadcast("http", payload);
    return { ok: true };
  });

  await fastify.ready();

  const httpServer = fastify.server;

  const wssDevice = new WebSocketServer({ noServer: true });
  const wssUi = new WebSocketServer({ noServer: true });

  wssUi.on("connection", (socket) => {
    uiClients.add(socket);
    socket.on("close", () => {
      uiClients.delete(socket);
    });
    socket.on("error", () => {
      uiClients.delete(socket);
    });
  });

  wssDevice.on("connection", (socket, req) => {
    if (INGEST_TOKEN) {
      const token = deviceTokenFromUrl(req.url);
      if (token !== INGEST_TOKEN) {
        socket.close(4401, "Unauthorized");
        return;
      }
    }

    socket.on("message", (data) => {
      let payload: unknown;
      const text = data.toString();
      try {
        payload = JSON.parse(text) as unknown;
      } catch {
        payload = text;
      }
      broadcast("ws-device", payload);
    });
  });

  httpServer.on("upgrade", (request, socket, head) => {
    try {
      const pathname = new URL(request.url ?? "/", "http://localhost").pathname;
      if (pathname === "/ws/ui") {
        wssUi.handleUpgrade(request, socket, head, (ws) => {
          wssUi.emit("connection", ws, request);
        });
        return;
      }
      if (pathname === "/ws/device") {
        wssDevice.handleUpgrade(request, socket, head, (ws) => {
          wssDevice.emit("connection", ws, request);
        });
        return;
      }
    } catch {
      // fall through
    }
    socket.destroy();
  });

  await fastify.listen({ port: PORT, host: "0.0.0.0" });
  fastify.log.info(`Ingest listening on ${PORT}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
