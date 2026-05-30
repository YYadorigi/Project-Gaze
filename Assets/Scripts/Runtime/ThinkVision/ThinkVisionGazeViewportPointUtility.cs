using AS3DPlugin;
using ProjectGaze.Gaze;
using UnityEngine;

namespace ProjectGaze.Hardware.ThinkVision
{
    public static class ThinkVisionGazeViewportPointUtility
    {
        public static Vector2 DisplayAreaToViewport(Camera targetCamera, Vector2 displayAreaPoint)
        {
            return GazeViewportPointUtility.DisplayAreaToViewport(
                ApplyStereoXCompensation(targetCamera, displayAreaPoint));
        }

        public static Vector2 ApplyStereoXCompensation(Camera targetCamera, Vector2 displayAreaPoint)
        {
            if (ShouldApplyStereoXCompensation(targetCamera, displayAreaPoint))
            {
                displayAreaPoint.x = ApplyStereoXCompensation(displayAreaPoint.x);
            }

            return displayAreaPoint;
        }

        public static bool ShouldApplyStereoXCompensation(Camera targetCamera, Vector2 displayAreaPoint)
        {
            return IsSideBySideStereoActive(targetCamera) && IsFinite(displayAreaPoint.x);
        }

        public static float ApplyStereoXCompensation(float displayAreaX)
        {
            if (displayAreaX > 1.0f + 0.001f)
            {
                return displayAreaX * 0.5f;
            }

            return 0.5f + ((displayAreaX - 0.5f) * 0.5f);
        }

        private static bool IsSideBySideStereoActive(Camera targetCamera)
        {
            var stereoCamera = targetCamera != null
                ? targetCamera.GetComponent<StereoCam>()
                : null;

            if (stereoCamera == null)
            {
                stereoCamera = Object.FindFirstObjectByType<StereoCam>();
            }

            return stereoCamera != null &&
                   stereoCamera.usedShader == StereoCam.AutoStereoShader.SideBySide;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
