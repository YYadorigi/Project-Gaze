using ProjectGaze.Hardware.ThinkVision;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    internal static class LayeredPagesCameraUtility
    {
        public static Camera ResolveAndConfigureCamera(IThinkVisionDisplayBridge displayBridge)
        {
            var camera = Camera.main;

            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            displayBridge?.Configure(camera);
            ApplyLayeredPagesComposition(camera);
            return camera;
        }

        private static void ApplyLayeredPagesComposition(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.transform.position = LayeredPagesSceneDefaults.CameraPosition;
            camera.transform.rotation = Quaternion.Euler(LayeredPagesSceneDefaults.CameraRotationEuler);
        }
    }
}
