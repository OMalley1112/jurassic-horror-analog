using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Central lookup for all shared game materials (force-recompile touch #2).
    /// Materials live in Assets/Resources/Materials so both the editor
    /// map builder and runtime scripts can load them by name.
    /// </summary>
    public static class GameMaterials
    {
        const string ResourcePath = "Materials/";
        const string AssetPath = "Assets/Resources/Materials/";

        public static Material Get(string name)
        {
            Material m = Resources.Load<Material>(ResourcePath + name);
            if (m != null) return m;

#if UNITY_EDITOR
            m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(AssetPath + name + ".mat");
            if (m != null) return m;
#endif

            Debug.LogWarning("[AnalogHorror] Missing material '" + name + "', using fallback.");
            var fallback = new Material(Shader.Find("Standard"));
            fallback.name = name + "_Fallback";
            fallback.color = new Color(0.5f, 0.5f, 0.5f);
            return fallback;
        }
    }
}
