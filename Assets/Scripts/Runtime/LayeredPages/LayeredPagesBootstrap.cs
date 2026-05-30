using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectGaze.Gaze
{
    public static class LayeredPagesBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            SceneComponentBootstrapUtility.EnsureSceneComponent<LayeredPagesDemo>(
                scene,
                LayeredPagesDemo.SceneName,
                "LayeredPagesEntry");
        }
    }
}
