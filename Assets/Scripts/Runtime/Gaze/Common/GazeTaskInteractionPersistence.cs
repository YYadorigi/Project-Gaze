using System;
using System.IO;
using System.Text;
using ProjectGaze.Gaze.Depth;
using UnityEngine;
using static ProjectGaze.Gaze.CsvPersistenceUtility;

namespace ProjectGaze.Gaze
{
    public static class GazeTaskInteractionPersistence
    {
        private const string TaskInteractionDirectoryName = "task-interactions";
        private const string TaskInteractionPrefix = "task-interactions";

        public static string BuildTaskInteractionRootPath(string experimentDataRootPath)
        {
            return Path.Combine(experimentDataRootPath ?? string.Empty, TaskInteractionDirectoryName);
        }

        public static string BuildSessionJsonPath(string experimentDataRootPath, string sceneId, string sessionId)
        {
            return Path.Combine(
                BuildTaskInteractionRootPath(experimentDataRootPath),
                $"{TaskInteractionPrefix}-{SanitizeFileToken(sceneId, "unknown")}-{SanitizeFileToken(sessionId, "unknown")}.json");
        }

        public static string BuildSessionCsvPath(string experimentDataRootPath, string sceneId, string sessionId)
        {
            return Path.Combine(
                BuildTaskInteractionRootPath(experimentDataRootPath),
                $"{TaskInteractionPrefix}-{SanitizeFileToken(sceneId, "unknown")}-{SanitizeFileToken(sessionId, "unknown")}.csv");
        }

        public static string BuildLatestJsonPath(string experimentDataRootPath, string sceneId)
        {
            return Path.Combine(
                BuildTaskInteractionRootPath(experimentDataRootPath),
                $"{TaskInteractionPrefix}-{SanitizeFileToken(sceneId, "unknown")}-latest.json");
        }

        public static string BuildLatestCsvPath(string experimentDataRootPath, string sceneId)
        {
            return Path.Combine(
                BuildTaskInteractionRootPath(experimentDataRootPath),
                $"{TaskInteractionPrefix}-{SanitizeFileToken(sceneId, "unknown")}-latest.csv");
        }

        public static void SaveSession(string experimentDataRootPath, GazeTaskInteractionSession session)
        {
            if (string.IsNullOrWhiteSpace(experimentDataRootPath))
            {
                throw new ArgumentException("Experiment data root path is required.", nameof(experimentDataRootPath));
            }

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            EnsureSessionFields(session);

            string rootPath = BuildTaskInteractionRootPath(experimentDataRootPath);
            string json = JsonUtility.ToJson(session, prettyPrint: true);
            string csv = BuildCsv(session);
            SessionArtifactPersistenceUtility.WriteJsonCsvArtifacts(
                rootPath,
                json,
                csv,
                BuildSessionJsonPath(experimentDataRootPath, session.SceneId, session.SessionId),
                BuildSessionCsvPath(experimentDataRootPath, session.SceneId, session.SessionId),
                BuildLatestJsonPath(experimentDataRootPath, session.SceneId),
                BuildLatestCsvPath(experimentDataRootPath, session.SceneId));
        }

        private static void EnsureSessionFields(GazeTaskInteractionSession session)
        {
            if (string.IsNullOrWhiteSpace(session.SessionId))
            {
                session.SessionId = GazeDepthPersistence.CreateSessionIdUtc();
            }

            if (string.IsNullOrWhiteSpace(session.SceneId))
            {
                session.SceneId = "UnknownScene";
            }

            var records = session.Records ?? Array.Empty<GazeTaskInteractionRecord>();
            int successCount = 0;
            int failureCount = 0;
            for (int index = 0; index < records.Length; index += 1)
            {
                if (records[index] != null && records[index].Success)
                {
                    successCount += 1;
                }
                else
                {
                    failureCount += 1;
                }
            }

            session.RecordCount = records.Length;
            session.SuccessCount = successCount;
            session.FailureCount = failureCount;
            session.SavedAtUtc = DateTime.UtcNow.ToString("O");
        }

        private static string BuildCsv(GazeTaskInteractionSession session)
        {
            StringBuilder builder = new();
            builder.AppendLine("SessionId,SceneId,EventIndex,TaskType,StartedAtUtc,CompletedAtUtc,ConfirmedPageId,PreviousMainPageId,NewMainPageId,LogoPageId,OpenedAtUtc,ClosedByPageId,ClosedAtUtc,MatchingMode,OpenMatchingMode,CloseMatchingMode,InputSource,HasPredictedDepth,PredictedRayDistance,HasOpenPredictedDepth,OpenPredictedRayDistance,HasClosePredictedDepth,ClosePredictedRayDistance,PredictedLayerId,SystemHitPageId,PreviewPageId,Success,FailureMarkedByUser,FeedbackSource,FeedbackTimestampUtc");

            var records = session.Records ?? Array.Empty<GazeTaskInteractionRecord>();
            for (int index = 0; index < records.Length; index += 1)
            {
                var record = records[index];
                if (record == null)
                {
                    continue;
                }

                AppendCsv(builder, session.SessionId);
                AppendCsv(builder, record.SceneId);
                AppendCsv(builder, record.EventIndex);
                AppendCsv(builder, record.TaskType);
                AppendCsv(builder, record.StartedAtUtc);
                AppendCsv(builder, record.CompletedAtUtc);
                AppendCsv(builder, record.ConfirmedPageId);
                AppendCsv(builder, record.PreviousMainPageId);
                AppendCsv(builder, record.NewMainPageId);
                AppendCsv(builder, record.LogoPageId);
                AppendCsv(builder, record.OpenedAtUtc);
                AppendCsv(builder, record.ClosedByPageId);
                AppendCsv(builder, record.ClosedAtUtc);
                AppendCsv(builder, record.MatchingMode);
                AppendCsv(builder, record.OpenMatchingMode);
                AppendCsv(builder, record.CloseMatchingMode);
                AppendCsv(builder, record.InputSource);
                AppendCsv(builder, record.HasPredictedDepth);
                AppendCsv(builder, record.PredictedRayDistance);
                AppendCsv(builder, record.HasOpenPredictedDepth);
                AppendCsv(builder, record.OpenPredictedRayDistance);
                AppendCsv(builder, record.HasClosePredictedDepth);
                AppendCsv(builder, record.ClosePredictedRayDistance);
                AppendCsv(builder, record.PredictedLayerId);
                AppendCsv(builder, record.SystemHitPageId);
                AppendCsv(builder, record.PreviewPageId);
                AppendCsv(builder, record.Success);
                AppendCsv(builder, record.FailureMarkedByUser);
                AppendCsv(builder, record.FeedbackSource);
                AppendCsv(builder, record.FeedbackTimestampUtc, endOfLine: true);
            }

            return builder.ToString();
        }

    }
}
