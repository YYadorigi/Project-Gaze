using UnityEngine;
using ProjectGaze.Gaze;
using ProjectGaze.Gaze.Depth;

namespace ProjectGaze.Gaze.Providers
{
    public sealed class InvensunA8GazeTrackingProvider : MonoBehaviour, IGazeTrackingProvider
    {
        private InvensunA8DeviceRuntime deviceRuntime;
        private Camera targetCamera;
        private IGazeDepthEstimator depthEstimator;

        public string ProviderName => "7Invensun A8";

        public bool IsAvailable => deviceRuntime != null && deviceRuntime.IsConnected;

        public void Initialize()
        {
            deviceRuntime = GetComponent<InvensunA8DeviceRuntime>() ?? gameObject.AddComponent<InvensunA8DeviceRuntime>();
            deviceRuntime.Initialize();
            CalibratedGazeDepthEstimator.TryLoadFromPersistentCalibration(out var calibratedDepthEstimator);
            depthEstimator = calibratedDepthEstimator;
        }

        public void ConfigureTargetCamera(Camera targetCamera)
        {
            this.targetCamera = targetCamera;
        }

        public bool TryGetSample(out GazeTrackingSample sample)
        {
            sample = default;

            if (!IsAvailable || deviceRuntime == null || !deviceRuntime.TryGetLatestSample(out var data))
            {
                return false;
            }

            if (!InvensunA8RecommendedGazeUtility.TryResolveViewportPoint(
                    data,
                    targetCamera,
                    out var viewportPoint))
            {
                return false;
            }

            bool hasPredictedWorldPoint = false;
            Vector3 predictedWorldPoint = default;
            float predictedRayDistance = 0f;
            if (depthEstimator != null &&
                depthEstimator.IsAvailable &&
                data.BinocularGaze.HasAnySignal &&
                depthEstimator.TryPredictDepth(targetCamera, viewportPoint, data.BinocularGaze, out var depthPrediction))
            {
                hasPredictedWorldPoint = depthPrediction.IsValid;
                predictedWorldPoint = depthPrediction.WorldPoint;
                predictedRayDistance = depthPrediction.RayDistance;
            }

            sample = new GazeTrackingSample(
                true,
                viewportPoint,
                data.SystemTimestamp,
                data.LeftGazePointValid,
                data.RightGazePointValid,
                hasPredictedWorldPoint,
                predictedWorldPoint,
                predictedRayDistance);
            return true;
        }

    }
}
