using System;

namespace ProjectGaze.Gaze
{
    [Serializable]
    public sealed class GazeTaskInteractionRecord
    {
        public string SceneId;
        public string TaskType;
        public int EventIndex;
        public string StartedAtUtc;
        public string CompletedAtUtc;
        public string ConfirmedPageId;
        public string PreviousMainPageId;
        public string NewMainPageId;
        public string MatchingMode;
        public string InputSource;
        public bool HasPredictedDepth;
        public float PredictedRayDistance;
        public string PredictedLayerId;
        public string SystemHitPageId;
        public string PreviewPageId;
        public string LogoPageId;
        public string OpenedAtUtc;
        public string ClosedByPageId;
        public string ClosedAtUtc;
        public string OpenMatchingMode;
        public string CloseMatchingMode;
        public bool HasOpenPredictedDepth;
        public bool HasClosePredictedDepth;
        public float OpenPredictedRayDistance;
        public float ClosePredictedRayDistance;
        public bool Success;
        public bool FailureMarkedByUser;
        public string FeedbackSource;
        public string FeedbackTimestampUtc;
    }

    [Serializable]
    public sealed class GazeTaskInteractionSession
    {
        public string SessionId;
        public string SceneId;
        public string StartedAtUtc;
        public string SavedAtUtc;
        public int RecordCount;
        public int SuccessCount;
        public int FailureCount;
        public GazeTaskInteractionRecord[] Records;
    }
}
