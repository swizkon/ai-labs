# Frozen log schema and MQ properties

This PoC uses a single JSON shape on stdout (one JSON object per line) across all services.

## JSON log fields

| Field | Required | Example | Notes |
|-------|----------|---------|--------|
| `@timestamp` | yes | ISO8601 | Serilog / UTC |
| `level` | yes | `Information` | |
| `service` | yes | `BroadcastParser` | Stable service name |
| `trace_id` | yes* | 32-char hex | From `Activity.TraceId`; omit only if no activity (should not happen in instrumented paths) |
| `span_id` | yes* | 16-char hex | From `Activity.SpanId` |
| `parent_span_id` | no | 16-char hex | When useful |
| `mix_number` | yes | `"0123456"` | Seven digits, string |
| `step` | yes | `http_ingress` | See Step values |
| `event` | yes | `start` \| `complete` \| `fail` | |
| `duration_ms` | on complete/fail | number | Elapsed for the step |
| `message` | yes | human text | |
| `error` | on fail | string | |

### Step values (enum-like)

- `http_ingress`
- `mongo_write`
- `mq_publish`
- `mq_consume`
- `instruction_lookup`
- `pim_process` (PIMIntegrator-specific processing)

### Message types (for `mq_publish` / `mq_consume`)

Examples: `vehicleObject`, `endOfLineObject`

## IBM MQ application-defined properties

Publishers **must** set:

| Property | Description |
|----------|-------------|
| `traceparent` | W3C traceparent value (`00-{trace_id}-{span_id}-{flags}`) from the **sending** span (typically the `mq_publish` activity’s context at send time) |
| `mix_number` | Same seven-digit string as in logs |

Message body remains JSON with business payload; consumers should restore `Activity` parent context from `traceparent` before logging.

Optional later: duplicate `trace_id` alone — not required when `traceparent` is present.
