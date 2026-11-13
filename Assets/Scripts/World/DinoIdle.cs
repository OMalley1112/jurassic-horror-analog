using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Subtle idle life for the primitive-built dinosaurs: breathing sway,
    /// slow head scanning and occasional distant roars (via AmbientAudio).
    /// </summary>
    public class DinoIdle : MonoBehaviour
    {
        [Header("Optional head pivot (rotates independently)")]
        public Transform head;

        [Header("Optional wing pivots (pteranodon twitch)")]
        public Transform wingL;
        public Transform wingR;

        [Header("Idle motion")]
        public float breatheAmount = 0.35f;   // body roll in degrees
        public float breatheSpeed = 0.7f;
        public float headAmount = 3f;          // head yaw sweep in degrees
        public float headPitchAmount = 1.4f;
        public float headSpeed = 0.3f;

        [Header("Roaring")]
        public bool canRoar = true;
        public float roarDelayMin = 22f;
        public float roarDelayMax = 65f;

        Quaternion baseRot;
        Quaternion baseHeadRot;
        Quaternion baseWingL;
        Quaternion baseWingR;
        float seed;
        float nextRoar;

        void Start()
        {
            Rebase();
            seed = Random.Range(0f, 100f);
            nextRoar = Time.time + Random.Range(roarDelayMin, roarDelayMax);
        }

        /// <summary>
        /// Re-capture the rest pose (called when a DinoHunter hands the body
        /// back, so the idle sway doesn't snap).
        /// </summary>
        public void Rebase()
        {
            baseRot = transform.localRotation;
            if (head != null) baseHeadRot = head.localRotation;
            if (wingL != null) baseWingL = wingL.localRotation;
            if (wingR != null) baseWingR = wingR.localRotation;
        }

        void Update()
        {
            // breathing sway — barely perceptible, but keeps it from feeling like a statue
            float b = Mathf.Sin(Time.time * breatheSpeed + seed) * breatheAmount;
            transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, b);

            // slow head scan, as if watching the area
            if (head != null)
            {
                float yaw = Mathf.Sin(Time.time * headSpeed + seed * 1.7f) * headAmount;
                float pitch = Mathf.Sin(Time.time * headSpeed * 0.63f + seed) * headPitchAmount;
                head.localRotation = baseHeadRot * Quaternion.Euler(pitch, yaw, 0f);
            }

            // nervous wing-fold twitch — it is about to take off
            if (wingL != null || wingR != null)
            {
                float tw = Mathf.Sin(Time.time * 0.83f + seed) * 1.7f
                         + Mathf.Sin(Time.time * 3.1f + seed * 2.3f) * 0.6f;
                if (wingL != null)
                    wingL.localRotation = baseWingL * Quaternion.Euler(0f, 0f, tw);
                if (wingR != null)
                    wingR.localRotation = baseWingR * Quaternion.Euler(0f, 0f, -tw);
            }

            if (canRoar && Time.time > nextRoar)
            {
                var amb = AmbientAudio.Instance;
                if (amb != null) amb.PlayRoar();
                nextRoar = Time.time + Random.Range(roarDelayMin, roarDelayMax);
            }
        }
    }
}
