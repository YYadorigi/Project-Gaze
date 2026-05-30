using NUnit.Framework;
using ProjectGaze.Gaze;
using ProjectGaze.Gaze.Providers;
using System.IO;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class InvensunA8InputUtilityTests
    {
        [Test]
        public void IsFlagSet_ReturnsTrueWhenRequestedBitIsPresent()
        {
            uint bitMask = 1u << (int)InvensunA8GazeValidityBit.GazePoint;

            bool result = InvensunA8BitMaskUtility.IsFlagSet(bitMask, InvensunA8GazeValidityBit.GazePoint);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsEyeOpen_PrefersBlinkFlagOverOpenness()
        {
            bool isOpen = InvensunA8BlinkSignalUtility.IsEyeOpen(
                blinkDetected: true,
                openness: 0.95f,
                opennessThreshold: 0.35f);

            Assert.That(isOpen, Is.False);
        }

        [Test]
        public void IsEyeOpen_FallsBackToGazeSignalWhenOpennessIsUnavailable()
        {
            bool isOpen = InvensunA8BlinkSignalUtility.IsEyeOpen(
                blinkDetected: false,
                openness: float.NaN,
                opennessThreshold: 0.35f);

            Assert.That(isOpen, Is.True);
        }

        [Test]
        public void IsEyeOpen_DoesNotTreatUnavailableOpennessAsClosed()
        {
            bool isOpen = InvensunA8BlinkSignalUtility.IsEyeOpen(
                blinkDetected: false,
                openness: float.NaN,
                opennessThreshold: 0.35f);

            Assert.That(isOpen, Is.True);
        }

        [Test]
        public void IsEyeOpen_TreatsZeroOpennessAsUnavailableAndFallsBackToGazeSignal()
        {
            bool isOpen = InvensunA8BlinkSignalUtility.IsEyeOpen(
                blinkDetected: false,
                openness: 0f,
                opennessThreshold: 0.35f);

            Assert.That(isOpen, Is.True);
        }

        [Test]
        public void ShouldCopyTemplateFile_ReturnsFalseForMetaFiles()
        {
            bool shouldCopy = InvensunA8RuntimeLayoutUtility.ShouldCopyTemplateFile(@"G:\Project\Assets\StreamingAssets\7ia8\config\detectEye.mnn.meta");

            Assert.That(shouldCopy, Is.False);
        }

        [Test]
        public void ShouldResetRuntimeDirectory_ReturnsTrueForArmeabiLogFolder()
        {
            string path = Path.Combine("config", "armeabi", "log");

            bool shouldReset = InvensunA8RuntimeLayoutUtility.ShouldResetRuntimeDirectory(path);

            Assert.That(shouldReset, Is.True);
        }

        [Test]
        public void BuildRuntimeConfigPath_PlacesConfigUnderSdkRoot()
        {
            string runtimeRootPath = Path.Combine("C:\\Temp", "7ia8-runtime");

            string configPath = InvensunA8RuntimeLayoutUtility.BuildRuntimeConfigPath(runtimeRootPath);

            Assert.That(configPath, Is.EqualTo(Path.Combine(runtimeRootPath, "config")));
        }

        [Test]
        public void BuildVendorStyleRuntimeRootPath_Matches7InvensunDemoLayout()
        {
            string runtimeRootPath = InvensunA8RuntimeLayoutUtility.BuildVendorStyleRuntimeRootPath("C:\\ProjectGaze");

            Assert.That(runtimeRootPath, Is.EqualTo(Path.Combine("C:\\ProjectGaze", "7ia8")));
        }

        [Test]
        public void DescribeRuntimeStartFailure_ExplainsCameraPidMismatch()
        {
            string message = InvensunA8NativeErrorMessages.DescribeRuntimeStartFailure(-2112, "C:\\ProjectGaze\\7ia8\\config");

            Assert.That(message, Does.Contain("camera PID"));
            Assert.That(message, Does.Contain("C:\\ProjectGaze\\7ia8\\config"));
        }

        [Test]
        public void GetDemoStyleCalibrationPoint_MatchesVendorDemoPointOrder()
        {
            Assert.That(InvensunA8CalibrationUtility.GetDemoStyleCalibrationPoint(0), Is.EqualTo(new UnityEngine.Vector2(0.50f, 0.50f)));
            Assert.That(InvensunA8CalibrationUtility.GetDemoStyleCalibrationPoint(1), Is.EqualTo(new UnityEngine.Vector2(0.08f, 0.08f)));
            Assert.That(InvensunA8CalibrationUtility.GetDemoStyleCalibrationPoint(2), Is.EqualTo(new UnityEngine.Vector2(0.92f, 0.92f)));
            Assert.That(InvensunA8CalibrationUtility.GetDemoStyleCalibrationPoint(8), Is.EqualTo(new UnityEngine.Vector2(0.08f, 0.50f)));
        }

        [Test]
        public void GetDemoStyleCalibrationAnchoredPosition_UsesEditorSafeInsetLayout()
        {
            Assert.That(InvensunA8CalibrationUtility.GetDemoStyleCalibrationAnchoredPosition(0), Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(InvensunA8CalibrationUtility.GetDemoStyleCalibrationAnchoredPosition(1), Is.EqualTo(new Vector2(-806.4f, 453.6f)));
            Assert.That(InvensunA8CalibrationUtility.GetDemoStyleCalibrationAnchoredPosition(2), Is.EqualTo(new Vector2(806.4f, -453.6f)));
            Assert.That(InvensunA8CalibrationUtility.GetDemoStyleCalibrationAnchoredPosition(8), Is.EqualTo(new Vector2(-806.4f, 0f)));
        }

        [Test]
        public void ConvertAnchoredPositionToSdkPoint_UsesTopLeftOriginLikeVendorCalibration()
        {
            Vector2 sdkPoint = InvensunA8CalibrationUtility.ConvertAnchoredPositionToSdkPoint(
                new Vector2(-806.4f, 453.6f),
                InvensunA8CalibrationUtility.VendorCalibrationReferenceResolution);

            Assert.That(sdkPoint.x, Is.EqualTo(0.08f).Within(0.0001f));
            Assert.That(sdkPoint.y, Is.EqualTo(0.08f).Within(0.0001f));
        }

        [Test]
        public void MeetsRecommendedScore_RequiresBothEyesToMeetThreshold()
        {
            bool accepted = InvensunA8CalibrationUtility.MeetsRecommendedScore(95.2f, 94.9f, 95.0f);

            Assert.That(accepted, Is.False);
        }

        [Test]
        public void BuildCalibrationMetadataPath_PlacesMetadataUnderCalibrationPersistenceRoot()
        {
            string calibrationRootPath = Path.Combine("C:\\Temp", "7ia8-calibration");

            string metadataPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationMetadataPath(calibrationRootPath);

            Assert.That(metadataPath, Is.EqualTo(Path.Combine(calibrationRootPath, "calibration-state.json")));
        }

        [Test]
        public void BuildCalibrationPersistenceRootPath_UsesDedicatedDirectoryInsteadOfRuntimeStagingRoot()
        {
            string calibrationRootPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationPersistenceRootPath("C:\\Temp");

            Assert.That(calibrationRootPath, Is.EqualTo(Path.Combine("C:\\Temp", "7ia8-calibration")));
        }

        [Test]
        public void BuildCalibrationCoefficientPath_PlacesCoefficientUnderCalibrationPersistenceRoot()
        {
            string calibrationRootPath = Path.Combine("C:\\Temp", "7ia8-calibration");

            string coefficientPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationCoefficientPath(calibrationRootPath);

            Assert.That(coefficientPath, Is.EqualTo(Path.Combine(calibrationRootPath, "coefficient.dat")));
        }

        [Test]
        public void ResolveNativeSdkWorkingDirectory_UsesApplicationDataParent()
        {
            string applicationDataPath = Path.Combine("C:\\Project Gaze", "Assets");

            string workingDirectory = InvensunA8RuntimeLayoutUtility.ResolveNativeSdkWorkingDirectory(
                applicationDataPath,
                "C:\\Fallback");

            Assert.That(workingDirectory, Is.EqualTo("C:\\Project Gaze"));
        }

        [Test]
        public void ResolveNativeSdkWorkingDirectory_FallsBackToCurrentDirectory()
        {
            string workingDirectory = InvensunA8RuntimeLayoutUtility.ResolveNativeSdkWorkingDirectory(
                null,
                "C:\\Fallback");

            Assert.That(workingDirectory, Is.EqualTo("C:\\Fallback"));
        }

        [Test]
        public void SaveCoefficient_RejectsUnexpectedByteLength()
        {
            string rootPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "invalid-coefficient");

            Assert.Throws<System.ArgumentException>(() =>
                InvensunA8CalibrationPersistenceUtility.SaveCoefficient(rootPath, new byte[16]));
        }

        [Test]
        public void TryResolveAcceptedCoefficientPath_RejectsUnacceptedMetadata()
        {
            string rootPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "rejected-calibration");
            InvensunA8CalibrationPersistenceUtility.SaveCoefficient(
                rootPath,
                new byte[InvensunA8CalibrationPersistenceUtility.ExpectedCoefficientBytes]);
            InvensunA8CalibrationPersistenceUtility.SaveMetadata(
                rootPath,
                new InvensunA8CalibrationMetadata
                {
                    AcceptedForPlay = false,
                    MeetsRecommendedThreshold = false,
                    HasDepthCalibrationModel = false
                });

            bool resolved = InvensunA8CalibrationPersistenceUtility.TryResolveAcceptedCoefficientPath(
                rootPath,
                out _,
                out _);

            Assert.That(resolved, Is.False);
        }

        [Test]
        public void TryResolveAcceptedCoefficientPath_ReturnsAcceptedCoefficient()
        {
            string rootPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "accepted-calibration");
            InvensunA8CalibrationPersistenceUtility.SaveCoefficient(
                rootPath,
                new byte[InvensunA8CalibrationPersistenceUtility.ExpectedCoefficientBytes]);
            InvensunA8CalibrationPersistenceUtility.SaveMetadata(
                rootPath,
                new InvensunA8CalibrationMetadata
                {
                    AcceptedForPlay = true,
                    MeetsRecommendedThreshold = true,
                    HasDepthCalibrationModel = true
                });

            bool resolved = InvensunA8CalibrationPersistenceUtility.TryResolveAcceptedCoefficientPath(
                rootPath,
                out string coefficientPath,
                out var metadata);

            Assert.That(resolved, Is.True);
            Assert.That(coefficientPath, Does.EndWith("coefficient.dat"));
            Assert.That(metadata.AcceptedForPlay, Is.True);
        }

        [Test]
        public void TryResolveAveragePupilCenter_RequiresBothEyesToBePresent()
        {
            bool resolved = InvensunA8PostureUtility.TryResolveAveragePupilCenter(
                new Vector2(0f, 0.4f),
                new Vector2(0.6f, 0.4f),
                out _);

            Assert.That(resolved, Is.False);
        }

        [Test]
        public void IsHeadCentered_ReturnsTrueWhenAveragePupilCenterFallsInsideVendorWindow()
        {
            bool centered = InvensunA8PostureUtility.IsHeadCentered(new Vector2(0.5f, 0.5f), 0.3f, 0.7f);

            Assert.That(centered, Is.True);
        }

        [Test]
        public void CalculateEyeAngleDegrees_MatchesVendorMoveHmdSignConvention()
        {
            float angle = InvensunA8PostureUtility.CalculateEyeAngleDegrees(
                new Vector2(0.4f, 0.3f),
                new Vector2(0.6f, 0.5f));

            Assert.That(angle, Is.LessThan(0f));
        }

    }
}
