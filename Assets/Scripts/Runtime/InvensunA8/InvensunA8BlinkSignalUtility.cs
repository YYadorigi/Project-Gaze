using UnityEngine;

namespace ProjectGaze.Gaze.Providers
{
    public static class InvensunA8BlinkSignalUtility
    {
        public static bool IsEyeOpen(
            bool blinkDetected,
            float openness,
            float opennessThreshold)
        {
            if (blinkDetected)
            {
                return false;
            }

            if (IsUsableOpenness(openness))
            {
                return openness > opennessThreshold;
            }

            return true;
        }

        private static bool IsUsableOpenness(float openness)
        {
            return !float.IsNaN(openness) &&
                   !float.IsInfinity(openness) &&
                   openness > 0.001f &&
                   openness <= 1.5f;
        }
    }
}
