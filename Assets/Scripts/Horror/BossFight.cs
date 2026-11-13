using System.Collections;
using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// The gate showdown. Reaching the main gate with all three power cells
    /// no longer ends the tape immediately: a short letterboxed cutscene
    /// shows the T-Rex slamming onto the road and taking the exit — then it
    /// becomes a real fight. The flare gun (LMB) staggers it; four hits
    /// drive it back into the jungle and the ending film rolls. Losing is
    /// the usual jumpscare, and the fight restarts on your next approach.
    /// </summary>
    public class BossFight : MonoBehaviour
    {
        /// <summary>True while the intro film runs — gameplay HUDs stand down.</summary>
        public static bool CutsceneRunning { get; private set; }

        ObjectiveSystem objective;
        FirstPersonController ctl;
        Transform player;
        CharacterController cc;
        Camera playerCam;
        Camera cutCam;
        AudioSource src;
        HorrorDirector director;
        DinoHunter rex;

        GameObject flash;

        public bool Active { get; private set; }   // rex found — the boss rules the gate
        public bool Won { get; private set; }      // driven off — the gate is yours

        enum Phase { Idle, Intro, Fight, Won }
        Phase phase = Phase.Idle;

        bool fightRunning;      // intro + fight + retreat walk (gates re-entry)
        bool introShown;        // a death skips the film on the next attempt
        bool staggering;
        bool skip;
        float staggerUntil;
        float nextShot;
        int hits;
        const int Need = 4;

        string caption = "";
        float captionA;

        GUIStyle capStyle;
        GUIStyle labelStyle;
        GUIStyle tinyStyle;
        bool stylesReady;

        static readonly Vector3 RetreatPos = new Vector3(-85f, 0f, 40f);

        // ------------------------------------------------------------- setup

        public void Begin(ObjectiveSystem obj)
        {
            objective = obj;
            ctl = obj.controller;
            player = obj.player;
            director = HorrorDirector.Instance;

            for (int i = 0; i < DinoHunter.Pack.Count; i++)
            {
                var h = DinoHunter.Pack[i];
                if (h != null && h.kind == DinoHunter.Kind.TRex) { rex = h; break; }
            }
            Active = rex != null;
            if (!Active)
                Debug.LogWarning("BossFight: no T-Rex found — gate escape unchanged.");

            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
        }

        /// <summary>Called every frame while the player stands at the gate.</summary>
        public void RequestFight()
        {
            if (!Active || Won || fightRunning) return;
            if (director != null && director.IsCaught) return;
            if (ctl != null && ctl.IsHeldFrozen) return;    // dialogue holds the player

            if (introShown) { fightRunning = true; StartFight(); }
            else StartCoroutine(Intro());
        }

        // ------------------------------------------------------------ intro

        IEnumerator Intro()
        {
            fightRunning = true;
            phase = Phase.Intro;
            CutsceneRunning = true;
            skip = false;

            Vector3 b = PlaceRex();
            Vector3 p = player != null ? player.position : Vector3.zero;
            p.y = 0f;
            Vector3 fwd = p - b;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.04f) fwd = Vector3.forward;
            fwd.Normalize();

            if (ctl != null) ctl.SetFrozen(true);
            if (ctl != null && ctl.camHolder != null)
            {
                playerCam = ctl.camHolder.GetComponent<Camera>();
                if (playerCam != null) playerCam.enabled = false;
            }
            if (player != null) cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            cutCam = new GameObject("BossCutCamera").AddComponent<Camera>();
            cutCam.fieldOfView = 60f;
            cutCam.nearClipPlane = 0.05f;

            // ---- SHOT 1: wide side view — the road out, taken ------------
            Vector3 mid = (b + p) * 0.5f;
            Vector3 side = Vector3.Cross(Vector3.up, (p - b).normalized);
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            side.Normalize();

            yield return Shot(2.4f,
                mid + side * 6.5f + Vector3.up * 2.0f,
                mid + side * 5.0f + Vector3.up * 1.7f,
                mid + Vector3.up * 1.9f, mid + Vector3.up * 2.2f,
                "EXT. MAIN GATE \u2014 THE ROAD OUT IS TAKEN", 0.45f);

            // ---- SHOT 2: low angle, it sees you --------------------------
            if (!skip)
            {
                caption = "IT IS NOT LETTING YOU LEAVE";
                captionA = 0f;
                Vector3 from = b + fwd * 6.8f + Vector3.up * 0.55f;
                Vector3 to = b + fwd * 5.4f + Vector3.up * 0.5f;
                Vector3 look = b + Vector3.up * 4.3f;
                bool lunged = false;
                float t = 0f;
                const float dur = 2.6f;

                if (cutCam != null)
                {
                    cutCam.transform.position = from;
                    cutCam.transform.rotation = Quaternion.LookRotation(look - from);
                }

                while (t < dur && !skip)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / dur);
                    float e = k * k * (3f - 2f * k);

                    if (cutCam != null)
                    {
                        Vector3 pos = Vector3.Lerp(from, to, e);
                        cutCam.transform.position = pos;
                        cutCam.transform.rotation = Quaternion.LookRotation(look - pos);
                    }

                    captionA = t > 0.4f ? Mathf.Min(1f, (t - 0.4f) / 0.5f) : 0f;

                    if (!lunged && t > 0.55f)
                    {
                        lunged = true;
                        var anim = rex != null ? rex.GetComponent<DinoAnimator>() : null;
                        if (anim != null) anim.Lunge();
                        if (rex != null) rex.PlayAttackSound();
                        if (ctl != null) ctl.Shake(0.34f, 0.85f);
                        if (director != null) director.Glitch(0.55f, false);
                    }

                    bool filmPaused = PauseSystem.Instance != null && PauseSystem.Instance.IsPaused;
                    if (!filmPaused && Input.GetKeyDown(KeyCode.R)) skip = true;

                    yield return null;
                }
                caption = "";
                captionA = 0f;
            }

            EndCutsceneCam();
            StartFight();
        }

        /// <summary>One letterboxed dolly shot with a caption in the bar.</summary>
        IEnumerator Shot(float dur, Vector3 from, Vector3 to,
            Vector3 lookA, Vector3 lookB, string cap, float capDelay)
        {
            caption = cap;
            captionA = 0f;
            float t = 0f;

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
                    Vector3 pos = Vector3.Lerp(from, to, e);
                    Vector3 look = Vector3.Lerp(lookA, lookB, e);
                    cutCam.transform.position = pos;
                    cutCam.transform.rotation = Quaternion.LookRotation(look - pos);
                }

                captionA = t > capDelay
                    ? Mathf.Min(1f, (t - capDelay) / 0.5f) : 0f;

                bool filmPaused = PauseSystem.Instance != null && PauseSystem.Instance.IsPaused;
                if (!filmPaused && Input.GetKeyDown(KeyCode.R)) skip = true;

                yield return null;
            }

            caption = "";
            captionA = 0f;
        }

        void EndCutsceneCam()
        {
            CutsceneRunning = false;
            if (cutCam != null) { Destroy(cutCam.gameObject); cutCam = null; }
            if (playerCam != null) playerCam.enabled = true;
            if (cc != null) cc.enabled = true;
        }

        // ------------------------------------------------------------- fight

        void StartFight()
        {
            CutsceneRunning = false;
            phase = Phase.Fight;
            fightRunning = true;
            introShown = true;
            hits = 0;
            staggering = false;
            nextShot = Time.time + 0.6f;

            PlaceRex();
            if (rex != null)
            {
                // committed senses: the blockade never loses you
                rex.detectRange = 200f;
                rex.loseRange = 200f;
                rex.beamRange = 200f;
                rex.ForceHunt();
            }

            if (director != null)
                director.Broadcast("\u25C9 FLARE GUN \u2014 LMB TO FIRE \u2014 DRIVE IT OFF THE ROAD",
                    4.6f);
        }

        void Update()
        {
            if (!Active) return;
            if (MainMenu.Instance != null && !MainMenu.Instance.Started) return;

            // died mid-show → the tape rewinds; retry on the next approach
            if (fightRunning && director != null && director.IsCaught)
            {
                OnDeath();
                return;
            }

            if (phase != Phase.Fight || Won) return;
            if (ctl != null && ctl.IsFrozen) return;     // pause / dialogue

            // it must stay committed to the hunt
            if (rex != null && !staggering && !rex.IsChasing) rex.ForceHunt();

            // stagger over — back on its feet and coming again
            if (staggering && Time.time >= staggerUntil)
            {
                staggering = false;
                if (rex != null) rex.ForceHunt();
            }

            if (!staggering && Time.time >= nextShot && Input.GetMouseButtonDown(0))
                Fire();
        }

        void Fire()
        {
            if (ctl == null || playerCam == null) return;
            nextShot = Time.time + 0.75f;

            Vector3 pos = playerCam.transform.position;
            Vector3 fwd = playerCam.transform.forward;

            // muzzle flash
            if (flash != null) Destroy(flash);
            flash = new GameObject("FlareMuzzle");
            flash.transform.position = pos + fwd * 1.3f + Vector3.down * 0.16f;
            var l = flash.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.72f, 0.4f);
            l.intensity = 4.5f;
            l.range = 15f;
            StartCoroutine(ClearFlash());

            if (src != null)
            {
                src.pitch = Random.Range(0.94f, 1.08f);
                src.PlayOneShot(ProceduralAudio.NoiseBurst(
                    0.28f, 24f, Random.Range(1, 9999), 0.7f), 0.8f);
            }
            ctl.Shake(0.1f, 0.22f);

            if (rex == null) return;
            Vector3 target = rex.transform.position + Vector3.up * (rex.eyeHeight * 0.75f);
            Vector3 to = target - pos;
            float dist = to.magnitude;
            if (dist > 34f) return;
            if (Vector3.Dot(fwd, to / dist) < 0.97f) return;   // ~14° aim cone

            Hit();
        }

        IEnumerator ClearFlash()
        {
            yield return new WaitForSeconds(0.09f);
            if (flash != null) Destroy(flash);
            flash = null;
        }

        void Hit()
        {
            hits++;
            staggering = true;
            staggerUntil = Time.time + 1.2f;
            nextShot = Mathf.Max(nextShot, Time.time + 0.4f);

            rex.Freeze();
            rex.PlayAttackSound();
            if (director != null)
            {
                director.Glitch(0.5f, false);
                director.Shake(0.3f, 0.6f);
                director.Broadcast(hits >= Need
                    ? "\u25C9 DIRECT HIT \u2014 IT'S BREAKING"
                    : "\u25C9 HIT \u2014 IT RECOILS   " + hits + "/" + Need, 2.4f);
            }
            StartCoroutine(Recoil());

            if (hits >= Need) StartCoroutine(WinRoutine());
        }

        /// <summary>It reels back off its feet on the hit.</summary>
        IEnumerator Recoil()
        {
            if (rex == null) yield break;
            Transform t = rex.transform;
            Quaternion baseRot = t.rotation;
            float time = 0f;

            while (time < 0.55f)
            {
                if (rex == null) yield break;
                time += Time.deltaTime;
                float pitch;
                if (time < 0.18f) pitch = Mathf.Lerp(0f, -13f, time / 0.18f);
                else pitch = Mathf.Lerp(-13f, 0f, Mathf.Clamp01((time - 0.18f) / 0.37f));
                t.rotation = baseRot * Quaternion.Euler(pitch, 0f, 0f);
                yield return null;
            }
            if (rex != null) t.rotation = baseRot;
        }

        IEnumerator WinRoutine()
        {
            if (director != null)
            {
                director.Glitch(0.7f, true);
                director.Shake(0.4f, 0.9f);
            }
            if (rex != null) rex.PlayAttackSound();
            if (objective != null && objective.ambience != null)
                objective.ambience.PlaySting();

            yield return new WaitForSeconds(1.0f);

            if (rex == null)
            {
                Won = true;
                fightRunning = false;
                phase = Phase.Won;
                yield break;
            }

            if (director != null)
                director.Broadcast(
                    "\u25C9 IT BACKS INTO THE JUNGLE \u2014 THE ROAD IS CLEAR", 4.2f);

            // done hunting for good, then it strolls off the set
            rex.detectRange = 0.01f;
            rex.loseRange = 0.01f;
            rex.beamRange = 0.01f;
            rex.Retreat(RetreatPos, 235f);
            phase = Phase.Won;

            yield return new WaitForSeconds(2.5f);

            Won = true;
            fightRunning = false;
        }

        // ------------------------------------------------------------ death

        void OnDeath()
        {
            StopAllCoroutines();
            CutsceneRunning = false;
            if (flash != null) { Destroy(flash); flash = null; }
            EndCutsceneCam();

            fightRunning = false;
            phase = Phase.Idle;
            introShown = true;      // they've seen it — straight back in on retry
            staggering = false;
            skip = false;
            caption = "";
            captionA = 0f;
            // the catch sequence resets the rex (ResetToHome) itself
        }

        // ----------------------------------------------------------- helpers

        /// <summary>Park it on the road between the player and the exit.</summary>
        Vector3 PlaceRex()
        {
            Vector3 b = PickBlockPos();
            if (rex == null) return b;

            rex.Freeze();
            rex.transform.position = b;
            Vector3 to = (player != null ? player.position : b + Vector3.forward) - b;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                rex.transform.rotation = Quaternion.LookRotation(to);
            return b;
        }

        Vector3 PickBlockPos()
        {
            Vector3 p = player != null ? player.position : Vector3.zero;
            p.y = 0f;

            Vector3 gate = objective != null ? objective.gatePoint : new Vector3(0f, 1.5f, 21f);
            gate.y = 0f;
            Vector3 toGate = gate - p;
            if (toGate.sqrMagnitude < 0.04f) toGate = Vector3.forward;
            Vector3 dir = toGate.normalized;
            Vector3 side = Vector3.Cross(Vector3.up, dir);

            Vector3[] tries =
            {
                ClampArena(p + dir * 7.5f),
                ClampArena(p + side * 7.5f),
                ClampArena(p - side * 7.5f),
                ClampArena(p - dir * 7.5f)
            };
            for (int i = 0; i < tries.Length; i++)
            {
                float d = Vector3.Distance(tries[i], p);
                if (d >= 5.5f && d <= 10.5f) return tries[i];
            }
            return tries[1];
        }

        /// <summary>Keep the showdown on the plaza / approach road.</summary>
        static Vector3 ClampArena(Vector3 q)
        {
            q.y = 0f;
            q.x = Mathf.Clamp(q.x, -9f, 9f);
            q.z = Mathf.Clamp(q.z, 10f, 20.5f);
            return q;
        }

        void OnDestroy()
        {
            if (CutsceneRunning) CutsceneRunning = false;
        }

        // -------------------------------------------------------------- GUI

        void OnGUI()
        {
            if (!Active) return;
            if (MainMenu.Instance != null && !MainMenu.Instance.Started) return;
            if (!stylesReady) EnsureStyles();

            float w = Screen.width;
            float h = Screen.height;

            // ---- the intro film: letterbox + caption + skip hint ----
            if (CutsceneRunning)
            {
                float bar = h * 0.11f;
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0f, 0f, w, bar), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, h - bar, w, bar), Texture2D.whiteTexture);

                if (captionA > 0f && caption.Length > 0)
                {
                    GUI.color = new Color(1f, 1f, 1f, captionA * 0.82f);
                    GUI.Label(new Rect(40f, h - bar + 3f, w - 80f, bar - 6f),
                        caption, capStyle);
                }

                if (!skip)
                {
                    float blink = (Time.unscaledTime % 1f) < 0.6f ? 0.34f : 0.12f;
                    GUI.color = new Color(1f, 1f, 1f, blink);
                    GUI.Label(new Rect(w - 170f, h - bar + 3f, 130f, bar - 6f),
                        "R  SKIP", capStyle);
                }
                GUI.color = Color.white;
                return;
            }

            if (phase != Phase.Fight || Won) return;

            // ---- fight HUD: stagger bar over the crosshair + flare status ----
            float cx = w * 0.5f;
            float by = h * 0.5f - 46f;

            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(0f, by - 17f, w, 14f),
                "IT IS BLOCKING THE ROAD \u2014 DRIVE IT BACK", labelStyle);

            const float segW = 30f;
            const float gap = 7f;
            float total = Need * segW + (Need - 1) * gap;
            float sx = cx - total * 0.5f;
            for (int i = 0; i < Need; i++)
            {
                GUI.color = i < hits
                    ? new Color(1f, 0.34f, 0.26f, 0.85f)
                    : new Color(1f, 1f, 1f, 0.14f);
                GUI.DrawTexture(new Rect(sx + i * (segW + gap), by, segW, 4f),
                    Texture2D.whiteTexture);
            }
            GUI.color = Color.white;

            bool ready = Time.time >= nextShot && !staggering;
            GUI.color = new Color(1f, 1f, 1f, ready ? 0.55f : 0.28f);
            GUI.Label(new Rect(w - 230f, h - 64f, 208f, 16f),
                ready ? "FLARE GUN   LMB" : "FLARE GUN   RECHARGING", tinyStyle);
            GUI.color = Color.white;
        }

        void EnsureStyles()
        {
            Font mono = HudFonts.Mono;
            Font ui = HudFonts.Ui;

            capStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleRight
            };
            if (mono != null) capStyle.font = mono;
            capStyle.normal.textColor = Color.white;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };
            if (ui != null) labelStyle.font = ui;
            labelStyle.normal.textColor = Color.white;

            tinyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleRight
            };
            if (mono != null) tinyStyle.font = mono;
            tinyStyle.normal.textColor = Color.white;

            stylesReady = true;
        }
    }
}
