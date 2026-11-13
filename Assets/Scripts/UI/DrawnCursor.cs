using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// The game's own cursor: a jittering tape-reticle with chromatic
    /// ghosts, a slow wobble and an expanding click ripple, drawn from
    /// OnGUI while the OS cursor is hidden. Used by the title menu and the
    /// pause card so the arrow never breaks the analog-horror frame.
    /// </summary>
    public static class DrawnCursor
    {
        static float clickTime = -10f;
        static Vector2 clickPos;

        /// <summary>Hide the OS cursor and unlock — call wherever the drawn
        /// cursor takes over (it wins every frame anyway).</summary>
        public static void Hide()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
        }

        /// <summary>Spawn the ripple at the current mouse position.</summary>
        public static void NoteClick()
        {
            Vector3 mp = Input.mousePosition;
            clickPos = new Vector2(mp.x, Screen.height - mp.y);
            clickTime = Time.unscaledTime;
        }

        /// <summary>Draw the reticle. Call from OnGUI (any event phase is
        /// safe — only repaint draws).</summary>
        public static void Draw()
        {
            if (Event.current.type != EventType.Repaint) return;

            float t = Time.unscaledTime;

            Vector3 mp = Input.mousePosition;
            Vector2 p = new Vector2(mp.x, Screen.height - mp.y);

            // tape instability: fast jitter + a slow lateral wobble
            float jx = (Mathf.PerlinNoise(t * 11f, 3.1f) - 0.5f) * 3.4f;
            float jy = (Mathf.PerlinNoise(7.7f, t * 11f) - 0.5f) * 3.4f;
            float wx = Mathf.Sin(t * 1.6f) * 1.3f;
            Vector2 c = p + new Vector2(jx + wx, jy);

            // chromatic ghosts first, clean white reticle on top
            DrawReticle(c + new Vector2(-2.5f, 0.5f), new Color(1f, 0.22f, 0.18f, 0.4f));
            DrawReticle(c + new Vector2(2.5f, -0.5f), new Color(0.3f, 0.88f, 1f, 0.4f));
            DrawReticle(c, new Color(1f, 1f, 1f, 0.95f));

            // click ripple — an expanding capture-frame square
            float age = t - clickTime;
            if (age < 0.42f)
            {
                float k = age / 0.42f;
                float e = 1f - (1f - k) * (1f - k);           // ease-out
                float size = Mathf.Lerp(12f, 46f, e);
                float a = (1f - k) * 0.65f;
                DrawSquareOutline(clickPos, size, 1.5f, new Color(1f, 1f, 1f, a));

                // little inner flash on the press itself
                if (k < 0.25f)
                    Fill(new Rect(clickPos.x - 3f, clickPos.y - 3f, 6f, 6f),
                        new Color(1f, 1f, 1f, (1f - k / 0.25f) * 0.8f));
            }
        }

        // -----------------------------------------------------------------

        static void DrawReticle(Vector2 c, Color col)
        {
            float th = 1.5f;

            // crosshair ticks with a gap around the centre
            float gap = 4f, len = 9f;
            Fill(new Rect(c.x - gap - len, c.y - th * 0.5f, len, th), col);
            Fill(new Rect(c.x + gap, c.y - th * 0.5f, len, th), col);
            Fill(new Rect(c.x - th * 0.5f, c.y - gap - len, th, len), col);
            Fill(new Rect(c.x - th * 0.5f, c.y + gap, th, len), col);

            // centre dot
            Fill(new Rect(c.x - 1.2f, c.y - 1.2f, 2.4f, 2.4f), col);

            // corner brackets — the tape's capture frame
            float r = 13f, arm = 6f;
            Color br = col; br.a *= 0.6f;

            Fill(new Rect(c.x - r, c.y - r, arm, th), br);            // TL
            Fill(new Rect(c.x - r, c.y - r, th, arm), br);
            Fill(new Rect(c.x + r - arm, c.y - r, arm, th), br);       // TR
            Fill(new Rect(c.x + r - th, c.y - r, th, arm), br);
            Fill(new Rect(c.x - r, c.y + r - th, arm, th), br);        // BL
            Fill(new Rect(c.x - r, c.y + r - arm, th, arm), br);
            Fill(new Rect(c.x + r - arm, c.y + r - th, arm, th), br);  // BR
            Fill(new Rect(c.x + r - th, c.y + r - arm, th, arm), br);
        }

        static void DrawSquareOutline(Vector2 c, float size, float th, Color col)
        {
            float h = size * 0.5f;
            Fill(new Rect(c.x - h, c.y - h, size, th), col);
            Fill(new Rect(c.x - h, c.y + h - th, size, th), col);
            Fill(new Rect(c.x - h, c.y - h, th, size), col);
            Fill(new Rect(c.x + h - th, c.y - h, th, size), col);
        }

        static void Fill(Rect r, Color col)
        {
            GUI.color = col;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
        }
    }
}
