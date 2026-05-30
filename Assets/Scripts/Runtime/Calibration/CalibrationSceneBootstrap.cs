using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectGaze.Gaze;

namespace ProjectGaze.Calibration
{
    public static class CalibrationSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            SceneComponentBootstrapUtility.EnsureSceneComponent<InvensunA8CalibrationSceneController>(
                scene,
                CalibrationSceneFlow.CalibrationSceneName,
                "InvensunCalibrationScene");
        }
    }
}
