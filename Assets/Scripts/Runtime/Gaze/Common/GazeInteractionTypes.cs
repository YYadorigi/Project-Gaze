using UnityEngine;

namespace ProjectGaze.Gaze
{
    public enum GazeInteractionMode
    {
        Idle,
        Previewing,
        Confirmed,
        SwitchPreview
    }

    public enum SpatialPageVisualState
    {
        Dormant,
        Preview,
        Confirmed,
        Suppressed
    }

    public enum PageSelectionMode
    {
        StereoGaze,
        MouseFallback
    }

    public enum GazeDepthMatchingMode
    {
        ViewportOnly,
        ContinuousWorldPoint,
        DiscreteDepthLayer
    }

    public readonly struct GazeTrackingSample
    {
        public GazeTrackingSample(
            bool isValid,
            Vector2 normalizedViewportPoint,
            long timestamp,
            bool leftEyeValid,
            bool rightEyeValid,
            bool hasPredictedWorldPoint = false,
            Vector3 predictedWorldPoint = default,
            float predictedRayDistance = 0f)
        {
            IsValid = isValid;
            NormalizedViewportPoint = normalizedViewportPoint;
            Timestamp = timestamp;
            LeftEyeValid = leftEyeValid;
            RightEyeValid = rightEyeValid;
            HasPredictedWorldPoint = hasPredictedWorldPoint;
            PredictedWorldPoint = predictedWorldPoint;
            PredictedRayDistance = predictedRayDistance;
        }

        public bool IsValid { get; }

        public Vector2 NormalizedViewportPoint { get; }

        public long Timestamp { get; }

        public bool LeftEyeValid { get; }

        public bool RightEyeValid { get; }

        public bool HasPredictedWorldPoint { get; }

        public Vector3 PredictedWorldPoint { get; }

        public float PredictedRayDistance { get; }
    }

    public readonly struct GazeHitResult
    {
        public GazeHitResult(
            bool trackingAvailable,
            bool hasHitPage,
            ISpatialTarget target,
            string targetId,
            GazeDepthMatchingMode matchingMode = GazeDepthMatchingMode.DiscreteDepthLayer,
            string depthLayerId = null,
            float depthLayerRayDistance = 0f)
        {
            TrackingAvailable = trackingAvailable;
            HasHitPage = hasHitPage;
            Target = target;
            TargetId = targetId;
            MatchingMode = matchingMode;
            DepthLayerId = depthLayerId;
            DepthLayerRayDistance = depthLayerRayDistance;
        }

        public bool TrackingAvailable { get; }

        public bool HasHitPage { get; }

        public ISpatialTarget Target { get; }

        public SpatialPage Page => Target as SpatialPage;

        public string TargetId { get; }

        public string PageId => TargetId;

        public GazeDepthMatchingMode MatchingMode { get; }

        public string DepthLayerId { get; }

        public float DepthLayerRayDistance { get; }
    }

    public readonly struct GazeInteractionInput
    {
        public GazeInteractionInput(
            bool trackingAvailable,
            string immediateHitPageId,
            string stableHitPageId,
            bool blinkConfirmed,
            float deltaTime)
        {
            TrackingAvailable = trackingAvailable;
            ImmediateHitPageId = immediateHitPageId;
            StableHitPageId = stableHitPageId;
            BlinkConfirmed = blinkConfirmed;
            DeltaTime = deltaTime;
        }

        public bool TrackingAvailable { get; }

        public string ImmediateHitPageId { get; }

        public string StableHitPageId { get; }

        public bool BlinkConfirmed { get; }

        public float DeltaTime { get; }
    }

    public readonly struct GazeInteractionSnapshot
    {
        public GazeInteractionSnapshot(
            GazeInteractionMode mode,
            string previewPageId,
            string confirmedPageId)
        {
            Mode = mode;
            PreviewPageId = previewPageId;
            ConfirmedPageId = confirmedPageId;
        }

        public GazeInteractionMode Mode { get; }

        public string PreviewPageId { get; }

        public string ConfirmedPageId { get; }
    }

}
