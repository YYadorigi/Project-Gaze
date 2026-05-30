using System.IO;
using NUnit.Framework;
using ProjectGaze.Gaze;
using ProjectGaze.Gaze.Depth;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class GazeTaskInteractionRecorderTests
    {
        [TearDown]
        public void TearDown()
        {
            GazeExperimentDataCapture.SetEnabledOverride(null);
        }

        [Test]
        public void RecordInteraction_DefaultsToSuccessAndWritesLatestFiles()
        {
            string experimentDataRootPath = CreateExperimentDataRootPath(nameof(RecordInteraction_DefaultsToSuccessAndWritesLatestFiles));
            var recorder = new GazeTaskInteractionRecorder(LayeredPagesDemo.SceneName, experimentDataRootPath);

            recorder.RecordInteraction(new GazeTaskInteractionRecord
            {
                TaskType = GazeTaskInteractionTypes.LayeredPageSelection,
                ConfirmedPageId = "Page_B"
            });

            Assert.That(recorder.Records, Has.Count.EqualTo(1));
            Assert.That(recorder.Records[0].Success, Is.True);
            Assert.That(recorder.Records[0].FailureMarkedByUser, Is.False);
            Assert.That(recorder.Records[0].EventIndex, Is.EqualTo(0));
            Assert.That(File.Exists(GazeTaskInteractionPersistence.BuildLatestJsonPath(experimentDataRootPath, LayeredPagesDemo.SceneName)), Is.True);
            Assert.That(File.Exists(GazeTaskInteractionPersistence.BuildLatestCsvPath(experimentDataRootPath, LayeredPagesDemo.SceneName)), Is.True);
        }

        [Test]
        public void TickSpaceFeedback_MarksLatestRecordFailedWithoutAddingRecord()
        {
            string experimentDataRootPath = CreateExperimentDataRootPath(nameof(TickSpaceFeedback_MarksLatestRecordFailedWithoutAddingRecord));
            var recorder = new GazeTaskInteractionRecorder(LayeredPagesDemo.SceneName, experimentDataRootPath);
            recorder.RecordInteraction(new GazeTaskInteractionRecord
            {
                TaskType = GazeTaskInteractionTypes.LayeredPageSelection,
                ConfirmedPageId = "Page_C"
            });

            bool marked = recorder.TickSpaceFeedback(spacePressed: true, hasPendingInteraction: false, nowSeconds: 10f);
            bool repeated = recorder.TickSpaceFeedback(spacePressed: true, hasPendingInteraction: false, nowSeconds: 11f);

            Assert.That(marked, Is.True);
            Assert.That(repeated, Is.False);
            Assert.That(recorder.Records, Has.Count.EqualTo(1));
            Assert.That(recorder.Records[0].Success, Is.False);
            Assert.That(recorder.Records[0].FailureMarkedByUser, Is.True);
            Assert.That(recorder.Records[0].FeedbackSource, Is.EqualTo(GazeTaskInteractionTypes.SpaceKeyFeedbackSource));
        }

        [Test]
        public void PendingFailure_IsAppliedToCompletedAgentRoundTrip()
        {
            string experimentDataRootPath = CreateExperimentDataRootPath(nameof(PendingFailure_IsAppliedToCompletedAgentRoundTrip));
            var recorder = new GazeTaskInteractionRecorder(DepthGatedAgentDemo.SceneName, experimentDataRootPath);

            bool accepted = recorder.TickSpaceFeedback(spacePressed: true, hasPendingInteraction: true, nowSeconds: 12f);
            var record = new GazeTaskInteractionRecord
            {
                TaskType = GazeTaskInteractionTypes.AgentPanelRoundTrip,
                LogoPageId = DepthGatedAgentPanelCoordinator.AgentLogoPageId,
                ClosedByPageId = "Web_A"
            };
            recorder.ApplyPendingFailure(record);
            recorder.RecordInteraction(record);

            Assert.That(accepted, Is.True);
            Assert.That(recorder.Records, Has.Count.EqualTo(1));
            Assert.That(recorder.Records[0].Success, Is.False);
            Assert.That(recorder.Records[0].FailureMarkedByUser, Is.True);
            Assert.That(recorder.HasPendingFailureFeedback, Is.False);
        }

        [Test]
        public void SaveSession_WritesExpectedCsvHeaderAndRecordValues()
        {
            string experimentDataRootPath = CreateExperimentDataRootPath(nameof(SaveSession_WritesExpectedCsvHeaderAndRecordValues));
            var recorder = new GazeTaskInteractionRecorder(LayeredPagesDemo.SceneName, experimentDataRootPath);

            recorder.RecordInteraction(new GazeTaskInteractionRecord
            {
                TaskType = GazeTaskInteractionTypes.LayeredPageSelection,
                ConfirmedPageId = "Page_D",
                MatchingMode = "DiscreteDepthLayer",
                InputSource = "MouseFallback"
            });

            string csv = File.ReadAllText(GazeTaskInteractionPersistence.BuildLatestCsvPath(experimentDataRootPath, LayeredPagesDemo.SceneName));

            Assert.That(csv, Does.StartWith("SessionId,SceneId,EventIndex,TaskType"));
            Assert.That(csv, Does.Contain("LayeredPageSelection"));
            Assert.That(csv, Does.Contain("Page_D"));
            Assert.That(csv, Does.Contain("DiscreteDepthLayer"));
            Assert.That(csv, Does.Contain("MouseFallback"));
        }

        [Test]
        public void SaveSession_EscapesCsvFieldsAndSanitizesFileNames()
        {
            string experimentDataRootPath = CreateExperimentDataRootPath(nameof(SaveSession_EscapesCsvFieldsAndSanitizesFileNames));
            var session = new GazeTaskInteractionSession
            {
                SessionId = "session/with:bad*chars",
                SceneId = "Scene:With/Bad*Chars",
                Records = new[]
                {
                    new GazeTaskInteractionRecord
                    {
                        SceneId = "Scene:With/Bad*Chars",
                        EventIndex = 0,
                        TaskType = GazeTaskInteractionTypes.LayeredPageSelection,
                        ConfirmedPageId = "Page, \"Quoted\"",
                        FeedbackSource = "Line\nBreak"
                    }
                }
            };

            GazeTaskInteractionPersistence.SaveSession(experimentDataRootPath, session);

            string csvPath = GazeTaskInteractionPersistence.BuildSessionCsvPath(
                experimentDataRootPath,
                session.SceneId,
                session.SessionId);
            string csv = File.ReadAllText(csvPath);

            Assert.That(csvPath, Does.Contain("Scene-With-Bad-Chars"));
            Assert.That(csvPath, Does.Contain("session-with-bad-chars"));
            Assert.That(csv, Does.Contain("\"Page, \"\"Quoted\"\"\""));
            Assert.That(csv, Does.Contain("\"Line\nBreak\""));
        }

        [Test]
        public void ExperimentDataCapture_WhenDisabled_DoesNotCreateTaskRecorder()
        {
            GazeExperimentDataCapture.SetEnabledOverride(false);

            var recorder = GazeExperimentDataCapture.CreateTaskInteractionRecorder(LayeredPagesDemo.SceneName);

            Assert.That(recorder, Is.Null);
        }

        [Test]
        public void ExperimentDataCapture_WhenEnabled_CreatesTaskRecorder()
        {
            GazeExperimentDataCapture.SetEnabledOverride(true);

            var recorder = GazeExperimentDataCapture.CreateTaskInteractionRecorder(LayeredPagesDemo.SceneName);

            Assert.That(GazeExperimentDataCapture.IsEnabled, Is.True);
            Assert.That(recorder, Is.Not.Null);
        }

        [Test]
        public void ExperimentDataCapture_WhenDisabled_DoesNotAttachRuntimeLayerRecorder()
        {
            var rootObject = new GameObject("RecorderRoot");
            try
            {
                GazeExperimentDataCapture.SetEnabledOverride(false);

                GazeExperimentDataCapture.AttachRuntimeLayerRecorder(
                    rootObject.transform,
                    null,
                    null,
                    null,
                    LayeredPagesDemo.SceneName);

                Assert.That(rootObject.GetComponent<GazeDepthRuntimeLayerRecorder>(), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void ExperimentDataCapture_EnvironmentVariableFalse_DisablesCapture()
        {
            string previousValue = System.Environment.GetEnvironmentVariable(GazeExperimentDataCapture.EnvironmentVariableName);
            try
            {
                GazeExperimentDataCapture.SetEnabledOverride(null);
                System.Environment.SetEnvironmentVariable(GazeExperimentDataCapture.EnvironmentVariableName, "false");

                Assert.That(GazeExperimentDataCapture.IsEnabled, Is.False);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable(GazeExperimentDataCapture.EnvironmentVariableName, previousValue);
                GazeExperimentDataCapture.SetEnabledOverride(null);
            }
        }

        private static string CreateExperimentDataRootPath(string testName)
        {
            string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TaskInteractionTests", testName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }

            Directory.CreateDirectory(path);
            return path;
        }
    }
}
