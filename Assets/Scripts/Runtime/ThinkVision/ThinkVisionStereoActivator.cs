using System.Collections;
using System.IO;
using AS3DPlugin;
using UnityEngine;

namespace ProjectGaze.Hardware.ThinkVision
{
    internal sealed class ThinkVisionStereoActivator : MonoBehaviour
    {
        private StereoCam stereoCamera;
        private Coroutine activationRoutine;

        public static ThinkVisionStereoActivator Attach(Camera camera, StereoCam stereoCamera)
        {
            var activator = camera.GetComponent<ThinkVisionStereoActivator>() ??
                            camera.gameObject.AddComponent<ThinkVisionStereoActivator>();

            activator.Initialize(stereoCamera);
            return activator;
        }

        public void Initialize(StereoCam stereoCamera)
        {
            this.stereoCamera = stereoCamera;
            ThinkVisionUrpPostProcessingGuard.Apply(GetComponent<Camera>(), stereoCamera);

            if (activationRoutine != null)
            {
                StopCoroutine(activationRoutine);
            }

            activationRoutine = StartCoroutine(EnsureStereoStartup());
        }

        public void CancelPendingActivation()
        {
            if (activationRoutine != null)
            {
                StopCoroutine(activationRoutine);
                activationRoutine = null;
            }
        }

        private IEnumerator EnsureStereoStartup()
        {
            if (stereoCamera == null)
            {
                yield break;
            }

            if (StereoCam.sysConfig == null && File.Exists(ThinkVisionBridgeEnvironment.GetConfigPath()))
            {
                StereoCam.InitSysconfig();
            }

            yield return null;
            yield return null;

            if (stereoCamera == null)
            {
                yield break;
            }

            if (StereoCam.sysConfig != null &&
                StereoCam.sysConfig.TryGetValue("SCREEN", out var screenConfig))
            {
                screenConfig["StereoMode"] = StereoCam.AutoStereoShader.SideBySide.ToString();
            }

            stereoCamera.usedShader = StereoCam.AutoStereoShader.SideBySide;
            stereoCamera.HoldBeforeSwitch3DOn = false;
            stereoCamera.FrustumSyncEnable = true;
            stereoCamera.initScreen();

            yield return null;
            ThinkVisionUrpPostProcessingGuard.Apply(GetComponent<Camera>(), stereoCamera);

            if (stereoCamera == null || stereoCamera.subCams == null || stereoCamera.subCams.Length < 2)
            {
                yield break;
            }

            if (stereoCamera.et != null)
            {
                stereoCamera.DelaySwitchTo3D();
                yield return new WaitForSeconds(0.4f);

                if (stereoCamera.et != null && stereoCamera.et.OnOff3D != (uint)EyeTracking.OnOff.ON)
                {
                    stereoCamera.SetScreenSBSImmediate();
                }

                yield break;
            }

            stereoCamera.SetScreenSBSImmediate();
        }
    }
}
