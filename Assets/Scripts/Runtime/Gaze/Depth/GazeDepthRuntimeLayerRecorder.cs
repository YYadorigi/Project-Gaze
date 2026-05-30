using System;
using System.Collections.Generic;
using ProjectGaze.Gaze.Providers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectGaze.Gaze.Depth
{
    public sealed class GazeDepthRuntimeLayerRecorder : MonoBehaviour
    {
        private const float AutoFlushIntervalSeconds = 5.0f;

        private readonly TargetMatcherPipeline matcherPipeline = new();
        private readonly List<GazeDepthRuntimeLayerRecord> records = new();

        private Camera targetCamera;
        private SpatialPageRegistry pageRegistry;
        private GazeInteractionController gazeController;
        private GazeDepthRuntimeLayerSession session;
        private long lastRecordedSampleTimestamp = long.MinValue;
        private float nextFlushTime;
        private int lastSavedRecordCount;

        public void Initialize(
            Camera targetCamera,
            SpatialPageRegistry pageRegistry,
            GazeInteractionController gazeController,
            string sceneId = null)
        {
            this.targetCamera = targetCamera;
            this.pageRegistry = pageRegistry;
            this.gazeController = gazeController;
            records.Clear();
            lastRecordedSampleTimestamp = long.MinValue;
            lastSavedRecordCount = 0;
            nextFlushTime = Time.unscaledTime + AutoFlushIntervalSeconds;

            session = new GazeDepthRuntimeLayerSession
            {
                SessionId = GazeDepthPersistence.CreateSessionIdUtc(),
                SceneId = string.IsNullOrWhiteSpace(sceneId)
                    ? SceneManager.GetActiveScene().name
                    : sceneId,
                TruthSource = "RuntimeSystemHitDepthLayer",
                DepthLayerProfileVersion = GazeDepthLayerProfile.SymmetricSevenV1ProfileVersion,
                StartedAtUtc = DateTime.UtcNow.ToString("O"),
                SavedAtUtc = DateTime.UtcNow.ToString("O"),
                RecordCount = 0,
                Records = Array.Empty<GazeDepthRuntimeLayerRecord>()
            };
        }

        private void LateUpdate()
        {
            if (session == null || gazeController == null || !gazeController.HasLastTrackingSample)
            {
                return;
            }

            var sample = gazeController.LastTrackingSample;
            if (!sample.IsValid || sample.Timestamp == lastRecordedSampleTimestamp)
            {
                return;
            }

            lastRecordedSampleTimestamp = sample.Timestamp;
            records.Add(BuildRecord(records.Count, sample, gazeController.LastHitResult));

            if (Time.unscaledTime >= nextFlushTime)
            {
                FlushIfDirty();
                nextFlushTime = Time.unscaledTime + AutoFlushIntervalSeconds;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                FlushIfDirty();
            }
        }

        private void OnDisable()
        {
            FlushIfDirty();
        }

        private void OnDestroy()
        {
            FlushIfDirty();
        }

        private GazeDepthRuntimeLayerRecord BuildRecord(
            int recordIndex,
            in GazeTrackingSample sample,
            in GazeHitResult systemHit)
        {
            string predictedLayerId = null;
            float predictedLayerRayDistance = 0f;
            float predictedLayerDistanceError = 0f;
            bool predictedLayerWithinTolerance = false;
            if (sample.PredictedRayDistance > 0f &&
                GazeDepthLayerResolver.TryResolveNearestLayer(sample.PredictedRayDistance, out var predictedLayer))
            {
                predictedLayerId = predictedLayer.LayerId;
                predictedLayerRayDistance = predictedLayer.RayDistance;
                predictedLayerDistanceError = predictedLayer.DistanceError;
                predictedLayerWithinTolerance = predictedLayer.IsWithinTolerance;
            }

            bool hasReferenceDepth = systemHit.HasHitPage && systemHit.DepthLayerRayDistance > 0f;
            float signedDepthError = hasReferenceDepth && sample.PredictedRayDistance > 0f
                ? sample.PredictedRayDistance - systemHit.DepthLayerRayDistance
                : 0f;
            float absoluteDepthError = hasReferenceDepth && sample.PredictedRayDistance > 0f
                ? Mathf.Abs(signedDepthError)
                : 0f;

            ResolveStrategyHit(GazeDepthMatchingMode.ViewportOnly, sample, out bool viewportHasHit, out string viewportPageId, out string viewportLayerId);
            ResolveStrategyHit(GazeDepthMatchingMode.ContinuousWorldPoint, sample, out bool continuousHasHit, out string continuousPageId, out string continuousLayerId);
            ResolveStrategyHit(GazeDepthMatchingMode.DiscreteDepthLayer, sample, out bool discreteHasHit, out string discretePageId, out string discreteLayerId);

            var snapshot = gazeController.CurrentSnapshot;
            return new GazeDepthRuntimeLayerRecord
            {
                RecordIndex = recordIndex,
                RecordedAtUtc = DateTime.UtcNow.ToString("O"),
                SampleTimestamp = sample.Timestamp,
                SceneId = session.SceneId,
                InputSource = gazeController.ActiveGazeProviderName,
                MatchingMode = gazeController.DepthMatchingMode.ToString(),
                GazeViewportX = sample.NormalizedViewportPoint.x,
                GazeViewportY = sample.NormalizedViewportPoint.y,
                HasPredictedDepth = sample.PredictedRayDistance > 0f,
                PredictedRayDistance = sample.PredictedRayDistance,
                PredictedNearestLayerId = predictedLayerId,
                PredictedNearestLayerRayDistance = predictedLayerRayDistance,
                PredictedNearestLayerDistanceError = predictedLayerDistanceError,
                PredictedNearestLayerWithinTolerance = predictedLayerWithinTolerance,
                HasSystemHit = systemHit.HasHitPage,
                SystemHitPageId = systemHit.PageId,
                SystemHitDepthLayerId = systemHit.DepthLayerId,
                SystemHitDepthLayerRayDistance = systemHit.DepthLayerRayDistance,
                SystemHitDepthLayerTolerance = systemHit.Target != null ? systemHit.Target.DepthLayerTolerance : 0f,
                HasReferenceDepth = hasReferenceDepth,
                SignedDepthError = signedDepthError,
                AbsoluteDepthError = absoluteDepthError,
                LayerMatched = hasReferenceDepth &&
                               !string.IsNullOrWhiteSpace(predictedLayerId) &&
                               string.Equals(systemHit.DepthLayerId, predictedLayerId, StringComparison.Ordinal),
                PreviewPageId = snapshot.PreviewPageId,
                ConfirmedPageId = snapshot.ConfirmedPageId,
                ViewportOnlyHasHit = viewportHasHit,
                ViewportOnlyPageId = viewportPageId,
                ViewportOnlyDepthLayerId = viewportLayerId,
                ContinuousWorldPointHasHit = continuousHasHit,
                ContinuousWorldPointPageId = continuousPageId,
                ContinuousWorldPointDepthLayerId = continuousLayerId,
                DiscreteDepthLayerHasHit = discreteHasHit,
                DiscreteDepthLayerPageId = discretePageId,
                DiscreteDepthLayerDepthLayerId = discreteLayerId
            };
        }

        private void ResolveStrategyHit(
            GazeDepthMatchingMode matchingMode,
            in GazeTrackingSample sample,
            out bool hasHit,
            out string pageId,
            out string depthLayerId)
        {
            hasHit = false;
            pageId = null;
            depthLayerId = null;

            if (targetCamera == null ||
                pageRegistry == null ||
                matcherPipeline == null ||
                !matcherPipeline.TryMatch(targetCamera, pageRegistry.Targets, sample, matchingMode, out var hitResult))
            {
                return;
            }

            hasHit = hitResult.HasHitPage;
            pageId = hitResult.PageId;
            depthLayerId = hitResult.DepthLayerId;
        }

        private void FlushIfDirty()
        {
            if (session == null || records.Count == 0 || records.Count == lastSavedRecordCount)
            {
                return;
            }

            try
            {
                session.Records = records.ToArray();
                session.RecordCount = session.Records.Length;
                session.SavedAtUtc = DateTime.UtcNow.ToString("O");
                string experimentDataRootPath = GazeDepthPersistence.ResolveUnityExperimentDataRootPath();
                GazeDepthPersistence.SaveRuntimeLayerSession(experimentDataRootPath, session);
                lastSavedRecordCount = records.Count;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to save runtime depth layer log: {exception.Message}");
            }
        }
    }
}
