using ProjectGaze.Gaze.Depth;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    public static class GazeExperimentDataCapture
    {
        public const string EnvironmentVariableName = "PROJECT_GAZE_EXPERIMENT_DATA";

        private static bool? overrideEnabled;

        public static bool IsEnabled => overrideEnabled ?? ResolveEnabledFromEnvironment();

        public static void SetEnabledOverride(bool? enabled)
        {
            overrideEnabled = enabled;
        }

        public static GazeTaskInteractionRecorder CreateTaskInteractionRecorder(string sceneId)
        {
            return IsEnabled
                ? GazeTaskInteractionRecorder.CreateForProjectScene(sceneId)
                : null;
        }

        public static bool TrySaveContinuousValidationSession(
            string experimentDataRootPath,
            GazeDepthContinuousValidationSession session)
        {
            if (!IsEnabled)
            {
                return false;
            }

            GazeDepthPersistence.SaveContinuousValidationSession(experimentDataRootPath, session);
            return true;
        }

        public static void AttachRuntimeLayerRecorder(
            Transform root,
            Camera targetCamera,
            SpatialPageRegistry pageRegistry,
            GazeInteractionController gazeController,
            string sceneId)
        {
            if (!IsEnabled || root == null)
            {
                return;
            }

            var depthLayerRecorder = root.gameObject.AddComponent<GazeDepthRuntimeLayerRecorder>();
            depthLayerRecorder.Initialize(targetCamera, pageRegistry, gazeController, sceneId);
        }

        private static bool ResolveEnabledFromEnvironment()
        {
            string value = System.Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            value = value.Trim();
            return !string.Equals(value, "0", System.StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(value, "false", System.StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(value, "off", System.StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(value, "no", System.StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(value, "disabled", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
