using UnityEngine;

namespace ProjectGaze.Gaze
{
    public interface ISpatialTarget
    {
        string TargetId { get; }

        Vector3 MatchingRegionCenterWorld { get; }

        float MatchingRegionRadiusWorld { get; }

        string DepthLayerId { get; }

        float DepthLayerRayDistance { get; }

        float DepthLayerTolerance { get; }

        bool HasDepthLayer { get; }
    }
}
