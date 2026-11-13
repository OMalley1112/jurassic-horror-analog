using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Emergency beacon: a red sweep light that keeps turning long after the
    /// people who switched it on are gone. Can pendulum, full-spin or strobe.
    /// </summary>
    public class BeaconRotator : MonoBehaviour
    {
        public enum Mode { Sweep, Spin, Strobe }

        public Mode mode = Mode.Sweep;
        public float speed = 48f;           // degrees per second (Sweep/Spin)
        public float arc = 150f;            // pendulum half-arc (Sweep)
        public float strobeRate = 2.4f;     // flashes per second (Strobe)

        public Light beaconLight;
        public float baseIntensity = 3f;

        float t;
        float strobeTimer;

        void Start()
        {
            if (beaconLight == null)
                beaconLight = GetComponentInChildren<Light>();
            if (beaconLight != null) baseIntensity = beaconLight.intensity;
        }

        void Update()
        {
            switch (mode)
            {
                case Mode.Sweep:
                    t += Time.deltaTime * speed * 0.01f;
                    transform.localRotation = Quaternion.Euler(
                        0f, Mathf.Sin(t) * arc, 0f);
                    break;

                case Mode.Spin:
                    transform.Rotate(0f, speed * Time.deltaTime, 0f,
                        Space.Self);
                    break;

                case Mode.Strobe:
                    strobeTimer += Time.deltaTime * strobeRate;
                    bool on = (strobeTimer % 1f) < 0.42f;
                    if (beaconLight != null)
                        beaconLight.intensity = on ? baseIntensity : 0f;
                    break;
            }
        }
    }
}
