using AS3DPlugin;
using ProjectGaze.Gaze;
using UnityEngine;

namespace ProjectGaze.Hardware.ThinkVision
{
    public static class ThinkVisionStereoRigHealth
    {
        public static bool TryValidateStereoRig(StereoCam stereoCamera, out string reason)
        {
            reason = null;

            if (stereoCamera == null)
            {
                reason = "StereoCam is missing.";
                return false;
            }

            if (stereoCamera.usedShader != StereoCam.AutoStereoShader.SideBySide)
            {
                reason = $"StereoCam is not in SideBySide mode. Current mode: {stereoCamera.usedShader}.";
                return false;
            }

            if (stereoCamera.subCams == null || stereoCamera.subCams.Length < 2)
            {
                reason = "StereoCam side-by-side sub cameras have not been created.";
                return false;
            }

            if (!TryValidateCamera(stereoCamera.subCams[0], "left stereo camera", requireEnabled: true, out reason) ||
                !TryValidateCamera(stereoCamera.subCams[1], "right stereo camera", requireEnabled: true, out reason))
            {
                return false;
            }

            if (!GazeViewportPointUtility.TryBuildWorldRay(stereoCamera.subCams[0], new Vector2(0.5f, 0.5f), out _) ||
                !GazeViewportPointUtility.TryBuildWorldRay(stereoCamera.subCams[1], new Vector2(0.5f, 0.5f), out _))
            {
                reason = "StereoCam sub cameras cannot produce finite center viewport rays.";
                return false;
            }

            reason = "ThinkVision stereo rig is healthy for depth calibration.";
            return true;
        }

        public static bool TryResolveCalibrationRayCamera(StereoCam stereoCamera, out Camera rayCamera, out string reason)
        {
            rayCamera = null;

            if (!TryValidateStereoRig(stereoCamera, out reason))
            {
                return false;
            }

            rayCamera = stereoCamera.subCams[0];
            reason = "Using ThinkVision left stereo camera for depth calibration rays.";
            return true;
        }

        private static bool TryValidateCamera(Camera camera, string label, bool requireEnabled, out string reason)
        {
            if (camera == null)
            {
                reason = $"{label} is missing.";
                return false;
            }

            if (requireEnabled && !camera.enabled)
            {
                reason = $"{label} is disabled.";
                return false;
            }

            if (!IsFiniteRect(camera.rect) || camera.rect.width <= 0f || camera.rect.height <= 0f)
            {
                reason = $"{label} has an invalid viewport rect: {camera.rect}.";
                return false;
            }

            if (!GazeViewportPointUtility.IsFiniteVector3(camera.transform.position) ||
                !GazeViewportPointUtility.IsFiniteVector3(camera.transform.localPosition) ||
                !GazeViewportPointUtility.IsFiniteVector3(camera.transform.localScale))
            {
                reason = $"{label} has a non-finite transform.";
                return false;
            }

            if (!IsFiniteQuaternion(camera.transform.rotation) ||
                !IsFiniteQuaternion(camera.transform.localRotation))
            {
                reason = $"{label} has a non-finite rotation.";
                return false;
            }

            if (!GazeViewportPointUtility.IsFiniteMatrix(camera.worldToCameraMatrix) ||
                !GazeViewportPointUtility.IsFiniteMatrix(camera.projectionMatrix))
            {
                reason = $"{label} has an invalid camera matrix.";
                return false;
            }

            reason = null;
            return true;
        }

        private static bool IsFiniteRect(Rect rect)
        {
            return GazeViewportPointUtility.IsFinite(rect.x) &&
                   GazeViewportPointUtility.IsFinite(rect.y) &&
                   GazeViewportPointUtility.IsFinite(rect.width) &&
                   GazeViewportPointUtility.IsFinite(rect.height);
        }

        private static bool IsFiniteQuaternion(Quaternion value)
        {
            return GazeViewportPointUtility.IsFinite(value.x) &&
                   GazeViewportPointUtility.IsFinite(value.y) &&
                   GazeViewportPointUtility.IsFinite(value.z) &&
                   GazeViewportPointUtility.IsFinite(value.w);
        }
    }
}
