using NUnit.Framework;
using ProjectGaze.Gaze.Depth;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class VergenceDepthEstimatorTests
    {
        [Test]
        public void TryEstimateDepthFromAngle_ConvertsVergenceAngleToDepth()
        {
            const float baseline = 0.063f;
            const float expectedDepth = 1.0f;
            float angle = 2f * Mathf.Atan(baseline / (2f * expectedDepth));

            bool estimated = VergenceDepthEstimator.TryEstimateDepthFromAngle(angle, baseline, out float depth);

            Assert.That(estimated, Is.True);
            Assert.That(depth, Is.EqualTo(expectedDepth).Within(0.0001f));
        }

        [Test]
        public void TryBuildErrorInterval_DepthErrorGrowsWithTargetDepth()
        {
            const float baseline = 0.063f;

            bool nearResolved = VergenceDepthEstimator.TryBuildErrorInterval(0.4f, baseline, 0.35f, out var nearInterval);
            bool farResolved = VergenceDepthEstimator.TryBuildErrorInterval(1.2f, baseline, 0.35f, out var farInterval);

            Assert.That(nearResolved, Is.True);
            Assert.That(farResolved, Is.True);
            Assert.That(farInterval.UpperError, Is.GreaterThan(nearInterval.UpperError));
            Assert.That(farInterval.LowerError, Is.GreaterThan(nearInterval.LowerError));
        }
    }
}
