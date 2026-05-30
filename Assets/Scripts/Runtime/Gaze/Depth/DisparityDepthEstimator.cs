using UnityEngine;

namespace ProjectGaze.Gaze.Depth
{
    public static class DisparityDepthEstimator
    {
        private const float MinimumSupportedDisparity = 0.0001f;

        public static DisparityDepthModel Train(GazeDepthCalibrationDataset dataset)
        {
            var model = new DisparityDepthModel();
            if (dataset?.Records == null || dataset.Records.Length < 2)
            {
                return model;
            }

            float sumX = 0f;
            float sumY = 0f;
            float sumXX = 0f;
            float sumXY = 0f;
            float minimumObservedDisparity = float.PositiveInfinity;
            float minimumRayDistance = float.PositiveInfinity;
            float maximumRayDistance = float.NegativeInfinity;
            int usableSampleCount = 0;

            foreach (var record in dataset.Records)
            {
                if (!TryResolveInverseDisparity(record?.Features, out float inverseDisparity))
                {
                    continue;
                }

                float targetRayDistance = record.TargetRayDistance;
                sumX += inverseDisparity;
                sumY += targetRayDistance;
                sumXX += inverseDisparity * inverseDisparity;
                sumXY += inverseDisparity * targetRayDistance;
                minimumObservedDisparity = Mathf.Min(minimumObservedDisparity, record.Features[GazeDepthFeatureExtractor.DisparityMagnitudeIndex]);
                minimumRayDistance = Mathf.Min(minimumRayDistance, targetRayDistance);
                maximumRayDistance = Mathf.Max(maximumRayDistance, targetRayDistance);
                usableSampleCount += 1;
            }

            if (usableSampleCount < 2)
            {
                return model;
            }

            float denominator = (usableSampleCount * sumXX) - (sumX * sumX);
            if (Mathf.Abs(denominator) <= Mathf.Epsilon)
            {
                return model;
            }

            model.Scale = ((usableSampleCount * sumXY) - (sumX * sumY)) / denominator;
            model.Offset = (sumY - (model.Scale * sumX)) / usableSampleCount;
            model.MinimumDisparity = Mathf.Max(MinimumSupportedDisparity, minimumObservedDisparity * 0.5f);
            model.MinimumRayDistance = minimumRayDistance;
            model.MaximumRayDistance = maximumRayDistance;
            model.IsTrained = true;
            return model;
        }

        public static bool TryPredict(
            float[] features,
            DisparityDepthModel model,
            out float predictedRayDistance)
        {
            predictedRayDistance = 0f;

            if (model == null || !model.IsTrained || !TryResolveInverseDisparity(features, out float inverseDisparity))
            {
                return false;
            }

            predictedRayDistance = (model.Scale * inverseDisparity) + model.Offset;
            predictedRayDistance = Mathf.Clamp(predictedRayDistance, model.MinimumRayDistance, model.MaximumRayDistance);
            return !float.IsNaN(predictedRayDistance) && !float.IsInfinity(predictedRayDistance);
        }

        private static bool TryResolveInverseDisparity(float[] features, out float inverseDisparity)
        {
            inverseDisparity = 0f;

            if (features == null ||
                features.Length <= GazeDepthFeatureExtractor.DisparityMagnitudeIndex)
            {
                return false;
            }

            float disparityMagnitude = Mathf.Abs(features[GazeDepthFeatureExtractor.DisparityMagnitudeIndex]);
            if (float.IsNaN(disparityMagnitude) || float.IsInfinity(disparityMagnitude))
            {
                return false;
            }

            disparityMagnitude = Mathf.Max(MinimumSupportedDisparity, disparityMagnitude);
            inverseDisparity = 1f / disparityMagnitude;
            return !float.IsNaN(inverseDisparity) && !float.IsInfinity(inverseDisparity);
        }
    }
}
