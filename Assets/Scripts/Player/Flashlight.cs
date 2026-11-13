using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Flashlight with a draining battery, low-battery flicker, and spare
    /// batteries on R. The actual Spot Light lives on the camera.
    /// </summary>
    public class Flashlight : MonoBehaviour
    {
        public Light spot;

        [Header("Battery")]
        public float battery = 100f;
        public float drainPerSecond = 0.55f;
        public int spareBatteries = 4;
        public float flickerThreshold = 18f;

        [Header("Keys")]
        public KeyCode toggleKey = KeyCode.F;
        public KeyCode reloadKey = KeyCode.R;

        public bool IsOn { get; private set; } = true;

        float baseIntensity;
        AudioSource clickSrc;
        GUIStyle style;
        bool styleReady;

        bool forcedOff;          // power cut (dread events)
        float flickerPulse;      // brief relight attempt while cut

        void Start()
        {
            if (spot == null)
            {
                var lights = GetComponentsInChildren<Light>(true);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Spot) { spot = lights[i]; break; }
                }
            }

            if (spot != null)
            {
                baseIntensity = spot.intensity;
                spot.intensity = baseIntensity;
            }

            clickSrc = gameObject.AddComponent<AudioSource>();
            clickSrc.playOnAwake = false;
            clickSrc.spatialBlend = 0f;
            clickSrc.clip = ProceduralAudio.Click(7);
            clickSrc.volume = 0.5f;
        }

        void Update()
        {
            // no battery drain or lamp keys while the title screen is up
            if (MainMenu.Instance != null && !MainMenu.Instance.Started) return;

            // the ending film owns the lamp: no toggles, no reloads over it
            var objective = ObjectiveSystem.Instance;
            bool ended = objective != null && objective.Escaped;
            bool paused = PauseSystem.Instance != null && PauseSystem.Instance.IsPaused;

            if (flickerPulse > 0f) flickerPulse -= Time.deltaTime;

            if (!ended && !paused &&
                Input.GetKeyDown(toggleKey) && battery > 0f && !forcedOff)
            {
                IsOn = !IsOn;
                clickSrc.Play();
            }

            if (!ended && !paused &&
                Input.GetKeyDown(reloadKey) && battery < 40f && spareBatteries > 0)
            {
                spareBatteries--;
                battery = 100f;
                if (!IsOn) IsOn = true;
                clickSrc.pitch = 0.8f;
                clickSrc.Play();
                clickSrc.pitch = 1f;
            }

            bool active = (IsOn && battery > 0f && !forcedOff) || flickerPulse > 0f;
            if (IsOn && battery > 0f && !forcedOff)
                battery = Mathf.Max(0f, battery - drainPerSecond * Time.deltaTime);

            if (spot == null) return;

            if (!active)
            {
                spot.intensity = Mathf.Lerp(spot.intensity, 0f, 12f * Time.deltaTime);
                return;
            }

            float intensity = baseIntensity;

            // stutter while the power is coming back
            if (flickerPulse > 0f)
                intensity *= Random.Range(0.1f, 0.55f);

            if (battery <= flickerThreshold)
            {
                // dying bulb: stutter and dim
                float health = battery / flickerThreshold;
                if (Random.value < 0.06f)
                    intensity *= Random.Range(0.05f, 0.45f);
                intensity *= Mathf.Lerp(0.45f, 1f, health);
            }

            spot.intensity = intensity;
        }

        /// <summary>Power cut: the light dies and F does nothing until restored.</summary>
        public void ForceOff()
        {
            forcedOff = true;
            flickerPulse = 0f;
            if (clickSrc != null) clickSrc.Play();
        }

        /// <summary>One stuttering attempt to relight during a blackout.</summary>
        public void TryFlicker()
        {
            flickerPulse = Random.Range(0.05f, 0.15f);
            if (clickSrc != null)
            {
                clickSrc.pitch = Random.Range(0.7f, 1.3f);
                clickSrc.Play();
                clickSrc.pitch = 1f;
            }
        }

        /// <summary>Power returns — the lamp is allowed to work again.</summary>
        public void RestorePower()
        {
            forcedOff = false;
            flickerPulse = 0f;
            if (clickSrc != null) clickSrc.Play();
        }

        void OnGUI()
        {
            // gameplay status only — never over the title screen, the boss
            // intro film, or the ending
            if (MainMenu.Instance != null && !MainMenu.Instance.Started) return;
            if (BossFight.CutsceneRunning) return;
            var objective = ObjectiveSystem.Instance;
            if (objective != null && objective.Escaped) return;

            if (!styleReady)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
                style.normal.textColor = new Color(0.75f, 1f, 0.75f, 0.8f);
                styleReady = true;
            }

            string status = IsOn && battery > 0f
                ? string.Format("FLASHLIGHT  {0:0}%", battery)
                : "FLASHLIGHT  DEAD  [R]";

            GUI.Label(new Rect(24f, Screen.height - 56f, 260f, 22f), status, style);
            GUI.Label(new Rect(24f, Screen.height - 38f, 260f, 22f),
                "SPARE CELLS: " + spareBatteries, style);
        }
    }
}
