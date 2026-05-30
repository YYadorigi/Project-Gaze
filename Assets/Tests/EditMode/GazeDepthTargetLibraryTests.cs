using System.Linq;
using NUnit.Framework;
using ProjectGaze.Gaze.Depth;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class GazeDepthCalibrationTargetLibraryTests
    {
        [Test]
        public void DefaultTargets_UseSevenSymmetricDepthCalibrationDistances()
        {
            var distances = Enumerable.Range(0, GazeDepthCalibrationTargetLibrary.TargetCount)
                .Select(index => GazeDepthCalibrationTargetLibrary.GetTarget(index).RayDistance)
                .Distinct()
                .ToArray();

            Assert.That(GazeDepthCalibrationTargetLibrary.TargetCount, Is.EqualTo(21));
            Assert.That(distances, Is.EquivalentTo(new[]
            {
                GazeDepthLayerProfile.Near3RayDistance,
                GazeDepthLayerProfile.Near2RayDistance,
                GazeDepthLayerProfile.Near1RayDistance,
                GazeDepthLayerProfile.ZeroRayDistance,
                GazeDepthLayerProfile.Far1RayDistance,
                GazeDepthLayerProfile.Far2RayDistance,
                GazeDepthLayerProfile.Far3RayDistance
            }));
        }
    }

    public sealed class GazeDepthContinuousValidationTargetLibraryTests
    {
        [Test]
        public void DefaultTargets_CoverContinuousNineToTwentySevenRange()
        {
            var distances = Enumerable.Range(0, GazeDepthContinuousValidationTargetLibrary.TargetCount)
                .Select(index => GazeDepthContinuousValidationTargetLibrary.GetTarget(index).RayDistance)
                .ToArray();

            Assert.That(GazeDepthContinuousValidationTargetLibrary.TargetCount, Is.EqualTo(19));
            Assert.That(distances.First(), Is.EqualTo(9.0f).Within(0.0001f));
            Assert.That(distances.Last(), Is.EqualTo(27.0f).Within(0.0001f));

            for (int index = 1; index < distances.Length; index += 1)
            {
                Assert.That(distances[index] - distances[index - 1], Is.EqualTo(1.0f).Within(0.0001f));
            }
        }

        [Test]
        public void DefaultTargets_UseDepthProfileInterpolatedViewportY()
        {
            var nearTarget = GazeDepthContinuousValidationTargetLibrary.GetTarget(0);
            var zeroTarget = GazeDepthContinuousValidationTargetLibrary.GetTarget(9);
            var farTarget = GazeDepthContinuousValidationTargetLibrary.GetTarget(18);
            var interpolatedTarget = GazeDepthContinuousValidationTargetLibrary.GetTarget(1);

            Assert.That(nearTarget.ViewportPoint, Is.EqualTo(new Vector2(0.5f, 0.70f)));
            Assert.That(zeroTarget.ViewportPoint, Is.EqualTo(new Vector2(0.5f, 0.50f)));
            Assert.That(farTarget.ViewportPoint, Is.EqualTo(new Vector2(0.5f, 0.30f)));
            Assert.That(interpolatedTarget.ViewportPoint.y, Is.EqualTo(0.68f).Within(0.0001f));
        }
    }
}
