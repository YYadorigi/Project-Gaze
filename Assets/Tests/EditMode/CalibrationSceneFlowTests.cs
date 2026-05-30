using NUnit.Framework;
using ProjectGaze.Calibration;
using ProjectGaze.Gaze;

namespace ProjectGaze.Tests
{
    public sealed class CalibrationSceneFlowTests
    {
        [SetUp]
        public void SetUp()
        {
            CalibrationSceneFlow.ResetSessionStateForTests();
        }

        [Test]
        public void HasPendingRequestedScene_ReturnsFalseByDefault()
        {
            Assert.That(CalibrationSceneFlow.HasPendingRequestedScene(), Is.False);
        }

        [Test]
        public void TryConsumeRequestedSceneAfterCalibration_ReturnsFalseWhenRequestIsMissing()
        {
            bool consumed = CalibrationSceneFlow.TryConsumeRequestedSceneAfterCalibration(
                forceMouseFallback: false,
                out string sceneName);

            Assert.That(consumed, Is.False);
            Assert.That(sceneName, Is.Null);
        }

        [Test]
        public void RequestSceneAfterCalibration_CanBeConsumedAsStereoFlow()
        {
            CalibrationSceneFlow.RequestSceneAfterCalibration(LayeredPagesDemo.SceneName);

            bool consumed = CalibrationSceneFlow.TryConsumeRequestedSceneAfterCalibration(
                forceMouseFallback: false,
                out string sceneName);

            Assert.That(consumed, Is.True);
            Assert.That(sceneName, Is.EqualTo(LayeredPagesDemo.SceneName));
            Assert.That(CalibrationSceneFlow.HasPendingRequestedScene(), Is.False);
            Assert.That(CalibrationSceneFlow.ShouldForceMouseFallbackThisSession(), Is.False);
        }

        [Test]
        public void RequestSceneAfterCalibration_CanBeConsumedAsMouseFallbackFlow()
        {
            CalibrationSceneFlow.RequestSceneAfterCalibration(LayeredPagesDemo.SceneName);

            bool consumed = CalibrationSceneFlow.TryConsumeRequestedSceneAfterCalibration(
                forceMouseFallback: true,
                out _);

            Assert.That(consumed, Is.True);
            Assert.That(CalibrationSceneFlow.ShouldForceMouseFallbackThisSession(), Is.True);
        }
    }
}
