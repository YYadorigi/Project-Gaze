using AS3DPlugin;
using NUnit.Framework;
using ProjectGaze.Gaze.Providers;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class InvensunA8RecommendedGazeUtilityTests
    {
        [Test]
        public void TryResolveViewportPoint_UsesRecommendedPointAndFlipsYAxis()
        {
            var sample = new InvensunA8RawGazeSample(
                new Vector2(0.40f, 0.40f),
                true,
                leftGazePointValid: true,
                rightGazePointValid: true,
                false,
                false,
                1f,
                1f,
                1L);

            bool resolved = InvensunA8RecommendedGazeUtility.TryResolveViewportPoint(
                sample,
                out var viewportPoint);

            Assert.That(resolved, Is.True);
            Assert.That(viewportPoint.x, Is.EqualTo(0.40f).Within(0.0001f));
            Assert.That(viewportPoint.y, Is.EqualTo(0.60f).Within(0.0001f));
        }

        [Test]
        public void TryResolveViewportPoint_RejectsInvalidRecommendedPoint()
        {
            var sample = new InvensunA8RawGazeSample(
                new Vector2(0f, 0f),
                false,
                leftGazePointValid: true,
                rightGazePointValid: true,
                false,
                false,
                1f,
                1f,
                1L);

            bool resolved = InvensunA8RecommendedGazeUtility.TryResolveViewportPoint(
                sample,
                out _);

            Assert.That(resolved, Is.False);
        }

        [Test]
        public void TryResolveViewportPoint_ClampsOutOfRangeDisplayPoints()
        {
            var sample = new InvensunA8RawGazeSample(
                new Vector2(10f, 10f),
                true,
                leftGazePointValid: true,
                rightGazePointValid: true,
                false,
                false,
                1f,
                1f,
                1L);

            bool resolved = InvensunA8RecommendedGazeUtility.TryResolveViewportPoint(
                sample,
                out var viewportPoint);

            Assert.That(resolved, Is.True);
            Assert.That(viewportPoint.x, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(viewportPoint.y, Is.EqualTo(0.0f).Within(0.0001f));
        }

        [Test]
        public void TryResolveViewportPoint_KeepsDemoStyleValidityCheckWhenRecommendedPointExceedsUnitRange()
        {
            var sample = new InvensunA8RawGazeSample(
                new Vector2(0.52f, 1.18f),
                true,
                leftGazePointValid: false,
                rightGazePointValid: false,
                false,
                false,
                1f,
                1f,
                1L);

            bool resolved = InvensunA8RecommendedGazeUtility.TryResolveViewportPoint(
                sample,
                out var viewportPoint);

            Assert.That(resolved, Is.True);
            Assert.That(viewportPoint.x, Is.EqualTo(0.52f).Within(0.0001f));
            Assert.That(viewportPoint.y, Is.EqualTo(0.0f).Within(0.0001f));
        }

        [Test]
        public void TryResolveViewportPoint_IgnoresThinkVisionSideBySideCompensationForCurrentBaselinePath()
        {
            var cameraObject = new GameObject("TestCamera");

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                var stereoCamera = cameraObject.AddComponent<StereoCam>();
                stereoCamera.usedShader = StereoCam.AutoStereoShader.SideBySide;

                var sample = new InvensunA8RawGazeSample(
                    new Vector2(1.60f, 0.30f),
                    true,
                    leftGazePointValid: true,
                    rightGazePointValid: true,
                    false,
                    false,
                    1f,
                    1f,
                    1L);

                bool resolved = InvensunA8RecommendedGazeUtility.TryResolveViewportPoint(
                    sample,
                    camera,
                    out var viewportPoint);

                Assert.That(resolved, Is.True);
                Assert.That(viewportPoint.x, Is.EqualTo(1.0f).Within(0.0001f));
                Assert.That(viewportPoint.y, Is.EqualTo(0.70f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TryResolveViewportPoint_KeepsRawRecommendedDisplayPointEvenWhenStereoCamExists()
        {
            var cameraObject = new GameObject("TestCamera");

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                var stereoCamera = cameraObject.AddComponent<StereoCam>();
                stereoCamera.usedShader = StereoCam.AutoStereoShader.SideBySide;

                var sample = new InvensunA8RawGazeSample(
                    new Vector2(0.75f, 0.30f),
                    true,
                    leftGazePointValid: true,
                    rightGazePointValid: true,
                    false,
                    false,
                    1f,
                    1f,
                    1L);

                bool resolved = InvensunA8RecommendedGazeUtility.TryResolveViewportPoint(
                    sample,
                    camera,
                    out var viewportPoint);

                Assert.That(resolved, Is.True);
                Assert.That(viewportPoint.x, Is.EqualTo(0.75f).Within(0.0001f));
                Assert.That(viewportPoint.y, Is.EqualTo(0.70f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

    }
}
