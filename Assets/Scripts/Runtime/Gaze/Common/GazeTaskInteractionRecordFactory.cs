using System;
using ProjectGaze.Gaze.Depth;

namespace ProjectGaze.Gaze
{
    public static class GazeTaskInteractionRecordFactory
    {
        public static GazeTaskInteractionRecord BuildBaseRecord(GazeInteractionController gazeController)
        {
            string nowUtc = DateTime.UtcNow.ToString("O");
            var snapshot = gazeController != null ? gazeController.CurrentSnapshot : default;
            var lastHit = gazeController != null ? gazeController.LastHitResult : default;
            bool hasPredictedDepth = gazeController != null &&
                                     gazeController.HasLastTrackingSample &&
                                     gazeController.LastTrackingSample.PredictedRayDistance > 0f;
            float predictedRayDistance = hasPredictedDepth
                ? gazeController.LastTrackingSample.PredictedRayDistance
                : 0f;

            return new GazeTaskInteractionRecord
            {
                StartedAtUtc = nowUtc,
                CompletedAtUtc = nowUtc,
                MatchingMode = gazeController != null ? gazeController.DepthMatchingMode.ToString() : null,
                InputSource = gazeController != null ? gazeController.ActiveGazeProviderName : null,
                HasPredictedDepth = hasPredictedDepth,
                PredictedRayDistance = predictedRayDistance,
                PredictedLayerId = ResolvePredictedLayerId(hasPredictedDepth, predictedRayDistance),
                SystemHitPageId = lastHit.HasHitPage ? lastHit.PageId : null,
                PreviewPageId = snapshot.PreviewPageId
            };
        }

        private static string ResolvePredictedLayerId(bool hasPredictedDepth, float predictedRayDistance)
        {
            if (hasPredictedDepth &&
                GazeDepthLayerResolver.TryResolveNearestLayer(predictedRayDistance, out var predictedLayer))
            {
                return predictedLayer.LayerId;
            }

            return null;
        }
    }
}
