using NUnit.Framework;
using ProjectGaze.Gaze;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class SpatialPageDepthTintTests
    {
        [Test]
        public void EvaluateDepthBias_UsesZeroPlaneAsNeutralPoint()
        {
            var profile = new SpatialPageDepthTintProfile(
                7.8f,
                2.9f,
                5.2f,
                0.90f,
                0.92f,
                0.42f,
                new Color(1.00f, 0.90f, 0.72f, 1f),
                new Color(0.14f, 0.22f, 0.42f, 1f));

            float bias = SpatialPageDepthTintUtility.EvaluateDepthBias(7.8f, profile);

            Assert.That(bias, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void EvaluateDepthBias_AmplifiesSmallOffsetsWithSegmentedNonLinearMapping()
        {
            var profile = new SpatialPageDepthTintProfile(
                7.8f,
                2.9f,
                5.2f,
                0.90f,
                0.92f,
                0.42f,
                new Color(1.00f, 0.90f, 0.72f, 1f),
                new Color(0.14f, 0.22f, 0.42f, 1f));

            float nearBias = SpatialPageDepthTintUtility.EvaluateDepthBias(6.6f, profile);
            float farBias = SpatialPageDepthTintUtility.EvaluateDepthBias(10.4f, profile);

            Assert.That(nearBias, Is.LessThan(-0.5f));
            Assert.That(farBias, Is.GreaterThan(0.65f));
        }

        [Test]
        public void ApplyDepthTint_LightensPagesThatMoveTowardViewer()
        {
            Color baseColor = new(0.78f, 0.79f, 0.82f, 0.88f);
            var profile = new SpatialPageDepthTintProfile(
                0f,
                1f,
                1f,
                0.90f,
                0.92f,
                0.42f,
                new Color(1.00f, 0.90f, 0.72f, 1f),
                new Color(0.14f, 0.22f, 0.42f, 1f));

            Color tinted = SpatialPageDepthTintUtility.ApplyDepthTint(baseColor, -1f, profile);

            Assert.That(tinted.r, Is.GreaterThan(baseColor.r));
            Assert.That(tinted.g, Is.GreaterThan(baseColor.g));
            Assert.That(tinted.r - tinted.b, Is.GreaterThan(baseColor.r - baseColor.b));
            Assert.That(tinted.a, Is.EqualTo(baseColor.a).Within(0.0001f));
        }

        [Test]
        public void ApplyDepthTint_CoolsAndDarkensPagesThatMoveDeeper()
        {
            Color baseColor = new(0.78f, 0.79f, 0.82f, 0.88f);
            var profile = new SpatialPageDepthTintProfile(
                0f,
                1f,
                1f,
                0.90f,
                0.92f,
                0.42f,
                new Color(1.00f, 0.90f, 0.72f, 1f),
                new Color(0.14f, 0.22f, 0.42f, 1f));

            Color tinted = SpatialPageDepthTintUtility.ApplyDepthTint(baseColor, 1f, profile);

            Assert.That(tinted.r, Is.LessThan(baseColor.r));
            Assert.That(tinted.g, Is.LessThan(baseColor.g));
            Assert.That(tinted.b, Is.LessThan(baseColor.b));
            Assert.That(tinted.b - tinted.r, Is.GreaterThan(baseColor.b - baseColor.r));
            Assert.That(tinted.a, Is.EqualTo(baseColor.a).Within(0.0001f));
        }
    }
}
