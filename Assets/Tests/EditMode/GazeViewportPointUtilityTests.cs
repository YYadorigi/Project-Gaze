using NUnit.Framework;
using ProjectGaze.Gaze;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class GazeViewportPointUtilityTests
    {
        [Test]
        public void DisplayAreaToViewport_InvertsYAxisAndClampsToViewportRange()
        {
            var viewportPoint = GazeViewportPointUtility.DisplayAreaToViewport(new Vector2(1.2f, -0.25f));

            Assert.That(viewportPoint.x, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(viewportPoint.y, Is.EqualTo(1.0f).Within(0.0001f));
        }

        [Test]
        public void TryResolveViewportPointFromScreenRay_ReconstructsViewportCoordinates()
        {
            var cameraObject = new GameObject("TestCamera");

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.transform.rotation = Quaternion.identity;

                var expectedViewport = new Vector2(0.78f, 0.32f);
                var screenRay = camera.ViewportPointToRay(new Vector3(expectedViewport.x, expectedViewport.y, 0f));

                bool resolved = GazeViewportPointUtility.TryResolveViewportPointFromScreenRay(
                    camera,
                    screenRay,
                    out var viewportPoint);

                Assert.That(resolved, Is.True);
                Assert.That(viewportPoint.x, Is.EqualTo(expectedViewport.x).Within(0.0001f));
                Assert.That(viewportPoint.y, Is.EqualTo(expectedViewport.y).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TryBuildWorldRay_ReturnsFalseForMissingCamera()
        {
            bool resolved = GazeViewportPointUtility.TryBuildWorldRay(
                null,
                new Vector2(0.5f, 0.5f),
                out _);

            Assert.That(resolved, Is.False);
        }

        [Test]
        public void TryBuildWorldRay_ReturnsFiniteRayForValidCamera()
        {
            var cameraObject = new GameObject("RayCamera");

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100f;

                bool resolved = GazeViewportPointUtility.TryBuildWorldRay(
                    camera,
                    new Vector2(0.5f, 0.5f),
                    out Ray ray);

                Assert.That(resolved, Is.True);
                Assert.That(GazeViewportPointUtility.IsFiniteVector3(ray.origin), Is.True);
                Assert.That(GazeViewportPointUtility.IsFiniteVector3(ray.direction), Is.True);
                Assert.That(ray.direction.sqrMagnitude, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void IsFiniteMatrix_RejectsSingularProjectionMatrix()
        {
            Assert.That(GazeViewportPointUtility.IsFiniteMatrix(Matrix4x4.zero), Is.False);
        }
    }
}
