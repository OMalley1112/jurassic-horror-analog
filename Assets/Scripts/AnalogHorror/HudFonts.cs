using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Shared OS fonts for every on-screen text — small, clean, game-like
    /// typography with zero font assets in the project. Falls back to the
    /// default IMGUI skin if the machine has none of the preferred faces.
    /// </summary>
    public static class HudFonts
    {
        static Font ui;
        static Font mono;
        static bool uiTried;
        static bool monoTried;

        /// <summary>Clean humanist sans (HUD, cards, prompts).</summary>
        public static Font Ui
        {
            get
            {
                if (!uiTried)
                {
                    uiTried = true;
                    ui = Load(new[]
                    {
                        "Segoe UI", "Inter", "SF Pro Text", "Helvetica Neue",
                        "Arial", "Tahoma"
                    });
                }
                return ui;
            }
        }

        /// <summary>Monospace (timecode, camcorder readouts).</summary>
        public static Font Mono
        {
            get
            {
                if (!monoTried)
                {
                    monoTried = true;
                    mono = Load(new[]
                    {
                        "Consolas", "SF Mono", "Menlo", "DejaVu Sans Mono",
                        "Courier New", "Lucida Console"
                    });
                }
                return mono;
            }
        }

        static Font Load(string[] names)
        {
            foreach (var n in names)
            {
                try
                {
                    var f = Font.CreateDynamicFontFromOSFont(n, 12);
                    if (f != null) return f;
                }
                catch { /* font not on this machine — try the next */ }
            }
            return null;        // caller keeps the default skin
        }
    }
}
