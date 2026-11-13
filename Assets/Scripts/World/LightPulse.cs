using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Slow breathing drift of a light's intensity — moonlight through the
    /// roof hole, distant floods — plus a faint electrical flutter so it
    /// never sits perfectly still. Pairs with (but does not fight)
    /// FlickeringLight: use one or the other on a given lamp.
    /// </summary>
    public class LightPulse : MonoBehaviour
    {
        [Header("Breathing — fraction of base intensity")]
        public float amount = 0.35f;             // dip depth at the trough
        public float speed = 0.11f;              // cycles/sec

        [Header("Electrical flutter")]
        public float flutterAmount = 0.05f;
        public float flutterSpeed = 5.3f;

        Light lamp;
        float baseIntensity;
        float seed;

        void Start()
        {
            lamp = GetComponent<Light>();
            if (lamp != null) baseIntensity = lamp.intensity;
            seed = Random.Range(0f, 100f);
        }

        void Update()
        {
            if (lamp == null) return;

            // 0..1 breathing curve
            float breathe = Mathf.Sin(Time.time * speed * Mathf.PI * 2f + seed) * 0.5f + 0.5f;
            float flutter = Mathf.Sin(Time.time * flutterSpeed + seed * 3.7f) * flutterAmount;

            lamp.intensity = baseIntensity * (1f - amount + amount * breathe)
                           + baseIntensity * flutter;
        }
    }
}
