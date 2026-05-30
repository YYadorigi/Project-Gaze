using UnityEngine;

namespace ProjectGaze.Gaze
{
    public sealed class GazeTaskInteractionSceneLogger
    {
        private readonly GazeTaskInteractionRecorder recorder;

        private GazeTaskInteractionSceneLogger(GazeTaskInteractionRecorder recorder)
        {
            this.recorder = recorder;
        }

        public static GazeTaskInteractionSceneLogger Create(string sceneId)
        {
            return new GazeTaskInteractionSceneLogger(
                GazeExperimentDataCapture.CreateTaskInteractionRecorder(sceneId));
        }

        public void TickSpaceFeedback(bool hasPendingInteraction)
        {
            recorder?.TickSpaceFeedback(
                GazeTaskInteractionRecorder.IsFailureFeedbackPressedThisFrame(),
                hasPendingInteraction,
                Time.unscaledTime);
        }

        public string BuildOverlayText(bool hasPendingInteraction)
        {
            return recorder?.BuildOverlayText(hasPendingInteraction, Time.unscaledTime);
        }

        public void FlushIfDirty()
        {
            recorder?.FlushIfDirty();
        }

        public void RecordLayeredPageSelection(
            GazeInteractionController gazeController,
            string confirmedPageId,
            string previousMainPageId,
            string newMainPageId)
        {
            if (recorder == null || gazeController == null)
            {
                return;
            }

            var record = GazeTaskInteractionRecordFactory.BuildBaseRecord(gazeController);
            record.TaskType = GazeTaskInteractionTypes.LayeredPageSelection;
            record.ConfirmedPageId = confirmedPageId;
            record.PreviousMainPageId = previousMainPageId;
            record.NewMainPageId = newMainPageId;
            recorder.RecordInteraction(record);
        }

        public void BeginAgentRoundTrip(
            DepthGatedAgentRoundTripState roundTripState,
            GazeInteractionController gazeController)
        {
            roundTripState?.Begin(gazeController);
        }

        public void CompleteAgentRoundTrip(
            DepthGatedAgentRoundTripState roundTripState,
            string closedByPageId,
            GazeInteractionController gazeController)
        {
            if (roundTripState == null)
            {
                return;
            }

            if (recorder == null)
            {
                roundTripState.Clear();
                return;
            }

            if (!roundTripState.TryBuildCompletedRecord(closedByPageId, gazeController, out var record))
            {
                return;
            }

            recorder.ApplyPendingFailure(record);
            recorder.RecordInteraction(record);
            roundTripState.Clear();
        }
    }
}
