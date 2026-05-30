using System.Collections;
using AS3DPlugin;
using ProjectGaze.Gaze;
using ProjectGaze.Hardware.ThinkVision;
using UnityEngine;

namespace ProjectGaze.Calibration
{
    internal sealed class CalibrationDisplayModeController
    {
        private readonly Camera camera;
        private readonly IThinkVisionDisplayBridge displayBridge;
        private StereoCam stereoCamera;
        private string depthCalibrationStereoStatus = "ThinkVision stereo rig has not been prepared for depth calibration.";

        public CalibrationDisplayModeController(Camera camera)
        {
            this.camera = camera;
            displayBridge = ThinkVisionDisplayBridgeFactory.Create();
        }

        public bool CanEnterStereoForDepthCalibration => displayBridge.IsStereoDisplayActive;

        public string StereoStatusText => displayBridge.StatusText;

        public string DepthCalibrationStereoStatus => depthCalibrationStereoStatus;

        public void ConfigureMonoForScreenCalibration()
        {
            if (camera == null)
            {
                return;
            }

            ThinkVisionFullscreenController.EnsureFullscreenWindow();
            ThinkVisionCameraDefaults.Apply(camera);

            var stereoCamera = camera.GetComponent<StereoCam>();
            this.stereoCamera = stereoCamera;

            if (this.stereoCamera == null)
            {
                ThinkVisionUrpPostProcessingGuard.Apply(camera);
                return;
            }

            var stereoActivator = camera.GetComponent<ThinkVisionStereoActivator>();
            stereoActivator?.CancelPendingActivation();

            if (StereoCam.sysConfig != null &&
                StereoCam.sysConfig.TryGetValue("SCREEN", out var screenConfig))
            {
                screenConfig["StereoMode"] = StereoCam.AutoStereoShader.Mono.ToString();
            }

            this.stereoCamera.usedShader = StereoCam.AutoStereoShader.Mono;
            this.stereoCamera.HoldBeforeSwitch3DOn = true;
            this.stereoCamera.initScreen();

            if (HasStereoSubCameras(this.stereoCamera))
            {
                this.stereoCamera.FrustumSyncEnable = true;
                this.stereoCamera.SwitchFrustumSyncEnable();
            }

            if (this.stereoCamera.et != null)
            {
                this.stereoCamera.et.Switch3D(EyeTracking.OnOff.OFF);
            }

            if (HasStereoSubCameras(this.stereoCamera))
            {
                this.stereoCamera.SetScreenMonoImmediate();
            }

            ThinkVisionUrpPostProcessingGuard.Apply(camera, this.stereoCamera);
        }

        public IEnumerator EnterStereoForDepthCalibration()
        {
            depthCalibrationStereoStatus = "Preparing ThinkVision stereo rig for z calibration...";

            if (camera == null || !CanEnterStereoForDepthCalibration)
            {
                depthCalibrationStereoStatus = camera == null
                    ? "Depth calibration cannot enter stereo mode because the calibration camera is missing."
                    : $"Depth calibration requires a ThinkVision stereo display. {displayBridge.StatusText}";
                yield break;
            }

            ThinkVisionFullscreenController.EnsureFullscreenWindow();
            ThinkVisionCameraDefaults.Apply(camera);

            var stereoActivator = camera.GetComponent<ThinkVisionStereoActivator>();
            stereoActivator?.CancelPendingActivation();

            stereoCamera = camera.GetComponent<StereoCam>() ?? camera.gameObject.AddComponent<StereoCam>();
            if (StereoCam.sysConfig == null)
            {
                StereoCam.InitSysconfig();
            }

            if (StereoCam.sysConfig != null &&
                StereoCam.sysConfig.TryGetValue("SCREEN", out var screenConfig))
            {
                screenConfig["StereoMode"] = StereoCam.AutoStereoShader.SideBySide.ToString();
            }

            stereoCamera.VirtualScreenWidth = 8.8f;
            stereoCamera.FrustumSyncEnable = false;
            stereoCamera.NearClamp = 0.1f;
            stereoCamera.ClearScreenWhileSwitch = false;
            stereoCamera.ExitBypass2DSwitch = false;
            stereoCamera._antiAliasing = 4;
            stereoCamera.ScriptsToCopy = System.Array.Empty<string>();
            stereoCamera.usedShader = StereoCam.AutoStereoShader.SideBySide;
            stereoCamera.HoldBeforeSwitch3DOn = true;
            ThinkVisionUrpPostProcessingGuard.Apply(camera, stereoCamera);

            _ = camera.GetComponent<StereoUI>() ?? camera.gameObject.AddComponent<StereoUI>();

            float waitedSeconds = 0f;
            while (waitedSeconds < 1.5f &&
                   (stereoCamera == null ||
                    stereoCamera.subCams == null ||
                    stereoCamera.subCams.Length < 2 ||
                    stereoCamera.subCams[0] == null ||
                    stereoCamera.subCams[1] == null))
            {
                waitedSeconds += Time.unscaledDeltaTime;
                yield return null;
            }

            if (stereoCamera == null ||
                stereoCamera.subCams == null ||
                stereoCamera.subCams.Length < 2 ||
                stereoCamera.subCams[0] == null ||
                stereoCamera.subCams[1] == null)
            {
                depthCalibrationStereoStatus = "ThinkVision stereo rig did not create two side-by-side sub cameras.";
                yield break;
            }

            stereoCamera.FrustumSyncEnable = false;
            stereoCamera.HoldBeforeSwitch3DOn = true;
            stereoCamera.usedShader = StereoCam.AutoStereoShader.SideBySide;
            stereoCamera.initScreen();
            stereoCamera.FrustumSyncEnable = true;
            stereoCamera.SwitchFrustumSyncEnable();
            stereoCamera.StereoStrength = ThinkVisionStereoSceneScale.ReducedGhostingStereoStrength;

            if (stereoCamera.et != null)
            {
                stereoCamera.et.Switch3D(EyeTracking.OnOff.ON);
            }

            stereoCamera.SetScreenSBSImmediate();
            yield return new WaitForSecondsRealtime(0.2f);
            ThinkVisionStereoSceneScale.ApplyCalibrationIfAvailable(camera, 10.0f, ThinkVisionStereoSceneScale.ReducedGhostingStereoStrength);
            ThinkVisionUrpPostProcessingGuard.Apply(camera, stereoCamera);

            ThinkVisionStereoRigHealth.TryValidateStereoRig(stereoCamera, out string validationReason);
            depthCalibrationStereoStatus = validationReason;
        }

        public Camera ResolveCalibrationRayCamera()
        {
            return TryResolveCalibrationRayCamera(out var rayCamera, out _)
                ? rayCamera
                : camera;
        }

        public bool TryResolveCalibrationRayCamera(out Camera rayCamera, out string reason)
        {
            if (stereoCamera != null &&
                stereoCamera.usedShader == StereoCam.AutoStereoShader.SideBySide)
            {
                return ThinkVisionStereoRigHealth.TryResolveCalibrationRayCamera(stereoCamera, out rayCamera, out reason);
            }

            rayCamera = camera;
            if (rayCamera == null)
            {
                reason = "Calibration ray camera is missing.";
                return false;
            }

            if (!GazeViewportPointUtility.TryBuildWorldRay(rayCamera, new Vector2(0.5f, 0.5f), out _))
            {
                reason = "Calibration ray camera cannot produce a finite center viewport ray.";
                return false;
            }

            reason = "Using mono calibration camera for depth rays.";
            return true;
        }

        private static bool HasStereoSubCameras(StereoCam stereoCamera)
        {
            return stereoCamera != null &&
                   stereoCamera.subCams != null &&
                   stereoCamera.subCams.Length >= 2 &&
                   stereoCamera.subCams[0] != null &&
                   stereoCamera.subCams[1] != null;
        }

        public Transform ResolveCalibrationFacingTransform()
        {
            return ResolveCalibrationRayCamera() != null
                ? ResolveCalibrationRayCamera().transform
                : camera != null ? camera.transform : null;
        }
    }
}
