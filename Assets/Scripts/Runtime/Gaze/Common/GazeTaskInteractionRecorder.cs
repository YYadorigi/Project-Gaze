using System;
using System.Collections.Generic;
using ProjectGaze.Gaze.Depth;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectGaze.Gaze
{
    public sealed class GazeTaskInteractionRecorder
    {
        private const float FeedbackMessageSeconds = 2.5f;

        private readonly List<GazeTaskInteractionRecord> records = new();
        private readonly string experimentDataRootPath;
        private string feedbackMessage;
        private float feedbackMessageUntilSeconds;
        private bool pendingFailureMarkedByUser;
        private string pendingFailureTimestampUtc;
        private int lastSavedRevision = -1;
        private int revision;

        public GazeTaskInteractionRecorder(string sceneId, string experimentDataRootPath)
        {
            if (string.IsNullOrWhiteSpace(sceneId))
            {
                throw new ArgumentException("Scene id is required.", nameof(sceneId));
            }

            if (string.IsNullOrWhiteSpace(experimentDataRootPath))
            {
                throw new ArgumentException("Experiment data root path is required.", nameof(experimentDataRootPath));
            }

            this.experimentDataRootPath = experimentDataRootPath;
            Session = new GazeTaskInteractionSession
            {
                SessionId = GazeDepthPersistence.CreateSessionIdUtc(),
                SceneId = sceneId,
                StartedAtUtc = DateTime.UtcNow.ToString("O"),
                SavedAtUtc = DateTime.UtcNow.ToString("O"),
                Records = Array.Empty<GazeTaskInteractionRecord>()
            };
        }

        public GazeTaskInteractionSession Session { get; }

        public IReadOnlyList<GazeTaskInteractionRecord> Records => records;

        public bool HasPendingFailureFeedback => pendingFailureMarkedByUser;

        public static GazeTaskInteractionRecorder CreateForProjectScene(string sceneId)
        {
            return new GazeTaskInteractionRecorder(
                sceneId,
                GazeDepthPersistence.ResolveUnityExperimentDataRootPath());
        }

        public static bool IsFailureFeedbackPressedThisFrame()
        {
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        public void RecordInteraction(GazeTaskInteractionRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            string nowUtc = DateTime.UtcNow.ToString("O");
            record.SceneId = Session.SceneId;
            record.EventIndex = records.Count;
            record.StartedAtUtc = string.IsNullOrWhiteSpace(record.StartedAtUtc) ? nowUtc : record.StartedAtUtc;
            record.CompletedAtUtc = string.IsNullOrWhiteSpace(record.CompletedAtUtc) ? nowUtc : record.CompletedAtUtc;
            record.Success = !record.FailureMarkedByUser;

            records.Add(record);
            revision += 1;
            SaveNow();
            SetFeedbackMessage($"Recorded interaction #{record.EventIndex}: {(record.Success ? "success" : "failed")}", Time.unscaledTime);
        }

        public void ApplyPendingFailure(GazeTaskInteractionRecord record)
        {
            if (record == null || !pendingFailureMarkedByUser)
            {
                return;
            }

            MarkRecordFailed(record, string.IsNullOrWhiteSpace(pendingFailureTimestampUtc)
                ? DateTime.UtcNow.ToString("O")
                : pendingFailureTimestampUtc);
            pendingFailureMarkedByUser = false;
            pendingFailureTimestampUtc = null;
        }

        public bool TickSpaceFeedback(bool spacePressed, bool hasPendingInteraction, float nowSeconds)
        {
            if (!spacePressed)
            {
                return false;
            }

            if (hasPendingInteraction)
            {
                pendingFailureMarkedByUser = true;
                pendingFailureTimestampUtc = DateTime.UtcNow.ToString("O");
                SetFeedbackMessage("Marked current interaction as failed", nowSeconds);
                return true;
            }

            return TryMarkLatestFailure(nowSeconds);
        }

        public bool TryMarkLatestFailure(float nowSeconds)
        {
            if (records.Count == 0)
            {
                SetFeedbackMessage("No completed interaction to mark yet", nowSeconds);
                return false;
            }

            var record = records[records.Count - 1];
            if (record.FailureMarkedByUser)
            {
                SetFeedbackMessage($"Interaction #{record.EventIndex} is already marked failed", nowSeconds);
                return false;
            }

            MarkRecordFailed(record, DateTime.UtcNow.ToString("O"));
            revision += 1;
            SaveNow();
            SetFeedbackMessage($"Marked interaction #{record.EventIndex} as failed", nowSeconds);
            return true;
        }

        public string BuildOverlayText(bool hasPendingInteraction, float nowSeconds)
        {
            var lines = new List<string>
            {
                records.Count == 0
                    ? "Last interaction: none"
                    : $"Last interaction #{records[records.Count - 1].EventIndex}: {(records[records.Count - 1].Success ? "success" : "failed")}"
            };

            if (hasPendingInteraction)
            {
                lines.Add("Agent round trip in progress.");
            }

            if (!string.IsNullOrWhiteSpace(feedbackMessage) && nowSeconds <= feedbackMessageUntilSeconds)
            {
                lines.Add(feedbackMessage);
            }

            lines.Add("Space: mark last interaction as failed");
            return string.Join("\n", lines);
        }

        public void FlushIfDirty()
        {
            if (revision == lastSavedRevision)
            {
                return;
            }

            SaveNow();
        }

        private void MarkRecordFailed(GazeTaskInteractionRecord record, string feedbackTimestampUtc)
        {
            record.Success = false;
            record.FailureMarkedByUser = true;
            record.FeedbackSource = GazeTaskInteractionTypes.SpaceKeyFeedbackSource;
            record.FeedbackTimestampUtc = feedbackTimestampUtc;
        }

        private void SaveNow()
        {
            try
            {
                Session.Records = records.ToArray();
                GazeTaskInteractionPersistence.SaveSession(experimentDataRootPath, Session);
                lastSavedRevision = revision;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to save task interaction log: {exception.Message}");
            }
        }

        private void SetFeedbackMessage(string message, float nowSeconds)
        {
            feedbackMessage = message;
            feedbackMessageUntilSeconds = nowSeconds + FeedbackMessageSeconds;
        }
    }

}
