using UnityEngine;

namespace AnalogHorror
{
    /// <summary>Uneven buzzing light with occasional blackouts.</summary>
    [RequireComponent(typeof(Light))]
    public class FlickeringLight : MonoBehaviour
    {
        public float baseIntensity = 1.5f;
        [Range(0f, 1f)] public float flickerAmount = 0.55f;
        public float buzzSpeed = 1.6f;
        public float blackoutChance = 0.004f;
        public float seed;

        Light lightComp;
        float blackoutUntil;

        void Awake()
        {
            lightComp = GetComponent<Light>();
            if (seed == 0f) seed = Random.Range(0f, 100f);
            if (baseIntensity <= 0f) baseIntensity = lightComp.intensity;
        }

        void Update()
        {
            if (lightComp == null) return;

            if (Random.value < blackoutChance)
                blackoutUntil = Time.time + Random.Range(0.06f, 0.7f);

            if (Time.time < blackoutUntil)
            {
                lightComp.intensity = baseIntensity * Random.Range(0f, 0.06f);
                return;
            }

            float n = Mathf.PerlinNoise(seed, Time.time * buzzSpeed);
            float factor = Mathf.Lerp(1f, n, flickerAmount);
            lightComp.intensity = baseIntensity * factor;
        }
    }
}
