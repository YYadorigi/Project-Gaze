using System;
using System.IO;
using UnityEngine;

namespace ProjectGaze.Gaze.Providers
{
    [Serializable]
    public sealed class InvensunA8CalibrationMetadata
    {
        public bool AcceptedForPlay;
        public bool MeetsRecommendedThreshold;
        public float LeftScore;
        public float RightScore;
        public float RecommendedThreshold;
        public bool HasDepthCalibrationModel;
        public int DepthCalibrationRecordCount;
        public string SavedAtUtc;
    }

    public static class InvensunA8CalibrationUtility
    {
        private const float OuterAnchoredX = 806.4f;
        private const float OuterAnchoredY = 453.6f;

        private static readonly Vector2[] DemoStyleCalibrationAnchoredPositions =
        {
            new(0f, 0f),
            new(-OuterAnchoredX, OuterAnchoredY),
            new(OuterAnchoredX, -OuterAnchoredY),
            new(OuterAnchoredX, OuterAnchoredY),
            new(-OuterAnchoredX, -OuterAnchoredY),
            new(0f, OuterAnchoredY),
            new(OuterAnchoredX, 0f),
            new(0f, -OuterAnchoredY),
            new(-OuterAnchoredX, 0f)
        };

        public static readonly Vector2 VendorCalibrationReferenceResolution = new(1920f, 1080f);

        public static int CalibrationPointCount => DemoStyleCalibrationAnchoredPositions.Length;

        public static Vector2 GetDemoStyleCalibrationPoint(int index)
        {
            return ConvertAnchoredPositionToSdkPoint(
                GetDemoStyleCalibrationAnchoredPosition(index),
                VendorCalibrationReferenceResolution);
        }

        public static Vector2 GetDemoStyleCalibrationAnchoredPosition(int index)
        {
            if (index < 0 || index >= DemoStyleCalibrationAnchoredPositions.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return DemoStyleCalibrationAnchoredPositions[index];
        }

        public static Vector2 ConvertAnchoredPositionToSdkPoint(
            Vector2 anchoredPosition,
            Vector2 referenceResolution)
        {
            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(referenceResolution));
            }

            return new Vector2(
                0.5f + (anchoredPosition.x / referenceResolution.x),
                0.5f - (anchoredPosition.y / referenceResolution.y));
        }

        public static bool MeetsRecommendedScore(float leftScore, float rightScore, float threshold)
        {
            return leftScore >= threshold && rightScore >= threshold;
        }
    }

    public static class InvensunA8PostureUtility
    {
        public static bool TryResolveAveragePupilCenter(
            Vector2 leftPupilCenter,
            Vector2 rightPupilCenter,
            out Vector2 averagePupilCenter)
        {
            averagePupilCenter = default;

            if (!IsUsablePupilCenter(leftPupilCenter) || !IsUsablePupilCenter(rightPupilCenter))
            {
                return false;
            }

            averagePupilCenter = (leftPupilCenter + rightPupilCenter) * 0.5f;
            return true;
        }

        public static bool IsHeadCentered(Vector2 averagePupilCenter, float minInclusive, float maxInclusive)
        {
            return averagePupilCenter.x >= minInclusive &&
                   averagePupilCenter.x <= maxInclusive &&
                   averagePupilCenter.y >= minInclusive &&
                   averagePupilCenter.y <= maxInclusive;
        }

        public static float CalculateEyeAngleDegrees(Vector2 leftPupilCenter, Vector2 rightPupilCenter)
        {
            if (!IsUsablePupilCenter(leftPupilCenter) || !IsUsablePupilCenter(rightPupilCenter))
            {
                return 0f;
            }

            float horizontalDistance = Mathf.Abs(leftPupilCenter.x - rightPupilCenter.x);
            float verticalDistance = Mathf.Abs(leftPupilCenter.y - rightPupilCenter.y);

            if (horizontalDistance <= Mathf.Epsilon)
            {
                return 0f;
            }

            float angleDegrees = Mathf.Atan(verticalDistance / horizontalDistance) * Mathf.Rad2Deg;
            return leftPupilCenter.y < rightPupilCenter.y ? -angleDegrees : angleDegrees;
        }

        private static bool IsUsablePupilCenter(Vector2 pupilCenter)
        {
            return !float.IsNaN(pupilCenter.x) &&
                   !float.IsNaN(pupilCenter.y) &&
                   !float.IsInfinity(pupilCenter.x) &&
                   !float.IsInfinity(pupilCenter.y) &&
                   pupilCenter.x > 0f &&
                   pupilCenter.y > 0f;
        }
    }

    public interface ICalibrationArtifactService
    {
        bool TryLoadAcceptedCalibration(string persistentDataPath, out InvensunA8CalibrationMetadata metadata);

        bool TryResolveAcceptedCoefficientPath(
            string persistentDataPath,
            out string coefficientPath,
            out InvensunA8CalibrationMetadata metadata);

        void SaveCoefficient(string persistentDataPath, byte[] coefficientBuffer);

        void SaveMetadata(string persistentDataPath, InvensunA8CalibrationMetadata metadata);
    }

    public sealed class CalibrationArtifactService : ICalibrationArtifactService
    {
        public bool TryLoadAcceptedCalibration(string persistentDataPath, out InvensunA8CalibrationMetadata metadata)
        {
            return InvensunA8CalibrationPersistenceUtility.TryLoadAcceptedCalibration(
                persistentDataPath,
                out metadata);
        }

        public bool TryResolveAcceptedCoefficientPath(
            string persistentDataPath,
            out string coefficientPath,
            out InvensunA8CalibrationMetadata metadata)
        {
            return InvensunA8CalibrationPersistenceUtility.TryResolveAcceptedCoefficientPath(
                persistentDataPath,
                out coefficientPath,
                out metadata);
        }

        public void SaveCoefficient(string persistentDataPath, byte[] coefficientBuffer)
        {
            InvensunA8CalibrationPersistenceUtility.SaveCoefficient(persistentDataPath, coefficientBuffer);
        }

        public void SaveMetadata(string persistentDataPath, InvensunA8CalibrationMetadata metadata)
        {
            InvensunA8CalibrationPersistenceUtility.SaveMetadata(persistentDataPath, metadata);
        }
    }

    public static class InvensunA8CalibrationPersistenceUtility
    {
        public const int ExpectedCoefficientBytes = InvensunA8Native.CoefficientBytes;

        public static bool TryLoadAcceptedCalibration(string persistentDataPath, out InvensunA8CalibrationMetadata metadata)
        {
            metadata = null;
            string calibrationRootPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationPersistenceRootPath(persistentDataPath);
            string coefficientPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationCoefficientPath(calibrationRootPath);
            string metadataPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationMetadataPath(calibrationRootPath);

            if (!TryLoadAcceptedCalibration(coefficientPath, metadataPath, out metadata))
            {
                string legacyRuntimeRootPath = InvensunA8RuntimeLayoutUtility.BuildRuntimeRootPath(persistentDataPath);
                string legacyCoefficientPath = InvensunA8RuntimeLayoutUtility.BuildRuntimeCoefficientPath(legacyRuntimeRootPath);
                string legacyMetadataPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationMetadataPath(legacyRuntimeRootPath);
                return TryLoadAcceptedCalibration(legacyCoefficientPath, legacyMetadataPath, out metadata);
            }

            return true;
        }

        public static bool TryResolveAcceptedCoefficientPath(
            string persistentDataPath,
            out string coefficientPath,
            out InvensunA8CalibrationMetadata metadata)
        {
            string calibrationRootPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationPersistenceRootPath(persistentDataPath);
            coefficientPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationCoefficientPath(calibrationRootPath);
            string metadataPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationMetadataPath(calibrationRootPath);

            if (TryLoadAcceptedCalibration(coefficientPath, metadataPath, out metadata))
            {
                return true;
            }

            string legacyRuntimeRootPath = InvensunA8RuntimeLayoutUtility.BuildRuntimeRootPath(persistentDataPath);
            coefficientPath = InvensunA8RuntimeLayoutUtility.BuildRuntimeCoefficientPath(legacyRuntimeRootPath);
            metadataPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationMetadataPath(legacyRuntimeRootPath);

            if (TryLoadAcceptedCalibration(coefficientPath, metadataPath, out metadata))
            {
                return true;
            }

            coefficientPath = null;
            metadata = null;
            return false;
        }

        private static bool TryLoadAcceptedCalibration(
            string coefficientPath,
            string metadataPath,
            out InvensunA8CalibrationMetadata metadata)
        {
            metadata = null;

            if (!File.Exists(coefficientPath) || !File.Exists(metadataPath))
            {
                return false;
            }

            try
            {
                metadata = JsonUtility.FromJson<InvensunA8CalibrationMetadata>(File.ReadAllText(metadataPath));
            }
            catch
            {
                metadata = null;
            }

            return metadata != null &&
                   metadata.AcceptedForPlay &&
                   metadata.HasDepthCalibrationModel &&
                   TryReadCoefficient(coefficientPath, out _, out _);
        }

        public static bool TryReadCoefficient(string coefficientPath, out byte[] coefficientBuffer, out string failureReason)
        {
            coefficientBuffer = null;
            failureReason = null;

            if (string.IsNullOrWhiteSpace(coefficientPath))
            {
                failureReason = "Coefficient path is empty.";
                return false;
            }

            if (!File.Exists(coefficientPath))
            {
                failureReason = $"Coefficient file does not exist: {coefficientPath}";
                return false;
            }

            byte[] buffer;
            try
            {
                buffer = File.ReadAllBytes(coefficientPath);
            }
            catch (Exception exception)
            {
                failureReason = $"Failed to read coefficient file '{coefficientPath}': {exception.Message}";
                return false;
            }

            if (!IsValidCoefficientBuffer(buffer))
            {
                failureReason = $"Coefficient file '{coefficientPath}' has {buffer?.Length ?? 0} bytes; expected {ExpectedCoefficientBytes}.";
                return false;
            }

            coefficientBuffer = buffer;
            return true;
        }

        public static bool IsValidCoefficientBuffer(byte[] coefficientBuffer)
        {
            return coefficientBuffer != null && coefficientBuffer.Length == ExpectedCoefficientBytes;
        }

        public static void SaveCoefficient(string persistentDataPath, byte[] coefficientBuffer)
        {
            if (!IsValidCoefficientBuffer(coefficientBuffer))
            {
                int byteCount = coefficientBuffer?.Length ?? 0;
                throw new ArgumentException(
                    $"Calibration coefficient buffer must contain exactly {ExpectedCoefficientBytes} bytes. Actual: {byteCount}.",
                    nameof(coefficientBuffer));
            }

            string calibrationRootPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationPersistenceRootPath(persistentDataPath);
            Directory.CreateDirectory(calibrationRootPath);
            File.WriteAllBytes(InvensunA8RuntimeLayoutUtility.BuildCalibrationCoefficientPath(calibrationRootPath), coefficientBuffer);
        }

        public static void SaveMetadata(string persistentDataPath, InvensunA8CalibrationMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            string calibrationRootPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationPersistenceRootPath(persistentDataPath);
            Directory.CreateDirectory(calibrationRootPath);
            File.WriteAllText(
                InvensunA8RuntimeLayoutUtility.BuildCalibrationMetadataPath(calibrationRootPath),
                JsonUtility.ToJson(metadata, prettyPrint: true));
        }
    }
}
