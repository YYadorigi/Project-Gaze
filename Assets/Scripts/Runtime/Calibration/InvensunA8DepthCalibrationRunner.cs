using System;
using System.Collections;
using System.Collections.Generic;
using ProjectGaze.Gaze;
using ProjectGaze.Gaze.Depth;
using UnityEngine;

namespace ProjectGaze.Calibration
{
    internal delegate bool TryResolveDepthCalibrationRayCameraDelegate(out Camera rayCamera, out string reason);

    internal delegate bool TryGetDepthCalibrationSampleDelegate(Vector2 targetViewportPoint, out BinocularGazeSample sample, out string rejectionReason);

    internal sealed class InvensunA8DepthCalibrationRunner
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

        public InvensunA8DepthCalibrationRunner(
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
            Action<string> setStatusMessage)
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
        }

        public bool Succeeded { get; private set; }

        public string FailureMessage { get; private set; }

        public GazeDepthCalibrationDataset Dataset { get; private set; }

        public GazeDepthModelBundle Model { get; private set; }

        public IEnumerator Run()
        {
            Succeeded = false;
            FailureMessage = null;
            Dataset = null;
            Model = null;

            setStatusMessage?.Invoke("Preparing depth calibration marker...");
            ensureVisual?.Invoke();
            setMarkerVisible?.Invoke(true);
            setCapturedRecordCount?.Invoke(0);
            setCurrentTargetIndex?.Invoke(-1);

            if (!TryResolveTargetWorldPoint(GazeDepthCalibrationTargetLibrary.GetTarget(0), out Vector3 initialWorldPoint, out string initialFailureReason))
            {
                setMarkerVisible?.Invoke(false);
                FailureMessage = initialFailureReason;
                yield break;
            }

            setMarkerPosition?.Invoke(initialWorldPoint);
            setMarkerScale?.Invoke(1f);
            orientMarkerToCamera?.Invoke();
            setStatusMessage?.Invoke("Depth calibration marker is ready. Keep your gaze fixed on each target.");

            var records = new List<GazeDepthCalibrationRecord>();

            for (int index = 0; index < GazeDepthCalibrationTargetLibrary.TargetCount; index += 1)
            {
                setCurrentTargetIndex?.Invoke(index);
                var target = GazeDepthCalibrationTargetLibrary.GetTarget(index);
                if (!TryResolveTargetWorldPoint(target, out Vector3 targetWorldPoint, out string targetFailureReason))
                {
                    setMarkerVisible?.Invoke(false);
                    FailureMessage = targetFailureReason;
                    yield break;
                }

                setStatusMessage?.Invoke($"Depth target {index + 1}/{GazeDepthCalibrationTargetLibrary.TargetCount}: moving marker...");

                if (index > 0 && animateMarkerMove != null)
                {
                    yield return animateMarkerMove(targetWorldPoint);
                }

                if (animateMarkerScaleCue != null)
                {
                    yield return animateMarkerScaleCue();
                }

                var accumulator = new BinocularGazeSampleAccumulator();
                float elapsedSeconds = 0f;
                string lastRejectionReason = "No depth samples attempted yet.";
                while (elapsedSeconds < sampleWindowSeconds)
                {
                    string rejectionReason = null;
                    if (tryGetSample != null &&
                        tryGetSample(target.ViewportPoint, out var binocularSample, out rejectionReason))
                    {
                        if (!accumulator.TryAddSample(binocularSample))
                        {
                            lastRejectionReason = "A8 callback sample was a duplicate timestamp or contained no usable signal.";
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(rejectionReason))
                    {
                        lastRejectionReason = rejectionReason;
                    }

                    setStatusMessage?.Invoke(
                        $"Depth target {index + 1}/{GazeDepthCalibrationTargetLibrary.TargetCount}: valid samples {accumulator.SampleCount}/{minimumSamples}. {lastRejectionReason}");
                    elapsedSeconds += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (!TryBuildRecord(accumulator, target, out var record, out string recordFailureReason))
                {
                    setMarkerVisible?.Invoke(false);
                    FailureMessage = $"Depth calibration target {index + 1} did not collect enough stable binocular samples. {recordFailureReason} Last rejection: {lastRejectionReason}";
                    yield break;
                }

                records.Add(record);
                setCapturedRecordCount?.Invoke(records.Count);
                yield return new WaitForSecondsRealtime(betweenTargetsDelaySeconds);
            }

            setMarkerVisible?.Invoke(false);
            Dataset = new GazeDepthCalibrationDataset
            {
                Records = records.ToArray(),
                SavedAtUtc = DateTime.UtcNow.ToString("O")
            };

            var baselineModel = DisparityDepthEstimator.Train(Dataset);
            if (!baselineModel.IsTrained)
            {
                FailureMessage = "Depth calibration baseline estimation failed because disparity samples were not usable.";
                yield break;
            }

            Model = new GazeDepthModelBundle
            {
                BaselineModel = baselineModel,
                SvrModel = LinearSvrDepthEstimator.Train(Dataset, baselineModel),
                TrainingSampleCount = records.Count,
                DepthCalibrationProfileVersion = GazeDepthLayerProfile.SymmetricSevenV1ProfileVersion,
                DepthLayerRayDistances = GazeDepthLayerProfile.BuildSymmetricSevenV1RayDistances(),
                SavedAtUtc = DateTime.UtcNow.ToString("O")
            };
            Succeeded = true;
        }

        private bool TryBuildRecord(
            BinocularGazeSampleAccumulator accumulator,
            GazeDepthCalibrationTarget target,
            out GazeDepthCalibrationRecord record,
            out string failureReason)
        {
            record = null;
            failureReason = null;

            if (accumulator == null)
            {
                failureReason = "No sample accumulator was available.";
                return false;
            }

            if (accumulator.SampleCount < minimumSamples)
            {
                failureReason = $"Captured {accumulator.SampleCount} valid samples; required {minimumSamples}.";
                return false;
            }

            if (!accumulator.TryBuildAverageSample(out var averagedSample))
            {
                failureReason = "Captured samples could not be averaged.";
                return false;
            }

            if (!GazeDepthFeatureExtractor.TryBuildFeatureVector(averagedSample, out var features))
            {
                failureReason = "Averaged binocular sample did not contain the stereo features required for depth estimation.";
                return false;
            }

            record = new GazeDepthCalibrationRecord
            {
                Features = features,
                TargetRayDistance = target.RayDistance,
                TargetViewportX = target.ViewportPoint.x,
                TargetViewportY = target.ViewportPoint.y
            };
            return true;
        }

        private bool TryResolveTargetWorldPoint(
            GazeDepthCalibrationTarget target,
            out Vector3 worldPoint,
            out string failureReason)
        {
            worldPoint = default;
            failureReason = null;

            string cameraFailureReason = null;
            if (tryResolveRayCamera == null ||
                !tryResolveRayCamera(out Camera rayCamera, out cameraFailureReason))
            {
                failureReason = string.IsNullOrWhiteSpace(cameraFailureReason)
                    ? "Depth calibration ray camera is unavailable."
                    : cameraFailureReason;
                return false;
            }

            if (!GazeViewportPointUtility.TryBuildWorldRay(rayCamera, target.ViewportPoint, out Ray worldRay))
            {
                failureReason = "Depth calibration ray camera produced an invalid viewport ray.";
                return false;
            }

            worldPoint = worldRay.GetPoint(target.RayDistance);
            if (!GazeViewportPointUtility.IsFiniteVector3(worldPoint))
            {
                failureReason = "Depth calibration target resolved to a non-finite world position.";
                return false;
            }

            return true;
        }
    }
}
