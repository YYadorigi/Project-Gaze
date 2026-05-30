using UnityEngine;

namespace ProjectGaze.Gaze.Depth
{
    public readonly struct GazeDepthLayerMatch
    {
        public GazeDepthLayerMatch(
            string layerId,
            float rayDistance,
            float tolerance,
            float distanceError,
            bool isWithinTolerance)
        {
            LayerId = layerId;
            RayDistance = rayDistance;
            Tolerance = tolerance;
            DistanceError = distanceError;
            IsWithinTolerance = isWithinTolerance;
        }

        public string LayerId { get; }

        public float RayDistance { get; }

        public float Tolerance { get; }

        public float DistanceError { get; }

        public bool IsWithinTolerance { get; }
    }

    public static class GazeDepthLayerResolver
    {
        public static bool TryResolveNearestLayer(float rayDistance, out GazeDepthLayerMatch match)
        {
            match = default;
            if (!GazeViewportPointUtility.IsFinite(rayDistance) || rayDistance <= 0f)
            {
                return false;
            }

            GazeDepthLayerDefinition bestLayer = default;
            float bestDistanceError = float.PositiveInfinity;
            bool hasLayer = false;

            for (int index = 0; index < GazeDepthLayerProfile.LayerCount; index += 1)
            {
                var layer = GazeDepthLayerProfile.GetLayer(index);
                float distanceError = Mathf.Abs(rayDistance - layer.RayDistance);
                if (distanceError >= bestDistanceError)
                {
                    continue;
                }

                bestLayer = layer;
                bestDistanceError = distanceError;
                hasLayer = true;
            }

            if (!hasLayer)
            {
                return false;
            }

            match = new GazeDepthLayerMatch(
                bestLayer.LayerId,
                bestLayer.RayDistance,
                bestLayer.Tolerance,
                bestDistanceError,
                bestDistanceError <= bestLayer.Tolerance);
            return true;
        }
    }
}
