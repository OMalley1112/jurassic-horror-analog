using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Title screen: a recovered-tape menu that gates the whole game.
    /// While it's open the player is frozen, every gameplay system idles,
    /// and the island keeps breathing behind the glass (lightning, static,
    /// slow camera drift). Keyboard + mouse.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public static MainMenu Instance { get; private set; }

        /// <summary>True once the run has begun (or immediately on restarts).</summary>
        public bool Started { get; private set; }

        [Header("Wired by builder")]
        public FirstPersonController controller;
        public ObjectiveSystem objective;

        static bool sessionShown;   // survives scene reloads inside one play session

        int sel;
        bool showControls;
        bool showSettings;
        int setSel;
        int dragRow = -1;
        Quaternion startRot;
        float driftSeed;

        GUIStyle titleStyle;
        GUIStyle subStyle;
        GUIStyle itemStyle;
        GUIStyle footStyle;
        GUIStyle ctrlStyle;
        GUIStyle valStyle;
        bool stylesReady;

        static readonly string[] Items = { "BEGIN TAPE", "SETTINGS", "CONTROLS", "QUIT" };

        static readonly string[] SetRows =
        {
            "MOUSE SENSITIVITY", "FIELD OF VIEW", "MASTER VOLUME",
            "INVERT Y AXIS", "HEAD BOB", "RESET DEFAULTS"
        };
        const int SetCount = 6;

        static readonly string[] Controls =
        {
            "WASD        MOVE           MOUSE      LOOK",
            "SHIFT       SPRINT         CTRL       CROUCH",
            "SPACE       JUMP           F          FLASHLIGHT",
            "R           SPARE BATTERY  E          INTERACT / TALK",
            "Q           THROW BONE     ESC        PAUSE THE TAPE"
        };

        /// <summary>True while the title screen owns the game.</summary>
        public static bool IsOpen => Instance != null && !Instance.Started;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            // scene restarted after an ending — go straight into the game
            if (sessionShown)
            {
                Started = true;
                return;
            }

            if (controller != null)
            {
                startRot = controller.transform.rotation;
                controller.SetFrozen(true);
            }
            driftSeed = Random.Range(0f, 100f);
            GameSettings.Load();
            AssertCursor();
        }

        void Update()
        {
            if (Started) return;

            // FPC.Start may have re-locked the cursor — win every frame
            AssertCursor();

            // slow drift so the tape behind the glass keeps living
            if (controller != null)
                controller.transform.rotation = startRot * Quaternion.Euler(
                    0f, Mathf.Sin(Time.unscaledTime * 0.11f + driftSeed) * 9f, 0f);

            if (Input.GetMouseButtonDown(0)) DrawnCursor.NoteClick();

            if (showSettings)
            {
                HandleSettings();
                return;
            }

            if (showControls)
            {
                if (Input.GetKeyDown(KeyCode.Escape) ||
                    Input.GetKeyDown(KeyCode.Backspace) ||
                    Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    Input.GetMouseButtonDown(0))
                    showControls = false;
                return;
            }

            // keyboard navigation
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                sel = (sel + Items.Length - 1) % Items.Length;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                sel = (sel + 1) % Items.Length;

            // mouse hover
            float w = Screen.width;
            float h = Screen.height;
            Vector3 mp = Input.mousePosition;
            Vector2 mouse = new Vector2(mp.x, Screen.height - mp.y);
            for (int i = 0; i < Items.Length; i++)
                if (ItemRect(i, w, h).Contains(mouse))
                    sel = i;

            bool confirm = Input.GetKeyDown(KeyCode.Return) ||
                           Input.GetKeyDown(KeyCode.KeypadEnter) ||
                           Input.GetKeyDown(KeyCode.Space) ||
                           Input.GetKeyDown(KeyCode.E) ||
                           Input.GetMouseButtonDown(0);
            if (confirm) Activate();
        }

        void Activate()
        {
            switch (sel)
            {
                case 0: Begin(); break;
                case 1: showSettings = true; setSel = 0; break;
                case 2: showControls = true; break;
                case 3: Quit(); break;
            }
        }

        // ------------------------------------------------------- settings ---

        Rect SettingsPanel(float w, float h)
        {
            return new Rect((w - 620f) * 0.5f, h * 0.34f, 620f,
                60f + SetCount * 34f);
        }

        Rect SetRowRect(int i, float w, float h)
        {
            Rect p = SettingsPanel(w, h);
            return new Rect(p.x + 26f, p.y + 46f + i * 34f, p.width - 52f, 30f);
        }

        Rect SetBarRect(int i, float w, float h)
        {
            Rect r = SetRowRect(i, w, h);
            return new Rect(r.x + 220f, r.y + 9f, 210f, 12f);
        }

        void HandleSettings()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            {
                showSettings = false;
                dragRow = -1;
                return;
            }

            float w = Screen.width;
            float h = Screen.height;
            Vector3 mp = Input.mousePosition;
            Vector2 mouse = new Vector2(mp.x, Screen.height - mp.y);

            // keyboard navigation
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                setSel = (setSel + SetCount - 1) % SetCount;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                setSel = (setSel + 1) % SetCount;

            // hover
            for (int i = 0; i < SetCount; i++)
                if (SetRowRect(i, w, h).Contains(mouse)) setSel = i;

            // keyboard adjust
            int dir = 0;
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dir = -1;
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = 1;
            if (dir != 0)
            {
                Adjust(setSel, dir);
                GameSettings.Save();
            }

            if (setSel == 5 &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                 Input.GetKeyDown(KeyCode.Space)))
                GameSettings.ResetDefaults();

            // click: act inside the panel, close outside it
            if (Input.GetMouseButtonDown(0))
            {
                Rect panel = SettingsPanel(w, h);
                if (!panel.Contains(mouse))
                {
                    showSettings = false;
                    dragRow = -1;
                    return;
                }

                for (int i = 0; i < SetCount; i++)
                {
                    if (!SetRowRect(i, w, h).Contains(mouse)) continue;
                    setSel = i;
                    if (i == 5) GameSettings.ResetDefaults();
                    else if (i == 3) ToggleInvert();
                    else { dragRow = i; SetFromBar(i, mouse.x, w, h); }
                    break;
                }
            }

            // drag the bar while held
            if (dragRow >= 0)
            {
                if (Input.GetMouseButton(0)) SetFromBar(dragRow, mouse.x, w, h);
                else { dragRow = -1; GameSettings.Save(); }
            }
        }

        void Adjust(int i, int dir)
        {
            switch (i)
            {
                case 0:
                    GameSettings.Sensitivity = Mathf.Clamp(
                        GameSettings.Sensitivity + dir * 0.2f, 0.4f, 6f);
                    break;
                case 1:
                    GameSettings.Fov = Mathf.Clamp(GameSettings.Fov + dir * 5f, 55f, 100f);
                    break;
                case 2:
                    GameSettings.Volume = Mathf.Clamp01(GameSettings.Volume + dir * 0.05f);
                    break;
                case 3:
                    ToggleInvert();
                    break;
                case 4:
                    GameSettings.HeadBob = Mathf.Clamp01(GameSettings.HeadBob + dir * 0.05f);
                    break;
            }
        }

        void ToggleInvert()
        {
            GameSettings.InvertY = !GameSettings.InvertY;
            GameSettings.Save();
        }

        void SetFromBar(int i, float x, float w, float h)
        {
            Rect bar = SetBarRect(i, w, h);
            float t = Mathf.Clamp01((x - bar.x) / Mathf.Max(bar.width, 1f));

            switch (i)
            {
                case 0:
                    GameSettings.Sensitivity =
                        Mathf.Round(Mathf.Lerp(0.4f, 6f, t) * 10f) / 10f;
                    break;
                case 1:
                    GameSettings.Fov = Mathf.Round(Mathf.Lerp(55f, 100f, t) / 5f) * 5f;
                    break;
                case 2:
                    GameSettings.Volume = Mathf.Round(t * 20f) / 20f;
                    break;
                case 4:
                    GameSettings.HeadBob = Mathf.Round(t * 20f) / 20f;
                    break;
            }
        }

        float SetFraction(int i)
        {
            switch (i)
            {
                case 0: return Mathf.InverseLerp(0.4f, 6f, GameSettings.Sensitivity);
                case 1: return Mathf.InverseLerp(55f, 100f, GameSettings.Fov);
                case 2: return GameSettings.Volume;
                case 3: return GameSettings.InvertY ? 1f : 0f;
                case 4: return GameSettings.HeadBob;
                default: return 0f;
            }
        }

        string SetReadout(int i)
        {
            switch (i)
            {
                case 0: return GameSettings.Sensitivity.ToString("0.0");
                case 1: return Mathf.RoundToInt(GameSettings.Fov) + "\u00B0";
                case 2: return Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
                case 3: return GameSettings.InvertY ? "ON" : "OFF";
                case 4: return Mathf.RoundToInt(GameSettings.HeadBob * 100f) + "%";
                default: return "";
            }
        }

        void Begin()
        {
            Started = true;
            sessionShown = true;

            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (controller != null)
            {
                controller.transform.rotation = startRot;
                controller.SetFrozen(false);
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (objective != null) objective.OnRunStarted();

            var dir = HorrorDirector.Instance;
            if (dir != null)
            {
                dir.SetTension(0f);
                dir.Glitch(0.85f, true);        // the tape starts rolling
            }
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void AssertCursor()
        {
            // the drawn tape-reticle replaces the OS arrow
            DrawnCursor.Hide();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------- GUI

        Rect ItemRect(int i, float w, float h)
        {
            return new Rect((w - 340f) * 0.5f, h * 0.52f + i * 36f, 340f, 32f);
        }

        void OnGUI()
        {
            if (Started) return;
            if (!stylesReady) EnsureStyles();

            float w = Screen.width;
            float h = Screen.height;

            // dark plate over the live feed
            GUI.color = new Color(0.012f, 0.014f, 0.018f, 0.9f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);

            // scanlines
            GUI.color = new Color(0f, 0f, 0f, 0.14f);
            for (float y = 0f; y < h; y += 6f)
                GUI.DrawTexture(new Rect(0f, y, w, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // title with chromatic bleed — analog horror, not arcade
            float tw = Mathf.Min(900f, w - 40f);
            Rect tr = new Rect((w - tw) * 0.5f, h * 0.26f, tw, 60f);
            GUI.color = new Color(1f, 0.25f, 0.2f, 0.5f);
            GUI.Label(new Rect(tr.x - 3f, tr.y, tr.width, tr.height),
                "JURASSIC PARK", titleStyle);
            GUI.color = new Color(0.25f, 0.85f, 1f, 0.45f);
            GUI.Label(new Rect(tr.x + 3f, tr.y, tr.width, tr.height),
                "JURASSIC PARK", titleStyle);
            GUI.color = Color.white;
            GUI.Label(tr, "JURASSIC PARK", titleStyle);

            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(0f, tr.y + 66f, w, 20f),
                "\u258C ISLAND SECURITY TAPE 04 \u2014 RECOVERED FOOTAGE", subStyle);
            GUI.color = Color.white;

            if (showSettings) DrawSettings(w, h);
            else if (showControls) DrawControls(w, h);
            else DrawItems(w, h);

            // footer
            GUI.color = new Color(1f, 1f, 1f, 0.34f);
            GUI.Label(new Rect(0f, h - 44f, w, 20f),
                showSettings ? "W / S  SELECT     A / D  ADJUST     CLICK BARS     ESC  BACK"
                             : showControls ? "ESC  BACK"
                                            : "W / S  SELECT     ENTER  CONFIRM     MOUSE ALSO WORKS",
                footStyle);
            GUI.color = Color.white;

            DrawnCursor.Draw();
        }

        void DrawItems(float w, float h)
        {
            for (int i = 0; i < Items.Length; i++)
            {
                Rect r = ItemRect(i, w, h);
                bool on = i == sel;

                if (on)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.07f);
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                }

                GUI.color = on ? new Color(1f, 1f, 1f, 0.95f)
                               : new Color(1f, 1f, 1f, 0.48f);
                GUI.Label(new Rect(r.x + 52f, r.y, r.width - 52f, r.height),
                    Items[i], itemStyle);
                if (on)
                    GUI.Label(new Rect(r.x + 20f, r.y, 32f, r.height),
                        "\u25BA", itemStyle);
            }
            GUI.color = Color.white;
        }

        void DrawControls(float w, float h)
        {
            float pw = 560f;
            float ph = 46f + Controls.Length * 26f;
            Rect p = new Rect((w - pw) * 0.5f, h * 0.4f, pw, ph);

            GUI.color = new Color(1f, 1f, 1f, 0.05f);
            GUI.DrawTexture(p, Texture2D.whiteTexture);

            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.Label(new Rect(p.x, p.y + 12f, pw, 22f), "CONTROLS", itemStyle);

            GUI.color = new Color(1f, 1f, 1f, 0.62f);
            for (int i = 0; i < Controls.Length; i++)
                GUI.Label(new Rect(p.x + 34f, p.y + 46f + i * 26f, pw - 60f, 22f),
                    Controls[i], ctrlStyle);

            GUI.color = Color.white;
        }

        void DrawSettings(float w, float h)
        {
            Rect p = SettingsPanel(w, h);

            GUI.color = new Color(1f, 1f, 1f, 0.05f);
            GUI.DrawTexture(p, Texture2D.whiteTexture);

            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.Label(new Rect(p.x, p.y + 12f, p.width, 22f), "SETTINGS", itemStyle);

            for (int i = 0; i < SetCount; i++)
            {
                Rect r = SetRowRect(i, w, h);
                bool on = i == setSel;

                if (on)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.07f);
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                }

                GUI.color = on ? new Color(1f, 1f, 1f, 0.95f)
                               : new Color(1f, 1f, 1f, 0.58f);
                GUI.Label(new Rect(r.x + 10f, r.y, 210f, r.height),
                    SetRows[i], ctrlStyle);

                if (i < 5)
                {
                    // track
                    Rect bar = SetBarRect(i, w, h);
                    GUI.color = new Color(1f, 1f, 1f, 0.13f);
                    GUI.DrawTexture(bar, Texture2D.whiteTexture);

                    // fill
                    float frac = SetFraction(i);
                    GUI.color = new Color(1f, 1f, 1f, on ? 0.8f : 0.45f);
                    GUI.DrawTexture(
                        new Rect(bar.x, bar.y, bar.width * frac, bar.height),
                        Texture2D.whiteTexture);

                    // knob
                    GUI.color = Color.white;
                    GUI.DrawTexture(
                        new Rect(bar.x + bar.width * frac - 1.5f, bar.y - 3f,
                                 3f, bar.height + 6f),
                        Texture2D.whiteTexture);

                    GUI.color = on ? Color.white : new Color(1f, 1f, 1f, 0.6f);
                    GUI.Label(new Rect(r.x + 444f, r.y, r.width - 444f, r.height),
                        SetReadout(i), valStyle);
                }
                else
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.5f);
                    GUI.Label(new Rect(r.x + r.width - 170f, r.y, 160f, r.height),
                        "CLICK OR ENTER", valStyle);
                }
            }
            GUI.color = Color.white;
        }

        void EnsureStyles()
        {
            Font ui = HudFonts.Ui;
            Font mono = HudFonts.Mono;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 44,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            if (ui != null) titleStyle.font = ui;
            titleStyle.normal.textColor = Color.white;

            subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            if (mono != null) subStyle.font = mono;
            subStyle.normal.textColor = Color.white;

            itemStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            if (ui != null) itemStyle.font = ui;
            itemStyle.normal.textColor = Color.white;

            footStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };
            if (mono != null) footStyle.font = mono;
            footStyle.normal.textColor = Color.white;

            ctrlStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            if (mono != null) ctrlStyle.font = mono;
            ctrlStyle.normal.textColor = Color.white;

            valStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleRight
            };
            if (mono != null) valStyle.font = mono;
            valStyle.normal.textColor = Color.white;

            stylesReady = true;
        }
    }
}
