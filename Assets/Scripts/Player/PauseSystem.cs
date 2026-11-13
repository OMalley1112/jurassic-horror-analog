using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Esc / click tape-pause. Freezes time, audio and the player, then
    /// draws the pause card over a picture that keeps hissing.
    /// </summary>
    public class PauseSystem : MonoBehaviour
    {
        public static PauseSystem Instance { get; private set; }

        public FirstPersonController controller;

        public bool IsPaused { get; private set; }

        GUIStyle headStyle;
        GUIStyle infoStyle;
        bool stylesReady;

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            // the title screen owns input until the run begins
            if (MainMenu.Instance != null && !MainMenu.Instance.Started) return;

            if (Input.GetKeyDown(KeyCode.Escape))
                Toggle();
            else if (IsPaused && Input.GetMouseButtonDown(0))
            {
                DrawnCursor.NoteClick();
                Toggle();               // click anywhere to keep rolling
            }
        }

        void Toggle()
        {
            IsPaused = !IsPaused;

            Time.timeScale = IsPaused ? 0f : 1f;
            AudioListener.pause = IsPaused;

            if (controller != null) controller.SetPauseFrozen(IsPaused);

            if (IsPaused)
            {
                // the drawn tape-reticle takes over from the OS arrow
                DrawnCursor.Hide();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OnGUI()
        {
            if (!IsPaused) return;
            if (!stylesReady) EnsureStyles();

            float w = Screen.width;
            float h = Screen.height;

            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(0f, h * 0.36f, w, 60f), "||  TAPE PAUSED", headStyle);

            if ((Time.unscaledTime % 1f) < 0.62f)
                GUI.Label(new Rect(0f, h * 0.56f, w, 30f),
                    "[ESC] / [CLICK]   CONTINUE", infoStyle);

            GUI.color = Color.white;
            DrawnCursor.Draw();
        }

        void EnsureStyles()
        {
            Font ui = HudFonts.Ui;

            headStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            if (ui != null) headStyle.font = ui;
            headStyle.normal.textColor = new Color(1f, 1f, 1f, 0.95f);

            infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            if (ui != null) infoStyle.font = ui;
            infoStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);

            stylesReady = true;
        }

        void OnDisable()
        {
            // never leave the tape stopped if this object goes away
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (Instance == this) Instance = null;
        }
    }
}
