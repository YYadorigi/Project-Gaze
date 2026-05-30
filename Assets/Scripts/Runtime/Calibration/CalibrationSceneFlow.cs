using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectGaze.Calibration
{
    public static class CalibrationSceneFlow
    {
        public const string CalibrationSceneName = "CalibrationScene";

        private static bool forceMouseFallbackThisSession;
        private static string requestedSceneName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetSessionState()
        {
            forceMouseFallbackThisSession = false;
            requestedSceneName = null;
        }

#if UNITY_INCLUDE_TESTS
        public static void ResetSessionStateForTests()
        {
            ResetSessionState();
        }
#endif

        public static void RequestSceneAfterCalibration(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("Requested scene name is required.", nameof(sceneName));
            }

            requestedSceneName = sceneName;
            forceMouseFallbackThisSession = false;
        }

        public static void CompleteCalibrationAndLoadRequestedScene()
        {
            if (TryConsumeRequestedSceneAfterCalibration(
                    forceMouseFallback: false,
                    out string sceneNameToLoad))
            {
                SceneManager.LoadScene(sceneNameToLoad);
            }
        }

        public static void SkipCalibrationAndLoadRequestedScene()
        {
            if (TryConsumeRequestedSceneAfterCalibration(
                    forceMouseFallback: true,
                    out string sceneNameToLoad))
            {
                SceneManager.LoadScene(sceneNameToLoad);
            }
        }

        public static bool TryConsumeRequestedSceneAfterCalibration(
            bool forceMouseFallback,
            out string sceneName)
        {
            sceneName = requestedSceneName;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                sceneName = null;
                return false;
            }

            forceMouseFallbackThisSession = forceMouseFallback;
            requestedSceneName = null;
            return true;
        }

        public static bool ShouldForceMouseFallbackThisSession()
        {
            return forceMouseFallbackThisSession;
        }

        public static bool HasPendingRequestedScene()
        {
            return !string.IsNullOrWhiteSpace(requestedSceneName);
        }
    }
}
