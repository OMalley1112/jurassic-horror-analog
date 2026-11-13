using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Generates all sound effects from code (no audio files needed).
    /// </summary>
    public static class ProceduralAudio
    {
        const int SampleRate = 44100;

        /// <summary>Short decaying noise burst. Used for footsteps, clicks, static, stings.</summary>
        /// <param name="decay">Higher = dies faster.</param>
        /// <param name="lowPass">0 = very muffled, 1 = full raw noise.</param>
        public static AudioClip NoiseBurst(float duration, float decay, int seed, float lowPass = 1f)
        {
            int n = Mathf.Max(8, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[n];
            var rnd = new System.Random(seed);
            float prev = 0f;
            float a = Mathf.Clamp01(lowPass);

            for (int i = 0; i < n; i++)
            {
                float w = (float)(rnd.NextDouble() * 2.0 - 1.0);
                prev = Mathf.Lerp(prev, w, a);
                float env = Mathf.Exp(-decay * (i / (float)SampleRate));
                data[i] = prev * env;
            }

            var clip = AudioClip.Create("burst_" + seed, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Seamlessly looping low drone for ambient dread.</summary>
        public static AudioClip LoopDrone(float duration, int seed, float baseFreq = 44f)
        {
            int n = Mathf.Max(64, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[n];
            var rnd = new System.Random(seed);

            const int partialCount = 7;
            var freqs = new float[partialCount];
            var amps = new float[partialCount];
            var phases = new float[partialCount];

            for (int k = 0; k < partialCount; k++)
            {
                float target = baseFreq * (k + 1) + (float)(rnd.NextDouble() * 6.0 - 3.0);
                // Snap to integer cycles over the clip length so the loop is seamless.
                freqs[k] = Mathf.Max(1f, Mathf.Round(target * duration)) / duration;
                amps[k] = 1f / (k + 1.4f);
                phases[k] = (float)(rnd.NextDouble() * Math2Pi);
            }

            float lfoFreq = 2f / duration; // exactly two cycles per clip
            float lfoPhase = (float)(rnd.NextDouble() * Math2Pi);

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float s = 0f;
                for (int k = 0; k < partialCount; k++)
                    s += amps[k] * Mathf.Sin(Math2Pi * freqs[k] * t + phases[k]);

                float lfo = 0.78f + 0.22f * Mathf.Sin(Math2Pi * lfoFreq * t + lfoPhase);
                data[i] = s * lfo * 0.22f;
            }

            Normalize(data, 0.85f);

            var clip = AudioClip.Create("drone_" + seed, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Distant dinosaur roar: sweeping growl + noise + tremolo.</summary>
        public static AudioClip Roar(float duration, int seed)
        {
            int n = Mathf.Max(64, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[n];
            var rnd = new System.Random(seed);

            float phase = 0f;
            float noisePrev = 0f;

            for (int i = 0; i < n; i++)
            {
                float p = i / (float)n;                       // 0..1 progress
                float t = i / (float)SampleRate;
                float freq = Mathf.Lerp(115f, 42f, p);        // downward sweep
                phase += Math2Pi * freq / SampleRate;
                if (phase > Math2Pi) phase -= Math2Pi;

                float saw = 2f * (phase / Math2Pi) - 1f;

                float w = (float)(rnd.NextDouble() * 2.0 - 1.0);
                noisePrev = Mathf.Lerp(noisePrev, w, 0.12f);  // muffled noise

                float env = Mathf.Pow(Mathf.Sin(Mathf.PI * p), 1.4f);
                float tremolo = 0.75f + 0.25f * Mathf.Sin(Math2Pi * 37f * t);

                data[i] = (float)System.Math.Tanh((saw * 0.65f + noisePrev * 0.5f) * env * tremolo * 2.2f) * 0.85f;
            }

            var clip = AudioClip.Create("roar_" + seed, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Sharp VHS-style scare sting (noise crack + falling tone).</summary>
        public static AudioClip Sting(int seed)
        {
            float duration = 0.7f;
            int n = Mathf.Max(64, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[n];
            var rnd = new System.Random(seed);
            float prev = 0f;

            for (int i = 0; i < n; i++)
            {
                float p = i / (float)n;
                float t = i / (float)SampleRate;

                float w = (float)(rnd.NextDouble() * 2.0 - 1.0);
                prev = Mathf.Lerp(prev, w, 0.85f);

                float noise = prev * Mathf.Exp(-9f * t);
                float toneFreq = Mathf.Lerp(1400f, 220f, p);
                float tone = Mathf.Sin(Math2Pi * toneFreq * t) * Mathf.Exp(-5.5f * t);

                data[i] = (float)System.Math.Tanh((noise * 0.9f + tone * 0.7f) * 1.6f) * 0.9f;
            }

            var clip = AudioClip.Create("sting_" + seed, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Click for flashlight switch / battery swap.</summary>
        public static AudioClip Click(int seed)
        {
            return NoiseBurst(0.05f, 90f, seed, 0.95f);
        }

        /// <summary>One heartbeat cycle (lub-dub, ~0.8s). Caller handles loop timing.</summary>
        public static AudioClip Heartbeat(int seed)
        {
            float duration = 0.8f;
            int n = Mathf.Max(64, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[n];
            var rnd = new System.Random(seed);

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = ThumpEnv(t, 0f) + 0.7f * ThumpEnv(t, 0.24f);
                float body = Mathf.Sin(Math2Pi * 52f * t);
                float w = (float)(rnd.NextDouble() * 2.0 - 1.0);
                data[i] = (float)System.Math.Tanh((body * 1.15f + w * 0.22f) * env * 1.8f) * 0.9f;
            }

            var clip = AudioClip.Create("heart_" + seed, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float ThumpEnv(float t, float t0)
        {
            float dt = t - t0;
            if (dt < 0f) return 0f;
            return (1f - Mathf.Exp(-260f * dt)) * Mathf.Exp(-16f * dt);
        }

        /// <summary>Single heavy stomp / huge footstep thud.</summary>
        public static AudioClip Stomp(int seed)
        {
            float duration = 1.1f;
            int n = Mathf.Max(64, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[n];
            var rnd = new System.Random(seed);
            float prev = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-7f * t) * (1f - Mathf.Exp(-500f * t));
                float w = (float)(rnd.NextDouble() * 2.0 - 1.0);
                prev = Mathf.Lerp(prev, w, 0.22f);
                float thud = Mathf.Sin(Math2Pi * 38f * t);
                data[i] = (float)System.Math.Tanh((thud + prev * 0.7f) * env * 2.4f) * 0.95f;
            }

            var clip = AudioClip.Create("stomp_" + seed, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Distant thunder: long muffled rumble that darkens over time.</summary>
        public static AudioClip Thunder(int seed, float duration = 4.5f)
        {
            int n = Mathf.Max(64, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[n];
            var rnd = new System.Random(seed);
            float prev = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / duration;
                float w = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float cutoff = Mathf.Lerp(0.30f, 0.03f, p);      // gets darker over time
                prev = Mathf.Lerp(prev, w, cutoff);

                float env = Mathf.Exp(-1.5f * t);
                float after = t > 0.8f ? 0.6f * Mathf.Exp(-0.9f * (t - 0.8f)) : 0f;
                data[i] = (float)System.Math.Tanh(prev * (1f + after) * env * 3.2f) * 0.95f;
            }

            Normalize(data, 0.95f);
            var clip = AudioClip.Create("thunder_" + seed, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Raptor screech: harsh rising-falling shriek with vibrato.</summary>
        public static AudioClip Screech(int seed, float duration = 1.15f)
        {
            int n = Mathf.Max(64, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[n];
            var rnd = new System.Random(seed);
            float phase = 0f;
            float noisePrev = 0f;

            for (int i = 0; i < n; i++)
            {
                float p = i / (float)n;
                float t = i / (float)SampleRate;

                float baseFreq = Mathf.Lerp(1150f, 2100f, Mathf.Sin(Mathf.PI * p));
                float vib = 1f + 0.16f * Mathf.Sin(Math2Pi * 34f * t);
                phase += Math2Pi * baseFreq * vib / SampleRate;
                if (phase > Math2Pi) phase -= Math2Pi;

                float saw = 2f * (phase / Math2Pi) - 1f;
                float w = (float)(rnd.NextDouble() * 2.0 - 1.0);
                noisePrev = Mathf.Lerp(noisePrev, w, 0.6f);

                float env = (1f - Mathf.Exp(-60f * t)) * Mathf.Pow(Mathf.Sin(Mathf.PI * p), 1.2f);
                data[i] = (float)System.Math.Tanh((saw * 0.8f + noisePrev * 0.45f) * env * 3f) * 0.85f;
            }

            var clip = AudioClip.Create("screech_" + seed, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Creepy whisper: sibilant high-passed noise with syllable wobble.</summary>
        public static AudioClip Whisper(int seed, float duration = 2.6f)
        {
            int n = Mathf.Max(64, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[n];
            var rnd = new System.Random(seed);
            float sm = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float w = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float s = Mathf.Lerp(sm, w, 0.35f);
                float hp = s - sm;                        // differencing = highpass
                sm = s;

                float a = Mathf.Abs(Mathf.Sin(Math2Pi * 3.1f * t + 0.4f));
                float b = Mathf.Abs(Mathf.Sin(Math2Pi * 5.7f * t));
                float syllables = Mathf.Clamp01(0.12f + 0.88f * a * (0.35f + 0.65f * b));
                float env = Mathf.Min(t / 0.15f, 1f) * Mathf.Min((duration - t) / 0.35f, 1f);

                data[i] = Mathf.Clamp(hp * syllables * env * 1.6f, -1f, 1f);
            }

            var clip = AudioClip.Create("whisper_" + seed, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Seamlessly looping wind bed (crossfaded tail = click-free loop).</summary>
        public static AudioClip WindLoop(float duration, int seed)
        {
            int n = Mathf.Max(64, Mathf.RoundToInt(duration * SampleRate));
            const int fade = 4096;
            var gen = new float[n + fade];
            var rnd = new System.Random(seed);
            float deep = 0f, mid = 0f;

            for (int i = 0; i < n + fade; i++)
            {
                float w = (float)(rnd.NextDouble() * 2.0 - 1.0);
                deep = Mathf.Lerp(deep, w, 0.02f);
                mid = Mathf.Lerp(mid, w, 0.18f);

                float t = i / (float)SampleRate;
                // integer cycle counts over the base length => LFO phase wraps seamlessly
                float gust = 0.55f
                    + 0.30f * Mathf.Sin(Math2Pi * (2f / duration) * t)
                    + 0.15f * Mathf.Sin(Math2Pi * (5f / duration) * t + 1.3f);

                gen[i] = (deep * 2.6f + mid * 0.7f) * gust;
            }

            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = gen[i];
            for (int i = 0; i < fade; i++)
            {
                float wgt = i / (float)fade;
                data[i] = data[i] * (1f - wgt) + gen[n + i] * wgt;
            }

            Normalize(data, 0.62f);
            var clip = AudioClip.Create("wind_" + seed, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void Normalize(float[] data, float peak)
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float a = Mathf.Abs(data[i]);
                if (a > max) max = a;
            }
            if (max <= 1e-5f) return;
            float gain = peak / max;
            for (int i = 0; i < data.Length; i++) data[i] *= gain;
        }

        const float Math2Pi = Mathf.PI * 2f;
    }
}
