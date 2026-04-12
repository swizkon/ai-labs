# Factory observability PoC (Grafana Loki + .NET)

End-to-end proof of concept for tracing a factory order flow across three modules:

| Service | Role |
|---------|------|
| **BroadcastParser** | HTTP ingress → MongoDB writes → IBM MQ publish |
| **PIMIntegrator** | Consumes `vehicleObject` messages from `FACTORY.PIM` |
| **InstructionsGenerator** | Consumes `endOfLineObject` messages from `FACTORY.INSTRUCTIONS` |

Structured logs use **Serilog** compact JSON on stdout with **`ActivitySource` / `Activity`** for W3C `trace_id` / `span_id` (see [`docs/SCHEMA.md`](docs/SCHEMA.md)). IBM MQ messages carry **`traceparent`** and **`mix_number`** as string properties and in the JSON envelope.

## Prerequisites

- Docker Desktop (or Docker Engine + Compose)
- Ports **1414**, **27017**, **3000**, **3100**, **8080** available

## Run the stack

From this directory (`observability`):

```bash
docker compose up -d --build
```

Wait until `ibm-mq` is healthy (first start can take a few minutes).

By default the apps use **`DEV.QUEUE.1`** (PIM) and **`DEV.QUEUE.2`** (Instructions), which the IBM MQ developer image creates with access for user **`app`**. To use custom queue names, define them in MQSC and grant `PUT`/`GET`/`INQ` to `app` (see `config/mq/99-custom.mqsc` as a starting point), then set `IbmMq__PimQueue` / `IbmMq__InstructionsQueue` accordingly.

## Send a test broadcast

```bash
curl -s -X POST http://localhost:8080/broadcast -H "Content-Type: application/json" -d "{\"mixNumber\":\"0123456\"}"
```

## Grafana

- URL: http://localhost:3000  
- Default login: **admin** / **admin**  
- Explore → Loki, or open dashboard **Factory pipeline (Loki)**.

### LogQL cheat sheet

Use the **JSON** parser so fields stay in the log line (not as high-cardinality labels):

```logql
{container=~".+"} | json | mix_number="0123456"
```

```logql
{container=~".+"} | json | trace_id="<paste 32-char hex>"
```

```logql
{container=~".+"} | json | service="BroadcastParser" | step="mq_publish"
```

```logql
{container=~".+"} | json | event="fail"
```

## Local development (without Docker)

1. Start MongoDB and IBM MQ (or point `appsettings.json` to real hosts).  
2. Ensure queues `FACTORY.PIM` and `FACTORY.INSTRUCTIONS` exist.  
3. Run projects from `src/` with `dotnet run`.

## Notes

- **Promtail** ships container logs to **Loki**; Grafana queries Loki.  
- Mix numbers must be **exactly seven digits** (`\d{7}`).  
- Do not promote `mix_number` or `trace_id` to Loki labels in production; filter with `| json` as above.
