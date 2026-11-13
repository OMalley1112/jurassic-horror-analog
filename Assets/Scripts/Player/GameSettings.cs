using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Player-tunable options. Loaded once from PlayerPrefs, edited live by
    /// the menu's SETTINGS screen and applied every frame by the
    /// FirstPersonController (sensitivity, FOV, invert-Y, head bob) and by
    /// the audio listener (master volume).
    /// </summary>
    public static class GameSettings
    {
        public const string KeySens = "jh_sens";
        public const string KeyFov = "jh_fov";
        public const string KeyVol = "jh_vol";
        public const string KeyInvert = "jh_invert";
        public const string KeyBob = "jh_bob";

        public const float SensDefault = 2.2f;
        public const float FovDefault = 72f;
        public const float VolDefault = 1f;
        public const float BobDefault = 1f;

        public static float Sensitivity = SensDefault;
        public static float Fov = FovDefault;
        public static float Volume = VolDefault;
        public static bool InvertY;
        public static float HeadBob = BobDefault;

        static bool loaded;

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(KeySens, SensDefault), 0.4f, 6f);
            Fov = Mathf.Clamp(PlayerPrefs.GetFloat(KeyFov, FovDefault), 55f, 100f);
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVol, VolDefault));
            InvertY = PlayerPrefs.GetInt(KeyInvert, 0) == 1;
            HeadBob = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyBob, BobDefault));
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat(KeySens, Sensitivity);
            PlayerPrefs.SetFloat(KeyFov, Fov);
            PlayerPrefs.SetFloat(KeyVol, Volume);
            PlayerPrefs.SetInt(KeyInvert, InvertY ? 1 : 0);
            PlayerPrefs.SetFloat(KeyBob, HeadBob);
            PlayerPrefs.Save();
        }

        public static void ResetDefaults()
        {
            Sensitivity = SensDefault;
            Fov = FovDefault;
            Volume = VolDefault;
            InvertY = false;
            HeadBob = BobDefault;
            Save();
        }
    }
}
