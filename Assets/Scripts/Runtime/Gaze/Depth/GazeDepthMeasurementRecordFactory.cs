using System;
using UnityEngine;

namespace ProjectGaze.Gaze.Depth
{
    public static class GazeDepthMeasurementRecordFactory
    {
        public static GazeDepthContinuousValidationRecord BuildContinuousValidationRecord(
            int targetIndex,
            Vector2 targetViewportPoint,
            float injectedRayDistance,
            float predictedRayDistance,
            long sampleTimestamp,
            string recordedAtUtc = null)
        {
            GazeDepthLayerResolver.TryResolveNearestLayer(injectedRayDistance, out var injectedLayer);
            GazeDepthLayerResolver.TryResolveNearestLayer(predictedRayDistance, out var predictedLayer);

            return new GazeDepthContinuousValidationRecord
            {
                TargetIndex = targetIndex,
                TargetViewportX = targetViewportPoint.x,
                TargetViewportY = targetViewportPoint.y,
                InjectedRayDistance = injectedRayDistance,
                PredictedRayDistance = predictedRayDistance,
                SignedError = predictedRayDistance - injectedRayDistance,
                AbsoluteError = Mathf.Abs(predictedRayDistance - injectedRayDistance),
                InjectedNearestLayerId = injectedLayer.LayerId,
                PredictedNearestLayerId = predictedLayer.LayerId,
                InjectedWithinLayerTolerance = injectedLayer.IsWithinTolerance,
                PredictedWithinLayerTolerance = predictedLayer.IsWithinTolerance,
                LayerMatched = string.Equals(injectedLayer.LayerId, predictedLayer.LayerId, StringComparison.Ordinal),
                SampleTimestamp = sampleTimestamp,
                RecordedAtUtc = string.IsNullOrWhiteSpace(recordedAtUtc)
                    ? DateTime.UtcNow.ToString("O")
                    : recordedAtUtc
            };
        }
    }
}
