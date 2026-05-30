using System;
using UnityEngine;

namespace ProjectGaze.Gaze.Depth
{
    public static class GazeDepthContinuousValidationTargetLibrary
    {
        public const int TargetCount = 19;
        public const float MinimumRayDistance = GazeDepthLayerProfile.Near3RayDistance;
        public const float MaximumRayDistance = GazeDepthLayerProfile.Far3RayDistance;
        public const float RayDistanceStep = 1.0f;
        public const float ViewportX = 0.5f;

        private static readonly GazeDepthCalibrationTarget[] ProfileAnchors =
        {
            new(new Vector2(ViewportX, 0.70f), GazeDepthLayerProfile.Near3RayDistance),
            new(new Vector2(ViewportX, 0.64f), GazeDepthLayerProfile.Near2RayDistance),
            new(new Vector2(ViewportX, 0.58f), GazeDepthLayerProfile.Near1RayDistance),
            new(new Vector2(ViewportX, 0.50f), GazeDepthLayerProfile.ZeroRayDistance),
            new(new Vector2(ViewportX, 0.42f), GazeDepthLayerProfile.Far1RayDistance),
            new(new Vector2(ViewportX, 0.36f), GazeDepthLayerProfile.Far2RayDistance),
            new(new Vector2(ViewportX, 0.30f), GazeDepthLayerProfile.Far3RayDistance)
        };

        public static GazeDepthCalibrationTarget GetTarget(int index)
        {
            if (index < 0 || index >= TargetCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            float rayDistance = MinimumRayDistance + (index * RayDistanceStep);
            return new GazeDepthCalibrationTarget(
                new Vector2(ViewportX, ResolveViewportY(rayDistance)),
                rayDistance);
        }

        public static float ResolveViewportY(float rayDistance)
        {
            if (rayDistance <= ProfileAnchors[0].RayDistance)
            {
                return ProfileAnchors[0].ViewportPoint.y;
            }

            for (int index = 1; index < ProfileAnchors.Length; index += 1)
            {
                var lower = ProfileAnchors[index - 1];
                var upper = ProfileAnchors[index];
                if (rayDistance > upper.RayDistance)
                {
                    continue;
                }

                float t = Mathf.InverseLerp(lower.RayDistance, upper.RayDistance, rayDistance);
                return Mathf.Lerp(lower.ViewportPoint.y, upper.ViewportPoint.y, t);
            }

            return ProfileAnchors[ProfileAnchors.Length - 1].ViewportPoint.y;
        }
    }
}
