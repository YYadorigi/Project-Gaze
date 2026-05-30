using System.IO;
using NUnit.Framework;
using ProjectGaze.Gaze;
using ProjectGaze.Gaze.Depth;
using ProjectGaze.SubjectTest;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class GazeDepthLayerResolverTests
    {
        [Test]
        public void TryResolveNearestLayer_ReturnsNearestLayerAndToleranceState()
        {
            bool resolved = GazeDepthLayerResolver.TryResolveNearestLayer(20.7f, out var match);

            Assert.That(resolved, Is.True);
            Assert.That(match.LayerId, Is.EqualTo(GazeDepthLayerProfile.Far1LayerId));
            Assert.That(match.RayDistance, Is.EqualTo(GazeDepthLayerProfile.Far1RayDistance).Within(0.0001f));
            Assert.That(match.DistanceError, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(match.IsWithinTolerance, Is.True);
        }

        [Test]
        public void TryResolveNearestLayer_RejectsInvalidDepth()
        {
            bool resolved = GazeDepthLayerResolver.TryResolveNearestLayer(float.NaN, out _);

            Assert.That(resolved, Is.False);
        }
    }

    public sealed class GazeDepthMeasurementRecordFactoryTests
    {
        [Test]
        public void BuildContinuousValidationRecord_ComputesErrorsAndLayerMatch()
        {
            var record = GazeDepthMeasurementRecordFactory.BuildContinuousValidationRecord(
                0,
                new Vector2(0.5f, 0.5f),
                GazeDepthLayerProfile.Near3RayDistance,
                GazeDepthLayerProfile.Near3RayDistance + 1.25f,
                42L,
                "2026-05-29T00:00:00.0000000Z");

            Assert.That(record.SignedError, Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(record.AbsoluteError, Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(record.InjectedNearestLayerId, Is.EqualTo(GazeDepthLayerProfile.Near3LayerId));
            Assert.That(record.PredictedNearestLayerId, Is.EqualTo(GazeDepthLayerProfile.Near3LayerId));
            Assert.That(record.LayerMatched, Is.True);
            Assert.That(record.SampleTimestamp, Is.EqualTo(42L));
        }
    }

    public sealed class GazeDepthMeasurementPersistenceTests
    {
        [TearDown]
        public void TearDown()
        {
            GazeExperimentDataCapture.SetEnabledOverride(null);
        }

        [Test]
        public void ResolveExperimentDataRootPath_UsesProjectRootForAssetsDirectory()
        {
            string projectRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Project Gaze");
            string applicationDataPath = Path.Combine(projectRoot, "Assets");

            string experimentDataRootPath = GazeDepthPersistence.ResolveExperimentDataRootPath(applicationDataPath);

            Assert.That(experimentDataRootPath, Is.EqualTo(Path.Combine(projectRoot, "ExperimentData")));
        }

        [Test]
        public void SaveContinuousValidationSession_WritesJsonCsvAndLatestFiles()
        {
            string rootPath = GazeDepthPersistence.BuildExperimentDataRootPath(
                Path.Combine(TestContext.CurrentContext.WorkDirectory, "continuous-depth-validation-persistence"));
            var session = new GazeDepthContinuousValidationSession
            {
                SessionId = "test-continuous-session",
                TruthSource = "InjectedRayDistance",
                ViewportLayout = "DepthProfileInterpolatedY",
                DepthLayerProfileVersion = GazeDepthLayerProfile.SymmetricSevenV1ProfileVersion,
                Records = new[]
                {
                    GazeDepthMeasurementRecordFactory.BuildContinuousValidationRecord(
                        0,
                        new Vector2(0.5f, 0.5f),
                        9.0f,
                        10.25f,
                        100L,
                        "2026-05-29T00:00:00.0000000Z")
                }
            };

            GazeDepthPersistence.SaveContinuousValidationSession(rootPath, session);

            string jsonPath = GazeDepthPersistence.BuildContinuousValidationJsonPath(rootPath, session.SessionId);
            string csvPath = GazeDepthPersistence.BuildContinuousValidationCsvPath(rootPath, session.SessionId);
            Assert.That(File.Exists(jsonPath), Is.True);
            Assert.That(File.Exists(csvPath), Is.True);
            Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationLatestJsonPath(rootPath)), Is.True);
            Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationLatestCsvPath(rootPath)), Is.True);
            Assert.That(File.ReadAllText(csvPath), Does.Contain("InjectedRayDistance"));
            Assert.That(File.ReadAllText(csvPath), Does.Contain("10.25"));
        }

        [Test]
        public void ExperimentDataCapture_WhenDisabled_DoesNotSaveContinuousValidationSession()
        {
            string rootPath = CreateExperimentDataRootPath(nameof(ExperimentDataCapture_WhenDisabled_DoesNotSaveContinuousValidationSession));
            var session = CreateContinuousValidationSession("disabled-continuous-session");
            GazeExperimentDataCapture.SetEnabledOverride(false);

            bool saved = GazeExperimentDataCapture.TrySaveContinuousValidationSession(rootPath, session);

            Assert.That(saved, Is.False);
            Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationJsonPath(rootPath, session.SessionId)), Is.False);
            Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationCsvPath(rootPath, session.SessionId)), Is.False);
            Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationLatestJsonPath(rootPath)), Is.False);
            Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationLatestCsvPath(rootPath)), Is.False);
        }

        [Test]
        public void ExperimentDataCapture_WhenEnabled_SavesContinuousValidationSession()
        {
            string rootPath = CreateExperimentDataRootPath(nameof(ExperimentDataCapture_WhenEnabled_SavesContinuousValidationSession));
            var session = CreateContinuousValidationSession("enabled-continuous-session");
            GazeExperimentDataCapture.SetEnabledOverride(true);

            bool saved = GazeExperimentDataCapture.TrySaveContinuousValidationSession(rootPath, session);

            Assert.That(saved, Is.True);
            Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationJsonPath(rootPath, session.SessionId)), Is.True);
            Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationCsvPath(rootPath, session.SessionId)), Is.True);
            Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationLatestJsonPath(rootPath)), Is.True);
            Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationLatestCsvPath(rootPath)), Is.True);
        }

        [Test]
        public void ExperimentDataCapture_ContinuousValidationRespectsSubjectTestFlowOverrideModes()
        {
            string rootPath = CreateExperimentDataRootPath(nameof(ExperimentDataCapture_ContinuousValidationRespectsSubjectTestFlowOverrideModes));
            var disabledSession = CreateContinuousValidationSession("subject-flow-disabled-session");
            GazeExperimentDataCapture.SetEnabledOverride(
                SubjectTestFlowController.ResolveDataCaptureOverride(SubjectTestFlowDataCaptureMode.ForceDisabled));

            bool disabledSaved = GazeExperimentDataCapture.TrySaveContinuousValidationSession(rootPath, disabledSession);

            var enabledSession = CreateContinuousValidationSession("subject-flow-enabled-session");
            GazeExperimentDataCapture.SetEnabledOverride(
                SubjectTestFlowController.ResolveDataCaptureOverride(SubjectTestFlowDataCaptureMode.ForceEnabled));
            bool enabledSaved = GazeExperimentDataCapture.TrySaveContinuousValidationSession(rootPath, enabledSession);

            Assert.That(disabledSaved, Is.False);
            Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationCsvPath(rootPath, disabledSession.SessionId)), Is.False);
            Assert.That(enabledSaved, Is.True);
            Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationCsvPath(rootPath, enabledSession.SessionId)), Is.True);
        }

        [Test]
        public void ExperimentDataCapture_EnvironmentVariableFalse_DoesNotSaveContinuousValidationSession()
        {
            string previousValue = System.Environment.GetEnvironmentVariable(GazeExperimentDataCapture.EnvironmentVariableName);
            string rootPath = CreateExperimentDataRootPath(nameof(ExperimentDataCapture_EnvironmentVariableFalse_DoesNotSaveContinuousValidationSession));
            var session = CreateContinuousValidationSession("environment-disabled-continuous-session");
            try
            {
                GazeExperimentDataCapture.SetEnabledOverride(null);
                System.Environment.SetEnvironmentVariable(GazeExperimentDataCapture.EnvironmentVariableName, "0");

                bool saved = GazeExperimentDataCapture.TrySaveContinuousValidationSession(rootPath, session);

                Assert.That(saved, Is.False);
                Assert.That(File.Exists(GazeDepthPersistence.BuildContinuousValidationCsvPath(rootPath, session.SessionId)), Is.False);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable(GazeExperimentDataCapture.EnvironmentVariableName, previousValue);
                GazeExperimentDataCapture.SetEnabledOverride(null);
            }
        }

        [Test]
        public void SaveRuntimeLayerSession_WritesJsonCsvAndLatestFiles()
        {
            string rootPath = GazeDepthPersistence.BuildExperimentDataRootPath(
                Path.Combine(TestContext.CurrentContext.WorkDirectory, "runtime-depth-layer-persistence"));
            var session = new GazeDepthRuntimeLayerSession
            {
                SessionId = "test-runtime-session",
                SceneId = LayeredPagesDemo.SceneName,
                TruthSource = "RuntimeSystemHitDepthLayer",
                DepthLayerProfileVersion = GazeDepthLayerProfile.SymmetricSevenV1ProfileVersion,
                Records = new[]
                {
                    new GazeDepthRuntimeLayerRecord
                    {
                        RecordIndex = 0,
                        SceneId = LayeredPagesDemo.SceneName,
                        MatchingMode = GazeDepthMatchingMode.DiscreteDepthLayer.ToString(),
                        PredictedRayDistance = 18.5f,
                        SystemHitPageId = "Page_A",
                        SystemHitDepthLayerId = GazeDepthLayerProfile.ZeroLayerId
                    }
                }
            };

            GazeDepthPersistence.SaveRuntimeLayerSession(rootPath, session);

            string jsonPath = GazeDepthPersistence.BuildRuntimeLayerJsonPath(rootPath, session.SceneId, session.SessionId);
            string csvPath = GazeDepthPersistence.BuildRuntimeLayerCsvPath(rootPath, session.SceneId, session.SessionId);
            Assert.That(File.Exists(jsonPath), Is.True);
            Assert.That(File.Exists(csvPath), Is.True);
            Assert.That(File.Exists(GazeDepthPersistence.BuildRuntimeLayerLatestJsonPath(rootPath, session.SceneId)), Is.True);
            Assert.That(File.Exists(GazeDepthPersistence.BuildRuntimeLayerLatestCsvPath(rootPath, session.SceneId)), Is.True);
            Assert.That(jsonPath, Does.Contain("runtime-depth-layer-LayeredPagesScene-test-runtime-session"));
            Assert.That(csvPath, Does.Contain("runtime-depth-layer-LayeredPagesScene-test-runtime-session"));
            Assert.That(File.ReadAllText(csvPath), Does.Contain("RuntimeSystemHitDepthLayer"));
            Assert.That(File.ReadAllText(csvPath), Does.Contain("Page_A"));
        }

        [Test]
        public void SaveRuntimeLayerSession_WritesSeparateLatestFilesPerScene()
        {
            string rootPath = GazeDepthPersistence.BuildExperimentDataRootPath(
                Path.Combine(TestContext.CurrentContext.WorkDirectory, "runtime-depth-layer-per-scene-persistence"));
            var layeredSession = new GazeDepthRuntimeLayerSession
            {
                SessionId = "layered-session",
                SceneId = LayeredPagesDemo.SceneName,
                TruthSource = "RuntimeSystemHitDepthLayer",
                Records = new[] { new GazeDepthRuntimeLayerRecord { SceneId = LayeredPagesDemo.SceneName } }
            };
            var agentSession = new GazeDepthRuntimeLayerSession
            {
                SessionId = "agent-session",
                SceneId = DepthGatedAgentDemo.SceneName,
                TruthSource = "RuntimeSystemHitDepthLayer",
                Records = new[] { new GazeDepthRuntimeLayerRecord { SceneId = DepthGatedAgentDemo.SceneName } }
            };

            GazeDepthPersistence.SaveRuntimeLayerSession(rootPath, layeredSession);
            GazeDepthPersistence.SaveRuntimeLayerSession(rootPath, agentSession);

            Assert.That(File.Exists(GazeDepthPersistence.BuildRuntimeLayerLatestCsvPath(rootPath, LayeredPagesDemo.SceneName)), Is.True);
            Assert.That(File.Exists(GazeDepthPersistence.BuildRuntimeLayerLatestCsvPath(rootPath, DepthGatedAgentDemo.SceneName)), Is.True);
            Assert.That(
                GazeDepthPersistence.BuildRuntimeLayerLatestCsvPath(rootPath, LayeredPagesDemo.SceneName),
                Is.Not.EqualTo(GazeDepthPersistence.BuildRuntimeLayerLatestCsvPath(rootPath, DepthGatedAgentDemo.SceneName)));
        }

        [Test]
        public void SaveRuntimeLayerSession_EscapesCsvFieldsAndSanitizesSessionId()
        {
            string rootPath = GazeDepthPersistence.BuildExperimentDataRootPath(
                Path.Combine(TestContext.CurrentContext.WorkDirectory, "runtime-depth-layer-sanitized-persistence"));
            var session = new GazeDepthRuntimeLayerSession
            {
                SessionId = "runtime/session:bad*chars",
                SceneId = LayeredPagesDemo.SceneName,
                TruthSource = "RuntimeSystemHitDepthLayer",
                DepthLayerProfileVersion = GazeDepthLayerProfile.SymmetricSevenV1ProfileVersion,
                Records = new[]
                {
                    new GazeDepthRuntimeLayerRecord
                    {
                        RecordIndex = 0,
                        SceneId = LayeredPagesDemo.SceneName,
                        InputSource = "Mouse,Fallback",
                        MatchingMode = "DiscreteDepthLayer",
                        PredictedRayDistance = 18.5f,
                        SystemHitPageId = "Page, \"Quoted\"",
                        PreviewPageId = "Line\nBreak",
                        SystemHitDepthLayerId = GazeDepthLayerProfile.ZeroLayerId
                    }
                }
            };

            GazeDepthPersistence.SaveRuntimeLayerSession(rootPath, session);

            string csvPath = GazeDepthPersistence.BuildRuntimeLayerCsvPath(rootPath, session.SceneId, session.SessionId);
            string csv = File.ReadAllText(csvPath);

            Assert.That(csvPath, Does.Contain("runtime-depth-layer-LayeredPagesScene-runtime-session-bad-chars"));
            Assert.That(csvPath, Does.Contain("runtime-session-bad-chars"));
            Assert.That(csv, Does.Contain("\"Mouse,Fallback\""));
            Assert.That(csv, Does.Contain("\"Page, \"\"Quoted\"\"\""));
            Assert.That(csv, Does.Contain("\"Line\nBreak\""));
        }

        private static string CreateExperimentDataRootPath(string testName)
        {
            string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "DepthMeasurementTests", testName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }

            Directory.CreateDirectory(path);
            return path;
        }

        private static GazeDepthContinuousValidationSession CreateContinuousValidationSession(string sessionId)
        {
            return new GazeDepthContinuousValidationSession
            {
                SessionId = sessionId,
                TruthSource = "InjectedRayDistance",
                ViewportLayout = "DepthProfileInterpolatedY",
                DepthLayerProfileVersion = GazeDepthLayerProfile.SymmetricSevenV1ProfileVersion,
                Records = new[]
                {
                    GazeDepthMeasurementRecordFactory.BuildContinuousValidationRecord(
                        0,
                        new Vector2(0.5f, 0.5f),
                        9.0f,
                        10.25f,
                        100L,
                        "2026-05-29T00:00:00.0000000Z")
                }
            };
        }
    }
}
