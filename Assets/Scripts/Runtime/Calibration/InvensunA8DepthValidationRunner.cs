using System;
using System.Collections;
using System.Collections.Generic;
using ProjectGaze.Gaze;
using ProjectGaze.Gaze.Depth;
using UnityEngine;

namespace ProjectGaze.Calibration
{
    internal sealed class InvensunA8DepthValidationRunner
    {
        private readonly TryResolveDepthCalibrationRayCameraDelegate tryResolveRayCamera;
        private readonly float sampleWindowSeconds;
        private readonly int minimumSamples;
        private readonly float betweenTargetsDelaySeconds;
        private readonly TryGetDepthCalibrationSampleDelegate tryGetSample;
        private readonly Action ensureVisual;
        private readonly Action<bool> setMarkerVisible;
        private readonly Action<Vector3> setMarkerPosition;
        private readonly Action<float> setMarkerScale;
        private readonly Action orientMarkerToCamera;
        private readonly Func<Vector3, IEnumerator> animateMarkerMove;
        private readonly Func<IEnumerator> animateMarkerScaleCue;
        private readonly Action<int> setCurrentTargetIndex;
        private readonly Action<int> setCapturedRecordCount;
        private readonly Action<string> setStatusMessage;
        private readonly IGazeDepthEstimator depthEstimator;

        public InvensunA8DepthValidationRunner(
            TryResolveDepthCalibrationRayCameraDelegate tryResolveRayCamera,
            float sampleWindowSeconds,
            int minimumSamples,
            float betweenTargetsDelaySeconds,
            TryGetDepthCalibrationSampleDelegate tryGetSample,
            Action ensureVisual,
            Action<bool> setMarkerVisible,
            Action<Vector3> setMarkerPosition,
            Action<float> setMarkerScale,
            Action orientMarkerToCamera,
            Func<Vector3, IEnumerator> animateMarkerMove,
            Func<IEnumerator> animateMarkerScaleCue,
            Action<int> setCurrentTargetIndex,
            Action<int> setCapturedRecordCount,
            Action<string> setStatusMessage,
            IGazeDepthEstimator depthEstimator)
        {
            this.tryResolveRayCamera = tryResolveRayCamera;
            this.sampleWindowSeconds = sampleWindowSeconds;
            this.minimumSamples = minimumSamples;
            this.betweenTargetsDelaySeconds = betweenTargetsDelaySeconds;
            this.tryGetSample = tryGetSample;
            this.ensureVisual = ensureVisual;
            this.setMarkerVisible = setMarkerVisible;
            this.setMarkerPosition = setMarkerPosition;
            this.setMarkerScale = setMarkerScale;
            this.orientMarkerToCamera = orientMarkerToCamera;
            this.animateMarkerMove = animateMarkerMove;
            this.animateMarkerScaleCue = animateMarkerScaleCue;
            this.setCurrentTargetIndex = setCurrentTargetIndex;
            this.setCapturedRecordCount = setCapturedRecordCount;
            this.setStatusMessage = setStatusMessage;
            this.depthEstimator = depthEstimator;
        }

        public bool Succeeded { get; private set; }

        public string FailureMessage { get; private set; }

        public GazeDepthContinuousValidationSession Session { get; private set; }

        public IEnumerator Run()
        {
            Succeeded = false;
            FailureMessage = null;
            Session = null;

            if (depthEstimator == null || !depthEstimator.IsAvailable)
            {
                FailureMessage = "Depth validation requires a trained depth estimator.";
                yield break;
            }

            setStatusMessage?.Invoke("Preparing continuous depth validation marker...");
            ensureVisual?.Invoke();
            setMarkerVisible?.Invoke(true);
            setCapturedRecordCount?.Invoke(0);
            setCurrentTargetIndex?.Invoke(-1);

            if (!TryResolveTargetWorldPoint(GazeDepthContinuousValidationTargetLibrary.GetTarget(0), out Vector3 initialWorldPoint, out string initialFailureReason))
            {
                setMarkerVisible?.Invoke(false);
                FailureMessage = initialFailureReason;
                yield break;
            }

            setMarkerPosition?.Invoke(initialWorldPoint);
            setMarkerScale?.Invoke(1f);
            orientMarkerToCamera?.Invoke();
            setStatusMessage?.Invoke("Continuous depth validation is ready. Keep your gaze fixed on each injected-depth marker.");

            var records = new List<GazeDepthContinuousValidationRecord>();

            for (int index = 0; index < GazeDepthContinuousValidationTargetLibrary.TargetCount; index += 1)
            {
                setCurrentTargetIndex?.Invoke(index);
                var target = GazeDepthContinuousValidationTargetLibrary.GetTarget(index);
                if (!TryResolveTargetWorldPoint(target, out Vector3 targetWorldPoint, out string targetFailureReason))
                {
                    setMarkerVisible?.Invoke(false);
                    FailureMessage = targetFailureReason;
                    yield break;
                }

                if (!TryResolveRayCamera(out Camera rayCamera, out string cameraFailureReason))
                {
                    setMarkerVisible?.Invoke(false);
                    FailureMessage = cameraFailureReason;
                    yield break;
                }

                setStatusMessage?.Invoke($"Validation depth {index + 1}/{GazeDepthContinuousValidationTargetLibrary.TargetCount}: moving marker...");

                if (index > 0 && animateMarkerMove != null)
                {
                    yield return animateMarkerMove(targetWorldPoint);
                }

                if (animateMarkerScaleCue != null)
                {
                    yield return animateMarkerScaleCue();
                }

                int targetRecordCount = 0;
                long lastAcceptedTimestamp = long.MinValue;
                float elapsedSeconds = 0f;
                string lastRejectionReason = "No validation samples attempted yet.";

                while (elapsedSeconds < sampleWindowSeconds)
                {
                    string rejectionReason = null;
                    if (tryGetSample != null &&
                        tryGetSample(target.ViewportPoint, out var binocularSample, out rejectionReason))
                    {
                        if (binocularSample.Timestamp == lastAcceptedTimestamp)
                        {
                            lastRejectionReason = "A8 callback sample was a duplicate timestamp.";
                        }
                        else if (depthEstimator.TryPredictDepth(rayCamera, target.ViewportPoint, binocularSample, out var prediction) &&
                                 prediction.IsValid)
                        {
                            lastAcceptedTimestamp = binocularSample.Timestamp;
                            records.Add(GazeDepthMeasurementRecordFactory.BuildContinuousValidationRecord(
                                index,
                                target.ViewportPoint,
                                target.RayDistance,
                                prediction.RayDistance,
                                binocularSample.Timestamp));
                            targetRecordCount += 1;
                            setCapturedRecordCount?.Invoke(records.Count);
                            lastRejectionReason = "Latest validation sample accepted.";
                        }
                        else
                        {
                            lastRejectionReason = "Depth estimator could not predict a valid ray distance for the latest sample.";
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(rejectionReason))
                    {
                        lastRejectionReason = rejectionReason;
                    }

                    setStatusMessage?.Invoke(
                        $"Validation depth {index + 1}/{GazeDepthContinuousValidationTargetLibrary.TargetCount} ({target.RayDistance:0.0}): valid predictions {targetRecordCount}/{minimumSamples}. {lastRejectionReason}");
                    elapsedSeconds += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (targetRecordCount < minimumSamples)
                {
                    setMarkerVisible?.Invoke(false);
                    FailureMessage = $"Depth validation target {index + 1} collected {targetRecordCount} predictions; required {minimumSamples}. Last rejection: {lastRejectionReason}";
                    yield break;
                }

                yield return new WaitForSecondsRealtime(betweenTargetsDelaySeconds);
            }

            setMarkerVisible?.Invoke(false);
            Session = new GazeDepthContinuousValidationSession
            {
                SessionId = GazeDepthPersistence.CreateSessionIdUtc(),
                TruthSource = "InjectedRayDistance",
                ViewportLayout = "DepthProfileInterpolatedY",
                DepthLayerProfileVersion = GazeDepthLayerProfile.SymmetricSevenV1ProfileVersion,
                TargetViewportX = GazeDepthContinuousValidationTargetLibrary.ViewportX,
                TargetViewportY = -1f,
                MinimumRayDistance = GazeDepthContinuousValidationTargetLibrary.MinimumRayDistance,
                MaximumRayDistance = GazeDepthContinuousValidationTargetLibrary.MaximumRayDistance,
                RayDistanceStep = GazeDepthContinuousValidationTargetLibrary.RayDistanceStep,
                TargetCount = GazeDepthContinuousValidationTargetLibrary.TargetCount,
                SampleWindowSeconds = sampleWindowSeconds,
                MinimumSamplesPerTarget = minimumSamples,
                RecordCount = records.Count,
                SavedAtUtc = DateTime.UtcNow.ToString("O"),
                Records = records.ToArray()
            };
            Succeeded = true;
        }

        private bool TryResolveTargetWorldPoint(
            GazeDepthCalibrationTarget target,
            out Vector3 worldPoint,
            out string failureReason)
        {
            worldPoint = default;
            failureReason = null;

            if (!TryResolveRayCamera(out Camera rayCamera, out failureReason))
            {
                return false;
            }

            if (!GazeViewportPointUtility.TryBuildWorldRay(rayCamera, target.ViewportPoint, out Ray worldRay))
            {
                failureReason = "Depth validation ray camera produced an invalid viewport ray.";
                return false;
            }

            worldPoint = worldRay.GetPoint(target.RayDistance);
            if (!GazeViewportPointUtility.IsFiniteVector3(worldPoint))
            {
                failureReason = "Depth validation target resolved to a non-finite world position.";
                return false;
            }

            return true;
        }

        private bool TryResolveRayCamera(out Camera rayCamera, out string failureReason)
        {
            failureReason = null;
            if (tryResolveRayCamera != null &&
                tryResolveRayCamera(out rayCamera, out failureReason))
            {
                return true;
            }

            rayCamera = null;
            failureReason = string.IsNullOrWhiteSpace(failureReason)
                ? "Depth validation ray camera is unavailable."
                : failureReason;
            return false;
        }
    }
}
