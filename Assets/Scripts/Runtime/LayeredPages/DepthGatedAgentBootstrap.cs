using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectGaze.Gaze
{
    public static class DepthGatedAgentBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            SceneComponentBootstrapUtility.EnsureSceneComponent<DepthGatedAgentDemo>(
                scene,
                DepthGatedAgentDemo.SceneName,
                "DepthGatedAgentDemo");
        }
    }
}
