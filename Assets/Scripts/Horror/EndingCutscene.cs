using System.Collections;
using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Movie ending: once the gate is reached the tape cuts to a directed
    /// sequence — letterboxed shots of the walk-out, a crane up over the
    /// island with film-splice flashes, then a credits card, before the
    /// END OF TAPE stats land. Spawned by ObjectiveSystem.Escape().
    /// R skips straight to the stats.
    /// </summary>
    public class EndingCutscene : MonoBehaviour
    {
        ObjectiveSystem objective;
        FirstPersonController ctl;
        Transform player;
        Camera playerCam;
        Camera cutCam;
        AudioSource src;
        AmbientAudio ambience;

        bool running, finished, skip;
        string caption = "";
        float captionA;
        float fade;          // black out 0..1
        float flash;         // white splice flash 0..1
        float creditT = -1f; // credits clock (<0 = not started)

        GUIStyle capStyle;
        GUIStyle credStyle;
        GUIStyle credTitleStyle;
        bool stylesReady;

        public bool Running => running;

        static readonly string[] Credits =
        {
            "JURASSIC PARK III",
            "",
            "THE RECOVERED TAPE",
            "",
            "STARRING",
            "MAYA   \u00B7   THE TRAPPED SURVIVOR",
            "RANGER BOONE   \u00B7   STATION 07",
            "VASQUEZ   \u00B7   GEOTHERMAL PLANT",
            "",
            "THE ISLAND   \u00B7   ITSELF",
            "MUSIC   \u00B7   PROCEDURAL AUDIO",
            "CAMERA   \u00B7   ONE TAPE, NO CUTS",
            "",
            "ANALOG HORROR PICTURES"
        };

        public void Begin(ObjectiveSystem obj)
        {
            objective = obj;
            ctl = obj.controller;
            player = obj.player;
            ambience = obj.ambience;

            // a camera of our own — the FPS camera writes its rotation
            // every frame and would fight a cutscene
            var go = new GameObject("CutsceneCamera");
            cutCam = go.AddComponent<Camera>();
            cutCam.fieldOfView = 62f;
            cutCam.nearClipPlane = 0.05f;

            if (ctl != null && ctl.camHolder != null)
            {
                playerCam = ctl.camHolder.GetComponent<Camera>();
                if (playerCam != null) playerCam.enabled = false;
            }

            var cc = player != null ? player.GetComponent<CharacterController>() : null;
            if (cc != null) cc.enabled = false;

            src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;

            running = true;
            StartCoroutine(Run());
        }

        /// <summary>R pressed — cut to the end card.</summary>
        public void Skip()
        {
            skip = true;
        }

        IEnumerator Run()
        {
            // ---- SHOT 1: roadside, the walk out to the gate ---------------
            yield return Shot(3.6f,
                new Vector3(5.6f, 1.02f, 13.8f), new Vector3(4.6f, 1.14f, 17.4f),
                new Vector3(0f, 1.5f, 19.6f), new Vector3(0f, 1.6f, 21.4f),
                "EXT. MAIN GATE \u2014 NIGHT", 0.4f,
                new Vector3(0f, 0.15f, 21.0f), 2.6f);

            // ---- SHOT 2: outside the arch, him arriving -------------------
            if (!skip)
                yield return Shot(3.4f,
                    new Vector3(0f, 1.36f, 27.4f), new Vector3(0f, 1.5f, 28.6f),
                    new Vector3(0f, 1.5f, 20.2f), new Vector3(0f, 1.4f, 21.2f),
                    "MAIN GATE \u2014 POWER RESTORED", 0.35f,
                    new Vector3(0f, 0.15f, 21.6f), 3.0f);

            // ---- SHOT 3: crane up over the island (the movie wide) -------
            if (!skip)
                yield return Shot(4.4f,
                    new Vector3(7.5f, 2.6f, 30.5f), new Vector3(13.5f, 15f, 38.5f),
                    new Vector3(0f, 2.0f, 20.5f), new Vector3(0f, 3.5f, -3f),
                    "ISLA SORNA \u2014 03:12 AM", 0.5f,
                    null, 0f, true);

            yield return RunCredits();
        }

        /// <summary>One film shot: slow dolly, look-at sweep, caption.</summary>
        IEnumerator Shot(float dur, Vector3 from, Vector3 to,
            Vector3 lookA, Vector3 lookB, string cap, float capDelay,
            Vector3? walkTarget, float walkDur, bool stormFlash = false)
        {
            caption = cap;
            captionA = 0f;
            float t = 0f;
            float walked = 0f;
            bool flashedA = false, flashedB = false;

            if (cutCam != null)
            {
                cutCam.transform.position = from;
                cutCam.transform.rotation = Quaternion.LookRotation(lookA - from);
            }

            while (t < dur && !skip)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = k * k * (3f - 2f * k);

                if (cutCam != null)
                {
                    Vector3 p = Vector3.Lerp(from, to, e);
                    Vector3 look = Vector3.Lerp(lookA, lookB, e);
                    cutCam.transform.position = p;
                    cutCam.transform.rotation = Quaternion.LookRotation(look - p);
                }

                // the walk-out: keep the body striding toward the gate
                if (walkTarget.HasValue && walked < walkDur && player != null)
                {
                    walked += Time.deltaTime;
                    player.rotation = Quaternion.identity;
                    player.position = Vector3.MoveTowards(player.position,
                        walkTarget.Value, 1.2f * Time.deltaTime);

                    if (ctl != null && ctl.body != null)
                        ctl.body.SetLocomotion(1.2f / 5.4f, true, 0f, false);
                }

                // caption fades up after the cut settles
                captionA = t > capDelay
                    ? Mathf.Min(1f, (t - capDelay) / 0.5f) : 0f;

                // photo-flash lightning on the crane shot
                if (stormFlash && !flashedA && t > 1.1f)
                {
                    flashedA = true;
                    flash = 0.85f;
                    if (ambience != null) ambience.PlayThunder();
                }
                if (stormFlash && !flashedB && t > 2.7f)
                {
                    flashedB = true;
                    flash = 0.7f;
                }

                bool filmPaused = PauseSystem.Instance != null && PauseSystem.Instance.IsPaused;
                if (!filmPaused && Input.GetKeyDown(KeyCode.R)) skip = true;

                yield return null;
            }

            caption = "";
            captionA = 0f;
        }

        IEnumerator RunCredits()
        {
            // fade to black — the picture ends, the tape keeps rolling
            float t = 0f;
            float fadeDur = skip ? 0.01f : 0.9f;
            while (t < fadeDur)
            {
                t += Time.deltaTime;
                fade = Mathf.Clamp01(t / fadeDur);
                yield return null;
            }
            fade = 1f;

            if (skip)
            {
                Finish();
                yield break;
            }

            if (ambience != null) ambience.PlaySting();

            // credits roll their own clock; R or ~10 s hands over to the card
            creditT = 0f;
            float hold = 0f;
            while (hold < 10f)
            {
                hold += Time.deltaTime;
                creditT += Time.deltaTime;
                bool filmPaused = PauseSystem.Instance != null && PauseSystem.Instance.IsPaused;
                if (!filmPaused && Input.GetKeyDown(KeyCode.R)) break;
                yield return null;
            }

            Finish();
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            running = false;

            if (playerCam != null) playerCam.enabled = true;
            if (ctl != null && ctl.camHolder != null)
            {
                var cc = ctl.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = true;
            }
            if (cutCam != null) Destroy(cutCam.gameObject);

            if (objective != null) objective.NotifyCutsceneDone();
            Destroy(gameObject);
        }

        void Update()
        {
            if (flash > 0f) flash -= Time.deltaTime * 4.5f;
        }

        // ---------------------------------------------------------------- GUI

        void OnGUI()
        {
            if (finished) return;
            if (!stylesReady) EnsureStyles();

            float w = Screen.width;
            float h = Screen.height;
            float bar = h * 0.11f;

            // letterbox bars
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, w, bar), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, h - bar, w, bar), Texture2D.whiteTexture);

            // scene caption inside the bottom bar
            if (captionA > 0f && caption.Length > 0)
            {
                GUI.color = new Color(1f, 1f, 1f, captionA * 0.82f);
                GUI.Label(new Rect(40f, h - bar + 3f, w - 80f, bar - 6f),
                    caption, capStyle);
            }

            // skip hint, blinking, tucked right
            if (!skip && creditT < 0f)
            {
                float blink = (Time.unscaledTime % 1f) < 0.6f ? 0.34f : 0.12f;
                GUI.color = new Color(1f, 1f, 1f, blink);
                GUI.Label(new Rect(w - 170f, h - bar + 3f, 130f, bar - 6f),
                    "R  SKIP", credStyle);
            }

            // splice flash over everything
            if (flash > 0f)
            {
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(flash));
                GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            }

            // black out
            if (fade > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(fade));
                GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            }

            if (creditT >= 0f) DrawCredits(w, h);

            GUI.color = Color.white;
        }

        void DrawCredits(float w, float h)
        {
            float top = h * 0.16f;
            float lineH = 19f;
            float startY = top + Mathf.Max(0f, h * 0.5f - Credits.Length * lineH * 0.5f - top);

            for (int i = 0; i < Credits.Length; i++)
            {
                float a = Mathf.Clamp01((creditT - i * 0.38f) / 0.6f);
                if (a <= 0f) continue;

                bool title = i == 0;
                GUI.color = new Color(1f, 1f, 1f, a * (title ? 0.96f : 0.7f));
                GUI.Label(new Rect(0f, startY + i * lineH, w, lineH + 6f),
                    Credits[i], title ? credTitleStyle : credStyle);
            }

            // once every line is up, blink the rewind prompt
            float all = (Credits.Length - 1) * 0.38f + 1.2f;
            if (creditT > all && (Time.unscaledTime % 1f) < 0.62f)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.55f);
                GUI.Label(new Rect(0f, h - h * 0.14f, w, 22f),
                    "R    REWIND TO BEGINNING", credStyle);
            }
            GUI.color = Color.white;
        }

        void EnsureStyles()
        {
            Font mono = HudFonts.Mono;
            Font ui = HudFonts.Ui;

            capStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };
            if (mono != null) capStyle.font = mono;
            capStyle.normal.textColor = Color.white;

            credStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            if (mono != null) credStyle.font = mono;
            credStyle.normal.textColor = Color.white;

            credTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            if (ui != null) credTitleStyle.font = ui;
            credTitleStyle.normal.textColor = Color.white;

            stylesReady = true;
        }
    }
}
