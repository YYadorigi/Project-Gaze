using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using static ProjectGaze.Gaze.CsvPersistenceUtility;

namespace ProjectGaze.Gaze.Depth
{
    public static class GazeDepthPersistence
    {
        private const string DatasetFileName = "depth-calibration-dataset.json";
        private const string ModelFileName = "depth-calibration-model.json";
        private const string ExperimentDataDirectoryName = "ExperimentData";
        private const string MeasurementsDirectoryName = "depth-measurements";
        private const string ContinuousValidationPrefix = "continuous-depth-validation";
        private const string RuntimeLayerPrefix = "runtime-depth-layer";

        public static string BuildDatasetPath(string calibrationRootPath)
        {
            return Path.Combine(calibrationRootPath ?? string.Empty, DatasetFileName);
        }

        public static string BuildModelPath(string calibrationRootPath)
        {
            return Path.Combine(calibrationRootPath ?? string.Empty, ModelFileName);
        }

        public static string BuildExperimentDataRootPath(string projectRootPath)
        {
            return Path.Combine(projectRootPath ?? string.Empty, ExperimentDataDirectoryName);
        }

        public static string BuildMeasurementRootPath(string experimentDataRootPath)
        {
            return Path.Combine(experimentDataRootPath ?? string.Empty, MeasurementsDirectoryName);
        }

        public static string ResolveProjectRootPath(string applicationDataPath, string currentDirectory = null)
        {
            if (!string.IsNullOrWhiteSpace(applicationDataPath))
            {
                try
                {
                    var dataDirectory = new DirectoryInfo(applicationDataPath);
                    if (string.Equals(dataDirectory.Name, "Assets", StringComparison.OrdinalIgnoreCase) &&
                        dataDirectory.Parent != null)
                    {
                        return dataDirectory.Parent.FullName;
                    }

                    if (dataDirectory.Name.EndsWith("_Data", StringComparison.OrdinalIgnoreCase) &&
                        dataDirectory.Parent != null)
                    {
                        return dataDirectory.Parent.FullName;
                    }
                }
                catch
                {
                }
            }

            return string.IsNullOrWhiteSpace(currentDirectory)
                ? Environment.CurrentDirectory
                : currentDirectory;
        }

        public static string ResolveExperimentDataRootPath(string applicationDataPath, string currentDirectory = null)
        {
            return BuildExperimentDataRootPath(ResolveProjectRootPath(applicationDataPath, currentDirectory));
        }

        public static string ResolveUnityExperimentDataRootPath()
        {
            return ResolveExperimentDataRootPath(Application.dataPath, Environment.CurrentDirectory);
        }

        public static string BuildContinuousValidationJsonPath(string experimentDataRootPath, string sessionId)
        {
            return Path.Combine(
                BuildMeasurementRootPath(experimentDataRootPath),
                $"{ContinuousValidationPrefix}-{SanitizeFileToken(sessionId, CreateSessionIdUtc())}.json");
        }

        public static string BuildContinuousValidationCsvPath(string experimentDataRootPath, string sessionId)
        {
            return Path.Combine(
                BuildMeasurementRootPath(experimentDataRootPath),
                $"{ContinuousValidationPrefix}-{SanitizeFileToken(sessionId, CreateSessionIdUtc())}.csv");
        }

        public static string BuildContinuousValidationLatestJsonPath(string experimentDataRootPath)
        {
            return Path.Combine(BuildMeasurementRootPath(experimentDataRootPath), $"{ContinuousValidationPrefix}-latest.json");
        }

        public static string BuildContinuousValidationLatestCsvPath(string experimentDataRootPath)
        {
            return Path.Combine(BuildMeasurementRootPath(experimentDataRootPath), $"{ContinuousValidationPrefix}-latest.csv");
        }

        public static string BuildRuntimeLayerJsonPath(string experimentDataRootPath, string sceneId, string sessionId)
        {
            return Path.Combine(
                BuildMeasurementRootPath(experimentDataRootPath),
                $"{RuntimeLayerPrefix}-{SanitizeFileToken(sceneId, "unknown")}-{SanitizeFileToken(sessionId, CreateSessionIdUtc())}.json");
        }

        public static string BuildRuntimeLayerCsvPath(string experimentDataRootPath, string sceneId, string sessionId)
        {
            return Path.Combine(
                BuildMeasurementRootPath(experimentDataRootPath),
                $"{RuntimeLayerPrefix}-{SanitizeFileToken(sceneId, "unknown")}-{SanitizeFileToken(sessionId, CreateSessionIdUtc())}.csv");
        }

        public static string BuildRuntimeLayerLatestJsonPath(string experimentDataRootPath, string sceneId)
        {
            return Path.Combine(
                BuildMeasurementRootPath(experimentDataRootPath),
                $"{RuntimeLayerPrefix}-{SanitizeFileToken(sceneId, "unknown")}-latest.json");
        }

        public static string BuildRuntimeLayerLatestCsvPath(string experimentDataRootPath, string sceneId)
        {
            return Path.Combine(
                BuildMeasurementRootPath(experimentDataRootPath),
                $"{RuntimeLayerPrefix}-{SanitizeFileToken(sceneId, "unknown")}-latest.csv");
        }

        public static string CreateSessionIdUtc()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        }

        public static void Save(string calibrationRootPath, GazeDepthCalibrationDataset dataset, GazeDepthModelBundle model)
        {
            if (string.IsNullOrWhiteSpace(calibrationRootPath))
            {
                throw new ArgumentException("Calibration root path is required.", nameof(calibrationRootPath));
            }

            if (dataset == null)
            {
                throw new ArgumentNullException(nameof(dataset));
            }

            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            Directory.CreateDirectory(calibrationRootPath);
            File.WriteAllText(BuildDatasetPath(calibrationRootPath), JsonUtility.ToJson(dataset, prettyPrint: true));
            File.WriteAllText(BuildModelPath(calibrationRootPath), JsonUtility.ToJson(model, prettyPrint: true));
        }

        public static void SaveContinuousValidationSession(
            string experimentDataRootPath,
            GazeDepthContinuousValidationSession session)
        {
            if (string.IsNullOrWhiteSpace(experimentDataRootPath))
            {
                throw new ArgumentException("Experiment data root path is required.", nameof(experimentDataRootPath));
            }

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            EnsureSessionId(session);
            session.RecordCount = session.Records?.Length ?? 0;
            session.SavedAtUtc = DateTime.UtcNow.ToString("O");

            WriteMeasurementArtifacts(
                experimentDataRootPath,
                JsonUtility.ToJson(session, prettyPrint: true),
                BuildContinuousValidationCsv(session),
                BuildContinuousValidationJsonPath(experimentDataRootPath, session.SessionId),
                BuildContinuousValidationCsvPath(experimentDataRootPath, session.SessionId),
                BuildContinuousValidationLatestJsonPath(experimentDataRootPath),
                BuildContinuousValidationLatestCsvPath(experimentDataRootPath));
        }

        public static void SaveRuntimeLayerSession(string experimentDataRootPath, GazeDepthRuntimeLayerSession session)
        {
            if (string.IsNullOrWhiteSpace(experimentDataRootPath))
            {
                throw new ArgumentException("Experiment data root path is required.", nameof(experimentDataRootPath));
            }

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            EnsureSessionId(session);
            if (string.IsNullOrWhiteSpace(session.SceneId))
            {
                session.SceneId = "UnknownScene";
            }

            session.RecordCount = session.Records?.Length ?? 0;
            session.SavedAtUtc = DateTime.UtcNow.ToString("O");

            WriteMeasurementArtifacts(
                experimentDataRootPath,
                JsonUtility.ToJson(session, prettyPrint: true),
                BuildRuntimeLayerCsv(session),
                BuildRuntimeLayerJsonPath(experimentDataRootPath, session.SceneId, session.SessionId),
                BuildRuntimeLayerCsvPath(experimentDataRootPath, session.SceneId, session.SessionId),
                BuildRuntimeLayerLatestJsonPath(experimentDataRootPath, session.SceneId),
                BuildRuntimeLayerLatestCsvPath(experimentDataRootPath, session.SceneId));
        }

        public static bool TryLoadModel(string calibrationRootPath, out GazeDepthModelBundle model)
        {
            model = null;
            string modelPath = BuildModelPath(calibrationRootPath);

            if (!File.Exists(modelPath))
            {
                return false;
            }

            try
            {
                model = JsonUtility.FromJson<GazeDepthModelBundle>(File.ReadAllText(modelPath));
            }
            catch
            {
                model = null;
            }

            return model != null &&
                   model.BaselineModel != null &&
                   model.BaselineModel.IsTrained;
        }

        private static void EnsureSessionId(GazeDepthContinuousValidationSession session)
        {
            if (string.IsNullOrWhiteSpace(session.SessionId))
            {
                session.SessionId = CreateSessionIdUtc();
            }
        }

        private static void EnsureSessionId(GazeDepthRuntimeLayerSession session)
        {
            if (string.IsNullOrWhiteSpace(session.SessionId))
            {
                session.SessionId = CreateSessionIdUtc();
            }
        }

        private static void WriteMeasurementArtifacts(
            string experimentDataRootPath,
            string json,
            string csv,
            string sessionJsonPath,
            string sessionCsvPath,
            string latestJsonPath,
            string latestCsvPath)
        {
            SessionArtifactPersistenceUtility.WriteJsonCsvArtifacts(
                BuildMeasurementRootPath(experimentDataRootPath),
                json,
                csv,
                sessionJsonPath,
                sessionCsvPath,
                latestJsonPath,
                latestCsvPath);
        }

        private static string BuildContinuousValidationCsv(GazeDepthContinuousValidationSession session)
        {
            StringBuilder builder = new();
            builder.AppendLine("SessionId,TruthSource,ViewportLayout,TargetIndex,TargetViewportX,TargetViewportY,InjectedRayDistance,PredictedRayDistance,SignedError,AbsoluteError,InjectedNearestLayerId,PredictedNearestLayerId,InjectedWithinLayerTolerance,PredictedWithinLayerTolerance,LayerMatched,SampleTimestamp,RecordedAtUtc");

            var records = session.Records ?? Array.Empty<GazeDepthContinuousValidationRecord>();
            for (int index = 0; index < records.Length; index += 1)
            {
                var record = records[index];
                AppendCsv(builder, session.SessionId);
                AppendCsv(builder, session.TruthSource);
                AppendCsv(builder, session.ViewportLayout);
                AppendCsv(builder, record.TargetIndex);
                AppendCsv(builder, record.TargetViewportX);
                AppendCsv(builder, record.TargetViewportY);
                AppendCsv(builder, record.InjectedRayDistance);
                AppendCsv(builder, record.PredictedRayDistance);
                AppendCsv(builder, record.SignedError);
                AppendCsv(builder, record.AbsoluteError);
                AppendCsv(builder, record.InjectedNearestLayerId);
                AppendCsv(builder, record.PredictedNearestLayerId);
                AppendCsv(builder, record.InjectedWithinLayerTolerance);
                AppendCsv(builder, record.PredictedWithinLayerTolerance);
                AppendCsv(builder, record.LayerMatched);
                AppendCsv(builder, record.SampleTimestamp);
                AppendCsv(builder, record.RecordedAtUtc, endOfLine: true);
            }

            return builder.ToString();
        }

        private static string BuildRuntimeLayerCsv(GazeDepthRuntimeLayerSession session)
        {
            StringBuilder builder = new();
            builder.AppendLine("SessionId,SceneId,TruthSource,RecordIndex,RecordedAtUtc,SampleTimestamp,InputSource,MatchingMode,GazeViewportX,GazeViewportY,HasPredictedDepth,PredictedRayDistance,PredictedNearestLayerId,PredictedNearestLayerRayDistance,PredictedNearestLayerDistanceError,PredictedNearestLayerWithinTolerance,HasSystemHit,SystemHitPageId,SystemHitDepthLayerId,SystemHitDepthLayerRayDistance,SystemHitDepthLayerTolerance,HasReferenceDepth,SignedDepthError,AbsoluteDepthError,LayerMatched,PreviewPageId,ConfirmedPageId,ViewportOnlyHasHit,ViewportOnlyPageId,ViewportOnlyDepthLayerId,ContinuousWorldPointHasHit,ContinuousWorldPointPageId,ContinuousWorldPointDepthLayerId,DiscreteDepthLayerHasHit,DiscreteDepthLayerPageId,DiscreteDepthLayerDepthLayerId");

            var records = session.Records ?? Array.Empty<GazeDepthRuntimeLayerRecord>();
            for (int index = 0; index < records.Length; index += 1)
            {
                var record = records[index];
                AppendCsv(builder, session.SessionId);
                AppendCsv(builder, record.SceneId);
                AppendCsv(builder, session.TruthSource);
                AppendCsv(builder, record.RecordIndex);
                AppendCsv(builder, record.RecordedAtUtc);
                AppendCsv(builder, record.SampleTimestamp);
                AppendCsv(builder, record.InputSource);
                AppendCsv(builder, record.MatchingMode);
                AppendCsv(builder, record.GazeViewportX);
                AppendCsv(builder, record.GazeViewportY);
                AppendCsv(builder, record.HasPredictedDepth);
                AppendCsv(builder, record.PredictedRayDistance);
                AppendCsv(builder, record.PredictedNearestLayerId);
                AppendCsv(builder, record.PredictedNearestLayerRayDistance);
                AppendCsv(builder, record.PredictedNearestLayerDistanceError);
                AppendCsv(builder, record.PredictedNearestLayerWithinTolerance);
                AppendCsv(builder, record.HasSystemHit);
                AppendCsv(builder, record.SystemHitPageId);
                AppendCsv(builder, record.SystemHitDepthLayerId);
                AppendCsv(builder, record.SystemHitDepthLayerRayDistance);
                AppendCsv(builder, record.SystemHitDepthLayerTolerance);
                AppendCsv(builder, record.HasReferenceDepth);
                AppendCsv(builder, record.SignedDepthError);
                AppendCsv(builder, record.AbsoluteDepthError);
                AppendCsv(builder, record.LayerMatched);
                AppendCsv(builder, record.PreviewPageId);
                AppendCsv(builder, record.ConfirmedPageId);
                AppendCsv(builder, record.ViewportOnlyHasHit);
                AppendCsv(builder, record.ViewportOnlyPageId);
                AppendCsv(builder, record.ViewportOnlyDepthLayerId);
                AppendCsv(builder, record.ContinuousWorldPointHasHit);
                AppendCsv(builder, record.ContinuousWorldPointPageId);
                AppendCsv(builder, record.ContinuousWorldPointDepthLayerId);
                AppendCsv(builder, record.DiscreteDepthLayerHasHit);
                AppendCsv(builder, record.DiscreteDepthLayerPageId);
                AppendCsv(builder, record.DiscreteDepthLayerDepthLayerId, endOfLine: true);
            }

            return builder.ToString();
        }

    }
}
