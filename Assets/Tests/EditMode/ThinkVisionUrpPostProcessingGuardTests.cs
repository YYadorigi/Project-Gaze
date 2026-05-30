using NUnit.Framework;
using AS3DPlugin;
using ProjectGaze.Hardware.ThinkVision;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectGaze.Tests
{
    public sealed class ThinkVisionUrpPostProcessingGuardTests
    {
        [Test]
        public void Apply_LeavesMsaaAvailableButDisablesTemporalAndPostProcessAntialiasing()
        {
            var cameraObject = new GameObject("ThinkVisionCamera");
            var volumeObject = new GameObject("ThinkVisionVolume");

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.allowMSAA = false;
                camera.allowDynamicResolution = true;
                ScalableBufferManager.ResizeBuffers(0.73f, 0.81f);
                var cameraData = camera.GetUniversalAdditionalCameraData();
                cameraData.renderPostProcessing = true;
                cameraData.antialiasing = AntialiasingMode.TemporalAntiAliasing;
                cameraData.volumeLayerMask = LayerMask.GetMask("Default");
                cameraData.volumeTrigger = camera.transform;
                cameraData.stopNaN = true;
                cameraData.dithering = true;

                var volume = volumeObject.AddComponent<Volume>();
                volume.enabled = true;
                volume.isGlobal = true;
                volume.weight = 1f;

                ThinkVisionUrpPostProcessingGuard.Apply(camera);

                Assert.That(ScalableBufferManager.widthScaleFactor, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(ScalableBufferManager.heightScaleFactor, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(camera.allowMSAA, Is.True);
                Assert.That(camera.allowDynamicResolution, Is.False);
                Assert.That(cameraData.renderPostProcessing, Is.False);
                Assert.That(cameraData.antialiasing, Is.EqualTo(AntialiasingMode.None));
                Assert.That(cameraData.volumeLayerMask.value, Is.EqualTo(0));
                Assert.That(cameraData.volumeTrigger, Is.Null);
                Assert.That(cameraData.stopNaN, Is.False);
                Assert.That(cameraData.dithering, Is.False);
                Assert.That(volume.enabled, Is.False);
                Assert.That(volume.weight, Is.EqualTo(0f));
            }
            finally
            {
                ScalableBufferManager.ResizeBuffers(1f, 1f);
                Object.DestroyImmediate(volumeObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TryValidateStereoRig_RejectsDisabledSubCamera()
        {
            var rootObject = new GameObject("StereoRoot");
            var leftObject = new GameObject("LeftStereoCamera");
            var rightObject = new GameObject("RightStereoCamera");

            try
            {
                var stereoCamera = rootObject.AddComponent<StereoCam>();
                stereoCamera.usedShader = StereoCam.AutoStereoShader.SideBySide;
                stereoCamera.subCams = new Camera[2];
                stereoCamera.subCams[0] = leftObject.AddComponent<Camera>();
                stereoCamera.subCams[1] = rightObject.AddComponent<Camera>();
                stereoCamera.subCams[0].rect = new Rect(0f, 0f, 0.5f, 1f);
                stereoCamera.subCams[1].rect = new Rect(0.5f, 0f, 0.5f, 1f);
                stereoCamera.subCams[0].enabled = false;
                stereoCamera.subCams[1].enabled = true;

                bool valid = ThinkVisionStereoRigHealth.TryValidateStereoRig(stereoCamera, out string reason);

                Assert.That(valid, Is.False);
                Assert.That(reason, Does.Contain("disabled"));
            }
            finally
            {
                Object.DestroyImmediate(rightObject);
                Object.DestroyImmediate(leftObject);
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TryValidateStereoRig_AcceptsFiniteSideBySideSubCameras()
        {
            var rootObject = new GameObject("StereoRoot");
            var leftObject = new GameObject("LeftStereoCamera");
            var rightObject = new GameObject("RightStereoCamera");

            try
            {
                var stereoCamera = rootObject.AddComponent<StereoCam>();
                stereoCamera.usedShader = StereoCam.AutoStereoShader.SideBySide;
                stereoCamera.subCams = new Camera[2];
                stereoCamera.subCams[0] = leftObject.AddComponent<Camera>();
                stereoCamera.subCams[1] = rightObject.AddComponent<Camera>();
                stereoCamera.subCams[0].rect = new Rect(0f, 0f, 0.5f, 1f);
                stereoCamera.subCams[1].rect = new Rect(0.5f, 0f, 0.5f, 1f);
                stereoCamera.subCams[0].enabled = true;
                stereoCamera.subCams[1].enabled = true;

                bool valid = ThinkVisionStereoRigHealth.TryValidateStereoRig(stereoCamera, out string reason);

                Assert.That(valid, Is.True);
                Assert.That(reason, Does.Contain("healthy"));
            }
            finally
            {
                Object.DestroyImmediate(rightObject);
                Object.DestroyImmediate(leftObject);
                Object.DestroyImmediate(rootObject);
            }
        }
    }
}
