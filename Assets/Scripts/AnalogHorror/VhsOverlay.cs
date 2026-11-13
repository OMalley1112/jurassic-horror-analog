using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// VHS camcorder look: post-processing (via VHSOverlay shader) plus a
    /// "REC" viewfinder HUD. Also handles glitch/static bursts.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class VhsOverlay : MonoBehaviour
    {
        [Header("Effect")]
        public Material effectMaterial;

        [HideInInspector] public float glitch;

        [Header("Tape HUD text")]
        public string cameraLabel = "CAM 04  VISITOR CENTER";
        public string tapeDate = "12 JUL 1993";

        AudioSource staticSrc;
        float startTime;
        float nextBurst;
        float nextIdleJitter;

        [HideInInspector] public float rewind;          // 0..1 tape being eaten
        string broadcastText;
        float broadcastUntil;

        GUIStyle recStyle;
        GUIStyle infoStyle;
        GUIStyle playStyle;
        GUIStyle bigStyle;
        GUIStyle broadcastStyle;
        bool stylesReady;

        /// <summary>Centered emergency-broadcast text on the tape.</summary>
        public void Broadcast(string text, float duration)
        {
            broadcastText = text;
            broadcastUntil = Time.time + duration;
        }

        void Start()
        {
            startTime = Time.time;
            nextBurst = Time.time + Random.Range(8f, 20f);

            staticSrc = gameObject.AddComponent<AudioSource>();
            staticSrc.playOnAwake = false;
            staticSrc.spatialBlend = 0f;
            staticSrc.volume = 0.55f;
        }

        void Update()
        {
            if (glitch > 0f)
                glitch = Mathf.Max(0f, glitch - Time.deltaTime * 2.4f);

            if (rewind > 0f)
                rewind = Mathf.Max(0f, rewind - Time.deltaTime * 0.09f);

            if (broadcastText != null && Time.time > broadcastUntil)
                broadcastText = null;

            if (Time.time > nextBurst)
            {
                TriggerGlitch(Random.Range(0.35f, 1f));
                nextBurst = Time.time + Random.Range(8f, 26f);
            }

            // rare tiny tape jitter even without a full burst
            if (Time.time > nextIdleJitter)
            {
                glitch = Mathf.Max(glitch, Random.value < 0.25f ? 0.12f : 0f);
                nextIdleJitter = Time.time + Random.Range(3f, 9f);
            }

            // the eaten tape drags the picture even between static bursts
            if (rewind > 0.05f)
                glitch = Mathf.Max(glitch, rewind * (0.55f +
                    0.45f * Mathf.PerlinNoise(Time.time * 9f, 3.7f)));
        }

        /// <summary>Force a static burst. 0..1 strength.</summary>
        public void TriggerGlitch(float amount)
        {
            glitch = Mathf.Clamp01(Mathf.Max(glitch, amount));

            if (staticSrc != null && amount > 0.5f && !staticSrc.isPlaying)
            {
                var clip = ProceduralAudio.NoiseBurst(0.45f, 7f, Random.Range(1, 9999), 0.9f);
                staticSrc.pitch = Random.Range(0.9f, 1.2f);
                staticSrc.PlayOneShot(clip, 0.5f);
            }
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (effectMaterial == null)
            {
                Graphics.Blit(src, dst);
                return;
            }
            effectMaterial.SetFloat("_Glitch", glitch);
            Graphics.Blit(src, dst, effectMaterial);
        }

        void EnsureStyles()
        {
            if (stylesReady) return;
            Font mono = HudFonts.Mono;
            Font ui = HudFonts.Ui;

            recStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            if (mono != null) recStyle.font = mono;
            recStyle.normal.textColor = new Color(1f, 1f, 1f, 0.9f);

            infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            if (mono != null) infoStyle.font = mono;
            infoStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);

            playStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            if (mono != null) playStyle.font = mono;
            playStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);

            bigStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            if (mono != null) bigStyle.font = mono;
            bigStyle.normal.textColor = new Color(1f, 1f, 1f, 0.92f);

            // alerts read as small bottom subtitles — like shipped games,
            // not arcade marquees
            broadcastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            if (ui != null) broadcastStyle.font = ui;
            broadcastStyle.normal.textColor = Color.white;

            stylesReady = true;
        }

        void OnGUI()
        {
            EnsureStyles();

            float w = Screen.width;
            float h = Screen.height;
            float m = 24f;              // viewfinder margin
            float corner = 34f;
            float thick = 2f;

            // --- corner brackets ---
            GUI.color = new Color(1f, 1f, 1f, 0.35f);
            DrawRect(m, m, corner, thick);                        // TL -
            DrawRect(m, m, thick, corner);                        // TL |
            DrawRect(w - m - corner, m, corner, thick);           // TR -
            DrawRect(w - m - thick, m, thick, corner);            // TR |
            DrawRect(m, h - m - thick, corner, thick);            // BL -
            DrawRect(m, h - m - corner, thick, corner);           // BL |
            DrawRect(w - m - corner, h - m - thick, corner, thick); // BR -
            DrawRect(w - m - thick, h - m - corner, thick, corner); // BR |
            GUI.color = Color.white;

            float t = Time.time - startTime;
            string timecode = string.Format("{0:00}:{1:00}:{2:00}:{3:00}",
                (int)(t / 3600f), (int)(t / 60f) % 60, (int)t % 60, (int)((t * 30f) % 30f));

            // --- top left: REC + tape mode ---
            bool blink = (Time.unscaledTime % 1f) < 0.55f;
            float x = m + 14f;
            float y = m + 10f;

            if (blink)
            {
                GUI.color = new Color(1f, 0.25f, 0.2f, 0.95f);
                GUI.Label(new Rect(x, y + 3f, 20f, 20f), "\u25CF");   // record dot
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(x + 24f, y, 260f, 30f), "REC", recStyle);
            GUI.Label(new Rect(x + 24f, y + 30f, 300f, 24f), "SP   " + timecode, infoStyle);

            // --- top right: camera id + date ---
            float rightW = 340f;
            GUI.Label(new Rect(w - m - 14f - rightW, y, rightW, 24f), cameraLabel, infoStyle);
            GUI.Label(new Rect(w - m - 14f - rightW, y + 24f, rightW, 24f), tapeDate, infoStyle);
            GUI.Label(new Rect(w - m - 14f - rightW, y + 48f, rightW, 24f),
                string.Format("BATT {0:0}%", Mathf.PingPong(Time.time * 4f, 6f) + 74f), infoStyle);

            // --- bottom left: PLAY ---
            GUI.Label(new Rect(m + 14f, h - m - 30f, 200f, 26f), "\u25B6 PLAY", playStyle);

            // --- bottom: emergency broadcast as a quiet subtitle line ---
            if (broadcastText != null)
            {
                float fade = Mathf.Clamp01((broadcastUntil - Time.time) / 0.5f);
                // dark drop, then the line — readable over any scene
                GUI.color = new Color(0f, 0f, 0f, 0.55f * fade);
                GUI.Label(new Rect(m + 1f, h - 104f, w - m * 2f, 20f),
                    broadcastText, broadcastStyle);
                GUI.color = new Color(1f, 1f, 1f, fade);
                GUI.Label(new Rect(m, h - 105f, w - m * 2f, 20f),
                    broadcastText, broadcastStyle);
                GUI.color = Color.white;
            }

            // --- center: the tape is being eaten ---
            if (rewind > 0.02f)
            {
                bool rewindBlink = (Time.unscaledTime % 0.5f) < 0.28f;
                GUI.color = new Color(1f, 1f, 1f, rewindBlink ? rewind : rewind * 0.5f);
                GUI.Label(new Rect(0f, h * 0.42f, w, 70f), "◀◀ REWIND", bigStyle);
                GUI.color = Color.white;
            }

            // --- bottom right: glitch warning ---
            if (glitch > 0.45f)
            {
                GUI.color = new Color(1f, 0.35f, 0.3f, 0.6f + 0.4f * Mathf.Sin(Time.time * 30f));
                GUI.Label(new Rect(w - m - 220f, h - m - 30f, 210f, 26f), "\u26A0 TRACKING", playStyle);
                GUI.color = Color.white;
            }
        }

        static void DrawRect(float x, float y, float w, float h)
        {
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        }
    }
}
