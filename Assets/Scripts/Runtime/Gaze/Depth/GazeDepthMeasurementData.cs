using System;

namespace ProjectGaze.Gaze.Depth
{
    [Serializable]
    public sealed class GazeDepthContinuousValidationRecord
    {
        public int TargetIndex;
        public float TargetViewportX;
        public float TargetViewportY;
        public float InjectedRayDistance;
        public float PredictedRayDistance;
        public float SignedError;
        public float AbsoluteError;
        public string InjectedNearestLayerId;
        public string PredictedNearestLayerId;
        public bool InjectedWithinLayerTolerance;
        public bool PredictedWithinLayerTolerance;
        public bool LayerMatched;
        public long SampleTimestamp;
        public string RecordedAtUtc;
    }

    [Serializable]
    public sealed class GazeDepthContinuousValidationSession
    {
        public string SessionId;
        public string TruthSource;
        public string ViewportLayout;
        public string DepthLayerProfileVersion;
        public float TargetViewportX;
        public float TargetViewportY;
        public float MinimumRayDistance;
        public float MaximumRayDistance;
        public float RayDistanceStep;
        public int TargetCount;
        public float SampleWindowSeconds;
        public int MinimumSamplesPerTarget;
        public int RecordCount;
        public string SavedAtUtc;
        public GazeDepthContinuousValidationRecord[] Records;
    }

    [Serializable]
    public sealed class GazeDepthRuntimeLayerRecord
    {
        public int RecordIndex;
        public string RecordedAtUtc;
        public long SampleTimestamp;
        public string SceneId;
        public string InputSource;
        public string MatchingMode;
        public float GazeViewportX;
        public float GazeViewportY;
        public bool HasPredictedDepth;
        public float PredictedRayDistance;
        public string PredictedNearestLayerId;
        public float PredictedNearestLayerRayDistance;
        public float PredictedNearestLayerDistanceError;
        public bool PredictedNearestLayerWithinTolerance;
        public bool HasSystemHit;
        public string SystemHitPageId;
        public string SystemHitDepthLayerId;
        public float SystemHitDepthLayerRayDistance;
        public float SystemHitDepthLayerTolerance;
        public bool HasReferenceDepth;
        public float SignedDepthError;
        public float AbsoluteDepthError;
        public bool LayerMatched;
        public string PreviewPageId;
        public string ConfirmedPageId;
        public bool ViewportOnlyHasHit;
        public string ViewportOnlyPageId;
        public string ViewportOnlyDepthLayerId;
        public bool ContinuousWorldPointHasHit;
        public string ContinuousWorldPointPageId;
        public string ContinuousWorldPointDepthLayerId;
        public bool DiscreteDepthLayerHasHit;
        public string DiscreteDepthLayerPageId;
        public string DiscreteDepthLayerDepthLayerId;
    }

    [Serializable]
    public sealed class GazeDepthRuntimeLayerSession
    {
        public string SessionId;
        public string SceneId;
        public string TruthSource;
        public string DepthLayerProfileVersion;
        public string StartedAtUtc;
        public string SavedAtUtc;
        public int RecordCount;
        public GazeDepthRuntimeLayerRecord[] Records;
    }
}
