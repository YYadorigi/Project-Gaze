using System;
using UnityEngine;

namespace ProjectGaze.Gaze.Depth
{
    public readonly struct GazeDepthCalibrationTarget
    {
        public GazeDepthCalibrationTarget(Vector2 viewportPoint, float rayDistance)
        {
            ViewportPoint = viewportPoint;
            RayDistance = rayDistance;
        }

        public Vector2 ViewportPoint { get; }

        public float RayDistance { get; }
    }

    public readonly struct GazeDepthPrediction
    {
        public GazeDepthPrediction(bool isValid, float rayDistance, Vector3 worldPoint)
        {
            IsValid = isValid;
            RayDistance = rayDistance;
            WorldPoint = worldPoint;
        }

        public bool IsValid { get; }

        public float RayDistance { get; }

        public Vector3 WorldPoint { get; }
    }

    [Serializable]
    public sealed class GazeDepthCalibrationRecord
    {
        public float[] Features;
        public float TargetRayDistance;
        public float TargetViewportX;
        public float TargetViewportY;
    }

    [Serializable]
    public sealed class GazeDepthCalibrationDataset
    {
        public GazeDepthCalibrationRecord[] Records;
        public string SavedAtUtc;
    }

    [Serializable]
    public sealed class DisparityDepthModel
    {
        public bool IsTrained;
        public float Scale;
        public float Offset;
        public float MinimumDisparity;
        public float MinimumRayDistance;
        public float MaximumRayDistance;
    }

    [Serializable]
    public sealed class LinearSvrDepthModel
    {
        public bool IsTrained;
        public float[] FeatureMeans;
        public float[] FeatureScales;
        public float[] Weights;
        public float Bias;
        public float Epsilon;
    }

    [Serializable]
    public sealed class GazeDepthModelBundle
    {
        public DisparityDepthModel BaselineModel;
        public LinearSvrDepthModel SvrModel;
        public int TrainingSampleCount;
        public string DepthCalibrationProfileVersion;
        public float[] DepthLayerRayDistances;
        public string SavedAtUtc;
    }

    public static class GazeDepthCalibrationTargetLibrary
    {
        private static readonly GazeDepthCalibrationTarget[] DefaultTargets =
        {
            new(new Vector2(0.30f, 0.68f), GazeDepthLayerProfile.Near3RayDistance),
            new(new Vector2(0.50f, 0.70f), GazeDepthLayerProfile.Near3RayDistance),
            new(new Vector2(0.70f, 0.68f), GazeDepthLayerProfile.Near3RayDistance),
            new(new Vector2(0.30f, 0.62f), GazeDepthLayerProfile.Near2RayDistance),
            new(new Vector2(0.50f, 0.64f), GazeDepthLayerProfile.Near2RayDistance),
            new(new Vector2(0.70f, 0.62f), GazeDepthLayerProfile.Near2RayDistance),
            new(new Vector2(0.30f, 0.56f), GazeDepthLayerProfile.Near1RayDistance),
            new(new Vector2(0.50f, 0.58f), GazeDepthLayerProfile.Near1RayDistance),
            new(new Vector2(0.70f, 0.56f), GazeDepthLayerProfile.Near1RayDistance),
            new(new Vector2(0.30f, 0.50f), GazeDepthLayerProfile.ZeroRayDistance),
            new(new Vector2(0.50f, 0.50f), GazeDepthLayerProfile.ZeroRayDistance),
            new(new Vector2(0.70f, 0.50f), GazeDepthLayerProfile.ZeroRayDistance),
            new(new Vector2(0.30f, 0.44f), GazeDepthLayerProfile.Far1RayDistance),
            new(new Vector2(0.50f, 0.42f), GazeDepthLayerProfile.Far1RayDistance),
            new(new Vector2(0.70f, 0.44f), GazeDepthLayerProfile.Far1RayDistance),
            new(new Vector2(0.30f, 0.38f), GazeDepthLayerProfile.Far2RayDistance),
            new(new Vector2(0.50f, 0.36f), GazeDepthLayerProfile.Far2RayDistance),
            new(new Vector2(0.70f, 0.38f), GazeDepthLayerProfile.Far2RayDistance),
            new(new Vector2(0.30f, 0.32f), GazeDepthLayerProfile.Far3RayDistance),
            new(new Vector2(0.50f, 0.30f), GazeDepthLayerProfile.Far3RayDistance),
            new(new Vector2(0.70f, 0.32f), GazeDepthLayerProfile.Far3RayDistance)
        };

        public static int TargetCount => DefaultTargets.Length;

        public static GazeDepthCalibrationTarget GetTarget(int index)
        {
            if (index < 0 || index >= DefaultTargets.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return DefaultTargets[index];
        }
    }
}
