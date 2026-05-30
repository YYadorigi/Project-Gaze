using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectGaze.Gaze
{
    internal static class SceneComponentBootstrapUtility
    {
        public static void EnsureSceneComponent<TComponent>(
            Scene scene,
            string sceneName,
            string objectName)
            where TComponent : Component
        {
            if (!string.Equals(scene.name, sceneName, StringComparison.Ordinal) ||
                UnityEngine.Object.FindFirstObjectByType<TComponent>() != null)
            {
                return;
            }

            new GameObject(objectName).AddComponent<TComponent>();
        }
    }
}
