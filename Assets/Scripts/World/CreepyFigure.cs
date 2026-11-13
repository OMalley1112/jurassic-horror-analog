using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// A motionless dark figure that vanishes with a static burst when you
    /// finally get close enough to make out what it is.
    ///
    /// Awareness twist: while you are NOT looking at it, it slowly turns to
    /// face you and leans in. The instant your view crosses it, it freezes
    /// mid-lean — caught in the act — until you look away again.
    /// </summary>
    public class CreepyFigure : MonoBehaviour
    {
        public VhsOverlay overlay;
        public AmbientAudio ambience;
        public float vanishDistance = 7.5f;
        public float requiredDot = 0.2f;

        [Header("Awareness — only moves when unobserved")]
        public float turnSpeed = 75f;           // deg/sec re-aiming at you
        public float leanAngle = 7f;            // deg of curiosity-lean
        public float leanSpeed = 4f;            // deg/sec settling

        bool gone;
        bool hasPose;
        float yaw;
        float lean;

        void Update()
        {
            if (gone) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 toFigure = transform.position - cam.transform.position;
            float distance = toFigure.magnitude;

            // are you looking at it right now? (same wide cone as the vanish)
            float dot = Vector3.Dot(cam.transform.forward, toFigure.normalized);
            bool observed = dot >= requiredDot;

            if (!observed)
            {
                // — you looked away: it turns toward you and leans in —
                if (!hasPose)
                {
                    yaw = transform.eulerAngles.y;
                    hasPose = true;
                }

                Vector3 flat = -toFigure;                 // figure → camera
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.01f)
                {
                    float targetYaw = Quaternion.LookRotation(flat.normalized,
                        Vector3.up).eulerAngles.y;
                    yaw = Mathf.MoveTowardsAngle(yaw, targetYaw,
                        turnSpeed * Time.deltaTime);
                }

                lean = Mathf.MoveTowards(lean, leanAngle, leanSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(lean, yaw, 0f);
            }
            // — observed: no writes at all — frozen exactly where you caught it

            if (distance > vanishDistance || !observed) return;

            Vanish();
        }

        void Vanish()
        {
            gone = true;

            if (overlay == null) overlay = FindObjectOfType<VhsOverlay>();
            if (ambience == null) ambience = FindObjectOfType<AmbientAudio>();

            if (overlay != null)
                overlay.TriggerGlitch(1f);

            if (ambience != null)
                ambience.PlaySting();

            Destroy(gameObject);
        }
    }
}
