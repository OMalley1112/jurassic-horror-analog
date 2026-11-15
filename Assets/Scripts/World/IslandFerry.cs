using System.Collections;
using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// The cross-island tour ferry. Stand at the rail on either dock, press E,
    /// the tape jumps (VHS glitch + fade) and you're standing on the other
    /// island. Both docks each carry one of these — the builder wires the
    /// pair so each points at the other.
    /// </summary>
    public class IslandFerry : MonoBehaviour
    {
        [Header("Wired by builder")]
        public Transform player;
        public FirstPersonController controller;
        public HorrorDirector director;
        public VhsOverlay overlay;

        [Header("Crossing")]
        public Vector3 targetPos;          // where the ride drops you
        public float targetYaw;            // which way you face on arrival
        public string destination = "JURASSIC WORLD";

        [Header("Tuning")]
        public float range = 3.6f;
        public float rideSeconds = 1.15f;

        public bool Riding { get; private set; }

        float fade;                        // 0 = clear picture, 1 = black
        bool announced;
        GUIStyle promptStyle;

        /// <summary>The tape must be rolling, quiet, and nobody mid-cutscene.</summary>
        static bool Blocked()
        {
            if (MainMenu.Instance != null && !MainMenu.Instance.Started) return true;
            if (PauseSystem.Instance != null && PauseSystem.Instance.IsPaused) return true;
            if (HorrorDirector.Instance != null && HorrorDirector.Instance.IsCaught) return true;
            if (BossFight.CutsceneRunning) return true;
            if (ObjectiveSystem.Instance != null && ObjectiveSystem.Instance.Escaped) return true;
            return false;
        }

        bool InRange => player != null &&
            (player.position - transform.position).sqrMagnitude <= range * range;

        void Update()
        {
            if (Riding || Blocked()) return;
            if (controller != null && controller.IsFrozen) return;

            if (!InRange)
            {
                announced = false;
                return;
            }

            if (!announced)
            {
                announced = true;
                if (director != null)
                    director.Broadcast("FERRY TO " + destination + " — PRESS E", 2.8f);
            }

            if (Input.GetKeyDown(KeyCode.E))
                StartCoroutine(Ride());
        }

        IEnumerator Ride()
        {
            Riding = true;
            if (director != null) director.Broadcast("FERRY CASTING OFF", 2.2f);
            if (controller != null) controller.SetFrozen(true);

            // fade to black — the crossing happens in the dark
            float half = Mathf.Max(0.2f, rideSeconds * 0.5f);
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                fade = Mathf.Clamp01(t / half);
                if (director != null && director.IsCaught) break;   // eaten mid-ride
                yield return null;
            }
            fade = 1f;

            // jump the tape to the other dock (same trick the rewind uses)
            if (player != null && (director == null || !director.IsCaught))
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.position = targetPos;
                player.rotation = Quaternion.Euler(0f, targetYaw, 0f);
                if (cc != null) cc.enabled = true;
            }

            if (overlay != null) overlay.TriggerGlitch(1f);
            if (director != null) director.Broadcast("DOCKING — " + destination, 3.0f);
            yield return new WaitForSeconds(0.45f);

            // fade back in
            t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime / Mathf.Max(0.2f, rideSeconds * 0.6f);
                fade = Mathf.Clamp01(t);
                yield return null;
            }
            fade = 0f;

            // if a raptor got us mid-crossing the catch routine owns the thaw
            if (director == null || !director.IsCaught)
                if (controller != null) controller.SetFrozen(false);

            Riding = false;
            announced = false;
        }

        void OnGUI()
        {
            if (fade > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, fade);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height),
                    Texture2D.blackTexture);
                GUI.color = Color.white;
            }

            if (Riding || fade > 0f || Blocked()) return;
            if (controller != null && controller.IsFrozen) return;
            if (!InRange) return;

            if (promptStyle == null)
            {
                promptStyle = new GUIStyle(GUI.skin.label);
                promptStyle.alignment = TextAnchor.MiddleCenter;
                promptStyle.fontSize = 11;              // HUD rule: small + muted
            }

            GUI.color = new Color(0.82f, 0.86f, 0.82f, 0.75f);
            GUI.Label(new Rect(0f, Screen.height * 0.62f, Screen.width, 20f),
                "E    FERRY  \u2014  " + destination, promptStyle);
            GUI.color = Color.white;
        }
    }
}
