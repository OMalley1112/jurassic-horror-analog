using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Procedural animation for the primitive-built dinosaurs: a real joint
    /// rig (hip → knee → ankle) drives a proper walk cycle, plus breathing
    /// torso, travelling tail wave, head scanning and an attack lunge.
    /// Takes over from DinoIdle when present.
    /// </summary>
    public class DinoAnimator : MonoBehaviour
    {
        [Header("Rig — wired by builder")]
        public Transform torso;
        public Transform[] hips;
        public Transform[] knees;
        public Transform[] ankles;
        public Transform[] tail;
        public Transform head;
        public Transform jaw;
        public Transform armL;
        public Transform armR;

        [Header("Source")]
        public DinoHunter hunter;

        [Header("Stride")]
        public float strideFrequency = 0.62f;   // cycles per metre travelled
        public float hipSwing = 24f;            // deg at full sprint
        public float kneeBend = 40f;            // deg at full sprint
        public float bodyBob = 0.07f;           // metres
        public float tailSway = 4f;             // deg per segment

        [Header("Idle")]
        public float breatheRoll = 0.55f;       // deg of body roll
        public float breatheSpeed = 0.7f;
        public float headScan = 4f;             // deg yaw sweep
        public float headScanSpeed = 0.3f;

        [Header("Attack")]
        public float jawOpen = 26f;
        public float lungePitch = 14f;

        Quaternion[] baseHip, baseKnee, baseAnkle, baseTail;
        Quaternion baseTorso, baseHead, baseJaw, baseArmL, baseArmR;
        Vector3 baseTorsoPos;

        float phase;
        float seed;
        float lunge;
        float speed;
        float nextJawFlick;

        public float CurrentSpeed => speed;

        void Start()
        {
            var idle = GetComponent<DinoIdle>();
            if (idle != null) idle.enabled = false;    // it would fight us

            if (hunter == null) hunter = GetComponent<DinoHunter>();

            seed = Random.Range(0f, 100f);
            nextJawFlick = Time.time + Random.Range(4f, 12f);
            Capture();
        }

        void Capture()
        {
            if (torso != null)
            {
                baseTorso = torso.localRotation;
                baseTorsoPos = torso.localPosition;
            }
            baseHip = CaptureArray(hips);
            baseKnee = CaptureArray(knees);
            baseAnkle = CaptureArray(ankles);
            baseTail = CaptureArray(tail);

            if (head != null) baseHead = head.localRotation;
            if (jaw != null) baseJaw = jaw.localRotation;
            if (armL != null) baseArmL = armL.localRotation;
            if (armR != null) baseArmR = armR.localRotation;
        }

        static Quaternion[] CaptureArray(Transform[] arr)
        {
            if (arr == null) return new Quaternion[0];
            var res = new Quaternion[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                res[i] = arr[i] != null ? arr[i].localRotation : Quaternion.identity;
            return res;
        }

        /// <summary>Snarl + head-down strike (called when it reaches you).</summary>
        public void Lunge()
        {
            lunge = 1f;
        }

        void Update()
        {
            speed = hunter != null ? hunter.CurrentSpeed : 0f;
            float runRef = hunter != null ? Mathf.Max(0.5f, hunter.runSpeed) : 4f;
            float norm = Mathf.Clamp01(speed / runRef);

            // stride advances with distance travelled, never fully stops
            phase += speed * strideFrequency * Mathf.PI * 2f * Time.deltaTime;
            if (lunge > 0f) lunge = Mathf.Max(0f, lunge - Time.deltaTime * 1.7f);

            AnimateLegs(norm);
            AnimateTorso(norm);
            AnimateTail(norm);
            AnimateHead(norm);
            AnimateArms(norm);
        }

        void AnimateLegs(float norm)
        {
            if (hips == null) return;

            for (int i = 0; i < hips.Length; i++)
            {
                if (hips[i] == null) continue;

                float p = phase + (i % 2 == 1 ? Mathf.PI : 0f);
                float s = Mathf.Sin(p);

                // small shuffle when standing, full sprint stride when running
                float amp = Mathf.Lerp(5f, hipSwing, norm);
                float hipA = s * amp;
                hips[i].localRotation = baseHip[i] * Quaternion.Euler(hipA, 0f, 0f);

                // knee folds when the leg is trailing
                float bend = Mathf.Max(0f, s) * Mathf.Lerp(6f, kneeBend, norm);

                if (knees != null && i < knees.Length && knees[i] != null)
                    knees[i].localRotation = baseKnee[i] * Quaternion.Euler(bend, 0f, 0f);

                // ankle counter-rotates so the foot stays flat on the ground
                if (ankles != null && i < ankles.Length && ankles[i] != null)
                    ankles[i].localRotation = baseAnkle[i] *
                        Quaternion.Euler(-(hipA + bend), 0f, 0f);
            }
        }

        void AnimateTorso(float norm)
        {
            if (torso == null) return;

            float bob = Mathf.Abs(Mathf.Sin(phase)) * bodyBob * (0.25f + norm);
            float breathe = Mathf.Sin(Time.time * breatheSpeed + seed) *
                            breatheRoll * (1f - norm * 0.7f);
            float roll = Mathf.Sin(phase) * 1.7f * (0.25f + norm);
            float lean = norm * 4.5f + lunge * lungePitch * 0.7f;

            torso.localPosition = baseTorsoPos + new Vector3(0f, bob, 0f);
            torso.localRotation = baseTorso *
                Quaternion.Euler(lean + breathe * 0.35f, 0f, roll + breathe);
        }

        void AnimateTail(float norm)
        {
            if (tail == null) return;

            // travelling wave: always drifting, much wider while running
            float wave = phase * 0.45f + Time.time * 0.6f;
            float amp = Mathf.Lerp(tailSway * 0.6f, tailSway * 2.4f, norm);

            for (int i = 0; i < tail.Length; i++)
            {
                if (tail[i] == null) continue;
                float a = Mathf.Sin(wave - i * 0.8f + seed) * amp *
                          (0.35f + 0.65f * (i + 1) / tail.Length);
                tail[i].localRotation = baseTail[i] * Quaternion.Euler(0f, a, 0f);
            }
        }

        void AnimateHead(float norm)
        {
            if (head == null) return;

            float scan = Mathf.Sin(Time.time * headScanSpeed + seed) *
                         headScan * (1f - norm * 0.55f);
            float pitch = Mathf.Sin(Time.time * headScanSpeed * 0.71f + seed * 1.7f) *
                          headScan * 0.45f;
            float bobHead = Mathf.Abs(Mathf.Sin(phase)) * 2.2f * norm;
            float strike = lunge * lungePitch;

            head.localRotation = baseHead *
                Quaternion.Euler(pitch + bobHead + strike, scan, 0f);

            // jaw: occasional flick, full gape on the strike
            if (jaw != null)
            {
                float open = lunge * jawOpen;
                if (Time.time > nextJawFlick)
                {
                    nextJawFlick = Time.time + Random.Range(5f, 16f);
                    lunge = Mathf.Max(lunge, 0.35f);   // reuse as a quick jaw pop
                }
                jaw.localRotation = baseJaw * Quaternion.Euler(open, 0f, 0f);
            }
        }

        void AnimateArms(float norm)
        {
            float twitch = Mathf.Sin(Time.time * 1.35f + seed) * 3f + norm * 9f;
            if (armL != null)
                armL.localRotation = baseArmL * Quaternion.Euler(twitch, 0f, 0f);
            if (armR != null)
                armR.localRotation = baseArmR * Quaternion.Euler(twitch * 0.8f, 0f, 0f);
        }
    }
}
