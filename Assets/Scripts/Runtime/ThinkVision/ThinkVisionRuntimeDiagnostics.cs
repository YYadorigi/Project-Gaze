using System.IO;
using AS3DPlugin;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectGaze.Hardware.ThinkVision
{
    internal sealed class ThinkVisionRuntimeDiagnostics : MonoBehaviour
    {
        private const float DisplayPollIntervalSeconds = 1f;

        private StereoCam stereoCamera;
        private float nextDisplayPollAt;
        private int detectedDisplayCount = -1;

        public static ThinkVisionRuntimeDiagnostics Attach(Camera camera, StereoCam stereoCamera)
        {
            var diagnostics = camera.GetComponent<ThinkVisionRuntimeDiagnostics>() ??
                              camera.gameObject.AddComponent<ThinkVisionRuntimeDiagnostics>();

            diagnostics.Initialize(stereoCamera);
            return diagnostics;
        }

        public void Initialize(StereoCam stereoCamera)
        {
            this.stereoCamera = stereoCamera;

            if (StereoCam.sysConfig == null && File.Exists(ThinkVisionBridgeEnvironment.GetConfigPath()))
            {
                StereoCam.InitSysconfig();
            }

            PollDisplayInfo(true);
        }

        public string BuildBridgeStatusText()
        {
            if (stereoCamera == null)
            {
                return "ThinkVision diagnostics are not attached to the camera.";
            }

            var panel3D = stereoCamera.et == null
                ? "Unavailable"
                : stereoCamera.et.OnOff3D == (uint)EyeTracking.OnOff.ON ? "On" : "Off";

            var eyeTracking = stereoCamera.et == null
                ? "Unavailable"
                : stereoCamera.et.OnOffET == (uint)EyeTracking.OnOff.ON ? "On" : "Off";

            var detectedEdid = string.IsNullOrWhiteSpace(StereoCam.detectedEDID) ? "NotDetected" : StereoCam.detectedEDID;
            var displayCount = detectedDisplayCount > 0 ? detectedDisplayCount : Display.displays.Length;
            var renderSafety = BuildRenderSafetyText();

            return $"ThinkVision bridge live. Mode {stereoCamera.usedShader}, panel 3D {panel3D}, eye tracking {eyeTracking}, virtual width {stereoCamera.VirtualScreenWidth:F2}, stereo strength {stereoCamera.StereoStrength:F2}, fullscreen {(Screen.fullScreen ? "On" : "Off")} ({Screen.fullScreenMode}), displays {displayCount}, EDID {detectedEdid}, {renderSafety}.";
        }

        private void Update()
        {
            if (stereoCamera == null)
            {
                return;
            }

            PollDisplayInfo(false);
        }

        private void PollDisplayInfo(bool force)
        {
            if (!force && Time.unscaledTime < nextDisplayPollAt)
            {
                return;
            }

            nextDisplayPollAt = Time.unscaledTime + DisplayPollIntervalSeconds;

            if (!ThinkVisionBridgeEnvironment.IsWindowsPlatform())
            {
                detectedDisplayCount = Display.displays.Length;
                return;
            }

            try
            {
                detectedDisplayCount = WindowHandler.GetDisplays().Count;
            }
            catch
            {
                detectedDisplayCount = Display.displays.Length;
            }
        }

        private string BuildRenderSafetyText()
        {
            return $"buffer {GetBufferScaleText()}, dyn res {GetDynamicResolutionStatus()}, post FX {GetPostProcessingStatus()}, AA {GetAntiAliasingStatus()}, keep Game view at 1x";
        }

        private static string GetBufferScaleText()
        {
            return $"{ScalableBufferManager.widthScaleFactor:F2}x{ScalableBufferManager.heightScaleFactor:F2}";
        }

        private string GetDynamicResolutionStatus()
        {
            var camera = GetComponent<Camera>();
            return camera != null && camera.allowDynamicResolution ? "On" : "Off";
        }

        private string GetPostProcessingStatus()
        {
            var cameraData = ResolveCameraData();
            return cameraData != null && cameraData.renderPostProcessing ? "On" : "Off";
        }

        private string GetAntiAliasingStatus()
        {
            var cameraData = ResolveCameraData();
            return cameraData != null ? cameraData.antialiasing.ToString() : "Unknown";
        }

        private UniversalAdditionalCameraData ResolveCameraData()
        {
            var camera = GetComponent<Camera>();
            return camera != null ? camera.GetUniversalAdditionalCameraData() : null;
        }
    }
}
