using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// A living survivor you can walk up to and talk to (E). Dialogue locks
    /// the player in place — talking in the dark is a commitment — while the
    /// NPC turns to face you, head tracking included, and speaks in short
    /// procedural voice blips. One conversation at a time, island-wide.
    /// </summary>
    public class NpcSurvivor : MonoBehaviour
    {
        /// <summary>The NPC currently talking, if any.</summary>
        public static NpcSurvivor Active { get; private set; }

        [Header("Dialogue")]
        public string npcName = "SURVIVOR";
        [TextArea(2, 6)] public string[] lines;
        public float talkRange = 3.6f;

        [Header("Wired by builder")]
        public FirstPersonController controller;
        public Transform head;

        public bool Nearby { get; private set; }

        bool talking;
        int idx;
        AudioSource voice;
        int voiceSeed;
        int blipsLeft;
        float nextBlip;

        GUIStyle nameStyle;
        GUIStyle lineStyle;
        GUIStyle promptStyle;
        bool stylesReady;

        static bool MenuOpen =>
            MainMenu.Instance != null && !MainMenu.Instance.Started;

        void Start()
        {
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0.35f;
            voice.dopplerLevel = 0f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.minDistance = 1.5f;
            voice.maxDistance = 14f;
            voiceSeed = Random.Range(1, 9999);
        }

        void Update()
        {
            // the tape is stopped — no keys reach the conversation
            if (PauseSystem.Instance != null && PauseSystem.Instance.IsPaused) return;

            var objective = ObjectiveSystem.Instance;
            bool over = objective != null && objective.Escaped;

            if (MenuOpen || over)
            {
                if (talking) Close();
                Nearby = false;
                return;
            }

            var director = HorrorDirector.Instance;
            bool caught = director != null && director.IsCaught;

            if (talking)
            {
                if (caught) { Close(); return; }

                FacePlayer(4f);

                // voice blips for the current line
                if (blipsLeft > 0 && Time.unscaledTime >= nextBlip)
                {
                    blipsLeft--;
                    nextBlip = Time.unscaledTime + Random.Range(0.13f, 0.26f);
                    voice.pitch = Random.Range(0.82f, 1.28f);
                    voice.PlayOneShot(ProceduralAudio.NoiseBurst(
                        Random.Range(0.08f, 0.16f), 55f,
                        voiceSeed + idx * 13 + blipsLeft, 0.42f), 0.42f);
                }

                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) ||
                    Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.KeypadEnter))
                    Next();

                Nearby = false;
                return;
            }

            // proximity prompt
            Nearby = false;
            if (Active != null || controller == null || caught) return;

            Vector3 to = controller.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > talkRange * talkRange) return;

            Nearby = true;
            FacePlayer(2.5f);

            if (Input.GetKeyDown(KeyCode.E))
                Open();
        }

        void LateUpdate()
        {
            // dialogue owns the player — re-assert if something else let go
            if (talking && controller != null && !controller.IsFrozen)
                controller.SetFrozen(true);
        }

        void FacePlayer(float speed)
        {
            if (controller == null) return;

            Vector3 to = controller.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(to), speed * Time.deltaTime);

            var cam = Camera.main;
            if (head != null && cam != null)
            {
                Vector3 hp = cam.transform.position - head.position;
                if (hp.sqrMagnitude > 0.01f)
                    head.rotation = Quaternion.Slerp(head.rotation,
                        Quaternion.LookRotation(hp), 3.5f * Time.deltaTime);
            }
        }

        void Open()
        {
            Active = this;
            talking = true;
            idx = -1;
            if (controller != null) controller.SetFrozen(true);
            Next();
        }

        void Next()
        {
            idx++;
            if (lines == null || idx >= lines.Length)
            {
                Close();
                return;
            }
            blipsLeft = Random.Range(2, 4);
            nextBlip = Time.unscaledTime;
        }

        void Close()
        {
            talking = false;
            if (Active == this) Active = null;

            if (controller == null) return;
            // only external holds belong here — the pause menu owns its own flag
            var director = HorrorDirector.Instance;
            bool caught = director != null && director.IsCaught;
            var objective = ObjectiveSystem.Instance;
            bool escaped = objective != null && objective.Escaped;
            controller.SetFrozen(caught || escaped);
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        // ---------------------------------------------------------------- GUI

        void OnGUI()
        {
            if (MenuOpen) return;
            var objective = ObjectiveSystem.Instance;
            if (objective != null && objective.Escaped) return;

            if (!stylesReady) EnsureStyles();

            if (talking) DrawDialogue();
            else if (Nearby && Active == null) DrawPrompt();
        }

        void DrawPrompt()
        {
            float w = Screen.width;
            float h = Screen.height;
            DrawShadowed(new Rect(0f, h * 0.62f, w, 22f),
                "E    TALK \u2014 " + npcName, promptStyle, 0.85f);
        }

        void DrawDialogue()
        {
            float w = Screen.width;
            float h = Screen.height;
            float bw = Mathf.Min(760f, w - 90f);
            Rect box = new Rect((w - bw) * 0.5f, h - 212f, bw, 106f);

            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;

            DrawShadowed(new Rect(box.x + 18f, box.y + 10f, bw - 36f, 16f),
                npcName, nameStyle, 0.9f);

            string line = (lines != null && idx >= 0 && idx < lines.Length)
                ? lines[idx] : "";
            DrawShadowed(new Rect(box.x + 18f, box.y + 32f, bw - 36f, 54f),
                line, lineStyle, 0.95f);

            if (blipsLeft <= 0 && (Time.unscaledTime % 1f) < 0.6f)
                DrawShadowed(new Rect(box.x + bw - 150f, box.y + box.height - 22f,
                        132f, 16f),
                    "E   CONTINUE", promptStyle, 0.7f);
        }

        static void DrawShadowed(Rect r, string text, GUIStyle style, float alpha)
        {
            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f * alpha);
            GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, style);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(r, text, style);
            GUI.color = prev;
        }

        void EnsureStyles()
        {
            Font ui = HudFonts.Ui;

            nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            if (ui != null) nameStyle.font = ui;
            nameStyle.normal.textColor = new Color(1f, 0.78f, 0.38f, 0.9f);

            lineStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true
            };
            if (ui != null) lineStyle.font = ui;
            lineStyle.normal.textColor = Color.white;

            promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            if (ui != null) promptStyle.font = ui;
            promptStyle.normal.textColor = Color.white;

            stylesReady = true;
        }
    }
}
