using System.IO;
using NUnit.Framework;
using ProjectGaze.Gaze;
using ProjectGaze.Gaze.Providers;
using ProjectGaze.Hardware.ThinkVision;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class GazeSceneInputModeResolverTests
    {
        [Test]
        public void ShouldUseStereoGazeInput_ReturnsFalseWhenCalibrationIsMissing()
        {
            string persistentDataPath = CreatePersistentDataPath(nameof(ShouldUseStereoGazeInput_ReturnsFalseWhenCalibrationIsMissing));

            bool shouldUseStereo = GazeSceneInputModeResolver.ShouldUseStereoGazeInput(
                new FakeThinkVisionDisplayBridge(isStereoDisplayActive: true),
                persistentDataPath,
                forceMouseFallback: false);

            Assert.That(shouldUseStereo, Is.False);
        }

        [Test]
        public void ShouldUseStereoGazeInput_ReturnsFalseWhenMouseFallbackIsForced()
        {
            string persistentDataPath = CreatePersistentDataPath(nameof(ShouldUseStereoGazeInput_ReturnsFalseWhenMouseFallbackIsForced));
            SaveAcceptedCalibration(persistentDataPath);

            bool shouldUseStereo = GazeSceneInputModeResolver.ShouldUseStereoGazeInput(
                new FakeThinkVisionDisplayBridge(isStereoDisplayActive: true),
                persistentDataPath,
                forceMouseFallback: true);

            Assert.That(shouldUseStereo, Is.False);
        }

        [Test]
        public void ShouldUseStereoGazeInput_ReturnsTrueWhenStereoAndAcceptedCalibrationAreAvailable()
        {
            string persistentDataPath = CreatePersistentDataPath(nameof(ShouldUseStereoGazeInput_ReturnsTrueWhenStereoAndAcceptedCalibrationAreAvailable));
            SaveAcceptedCalibration(persistentDataPath);

            bool shouldUseStereo = GazeSceneInputModeResolver.ShouldUseStereoGazeInput(
                new FakeThinkVisionDisplayBridge(isStereoDisplayActive: true),
                persistentDataPath,
                forceMouseFallback: false);

            Assert.That(shouldUseStereo, Is.True);
        }

        private static string CreatePersistentDataPath(string testName)
        {
            string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "GazeInputModeTests", testName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }

            Directory.CreateDirectory(path);
            return path;
        }

        private static void SaveAcceptedCalibration(string persistentDataPath)
        {
            InvensunA8CalibrationPersistenceUtility.SaveCoefficient(
                persistentDataPath,
                new byte[InvensunA8CalibrationPersistenceUtility.ExpectedCoefficientBytes]);
            InvensunA8CalibrationPersistenceUtility.SaveMetadata(
                persistentDataPath,
                new InvensunA8CalibrationMetadata
                {
                    AcceptedForPlay = true,
                    MeetsRecommendedThreshold = true,
                    HasDepthCalibrationModel = true,
                    DepthCalibrationRecordCount = 1
                });
        }

        private sealed class FakeThinkVisionDisplayBridge : IThinkVisionDisplayBridge
        {
            public FakeThinkVisionDisplayBridge(bool isStereoDisplayActive)
            {
                IsStereoDisplayActive = isStereoDisplayActive;
            }

            public bool IsVendorRuntimeDetected => true;

            public bool IsStereoDisplayActive { get; }

            public string StatusText => "Fake ThinkVision display bridge.";

            public void Configure(Camera camera)
            {
            }
        }
    }
}
