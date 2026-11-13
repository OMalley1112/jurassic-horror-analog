using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Pendulum sway for hanging or hinged props: signs on chains, roof
    /// vines, the paddock gate on its hinges. Two overlaid sine waves keep
    /// the motion from feeling mechanical — it should read as "wind you
    /// can't feel" until you notice it.
    /// </summary>
    public class Swing : MonoBehaviour
    {
        [Header("Motion — degrees around Axis")]
        public float amplitude = 3.5f;
        public float speed = 0.55f;              // main wave, cycles/sec
        public Vector3 axis = new Vector3(1f, 0f, 0f);
        public float phase;                      // builder assigns variety

        [Header("Secondary wobble (fraction of amplitude)")]
        public float wobbleAmount = 0.35f;
        public float wobbleSpeed = 1.7f;

        Quaternion baseRot;

        void Start()
        {
            baseRot = transform.localRotation;
            if (phase == 0f) phase = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            Vector3 ax = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.right;

            float a = Mathf.Sin(Time.time * speed * Mathf.PI * 2f + phase) * amplitude
                    + Mathf.Sin(Time.time * wobbleSpeed * Mathf.PI * 2f + phase * 2.3f)
                      * amplitude * wobbleAmount;

            transform.localRotation = baseRot * Quaternion.AngleAxis(a, ax);
        }
    }
}
