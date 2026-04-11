import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const out = path.join(__dirname, "..", "public", "notify.wav");

const sampleRate = 8000;
const freq = 880;
const durationSec = 0.12;
const numSamples = Math.floor(sampleRate * durationSec);
const dataSize = numSamples * 2;
const buffer = Buffer.alloc(44 + dataSize);

buffer.write("RIFF", 0);
buffer.writeUInt32LE(36 + dataSize, 4);
buffer.write("WAVE", 8);
buffer.write("fmt ", 12);
buffer.writeUInt32LE(16, 16);
buffer.writeUInt16LE(1, 20);
buffer.writeUInt16LE(1, 22);
buffer.writeUInt32LE(sampleRate, 24);
buffer.writeUInt32LE(sampleRate * 2, 28);
buffer.writeUInt16LE(2, 32);
buffer.writeUInt16LE(16, 34);
buffer.write("data", 36);
buffer.writeUInt32LE(dataSize, 40);

for (let i = 0; i < numSamples; i++) {
  const t = i / sampleRate;
  const envelope = Math.min(1, i / (sampleRate * 0.01)) * Math.min(1, (numSamples - i) / (sampleRate * 0.04));
  const sample = Math.sin(2 * Math.PI * freq * t) * 0.35 * envelope;
  const int16 = Math.max(-32768, Math.min(32767, Math.round(sample * 32767)));
  buffer.writeInt16LE(int16, 44 + i * 2);
}

fs.mkdirSync(path.dirname(out), { recursive: true });
fs.writeFileSync(out, buffer);
console.log("Wrote", out);
