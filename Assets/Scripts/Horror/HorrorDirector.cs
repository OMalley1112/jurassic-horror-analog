using System.Collections;
using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Central nervous system of the horror layer: owns the "tape rewind"
    /// catch/respawn sequence, camera shake, emergency broadcasts and the
    /// global tension value that the other horror components publish to.
    /// </summary>
    public class HorrorDirector : MonoBehaviour
    {
        public static HorrorDirector Instance { get; private set; }

        [Header("Wired by builder")]
        public Transform player;
        public Vector3 spawnPoint = new Vector3(0f, 0.15f, 6f);
        public VhsOverlay overlay;
        public FirstPersonController controller;
        public Flashlight flashlight;
        public AmbientAudio ambience;

        [Header("Tension (read-only at runtime)")]
        [Range(0f, 1f)] public float tension;
        [Range(0f, 1f)] public float displayedTension;

        [Header("Catch settings")]
        public float rewindStatic = 1.15f;   // seconds of full static before respawn
        public float respawnDelay = 1.35f;
        public float catchCooldown = 9f;

        public bool IsCaught { get; private set; }

        /// <summary>Times the tape has been eaten — shown on the end card.</summary>
        public int CatchCount { get; private set; }

        readonly System.Collections.Generic.List<DinoHunter> hunters =
            new System.Collections.Generic.List<DinoHunter>();

        float lastCatchTime = -999f;
        AudioSource rewindSrc;
        AudioClip[] staticClips;

        void Awake()
        {
            Instance = this;
            rewindSrc = gameObject.AddComponent<AudioSource>();
            rewindSrc.playOnAwake = false;
            rewindSrc.spatialBlend = 0f;

            staticClips = new AudioClip[3];
            for (int i = 0; i < staticClips.Length; i++)
                staticClips[i] = ProceduralAudio.NoiseBurst(0.5f, 6f, 900 + i * 31, 0.9f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            // smooth the displayed tension so HUD / heartbeat never snaps
            displayedTension = Mathf.Lerp(displayedTension, tension,
                2.5f * Time.deltaTime);
        }

        public void Register(DinoHunter hunter)
        {
            if (hunter != null && !hunters.Contains(hunter)) hunters.Add(hunter);
        }

        // ------------------------------------------------------------- events

        /// <summary>Camera kick (lightning, roar, catch, footsteps overhead).</summary>
        public void Shake(float amplitude, float duration)
        {
            if (controller != null) controller.Shake(amplitude, duration);
        }

        /// <summary>Centered emergency-broadcast style tape message.</summary>
        public void Broadcast(string text, float duration = 3.4f)
        {
            if (overlay != null) overlay.Broadcast(text, duration);
        }

        /// <summary>Hard static burst + optional shake.</summary>
        public void Glitch(float amount, bool noisy = true)
        {
            if (overlay != null) overlay.TriggerGlitch(amount);
            if (noisy && amount > 0.6f && rewindSrc != null && staticClips != null)
            {
                rewindSrc.pitch = Random.Range(0.9f, 1.25f);
                rewindSrc.PlayOneShot(
                    staticClips[Random.Range(0, staticClips.Length)], 0.45f);
            }
        }

        // -------------------------------------------------------- catch / rewind

        /// <summary>A dinosaur reached the player: eat the tape, rewind, respawn.</summary>
        public void CatchPlayer(DinoHunter hunter)
        {
            if (IsCaught) return;
            if (Time.time - lastCatchTime < catchCooldown) return;
            lastCatchTime = Time.time;
            StartCoroutine(CatchRoutine(hunter));
        }

        IEnumerator CatchRoutine(DinoHunter hunter)
        {
            IsCaught = true;
            CatchCount++;

            // 1 — the last clean frame on the tape: it fills the screen
            if (controller != null)
            {
                controller.SetFrozen(true);
                if (hunter != null)
                    controller.SnapLookAt(hunter.transform.position +
                        Vector3.up * (hunter.eyeHeight * 0.75f));
            }
            if (ambience != null) ambience.PlaySting();
            if (hunter != null) hunter.PlayAttackSound();
            Shake(0.32f, 0.6f);

            // 1b — THE JUMPSCARE: jaws at the lens, white flash, roar
            JumpScare.Play(controller,
                hunter != null ? hunter.kind : DinoHunter.Kind.Raptor, overlay);

            yield return new WaitForSeconds(0.55f);      // eye to eye with it

            // 2 — slam the tape: full static, violent shake, light dies
            if (overlay != null)
            {
                overlay.rewind = 1f;
                overlay.TriggerGlitch(1f);
            }
            Shake(0.55f, 1.1f);
            if (flashlight != null) flashlight.ForceOff();

            yield return new WaitForSeconds(respawnDelay);

            // 3 — teleport back to spawn, reset every hunter to its post
            if (controller != null)
            {
                var cc = controller.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                if (player != null)
                {
                    player.position = spawnPoint;
                    player.rotation = Quaternion.identity;
                }
                if (cc != null) cc.enabled = true;
                controller.SetFrozen(false);
                controller.ResetShake();
            }

            for (int i = 0; i < hunters.Count; i++)
                if (hunters[i] != null) hunters[i].ResetToHome();

            // 4 — tape "resumes" after a last burst
            Glitch(0.9f, true);
            Broadcast("◄◄  SIGNAL LOST — RESPOOLING TAPE  ►►", 3.2f);
            tension = 0.85f;

            yield return new WaitForSeconds(rewindStatic);
            if (overlay != null) overlay.rewind = 0f;

            IsCaught = false;
        }

        /// <summary>Called by tension system every frame.</summary>
        public void SetTension(float value)
        {
            tension = Mathf.Clamp01(value);
        }
    }
}
