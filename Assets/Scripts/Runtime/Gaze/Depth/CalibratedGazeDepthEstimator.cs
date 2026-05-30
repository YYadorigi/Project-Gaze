using ProjectGaze.Gaze.Providers;
using UnityEngine;

namespace ProjectGaze.Gaze.Depth
{
    public interface IGazeDepthEstimator
    {
        bool IsAvailable { get; }

        bool TryPredictDepth(
            Camera targetCamera,
            Vector2 normalizedViewportPoint,
            in BinocularGazeSample sample,
            out GazeDepthPrediction prediction);
    }

    public sealed class CalibratedGazeDepthEstimator : IGazeDepthEstimator
    {
        private readonly GazeDepthModelBundle model;

        public CalibratedGazeDepthEstimator(GazeDepthModelBundle model)
        {
            this.model = model;
        }

        public bool IsAvailable =>
            model != null &&
            model.BaselineModel != null &&
            model.BaselineModel.IsTrained;

        public bool TryPredictDepth(
            Camera targetCamera,
            Vector2 normalizedViewportPoint,
            in BinocularGazeSample sample,
            out GazeDepthPrediction prediction)
        {
            prediction = default;

            if (!IsAvailable ||
                targetCamera == null ||
                !GazeDepthFeatureExtractor.TryBuildFeatureVector(sample, out var features) ||
                !DisparityDepthEstimator.TryPredict(features, model.BaselineModel, out float predictedRayDistance))
            {
                return false;
            }

            if (model.SvrModel != null && model.SvrModel.IsTrained)
            {
                predictedRayDistance += LinearSvrDepthEstimator.PredictResidual(features, model.SvrModel);
            }

            predictedRayDistance = Mathf.Clamp(
                predictedRayDistance,
                model.BaselineModel.MinimumRayDistance,
                model.BaselineModel.MaximumRayDistance);

            var worldRay = GazeViewportPointUtility.BuildWorldRay(targetCamera, normalizedViewportPoint);
            prediction = new GazeDepthPrediction(true, predictedRayDistance, worldRay.GetPoint(predictedRayDistance));
            return true;
        }

        public static bool TryLoadFromPersistentCalibration(out CalibratedGazeDepthEstimator estimator)
        {
            estimator = null;
            string calibrationRootPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationPersistenceRootPath(Application.persistentDataPath);
            if (!GazeDepthPersistence.TryLoadModel(calibrationRootPath, out var model))
            {
                return false;
            }

            estimator = new CalibratedGazeDepthEstimator(model);
            return estimator.IsAvailable;
        }
    }
}
