using AS3DPlugin;
using UnityEngine;

namespace ProjectGaze.Hardware.ThinkVision
{
    internal static class ThinkVisionCameraDefaults
    {
        public static void Apply(Camera camera)
        {
            camera.transform.position = new Vector3(0f, 1.95f, -10.5f);
            camera.transform.rotation = Quaternion.Euler(5f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.08f, 0.12f);
            camera.fieldOfView = 46f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
        }
    }

    internal static class ThinkVisionFullscreenController
    {
        public static void EnsureFullscreenWindow()
        {
            var width = Display.main != null ? Display.main.systemWidth : Screen.currentResolution.width;
            var height = Display.main != null ? Display.main.systemHeight : Screen.currentResolution.height;

            if (width <= 0 || height <= 0)
            {
                width = Screen.currentResolution.width;
                height = Screen.currentResolution.height;
            }

            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
            Screen.fullScreen = true;
            Application.runInBackground = true;
        }
    }

    internal static class ThinkVisionStereoSceneScale
    {
        public const float ReducedGhostingStereoStrength = 0.3f;

        public static void ApplyCalibrationIfAvailable(Camera camera, float virtualScreenWidth, float stereoStrength)
        {
            if (camera == null)
            {
                return;
            }

            var stereoCamera = camera.GetComponent<StereoCam>();
            if (stereoCamera == null)
            {
                return;
            }

            if (stereoCamera.thisCam != null || stereoCamera.StereoRigs != null)
            {
                stereoCamera.ReSetVirtualScreenWidth(virtualScreenWidth);
                stereoCamera.StereoStrength = stereoStrength;
                return;
            }

            stereoCamera.VirtualScreenWidth = virtualScreenWidth;
            stereoCamera.StereoStrength = stereoStrength;
        }
    }
}
