using UnityEngine;

namespace GemsSort.Game
{
    /// <summary>
    /// Centralised resolver for Unity's built-in legacy UI font. Newer Unity versions
    /// (2023+) renamed Arial.ttf to LegacyRuntime.ttf; this helper tries both so the
    /// runtime auto-built UI keeps working across versions without throwing.
    /// </summary>
    public static class GemsSortFont
    {
        private static Font cached;

        public static Font Default
        {
            get
            {
                if (cached != null)
                {
                    return cached;
                }

                cached = TryGetBuiltinFont("LegacyRuntime.ttf");
                if (cached != null)
                {
                    return cached;
                }

                cached = TryGetBuiltinFont("Arial.ttf");
                if (cached != null)
                {
                    return cached;
                }

                // Last-ditch fallback: try the legacy operating system font.
                cached = Font.CreateDynamicFontFromOSFont("Arial", 16);
                return cached;
            }
        }

        private static Font TryGetBuiltinFont(string resourceName)
        {
            try
            {
                return Resources.GetBuiltinResource<Font>(resourceName);
            }
            catch
            {
                return null;
            }
        }
    }
}
