using UnityEngine;

namespace ProjectGaze.Gaze
{
    internal static class UnityObjectLifecycleUtility
    {
        public static void DestroyObject(Object unityObject)
        {
            if (unityObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(unityObject);
            }
            else
            {
                Object.DestroyImmediate(unityObject);
            }
        }

        public static void UnloadAssetOrDestroy(Object asset)
        {
            if (asset == null)
            {
                return;
            }

            try
            {
                Resources.UnloadAsset(asset);
            }
            catch
            {
                DestroyObject(asset);
            }
        }
    }
}
