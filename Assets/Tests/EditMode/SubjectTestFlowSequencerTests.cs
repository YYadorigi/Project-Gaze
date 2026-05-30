using NUnit.Framework;
using ProjectGaze.SubjectTest;

namespace ProjectGaze.Tests
{
    public sealed class SubjectTestFlowSequencerTests
    {
        [Test]
        public void StartCalibration_EntersCalibrationStage()
        {
            var sequencer = new SubjectTestFlowSequencer();

            sequencer.StartCalibration();

            Assert.That(sequencer.Stage, Is.EqualTo(SubjectTestFlowStage.Calibration));
        }

        [Test]
        public void NotifySceneLoaded_StartsLayeredPagesTimer()
        {
            var sequencer = new SubjectTestFlowSequencer();

            sequencer.NotifySceneLoaded(SubjectTestFlowSequencer.LayeredPagesSceneName);

            Assert.That(sequencer.Stage, Is.EqualTo(SubjectTestFlowStage.LayeredPages));
            Assert.That(sequencer.RemainingSeconds, Is.EqualTo(SubjectTestFlowSequencer.FormalSceneDurationSeconds).Within(0.0001f));
        }

        [Test]
        public void Tick_AfterLayeredPagesDuration_LoadsDepthGatedAgentScene()
        {
            var sequencer = new SubjectTestFlowSequencer();
            sequencer.NotifySceneLoaded(SubjectTestFlowSequencer.LayeredPagesSceneName);

            string sceneToLoad = sequencer.Tick(SubjectTestFlowSequencer.FormalSceneDurationSeconds);

            Assert.That(sceneToLoad, Is.EqualTo(SubjectTestFlowSequencer.DepthGatedAgentSceneName));
            Assert.That(sequencer.Stage, Is.EqualTo(SubjectTestFlowStage.LoadingDepthGatedAgent));
        }

        [Test]
        public void Tick_AfterAgentDuration_CompletesFlow()
        {
            var sequencer = new SubjectTestFlowSequencer();
            sequencer.NotifySceneLoaded(SubjectTestFlowSequencer.DepthGatedAgentSceneName);

            string sceneToLoad = sequencer.Tick(SubjectTestFlowSequencer.FormalSceneDurationSeconds);

            Assert.That(sceneToLoad, Is.Null);
            Assert.That(sequencer.Stage, Is.EqualTo(SubjectTestFlowStage.Completed));
        }

        [Test]
        public void ResolveDataCaptureOverride_EnvironmentVariable_ReturnsNullOverride()
        {
            bool? dataCaptureOverride = SubjectTestFlowController.ResolveDataCaptureOverride(
                SubjectTestFlowDataCaptureMode.EnvironmentVariable);

            Assert.That(dataCaptureOverride, Is.Null);
        }

        [Test]
        public void ResolveDataCaptureOverride_ForcedModes_ReturnExpectedOverrides()
        {
            Assert.That(
                SubjectTestFlowController.ResolveDataCaptureOverride(SubjectTestFlowDataCaptureMode.ForceEnabled),
                Is.True);
            Assert.That(
                SubjectTestFlowController.ResolveDataCaptureOverride(SubjectTestFlowDataCaptureMode.ForceDisabled),
                Is.False);
        }
    }
}
