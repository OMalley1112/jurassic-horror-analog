using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Drives the procedurally built player model: walk/run arm-and-leg swing,
    /// crouch folding (hips drop, thighs forward, knees bend, torso leans),
    /// airborne tuck, sprint lean and idle breathing. Called by
    /// FirstPersonController every frame.
    /// </summary>
    public class PlayerBodyAnimator : MonoBehaviour
    {
        [Header("Rig (wired by builder)")]
        public Transform hips;
        public Transform torso;
        public Transform leftLegPivot;
        public Transform rightLegPivot;
        public Transform leftKneePivot;
        public Transform rightKneePivot;
        public Transform leftArmPivot;
        public Transform rightArmPivot;
        public Transform leftElbowPivot;
        public Transform rightElbowPivot;

        [Header("Stance heights")]
        public float crouchHipsY = 0.55f;
        public float crouchTorsoDrop = 0.34f;   // how far the chest sinks when crouched

        Vector3 lastPos;
        Vector3 torsoBasePos;
        float hipsBaseY;
        float smoothSpeed;
        float phase;
        float crouchT;
        float airT;

        void Start()
        {
            lastPos = transform.position;
            if (torso != null) torsoBasePos = torso.localPosition;
            if (hips != null) hipsBaseY = hips.localPosition.y;
        }

        /// <summary>Called by FirstPersonController each frame.</summary>
        /// <param name="normalizedSpeed">0..1 of run speed.</param>
        /// <param name="grounded">False while jumping/falling.</param>
        /// <param name="crouch01">0 standing, 1 crouched (already smoothed).</param>
        /// <param name="sprinting">Holding a full-speed run right now.</param>
        public void SetLocomotion(float normalizedSpeed, bool grounded,
            float crouch01 = 0f, bool sprinting = false)
        {
            float dt = Time.deltaTime;

            Vector3 pos = transform.position;
            Vector3 delta = pos - lastPos;
            delta.y = 0f;
            lastPos = pos;

            float instant = delta.magnitude / Mathf.Max(dt, 1e-5f);
            smoothSpeed = Mathf.Lerp(smoothSpeed, instant, 10f * dt);

            crouchT = Mathf.Lerp(crouchT, Mathf.Clamp01(crouch01), 11f * dt);
            airT = Mathf.Lerp(airT, grounded ? 0f : 1f, 9f * dt);

            float amp = Mathf.Clamp01(smoothSpeed / 5.4f);
            float sprintT = sprinting ? amp : 0f;
            bool moving = amp > 0.05f && grounded;

            if (moving)
                phase += smoothSpeed * dt * Mathf.Lerp(2.6f, 3.5f, sprintT);

            float s = moving ? Mathf.Sin(phase) : 0f;

            // stride: sprinting stretches it, crouching and air shorten it
            float stride = Mathf.Lerp(42f, 58f, sprintT)
                         * (1f - 0.55f * crouchT) * (1f - 0.65f * airT);
            float legSwing = s * stride * amp;

            // ---- legs: thigh swing + knee fold ----------------------------
            // positive X = backward for a down-pointing limb
            float crouchThigh = -70f * crouchT;          // thighs forward when folded
            float airThigh = -24f * airT;                // knees tuck up in flight

            SetAngle(leftLegPivot, legSwing + crouchThigh + airThigh);
            SetAngle(rightLegPivot, -legSwing + crouchThigh + airThigh);

            // knee bends through the swing-through, is fully folded when
            // crouched, and pulls up while airborne
            float kneeL = Mathf.Max(0f, -s) * 52f * amp * (1f - crouchT)
                        + Mathf.Max(0f, -s) * 16f * amp * crouchT
                        + 76f * crouchT + 42f * airT;
            float kneeR = Mathf.Max(0f, s) * 52f * amp * (1f - crouchT)
                        + Mathf.Max(0f, s) * 16f * amp * crouchT
                        + 76f * crouchT + 42f * airT;
            SetAngle(leftKneePivot, kneeL);
            SetAngle(rightKneePivot, kneeR);

            // ---- arms: opposite the legs, pump harder when sprinting ------
            float armFactor = Mathf.Lerp(0.7f, 1.05f, sprintT);
            float armSwing = -legSwing * armFactor - 6f - 8f * crouchT;

            SetAngle(leftArmPivot, armSwing);
            SetAngle(rightArmPivot, -armSwing);

            // elbows: permanently a little bent (carrying the light), more so
            // while running, with a small phase kick
            float elbowBase = 16f + 30f * amp + 16f * sprintT + 10f * crouchT;
            SetAngle(leftElbowPivot, -(elbowBase + Mathf.Max(0f, s) * 12f * amp));
            SetAngle(rightElbowPivot, -(elbowBase + Mathf.Max(0f, -s) * 12f * amp));

            // ---- hips: stance height + stride bob --------------------------
            if (hips != null)
            {
                float baseY = Mathf.Lerp(hipsBaseY, crouchHipsY, crouchT);
                float bob = moving ? Mathf.Abs(Mathf.Sin(phase)) * 0.035f * amp
                                        * (1f - 0.7f * crouchT) : 0f;
                Vector3 hp = hips.localPosition;
                hp.y = Mathf.Lerp(hp.y, baseY + bob, 12f * dt);
                hp.z = Mathf.Lerp(hp.z, -0.07f * crouchT, 10f * dt);   // butt back
                hips.localPosition = hp;
            }

            // ---- torso: lean into the run, fold over when crouching -------
            if (torso != null)
            {
                float breathe = Mathf.Sin(Time.time * 1.7f) * 1.1f;
                float lean = 15f * sprintT + 5f * amp * (1f - crouchT)
                           + 26f * crouchT + 6f * airT;
                torso.localRotation = Quaternion.Euler(lean + breathe, 0f, 0f);

                Vector3 tp = torsoBasePos;
                tp.y = Mathf.Lerp(tp.y,
                    torsoBasePos.y - crouchTorsoDrop * crouchT, 12f * dt);
                tp.y += Mathf.Sin(Time.time * 1.7f) * 0.006f;
                tp.x = Mathf.Cos(phase) * 0.02f * amp;                  // sway
                tp.z = Mathf.Lerp(tp.z, -0.05f * crouchT, 10f * dt);
                torso.localPosition = tp;
            }
        }

        static void SetAngle(Transform t, float x)
        {
            if (t != null)
                t.localRotation = Quaternion.Euler(x, 0f, 0f);
        }
    }
}
