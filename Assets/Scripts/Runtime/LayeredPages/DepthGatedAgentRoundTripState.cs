using System;

namespace ProjectGaze.Gaze
{
    public sealed class DepthGatedAgentRoundTripState
    {
        private string openedAtUtc;
        private string openMatchingMode;
        private bool hasOpenPredictedDepth;
        private float openPredictedRayDistance;

        public bool HasPending { get; private set; }

        public void Begin(GazeInteractionController gazeController, string openedAtUtcOverride = null)
        {
            HasPending = true;
            openedAtUtc = string.IsNullOrWhiteSpace(openedAtUtcOverride)
                ? DateTime.UtcNow.ToString("O")
                : openedAtUtcOverride;
            openMatchingMode = gazeController != null
                ? gazeController.DepthMatchingMode.ToString()
                : null;
            hasOpenPredictedDepth = gazeController != null &&
                                    gazeController.HasLastTrackingSample &&
                                    gazeController.LastTrackingSample.PredictedRayDistance > 0f;
            openPredictedRayDistance = hasOpenPredictedDepth
                ? gazeController.LastTrackingSample.PredictedRayDistance
                : 0f;
        }

        public bool TryBuildCompletedRecord(
            string closedByPageId,
            GazeInteractionController gazeController,
            out GazeTaskInteractionRecord record,
            string closedAtUtcOverride = null)
        {
            record = null;
            if (!HasPending)
            {
                return false;
            }

            string closedAtUtc = string.IsNullOrWhiteSpace(closedAtUtcOverride)
                ? DateTime.UtcNow.ToString("O")
                : closedAtUtcOverride;
            record = GazeTaskInteractionRecordFactory.BuildBaseRecord(gazeController);
            record.TaskType = GazeTaskInteractionTypes.AgentPanelRoundTrip;
            record.StartedAtUtc = string.IsNullOrWhiteSpace(openedAtUtc)
                ? closedAtUtc
                : openedAtUtc;
            record.CompletedAtUtc = closedAtUtc;
            record.ConfirmedPageId = closedByPageId;
            record.LogoPageId = DepthGatedAgentPanelCoordinator.AgentLogoPageId;
            record.OpenedAtUtc = record.StartedAtUtc;
            record.ClosedByPageId = closedByPageId;
            record.ClosedAtUtc = closedAtUtc;
            record.OpenMatchingMode = openMatchingMode;
            record.CloseMatchingMode = record.MatchingMode;
            record.HasOpenPredictedDepth = hasOpenPredictedDepth;
            record.OpenPredictedRayDistance = openPredictedRayDistance;
            record.HasClosePredictedDepth = record.HasPredictedDepth;
            record.ClosePredictedRayDistance = record.PredictedRayDistance;
            return true;
        }

        public void Clear()
        {
            HasPending = false;
            openedAtUtc = null;
            openMatchingMode = null;
            hasOpenPredictedDepth = false;
            openPredictedRayDistance = 0f;
        }
    }
}
