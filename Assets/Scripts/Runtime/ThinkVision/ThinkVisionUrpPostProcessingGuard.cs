using AS3DPlugin;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ProjectGaze.Hardware.ThinkVision
{
    public static class ThinkVisionUrpPostProcessingGuard
    {
        // Keep the ThinkVision path free from URP post effects that can distort stereo output.
        public static void Apply(Camera camera)
        {
            Apply(camera, null);
        }

        internal static void Apply(Camera camera, StereoCam stereoCamera)
        {
            ScalableBufferManager.ResizeBuffers(1f, 1f);
            ApplyToCamera(camera);

            if (stereoCamera != null)
            {
                ApplyToCamera(stereoCamera.thisCam != null ? stereoCamera.thisCam : stereoCamera.GetComponent<Camera>());

                if (stereoCamera.subCams != null)
                {
                    foreach (var subCamera in stereoCamera.subCams)
                    {
                        ApplyToCamera(subCamera);
                    }
                }
            }

            DisableVolumesInScene(ResolveScene(camera, stereoCamera));
        }

        internal static void ApplyToCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            var cameraData = camera.GetUniversalAdditionalCameraData();
            camera.allowMSAA = true;
            camera.allowDynamicResolution = false;
            cameraData.renderPostProcessing = false;
            // Keep MSAA available, but do not allow post-process AA modes such as TAA.
            cameraData.antialiasing = AntialiasingMode.None;
            cameraData.volumeLayerMask = 0;
            cameraData.volumeTrigger = null;
            cameraData.stopNaN = false;
            cameraData.dithering = false;
        }

        private static Scene ResolveScene(Camera camera, StereoCam stereoCamera)
        {
            if (camera != null)
            {
                return camera.gameObject.scene;
            }

            return stereoCamera != null ? stereoCamera.gameObject.scene : default;
        }

        private static void DisableVolumesInScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            foreach (var volume in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (volume == null || volume.gameObject.scene != scene)
                {
                    continue;
                }

                volume.enabled = false;
                volume.weight = 0f;
            }
        }
    }
}
