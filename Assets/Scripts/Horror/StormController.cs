using System.Collections;
using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Night storm: irregular lightning flashes that bleach the whole island
    /// white for a few frames, thunder that rumbles a moment later (distance
    /// = delay), and the occasional emergency broadcast the storm interrupts.
    /// </summary>
    public class StormController : MonoBehaviour
    {
        [Header("Wired by builder")]
        public HorrorDirector director;
        public Light moon;                 // the directional moon light
        public Light[] flickerLights;      // every scene light that can die

        [Header("Flash")]
        public float flashIntensity = 2.6f;
        public float flashColorTemp = 0.12f;   // seconds per strobe
        public float minGap = 14f;
        public float maxGap = 46f;

        [Header("Ambient flash")]
        public Color ambientBase = new Color(0.075f, 0.085f, 0.115f);
        public Color ambientFlash = new Color(0.62f, 0.68f, 0.82f);
        public Color moonBase = new Color(0.55f, 0.63f, 0.82f);
        public Color moonFlash = new Color(0.95f, 0.97f, 1f);

        [Header("Broadcasts")]
        public string[] broadcasts = new[]
        {
            "⚠ EMERGENCY BROADCAST — CONTAINMENT FAILURE SECTOR 4",
            "⚠ ALL PERSONNEL: DO NOT APPROACH THE PADDOCK FENCES",
            "⚠ VISITOR EVACUATION SUSPENDED — STAY INSIDE",
            "⚠ POWER GRID UNSTABLE — GENERATOR 3 OFFLINE",
            "⚠ INGEN SECURITY: LOST CONTACT WITH UNIT 07"
        };
        public float broadcastIntervalMin = 55f;
        public float broadcastIntervalMax = 130f;

        float baseMoonIntensity;
        float nextFlash;
        float nextBroadcast;
        AudioSource thunderSrc;
        float[] lightBaseIntensity;

        void Start()
        {
            if (moon != null)
            {
                baseMoonIntensity = moon.intensity;
                moon.color = moonBase;
            }

            // snapshot every scene light so flashes can surge them, then restore
            if (flickerLights != null)
            {
                lightBaseIntensity = new float[flickerLights.Length];
                for (int i = 0; i < flickerLights.Length; i++)
                    lightBaseIntensity[i] = flickerLights[i] != null
                        ? flickerLights[i].intensity : 0f;
            }

            thunderSrc = gameObject.AddComponent<AudioSource>();
            thunderSrc.playOnAwake = false;
            thunderSrc.spatialBlend = 0f;
            thunderSrc.volume = 0.85f;

            nextFlash = Time.time + Random.Range(6f, 18f);
            nextBroadcast = Time.time + Random.Range(broadcastIntervalMin,
                Mathf.Max(broadcastIntervalMin + 5f, broadcastIntervalMax));
        }

        void Update()
        {
            if (Time.time > nextFlash)
            {
                StartCoroutine(FlashRoutine());
                nextFlash = Time.time + Random.Range(minGap, maxGap);
            }

            if (broadcasts != null && broadcasts.Length > 0 && Time.time > nextBroadcast)
            {
                if (MainMenu.Instance != null && !MainMenu.Instance.Started)
                {
                    nextBroadcast = Time.time + 10f;   // hold until the tape rolls
                }
                else
                {
                    if (director != null && !director.IsCaught)
                        director.Broadcast(broadcasts[Random.Range(0, broadcasts.Length)],
                            Random.Range(3.6f, 5.2f));
                    nextBroadcast = Time.time + Random.Range(broadcastIntervalMin,
                        Mathf.Max(broadcastIntervalMin + 5f, broadcastIntervalMax));
                }
            }
        }

        IEnumerator FlashRoutine()
        {
            // 1-3 strobes of decreasing strength
            int strobes = Random.Range(1, 4);
            float total = 0f;

            for (int i = 0; i < strobes; i++)
            {
                float strength = Mathf.Lerp(1f, 0.35f, i / Mathf.Max(1f, strobes - 1f));
                yield return StartCoroutine(FlashOn(strength));
                total += flashColorTemp;

                float gap = Random.Range(0.03f, 0.13f);
                yield return new WaitForSeconds(gap);
                total += gap;
            }

            yield return StartCoroutine(FlashOff());

            // thunder after the light — delay sells the distance
            float delay = Random.Range(0.5f, 2.6f);
            yield return new WaitForSeconds(delay);
            PlayThunder(delay);

            // a big flash makes the tape jump
            if (strobes >= 2 && director != null)
            {
                director.Glitch(Random.Range(0.3f, 0.6f), false);
                director.Shake(0.035f, 0.4f);
            }
        }

        IEnumerator FlashOn(float strength)
        {
            float dur = flashColorTemp * Random.Range(0.7f, 1.4f);
            float elapsed = 0f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float k = strength * (1f - elapsed / dur * 0.35f);

                if (moon != null)
                {
                    moon.intensity = Mathf.Lerp(baseMoonIntensity, flashIntensity, k);
                    moon.color = Color.Lerp(moonBase, moonFlash, k);
                }
                RenderSettings.ambientLight = Color.Lerp(ambientBase, ambientFlash, k);
                DimSceneLights(1f + k * 0.9f);

                yield return null;
            }
        }

        IEnumerator FlashOff()
        {
            float elapsed = 0f;
            float dur = 0.35f;
            Color fromAmbient = RenderSettings.ambientLight;
            float fromMoon = moon != null ? moon.intensity : 0f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float k = 1f - elapsed / dur;

                if (moon != null)
                {
                    moon.intensity = Mathf.Lerp(baseMoonIntensity, fromMoon, k);
                    moon.color = Color.Lerp(moonBase, moonFlash, k * 0.5f);
                }
                RenderSettings.ambientLight = Color.Lerp(ambientBase, fromAmbient, k);
                DimSceneLights(1f + k * 0.4f);

                yield return null;
            }

            if (moon != null)
            {
                moon.intensity = baseMoonIntensity;
                moon.color = moonBase;
            }
            RenderSettings.ambientLight = ambientBase;
            DimSceneLights(1f);
        }

        void DimSceneLights(float multiplier)
        {
            if (flickerLights == null || lightBaseIntensity == null) return;
            for (int i = 0; i < flickerLights.Length; i++)
            {
                var l = flickerLights[i];
                if (l == null) continue;
                // power surge: practical lights wash out during the flash
                l.intensity = lightBaseIntensity[i] * multiplier;
            }
        }

        void PlayThunder(float delay)
        {
            if (thunderSrc == null) return;
            var clip = ProceduralAudio.Thunder(Random.Range(1, 9999),
                Random.Range(3.4f, 6f));
            thunderSrc.pitch = Random.Range(0.85f, 1.1f);
            thunderSrc.volume = Mathf.Lerp(0.95f, 0.4f, Mathf.Clamp01(delay / 2.6f));
            thunderSrc.PlayOneShot(clip);

            // the deep rumble you feel in your chest
            if (director != null && delay < 1.4f)
                director.Shake(0.02f, 1.2f);
        }
    }
}
