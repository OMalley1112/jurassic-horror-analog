using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Minimal game HUD in the style of modern releases: tiny, muted,
    /// OS-font typography that never shouts — objective line + cell pips,
    /// a hairline stamina bar, one-line prompts, the tape-cuts counter and
    /// the end card. The danger vignette lives here too.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        public ObjectiveSystem objective;
        public FirstPersonController controller;
        public HorrorDirector director;
        public BaitThrower bait;

        GUIStyle objStyle;        // objective line
        GUIStyle tinyStyle;       // counters, states
        GUIStyle promptStyle;     // interaction prompt
        GUIStyle endStyle;        // end card title
        GUIStyle endInfoStyle;    // end card body
        bool stylesReady;
        Texture2D vignette;

        void OnGUI()
        {
            if (objective == null) return;
            if (MainMenu.Instance != null && !MainMenu.Instance.Started) return;
            if (BossFight.CutsceneRunning) return;    // the boss intro owns the frame
            if (!stylesReady) EnsureStyles();

            float w = Screen.width;
            float h = Screen.height;
            float m = 22f;

            if (objective.Escaped)
            {
                if (objective.ShowEndCard) DrawEndCard(w, h);
                return;                     // no gameplay HUD over the end card
            }

            // --- danger vignette: edges bleed as it closes ---
            if (director != null && vignette != null)
            {
                float threat = Mathf.Clamp01(director.displayedTension);
                float alpha = 0.1f + 0.72f * threat * threat;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(new Rect(0f, 0f, w, h), vignette);
                GUI.color = Color.white;
            }

            // --- subtle center dot (modern-FP feel, nothing more) ---
            GUI.color = new Color(1f, 1f, 1f, 0.3f);
            DrawRect(w * 0.5f - 1f, h * 0.5f - 1f, 2f, 2f);
            GUI.color = Color.white;

            // --- top-left, tucked under the camcorder REC block ---
            float x = m + 34f;
            float y = m + 62f;
            string state = objective.Collected >= objective.requiredCells
                ? "OBJECTIVE   REACH THE MAIN GATE"
                : "OBJECTIVE   RECOVER POWER CELLS";
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            GUI.Label(new Rect(x, y, 360f, 16f), state, objStyle);

            for (int i = 0; i < objective.requiredCells; i++)
            {
                bool has = i < objective.Collected;
                GUI.color = has
                    ? new Color(0.6f, 1f, 0.75f, 0.85f)
                    : new Color(1f, 1f, 1f, 0.16f);
                DrawRect(x + i * 19f, y + 15f, 14f, 3f);
            }
            GUI.color = Color.white;

            // --- bottom-left: counters, stamina hairline, bait kit ---
            float bx = m + 12f;
            float by = h - m - 42f;
            float bw = 120f;
            float bh = 4f;

            if (director != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.42f);
                GUI.Label(new Rect(bx, by - 17f, 260f, 14f),
                    "TAPE CUTS   " + director.CatchCount, tinyStyle);
            }

            if (controller != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.14f);
                DrawRect(bx, by, bw, bh);

                float s01 = Mathf.Clamp01(controller.Stamina01);
                GUI.color = Color.Lerp(
                    new Color(1f, 0.42f, 0.34f, 0.7f),
                    new Color(1f, 1f, 1f, 0.6f),
                    Mathf.SmoothStep(0f, 1f, s01));
                DrawRect(bx, by, bw * s01, bh);
                GUI.color = Color.white;

                if (controller.IsCrouched)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.45f);
                    GUI.Label(new Rect(bx + bw + 10f, by - 5f, 90f, 14f),
                        "CROUCHED", tinyStyle);
                    GUI.color = Color.white;
                }
            }

            if (bait != null)
            {
                GUI.color = bait.BonesLeft > 0
                    ? new Color(1f, 1f, 1f, 0.5f)
                    : new Color(1f, 1f, 1f, 0.24f);
                GUI.Label(new Rect(bx, by + 10f, 300f, 14f),
                    "BONES  " + bait.BonesLeft + "    Q  THROW", tinyStyle);
                GUI.color = Color.white;
            }

            // --- one-line interaction prompt under the crosshair ---
            if (objective.NearCell != null)
                DrawShadowText(new Rect(0f, h * 0.58f, w, 22f),
                    "E    TAKE POWER CELL", promptStyle, 0.85f);
        }

        void DrawEndCard(float w, float h)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.92f);
            DrawRect(0f, 0f, w, h);
            GUI.color = Color.white;

            GUI.Label(new Rect(0f, h * 0.34f, w, 40f), "END OF TAPE", endStyle);

            int surv = Mathf.FloorToInt(objective.Elapsed);
            string stats = string.Format(
                "SURVIVED   {0:00}:{1:00}        POWER CELLS   {2}/{3}        TAPE CUTS   {4}",
                surv / 60, surv % 60,
                objective.Collected, objective.requiredCells,
                director != null ? director.CatchCount : 0);
            GUI.color = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(0f, h * 0.34f + 54f, w, 22f), stats, endInfoStyle);

            if ((Time.unscaledTime % 1f) < 0.62f)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.8f);
                GUI.Label(new Rect(0f, h * 0.6f, w, 22f),
                    "R    REWIND TO BEGINNING", endInfoStyle);
            }
            GUI.color = Color.white;
        }

        void EnsureStyles()
        {
            if (stylesReady) return;

            Font ui = HudFonts.Ui;

            objStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            if (ui != null) objStyle.font = ui;
            objStyle.normal.textColor = Color.white;

            tinyStyle = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            if (ui != null) tinyStyle.font = ui;
            tinyStyle.normal.textColor = Color.white;

            promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            if (ui != null) promptStyle.font = ui;
            promptStyle.normal.textColor = Color.white;

            endStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            if (ui != null) endStyle.font = ui;
            endStyle.normal.textColor = Color.white;

            endInfoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            if (ui != null) endInfoStyle.font = ui;
            endInfoStyle.normal.textColor = Color.white;

            // danger vignette: transparent center, blood-dark corners
            const int N = 112;
            vignette = new Texture2D(N, N, TextureFormat.RGBA32, false);
            vignette.wrapMode = TextureWrapMode.Clamp;
            for (int yy = 0; yy < N; yy++)
            {
                for (int xx = 0; xx < N; xx++)
                {
                    float nx = (xx / (float)(N - 1)) * 2f - 1f;
                    float ny = (yy / (float)(N - 1)) * 2f - 1f;
                    float d = Mathf.Sqrt(nx * nx + ny * ny) / 1.41421356f;
                    float a = Mathf.SmoothStep(0.42f, 0.98f, d);
                    vignette.SetPixel(xx, yy, new Color(0.36f, 0.02f, 0.02f, a));
                }
            }
            vignette.Apply();

            stylesReady = true;
        }

        /// <summary>Text with a 1px dark drop for readability over any scene.</summary>
        static void DrawShadowText(Rect rect, string text, GUIStyle style, float alpha)
        {
            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f * alpha);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(rect, text, style);
            GUI.color = prev;
        }

        static void DrawRect(float x, float y, float w, float h)
        {
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        }
    }
}
