using AS3DPlugin;
using NUnit.Framework;
using ProjectGaze.Hardware.ThinkVision;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class ThinkVisionGazeViewportPointUtilityTests
    {
        [Test]
        public void DisplayAreaToViewport_CompensatesSideBySideDisplayXInHardwareLayer()
        {
            var cameraObject = new GameObject("TestCamera");

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                var stereoCamera = cameraObject.AddComponent<StereoCam>();
                stereoCamera.usedShader = StereoCam.AutoStereoShader.SideBySide;

                Vector2 viewportPoint = ThinkVisionGazeViewportPointUtility.DisplayAreaToViewport(
                    camera,
                    new Vector2(1.60f, 0.30f));

                Assert.That(viewportPoint.x, Is.EqualTo(0.80f).Within(0.0001f));
                Assert.That(viewportPoint.y, Is.EqualTo(0.70f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
