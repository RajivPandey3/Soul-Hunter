#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Generates Soul Hunter's own retro sound effects and stage music as WAV files (no external
    /// samples, so no licence questions). Deterministic: the same code always writes the same audio.
    /// SFX go to Assets/_Project/Audio/SFX, music loops (one per stage + menu) to Assets/_Project/Audio/Music.
    /// Menu: Soul Hunter/Content/Generate Retro Audio, or CLI:
    /// -executeMethod SoulHunter.Editor.Content.RetroAudioSynth.GenerateAll -quit
    /// </summary>
    public static class RetroAudioSynth
    {
        public const int Rate = 22050;
        public const string SfxFolder = "Assets/_Project/Audio/SFX";
        public const string MusicFolder = "Assets/_Project/Audio/Music";

        public static readonly string[] SfxNames =
        {
            "EnemyDeath", "PlayerHurt", "GameOver", "StageClear", "BossWarning", "UIClick", "UIConfirm",
            "Shoot", "Slash", "Magic", "Explosion", "Unlock",
        };

        [MenuItem("Soul Hunter/Content/Generate Retro Audio")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(SfxFolder);
            Directory.CreateDirectory(MusicFolder);
            foreach (var name in SfxNames) Write($"{SfxFolder}/{name}.wav", Sfx(name));
            foreach (var old in new[] { "ChestOpen", "GemPickup", "LevelUp", "EnemyHit", "Heal" }) FadeIn($"{SfxFolder}/{old}.wav", .003f);
            Write($"{MusicFolder}/Music_Menu.wav", Music(Themes[0]));
            for (int stage = 1; stage <= 10; stage++) Write($"{MusicFolder}/Music_Stage{stage:00}.wav", Music(Themes[stage]));
            AssetDatabase.Refresh();

            foreach (var name in SfxNames) ConfigureImport($"{SfxFolder}/{name}.wav", false);
            ConfigureImport($"{MusicFolder}/Music_Menu.wav", true);
            for (int stage = 1; stage <= 10; stage++) ConfigureImport($"{MusicFolder}/Music_Stage{stage:00}.wav", true);
            Debug.Log($"[RetroAudioSynth] Wrote {SfxNames.Length} sound effects and 11 music loops.");
        }

        private static void ConfigureImport(string path, bool music)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = music ? 0.6f : 1f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }

        // ---------- oscillators and helpers ----------

        private static float Square(double phase, float duty = .5f) => (phase - Math.Floor(phase)) < duty ? 1f : -1f;
        private static float Triangle(double phase) { double p = phase - Math.Floor(phase); return (float)(p < .5 ? 4 * p - 1 : 3 - 4 * p); }
        private static float Saw(double phase) => (float)(2 * (phase - Math.Floor(phase)) - 1);
        private static float Sine(double phase) => (float)Math.Sin(phase * Math.PI * 2);
        private static float Midi(float note) => 440f * Mathf.Pow(2f, (note - 69f) / 12f);

        private static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];

        /// <summary>Plays a tone with a glide and an attack/decay envelope into buf.</summary>
        private static void Tone(float[] buf, float start, float length, float fromHz, float toHz, Func<double, float> wave,
            float gain, float attack = .005f, float release = .05f, float vibratoHz = 0f, float vibratoDepth = 0f)
        {
            int s0 = Mathf.Max(0, (int)(start * Rate)), n = (int)(length * Rate);
            double phase = 0;
            for (int i = 0; i < n && s0 + i < buf.Length; i++)
            {
                float t = i / (float)Rate, u = i / (float)n;
                float hz = Mathf.Lerp(fromHz, toHz, u) * (1f + vibratoDepth * Mathf.Sin(2 * Mathf.PI * vibratoHz * t));
                phase += hz / Rate;
                float env = Mathf.Min(1f, t / Mathf.Max(attack, 1e-4f)) * Mathf.Clamp01((length - t) / Mathf.Max(release, 1e-4f));
                buf[s0 + i] += wave(phase) * env * gain;
            }
        }

        /// <summary>Noise burst through a one-pole low-pass whose cutoff glides; decays over its length.</summary>
        private static void Noise(float[] buf, System.Random rng, float start, float length, float gain, float cutoffFrom, float cutoffTo,
            float attack = .002f, float curve = 1f)
        {
            int s0 = Mathf.Max(0, (int)(start * Rate)), n = (int)(length * Rate);
            float y = 0f;
            for (int i = 0; i < n && s0 + i < buf.Length; i++)
            {
                float t = i / (float)Rate, u = i / (float)n;
                float cutoff = Mathf.Lerp(cutoffFrom, cutoffTo, u);
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
                y += a * ((float)rng.NextDouble() * 2f - 1f - y);
                float env = Mathf.Min(1f, t / attack) * Mathf.Pow(1f - u, curve);
                buf[s0 + i] += y * env * gain;
            }
        }

        private static float[] Finish(float[] buf, float peak = .85f)
        {
            float max = 1e-6f;
            for (int i = 0; i < buf.Length; i++) { buf[i] = (float)Math.Tanh(buf[i] * 1.2f); max = Mathf.Max(max, Mathf.Abs(buf[i])); }
            for (int i = 0; i < buf.Length; i++) buf[i] *= peak / max;
            return buf;
        }

        // ---------- sound effects ----------

        private static float[] Sfx(string name)
        {
            int seed = 0x51;
            foreach (char c in name) seed = seed * 31 + c; // stable across runtimes, unlike string.GetHashCode
            var rng = new System.Random(seed & 0x7fffffff);
            float[] b;
            switch (name)
            {
                case "EnemyDeath":
                    b = Buffer(.28f);
                    Tone(b, 0, .22f, 620, 110, p => Square(p, .3f), .45f, .002f, .08f);
                    Noise(b, rng, 0, .25f, .5f, 3500, 400, .001f, 1.5f);
                    return Finish(b, .7f);
                case "PlayerHurt":
                    b = Buffer(.32f);
                    Tone(b, 0, .3f, 240, 95, p => Square(p, .5f), .5f, .002f, .1f, 28f, .06f);
                    Noise(b, rng, 0, .12f, .45f, 2500, 800);
                    return Finish(b, .8f);
                case "GameOver":
                    b = Buffer(2.6f);
                    float[] notes = { 69, 65, 62, 57 };
                    for (int i = 0; i < notes.Length; i++) Tone(b, i * .42f, .5f, Midi(notes[i]), Midi(notes[i]), Triangle, .5f, .01f, .2f, 5f, .01f);
                    Tone(b, 1.68f, .9f, Midi(45), Midi(40), p => Square(p, .5f), .35f, .02f, .6f);
                    return Finish(b, .8f);
                case "StageClear":
                    b = Buffer(1.7f);
                    float[] up = { 72, 76, 79, 84, 88 };
                    for (int i = 0; i < up.Length; i++)
                    {
                        Tone(b, i * .12f, .35f, Midi(up[i]), Midi(up[i]), p => Square(p, .25f), .35f, .003f, .2f);
                        Tone(b, i * .12f + .18f, .3f, Midi(up[i]), Midi(up[i]), p => Square(p, .25f), .12f, .003f, .2f); // echo
                    }
                    Tone(b, .6f, 1f, Midi(84), Midi(84), Triangle, .35f, .01f, .8f, 6f, .01f);
                    return Finish(b, .8f);
                case "BossWarning":
                    b = Buffer(1.6f);
                    for (int i = 0; i < 6; i++) Tone(b, i * .25f, .24f, i % 2 == 0 ? 330 : 247, i % 2 == 0 ? 330 : 247, Saw, .35f, .01f, .04f, 9f, .02f);
                    Tone(b, 0, 1.5f, 55, 50, Sine, .5f, .05f, .4f);
                    return Finish(b, .8f);
                case "UIClick":
                    b = Buffer(.06f);
                    Tone(b, 0, .05f, 1250, 1050, p => Square(p, .5f), .4f, .001f, .03f);
                    return Finish(b, .5f);
                case "UIConfirm":
                    b = Buffer(.2f);
                    Tone(b, 0, .07f, 880, 880, p => Square(p, .5f), .4f, .001f, .02f);
                    Tone(b, .07f, .12f, 1320, 1320, p => Square(p, .5f), .4f, .001f, .06f);
                    return Finish(b, .55f);
                case "Shoot":
                    b = Buffer(.13f);
                    Tone(b, 0, .12f, 950, 280, p => Square(p, .25f), .45f, .001f, .06f);
                    Noise(b, rng, 0, .04f, .25f, 5000, 2000);
                    return Finish(b, .6f);
                case "Slash":
                    b = Buffer(.2f);
                    Noise(b, rng, 0, .19f, .8f, 900, 5500, .06f, 2f);
                    return Finish(b, .6f);
                case "Magic":
                    b = Buffer(.3f);
                    Tone(b, 0, .28f, 700, 1500, Sine, .45f, .005f, .15f, 32f, .03f);
                    Tone(b, .03f, .25f, 1400, 2600, Triangle, .18f, .005f, .15f, 20f, .02f);
                    return Finish(b, .55f);
                case "Explosion":
                    b = Buffer(.7f);
                    Noise(b, rng, 0, .68f, .9f, 2200, 150, .002f, 1.4f);
                    Tone(b, 0, .4f, 90, 40, Sine, .7f, .002f, .3f);
                    return Finish(b, .8f);
                case "Unlock":
                    b = Buffer(1.1f);
                    float[] sparkle = { 79, 83, 86, 91, 95 };
                    for (int i = 0; i < sparkle.Length; i++) Tone(b, i * .09f, .5f, Midi(sparkle[i]), Midi(sparkle[i]), Triangle, .35f, .002f, .4f);
                    return Finish(b, .7f);
                default: throw new ArgumentException(name);
            }
        }

        // ---------- music ----------

        public sealed class Theme
        {
            public string Name; public int Bpm; public int Root; public int[] Scale; public int[] Progression; public float LeadDuty; public bool TriangleLead; public int Drums; public int Seed;
        }

        private static readonly int[] Minor = { 0, 2, 3, 5, 7, 8, 10 };
        private static readonly int[] HarmonicMinor = { 0, 2, 3, 5, 7, 8, 11 };
        private static readonly int[] Phrygian = { 0, 1, 3, 5, 7, 8, 10 };
        private static readonly int[] Dorian = { 0, 2, 3, 5, 7, 9, 10 };
        private static readonly int[] Mixolydian = { 0, 2, 4, 5, 7, 9, 10 };
        private static readonly int[] PhrygianDominant = { 0, 1, 4, 5, 7, 8, 10 };

        /// <summary>Index 0 = menu, 1..10 = stages. Progression entries are scale degrees (0-based).</summary>
        public static readonly Theme[] Themes =
        {
            new Theme { Name = "Menu", Bpm = 88, Root = 57, Scale = Minor, Progression = new[] { 0, 5, 2, 6 }, LeadDuty = .5f, TriangleLead = true, Drums = 1, Seed = 100 },
            new Theme { Name = "Cursed Graveyard", Bpm = 100, Root = 50, Scale = Minor, Progression = new[] { 0, 5, 2, 6 }, LeadDuty = .25f, Drums = 2, Seed = 1 },
            new Theme { Name = "Dark Forest", Bpm = 108, Root = 52, Scale = Minor, Progression = new[] { 0, 3, 6, 2 }, LeadDuty = .5f, TriangleLead = true, Drums = 2, Seed = 2 },
            new Theme { Name = "Forgotten Village", Bpm = 116, Root = 55, Scale = Mixolydian, Progression = new[] { 0, 4, 5, 3 }, LeadDuty = .25f, Drums = 2, Seed = 3 },
            new Theme { Name = "Ruined Castle", Bpm = 124, Root = 48, Scale = HarmonicMinor, Progression = new[] { 0, 5, 3, 4 }, LeadDuty = .125f, Drums = 3, Seed = 4 },
            new Theme { Name = "Crimson Swamp", Bpm = 96, Root = 53, Scale = Phrygian, Progression = new[] { 0, 1, 0, 6 }, LeadDuty = .5f, Drums = 2, Seed = 5 },
            new Theme { Name = "Frozen Peaks", Bpm = 110, Root = 57, Scale = Minor, Progression = new[] { 0, 6, 5, 6 }, LeadDuty = .5f, TriangleLead = true, Drums = 1, Seed = 6 },
            new Theme { Name = "Sea of Lost Souls", Bpm = 104, Root = 47, Scale = Dorian, Progression = new[] { 0, 3, 0, 6 }, LeadDuty = .25f, Drums = 2, Seed = 7 },
            new Theme { Name = "Blood Arenas", Bpm = 134, Root = 50, Scale = PhrygianDominant, Progression = new[] { 0, 1, 0, 6 }, LeadDuty = .125f, Drums = 3, Seed = 8 },
            new Theme { Name = "Throne of Death", Bpm = 120, Root = 51, Scale = HarmonicMinor, Progression = new[] { 0, 5, 6, 4 }, LeadDuty = .25f, Drums = 3, Seed = 9 },
            new Theme { Name = "Soul Core", Bpm = 140, Root = 54, Scale = Minor, Progression = new[] { 0, 5, 2, 6 }, LeadDuty = .25f, Drums = 3, Seed = 10 },
        };

        private static int Degree(Theme t, int degree, int octave = 0)
        {
            int n = t.Scale.Length;
            int d = ((degree % n) + n) % n;
            return t.Root + t.Scale[d] + 12 * (octave + Mathf.FloorToInt(degree / (float)n));
        }

        /// <summary>8 bars of 4/4: bass, arpeggio, a two-phrase lead melody and drums; loops seamlessly.</summary>
        public static float[] Music(Theme t)
        {
            var rng = new System.Random(t.Seed);
            float beat = 60f / t.Bpm, bar = beat * 4f;
            const int bars = 8;
            var b = Buffer(bar * bars);

            // Lead: phrase A (bars 0-3), repeated as A' (bars 4-7) with a changed ending.
            var phrase = new List<(float time, float length, int degree)>();
            int degreeNow = 4;
            for (int barIndex = 0; barIndex < 4; barIndex++)
                for (int step = 0; step < 8; step++)
                {
                    if (rng.NextDouble() < .28) continue; // rests
                    int len = rng.NextDouble() < .3 && step < 7 ? 2 : 1; // never ring past the bar (loop seam)
                    degreeNow = Mathf.Clamp(degreeNow + rng.Next(-2, 3), 0, 9);
                    phrase.Add((barIndex * bar + step * beat / 2f, len * beat / 2f * .95f, degreeNow));
                    step += len - 1;
                }

            for (int barIndex = 0; barIndex < bars; barIndex++)
            {
                float t0 = barIndex * bar;
                int chord = t.Progression[barIndex % t.Progression.Length];
                // Bass: eighth notes on the chord root, octave jump on the off-beats.
                for (int i = 0; i < 8; i++)
                {
                    int note = Degree(t, chord, -1) + (i % 2 == 1 ? 12 : 0);
                    Tone(b, t0 + i * beat / 2f, beat / 2f * .9f, Midi(note), Midi(note), Triangle, .32f, .003f, .03f);
                }
                // Arpeggio: chord tones in sixteenths.
                int[] arp = { 0, 2, 4, 2 };
                for (int i = 0; i < 16; i++)
                {
                    int note = Degree(t, chord + arp[i % 4], 1);
                    Tone(b, t0 + i * beat / 4f, beat / 4f * .8f, Midi(note), Midi(note), p => Square(p, .25f), .07f, .002f, .03f);
                }
                // Drums.
                for (int i = 0; i < 8; i++)
                {
                    float at = t0 + i * beat / 2f;
                    bool kick = i == 0 || i == 4 || (t.Drums >= 3 && (i == 3 || i == 6));
                    bool snare = t.Drums >= 2 && (i == 2 || i == 6);
                    if (kick) Tone(b, at, .18f, 110, 45, Sine, .55f, .001f, .12f);
                    if (snare) Noise(b, rng, at, .14f, .3f, 4500, 1500);
                    Noise(b, rng, at, .035f, t.Drums >= 2 ? .09f : .05f, 9000, 7000);
                }
            }
            foreach (var n in phrase)
                for (int repeat = 0; repeat < 2; repeat++)
                {
                    int degree = n.degree;
                    if (repeat == 1 && n.time >= 3 * bar) degree = Mathf.Max(0, degree - 2); // A' resolves lower
                    int note = Degree(t, degree, 1);
                    float start = n.time + repeat * 4 * bar;
                    if (t.TriangleLead) Tone(b, start, n.length, Midi(note), Midi(note), Triangle, .28f, .005f, .05f, 5.5f, .006f);
                    else Tone(b, start, n.length, Midi(note), Midi(note), p => Square(p, t.LeadDuty), .15f, .004f, .05f, 5.5f, .006f);
                }
            return Finish(b, .75f);
        }

        // ---------- WAV ----------

        /// <summary>Ramps the first seconds of a 16-bit PCM WAV up from silence, removing the click at its start (run once per source file).</summary>
        public static void FadeIn(string path, float seconds)
        {
            byte[] bytes = File.ReadAllBytes(path);
            int rate = BitConverter.ToInt32(bytes, 24), channels = BitConverter.ToInt16(bytes, 22), i = 12;
            while (i < bytes.Length - 8)
            {
                string id = System.Text.Encoding.ASCII.GetString(bytes, i, 4);
                int len = BitConverter.ToInt32(bytes, i + 4);
                if (id == "data")
                {
                    int frames = Mathf.Min(len / (2 * channels), Mathf.CeilToInt(seconds * rate));
                    for (int f = 0; f < frames; f++)
                        for (int c = 0; c < channels; c++)
                        {
                            int at = i + 8 + 2 * (f * channels + c);
                            short s = BitConverter.ToInt16(bytes, at);
                            short faded = (short)Mathf.RoundToInt(s * (f / (float)frames));
                            bytes[at] = (byte)(faded & 0xff); bytes[at + 1] = (byte)((faded >> 8) & 0xff);
                        }
                    File.WriteAllBytes(path, bytes);
                    return;
                }
                i += 8 + len;
            }
        }

        public static void Write(string path, float[] samples)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                int dataBytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + dataBytes); w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(dataBytes);
                foreach (float s in samples) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
            }
        }
    }
}
#endif
