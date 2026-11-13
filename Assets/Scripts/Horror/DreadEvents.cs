using System.Collections;
using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Random unease: whispers from behind you, heavy footsteps that approach
    /// and stop just short, sudden flashlight blackouts, a shadow that crosses
    /// the road ahead, and a dead radio that still has something to say.
    /// Stand still for too long and the tape starts hunting YOU.
    /// </summary>
    public class DreadEvents : MonoBehaviour
    {
        [Header("Wired by builder")]
        public HorrorDirector director;

        [Header("Timing")]
        public float firstEventMin = 25f;
        public float firstEventMax = 70f;
        public float intervalMin = 40f;
        public float intervalMax = 105f;
        [Range(0f, 1f)] public float tensionGate = 0.18f; // events only past this dread

        [Header("Blackout")]
        public float blackoutMin = 2.2f;
        public float blackoutMax = 4.5f;

        [Header("Idleness")]
        public float idleLimit = 14f;        // stand still this long and it notices
        public float idleMoveRadius = 1.2f;  // metres of drift that counts as moving

        static readonly string[] RadioLines =
        {
            "RADIO \u258C \u2026unit seven, do you copy\u2026",
            "RADIO \u258C \u2026the fence is down, I repeat, the fence is down\u2026",
            "RADIO \u258C \u2026don't follow the lights on the ridge\u2026",
            "RADIO \u258C \u2026it's not the storm \u2014 it's walking\u2026",
            "RADIO \u258C \u2026all teams to the gate, abandon the\u2014\u2026",
            "RADIO \u258C \u2026something's in the aviary, something's in the\u2026"
        };

        AudioSource src;
        float nextEvent;
        float idleT;
        Vector3 lastPos;
        bool posSeeded;
        static Material shadowMat;

        void Start()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            nextEvent = Time.time + Random.Range(firstEventMin, firstEventMax);
        }

        void Update()
        {
            // title screen: hold the first event back, don't count idleness
            if (MainMenu.Instance != null && !MainMenu.Instance.Started)
            {
                nextEvent = Time.time + 20f;
                idleT = 0f;
                posSeeded = false;
                return;
            }

            if (director == null || director.IsCaught) return;

            // the tape is on its way out — no more dread over the ending film
            var objective = ObjectiveSystem.Instance;
            if (objective != null && objective.Escaped) return;

            UpdateIdle();

            if (Time.time < nextEvent) return;

            nextEvent = Time.time + Random.Range(intervalMin, intervalMax);

            // only when you're already uneasy — calm nights stay calm
            if (director.displayedTension < tensionGate) return;

            int pick = Random.Range(0, 100);
            if (pick < 24) StartCoroutine(WhisperEvent());
            else if (pick < 48) StartCoroutine(FootstepEvent());
            else if (pick < 62) StartCoroutine(BlackoutEvent());
            else if (pick < 73) StartCoroutine(DistantImpact());
            else if (pick < 88) StartCoroutine(CrossingFigure());
            else StartCoroutine(RadioEvent());
        }

        /// <summary>Stand still past the limit and the tape starts hunting you.</summary>
        void UpdateIdle()
        {
            if (director.player == null) return;

            if (!posSeeded)
            {
                posSeeded = true;
                lastPos = director.player.position;
                return;
            }

            if ((director.player.position - lastPos).sqrMagnitude >
                idleMoveRadius * idleMoveRadius)
            {
                lastPos = director.player.position;
                idleT = 0f;
                return;
            }

            idleT += Time.deltaTime;
            if (idleT < idleLimit) return;
            if (director.displayedTension < tensionGate)
            {
                idleT = idleLimit * 0.5f;
                return;
            }

            idleT = 0f;
            nextEvent = Mathf.Max(nextEvent,
                Time.time + Random.Range(intervalMin, intervalMax) * 0.6f);

            if (Random.value < 0.5f) StartCoroutine(WhisperEvent());
            else StartCoroutine(FootstepEvent());

            if (director.overlay != null)
                director.overlay.glitch = Mathf.Max(director.overlay.glitch, 0.4f);
            director.Broadcast("\u26A0 SUBJECT STATIONARY \u2014 MOVEMENT ADVISED", 2.6f);
        }

        // ------------------------------------------------------------ events

        /// <summary>A voice right behind your shoulder.</summary>
        IEnumerator WhisperEvent()
        {
            var cam = Camera.main;
            if (cam == null) yield break;

            var go = new GameObject("WhisperSource");
            go.transform.position = cam.transform.position
                - cam.transform.forward * 0.9f
                + cam.transform.right * Random.Range(-0.5f, 0.5f);

            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0.85f;
            s.minDistance = 0.6f;
            s.maxDistance = 7f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.pitch = Random.Range(0.9f, 1.12f);
            s.clip = ProceduralAudio.Whisper(Random.Range(1, 9999),
                Random.Range(2.2f, 3.2f));
            s.volume = 0.55f;
            s.Play();

            // the tape reacts to the voice
            if (director.overlay != null)
                director.overlay.glitch = Mathf.Max(director.overlay.glitch, 0.3f);

            yield return new WaitForSeconds(s.clip.length + 0.4f);
            Destroy(go);
        }

        /// <summary>Heavy steps that walk toward you, then just… stop.</summary>
        IEnumerator FootstepEvent()
        {
            var cam = Camera.main;
            if (cam == null) yield break;

            var go = new GameObject("PhantomSteps");
            go.transform.SetParent(transform, false);

            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 2f;
            s.maxDistance = 34f;

            float startDist = Random.Range(17f, 26f);
            float endDist = Random.Range(2.4f, 5.5f);
            int steps = Random.Range(5, 9);

            // bake this event's thuds once
            var thuds = new AudioClip[3];
            for (int i = 0; i < thuds.Length; i++)
                thuds[i] = ProceduralAudio.Stomp(700 + i * 17);

            Vector3 bearing = cam.transform.forward * Random.Range(-0.8f, 0.8f)
                            + cam.transform.right * Random.Range(-1f, 1f);
            if (bearing.sqrMagnitude < 0.05f) bearing = cam.transform.forward;
            bearing.Normalize();

            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)Mathf.Max(1, steps - 1);
                float dist = Mathf.Lerp(startDist, endDist, t);

                go.transform.position = cam.transform.position
                    + bearing * dist + Vector3.down * 1.5f;

                s.pitch = Random.Range(0.82f, 1.02f);
                s.volume = Mathf.Lerp(0.35f, 0.85f, t);
                s.PlayOneShot(thuds[i % thuds.Length]);

                // the ground answers back
                if (t > 0.7f) director.Shake(0.02f * t, 0.3f);

                yield return new WaitForSeconds(Random.Range(0.44f, 0.62f));
            }

            // and then silence — which is worse
            yield return new WaitForSeconds(Random.Range(1.4f, 3f));
            Destroy(go);
        }

        /// <summary>Your light dies and refuses to come back for a few seconds.</summary>
        IEnumerator BlackoutEvent()
        {
            var fl = director.flashlight;
            if (fl == null) yield break;

            float dur = Random.Range(blackoutMin, blackoutMax);

            director.Glitch(0.55f, false);
            director.Broadcast("\u26A0 VOLTAGE DROP", 2f);
            fl.ForceOff();

            yield return new WaitForSeconds(dur * 0.45f);

            // stuttering attempts to relight
            for (int i = 0; i < 3; i++)
            {
                fl.TryFlicker();
                yield return new WaitForSeconds(Random.Range(0.12f, 0.3f));
            }

            yield return new WaitForSeconds(dur * 0.55f);

            // never relight the lamp over the ending film — the exit stays dark
            var objective = ObjectiveSystem.Instance;
            if (objective != null && objective.Escaped) yield break;

            fl.TryFlicker();
            fl.RestorePower();
        }

        /// <summary>Something enormous impacts a fence far away.</summary>
        IEnumerator DistantImpact()
        {
            var go = new GameObject("DistantImpact");
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0.7f;
            s.pitch = Random.Range(0.8f, 1.05f);
            s.clip = ProceduralAudio.Stomp(Random.Range(1, 9999));
            s.Play();

            director.Shake(0.06f, 0.7f);
            if (director.overlay != null)
                director.overlay.TriggerGlitch(0.45f);

            if (Random.value < 0.5f && director.ambience != null)
            {
                yield return new WaitForSeconds(Random.Range(0.7f, 1.6f));
                director.ambience.PlayRoar();
            }

            yield return new WaitForSeconds(2f);
            Destroy(go);
        }

        /// <summary>A shadow crosses the road ahead. It was not there a second ago.</summary>
        IEnumerator CrossingFigure()
        {
            var cam = Camera.main;
            if (cam == null) yield break;

            float dist = Random.Range(22f, 36f);
            Vector3 flat = cam.transform.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) flat = Vector3.forward;
            flat.Normalize();

            Vector3 right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.01f) right = Vector3.Cross(Vector3.up, flat);

            Vector3 centre = cam.transform.position + flat * dist;
            centre.y = cam.transform.position.y - 1.62f;   // eye height → ground

            var fig = new GameObject("CrossingShadow");
            Part(fig.transform, new Vector3(-0.12f, 0.45f, 0f), new Vector3(0.16f, 0.9f, 0.16f));
            Part(fig.transform, new Vector3(0.12f, 0.45f, 0f), new Vector3(0.16f, 0.9f, 0.16f));
            Part(fig.transform, new Vector3(0f, 1.3f, 0f), new Vector3(0.5f, 1f, 0.3f));
            Part(fig.transform, new Vector3(-0.3f, 1.3f, 0f), new Vector3(0.12f, 0.9f, 0.12f));
            Part(fig.transform, new Vector3(0.3f, 1.3f, 0f), new Vector3(0.12f, 0.9f, 0.12f));
            Part(fig.transform, new Vector3(0f, 1.98f, 0f), new Vector3(0.26f, 0.28f, 0.26f));

            float span = Random.Range(14f, 20f);
            float dur = Random.Range(0.55f, 0.8f);
            Vector3 from = centre - right * (span * 0.5f);
            Vector3 to = centre + right * (span * 0.5f);

            fig.transform.rotation = Quaternion.LookRotation(to - from);
            fig.transform.position = from;

            var s = fig.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0.7f;
            s.minDistance = 4f;
            s.maxDistance = 40f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.PlayOneShot(ProceduralAudio.Stomp(Random.Range(1, 9999)), 0.3f);

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                fig.transform.position = Vector3.Lerp(from, to, k)
                    + Vector3.up * (Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f)) * 0.14f);
                yield return null;
            }

            if (director.overlay != null)
                director.overlay.TriggerGlitch(0.4f);
            Destroy(fig);
        }

        /// <summary>A dead radio somewhere in the dark still has something to say.</summary>
        IEnumerator RadioEvent()
        {
            var cam = Camera.main;
            if (cam == null) yield break;

            float ang = Random.Range(0f, 360f);
            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
            float d = Random.Range(7f, 15f);

            var go = new GameObject("DeadRadio");
            Vector3 p = cam.transform.position + dir * d;
            p.y = cam.transform.position.y - 1.62f;
            go.transform.position = p;

            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 2f;
            s.maxDistance = 30f;
            s.dopplerLevel = 0f;

            director.Broadcast(RadioLines[Random.Range(0, RadioLines.Length)], 4.2f);

            int blips = Random.Range(7, 11);
            for (int i = 0; i < blips; i++)
            {
                s.pitch = Random.Range(0.72f, 1.35f);
                s.volume = Random.Range(0.3f, 0.6f);
                s.PlayOneShot(ProceduralAudio.NoiseBurst(
                    Random.Range(0.07f, 0.18f), 48f, Random.Range(1, 9999), 0.38f));
                yield return new WaitForSeconds(Random.Range(0.13f, 0.27f));
            }

            yield return new WaitForSeconds(0.6f);
            Destroy(go);
        }

        // ------------------------------------------------------------ helpers

        /// <summary>One dark, collider-less body part for the crossing shadow.</summary>
        static void Part(Transform parent, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "ShadowPart";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;

            var r = go.GetComponent<MeshRenderer>();
            if (r != null) r.sharedMaterial = Shadow();

            var c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }

        static Material Shadow()
        {
            if (shadowMat != null) return shadowMat;
            shadowMat = Resources.Load<Material>("Materials/ShadowBlack");
            if (shadowMat == null)
            {
                shadowMat = new Material(Shader.Find("Standard"));
                shadowMat.color = new Color(0.015f, 0.015f, 0.02f);
            }
            return shadowMat;
        }
    }
}
