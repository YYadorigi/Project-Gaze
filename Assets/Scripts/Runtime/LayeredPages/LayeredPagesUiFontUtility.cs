using UnityEngine;

namespace ProjectGaze.Gaze
{
    internal static class LayeredPagesUiFontUtility
    {
        private static Font cachedFont;

        public static Font GetDefaultFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return cachedFont;
        }
    }
}
