<script lang="ts">
  import { onDestroy, onMount } from "svelte";
  import soundbank from "virtual:soundbank";

  type Envelope = {
    ts: string;
    source: "http" | "ws-device";
    payload: Payload;
  };

  type Payload = {
    source: string;
    occupancy: boolean;
  };

  type Conn = "connecting" | "open" | "closed" | "error";

  let status = $state<Conn>("connecting");
  let messages = $state<Envelope[]>([]);
  let audioEnabled = $state(false);
  let audioRef: HTMLAudioElement | null = $state(null);
  let resetAudioRef: HTMLAudioElement | null = $state(null);
  let played = $state<{ ts: string; sound: string }[]>([]);
  let wakeLock: WakeLockSentinel | null = null;
  let awake = $state(false);

  const MAX = 100;
  const COOLDOWN_MS = 30_000;
  let lastPlayedAt = 0;

  function soundName(url: string): string {
    const file = url.split("/").pop() ?? url;
    try {
      return decodeURIComponent(file);
    } catch {
      return file;
    }
  }

  function wsUrl(): string {
    const fromEnv = import.meta.env.VITE_WS_URL as string | undefined;
    if (fromEnv) {
      return fromEnv;
    }
    const proto = location.protocol === "https:" ? "wss:" : "ws:";
    return `${proto}//${location.host}/ws/ui`;
  }

  let socket: WebSocket | null = null;
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null;

  function clearReconnect(): void {
    if (reconnectTimer !== null) {
      clearTimeout(reconnectTimer);
      reconnectTimer = null;
    }
  }

  function playChime(): void {
    const el = audioRef;
    if (!el || !audioEnabled || soundbank.length === 0) return;
    if (!el.paused && !el.ended) return;
    const now = Date.now();
    if (now - lastPlayedAt < COOLDOWN_MS) return;
    lastPlayedAt = now;
    const sound = soundbank[Math.floor(Math.random() * soundbank.length)];
    played = [{ ts: new Date().toISOString(), sound }, ...played].slice(0, MAX);
    el.src = sound;
    el.currentTime = 0;
    void el.play().catch(() => {
      // autoplay or decode
    });
  }

  function resetChime(): void {
    if (audioRef) {
      audioRef.pause();
      audioRef.currentTime = 0;
    }
    /*
    const el = resetAudioRef;
    if (!el || !audioEnabled) return;
    el.currentTime = 0;
    void el.play().catch(() => {
      // autoplay or decode
    });
    */
  }

  function connect(): void {
    clearReconnect();
    socket?.close();
    status = "connecting";
    const ws = new WebSocket(wsUrl());
    socket = ws;
    ws.onopen = () => {
      status = "open";
    };
    ws.onclose = () => {
      status = "closed";
      socket = null;
      reconnectTimer = setTimeout(connect, 2000);
    };
    ws.onerror = () => {
      status = "error";
    };
    ws.onmessage = (ev: MessageEvent<string>) => {
      try {
        const data = JSON.parse(ev.data) as Envelope;
        messages = [data, ...messages].slice(0, MAX);

        if (data.payload.occupancy === true) {
          playChime();
        }

        if (data.payload.occupancy == false) {
          resetChime();
        }
        
      } catch {
        /* ignore malformed */
      }
    };
  }

  async function requestWakeLock(): Promise<void> {
    if (!("wakeLock" in navigator) || document.visibilityState !== "visible") return;
    if (wakeLock && !wakeLock.released) return;
    try {
      wakeLock = await navigator.wakeLock.request("screen");
      awake = true;
      wakeLock.addEventListener("release", () => {
        awake = false;
      });
    } catch {
      // not allowed (insecure context, battery saver, etc.)
      awake = false;
    }
  }

  function onVisibilityChange(): void {
    // the browser releases the lock when the tab is hidden; re-acquire on return
    if (document.visibilityState === "visible") void requestWakeLock();
  }

  function enableAudio(): void {
    audioEnabled = true;
    void requestWakeLock();
    const el = audioRef;
    if (el) {
      void el
        .play()
        .then(() => {
          el.pause();
          el.currentTime = 0;
        })
        .catch(() => {
          /* still blocked */
        });
    }
  }

  onMount(() => {
    connect();
    document.addEventListener("visibilitychange", onVisibilityChange);
    void requestWakeLock();
  });

  onDestroy(() => {
    clearReconnect();
    socket?.close();
    document.removeEventListener("visibilitychange", onVisibilityChange);
    void wakeLock?.release();
  });

  function formatPayload(p: unknown): string {
    if (typeof p === "string") return p;
    try {
      return JSON.stringify(p, null, 2);
    } catch {
      return String(p);
    }
  }
</script>

<audio bind:this={resetAudioRef} src="/notify.wav" preload="auto"></audio>

<audio bind:this={audioRef} src={soundbank[0]} preload="auto"></audio>

<main class="dashboard">
  <header class="head">
    <h1>IoT ingest soundbank</h1>
    <p class="sub">
      Live messages from <code>POST /ingest</code> and <code>/ws/device</code>
    </p>
    <div class="row">
      <span class="pill" data-state={status}>
        {status === "open"
          ? "Connected"
          : status === "connecting"
            ? "Connecting…"
            : status === "error"
              ? "Error"
              : "Disconnected"}
      </span>
      {#if !audioEnabled}
        <section class="audio-banner" aria-label="Audio">
          <p>
            Browsers often block sound until you interact with the page. Enable
            audio to hear a chime on each message.
          </p>
          <button type="button" class="btn" onclick={enableAudio}>
            Enable audio
          </button>
        </section>
      {:else}
        <span class="muted">Sound on</span>
      {/if}
      <span class="muted">{awake ? "Screen stays awake" : "Wake lock off"}</span>
    </div>
  </header>

  <section class="played" aria-label="Played sounds">
    <h2>Played sounds</h2>
    {#if played.length === 0}
      <p class="empty muted">No sounds played yet.</p>
    {:else}
      <ul class="list">
        {#each played as p, i (`${p.ts}-${i}`)}
          <li class="card">
            <div class="meta">
              <time datetime={p.ts}>{p.ts}</time>
              <span class="src">{soundName(p.sound)}</span>
            </div>
          </li>
        {/each}
      </ul>
    {/if}
  </section>

  <section class="feed" aria-live="polite">
    {#if messages.length === 0}
      <p class="empty muted">Waiting for data…</p>
    {:else}
      <ul class="list">
        {#each messages as m, i (`${m.ts}-${i}`)}
          <li class="card">
            <div class="meta">
              <time datetime={m.ts}>{m.ts}</time>
              <span class="src">{m.source}</span>
            </div>
            <pre class="payload">{formatPayload(m.payload)}</pre>
          </li>
        {/each}
      </ul>
    {/if}
  </section>
</main>

<style>
  .dashboard {
    text-align: left;
    padding: 1.5rem clamp(1rem, 4vw, 2rem) 3rem;
    max-width: 52rem;
    margin: 0 auto;
    box-sizing: border-box;
  }

  .head h1 {
    font-size: 1.75rem;
    margin: 0 0 0.35rem;
    letter-spacing: -0.02em;
  }

  .sub {
    margin: 0 0 1rem;
    color: var(--text);
  }

  .sub code {
    font-size: 0.9em;
  }

  .row {
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    gap: 0.75rem 1rem;
  }

  .pill {
    display: inline-block;
    padding: 0.25rem 0.65rem;
    border-radius: 999px;
    font-size: 0.85rem;
    font-family: var(--mono);
    border: 1px solid var(--border);
    background: var(--code-bg);
    color: var(--text-h);
  }

  .pill[data-state="open"] {
    border-color: var(--accent-border);
    background: var(--accent-bg);
    color: var(--accent);
  }

  .pill[data-state="connecting"] {
    opacity: 0.85;
  }

  .pill[data-state="closed"],
  .pill[data-state="error"] {
    color: var(--text);
  }

  .audio-banner {
    flex: 1 1 16rem;
    margin: 0;
    padding: 0.75rem 1rem;
    border-radius: 8px;
    border: 1px solid var(--border);
    background: var(--social-bg);
    text-align: left;
  }

  .audio-banner p {
    margin: 0 0 0.6rem;
    font-size: 0.95rem;
  }

  .btn {
    font: inherit;
    cursor: pointer;
    padding: 0.4rem 0.85rem;
    border-radius: 6px;
    border: 2px solid var(--accent-border);
    background: var(--accent-bg);
    color: var(--text-h);
  }

  .btn:focus-visible {
    outline: 2px solid var(--accent);
    outline-offset: 2px;
  }

  .muted {
    color: var(--text);
    font-size: 0.9rem;
  }

  .feed {
    margin-top: 1.5rem;
  }

  .played {
    margin-top: 1.5rem;
  }

  .played h2 {
    font-size: 1.1rem;
    margin: 0 0 0.75rem;
  }

  .played .meta {
    margin-bottom: 0;
  }

  .empty {
    margin: 0;
  }

  .list {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .card {
    border: 1px solid var(--border);
    border-radius: 8px;
    padding: 0.75rem 1rem;
    background: var(--bg);
    box-shadow: var(--shadow);
  }

  .meta {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem 1rem;
    align-items: baseline;
    font-size: 0.8rem;
    margin-bottom: 0.5rem;
    color: var(--text);
  }

  .meta time {
    font-family: var(--mono);
  }

  .src {
    font-family: var(--mono);
    padding: 0.1rem 0.4rem;
    border-radius: 4px;
    background: var(--code-bg);
    color: var(--text-h);
  }

  .payload {
    margin: 0;
    font-family: var(--mono);
    font-size: 0.85rem;
    line-height: 1.45;
    white-space: pre-wrap;
    word-break: break-word;
    color: var(--text-h);
  }
</style>
