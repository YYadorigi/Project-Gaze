using UnityEngine;

namespace ProjectGaze.Gaze.Depth
{
    public readonly struct BinocularEyeObservation
    {
        public BinocularEyeObservation(
            bool hasScreenPoint,
            Vector2 screenPointNormalized,
            bool hasPupilCenter,
            Vector2 pupilCenterNormalized,
            bool hasGazeOrigin,
            Vector3 gazeOrigin,
            bool hasGazeDirection,
            Vector3 gazeDirection)
        {
            HasScreenPoint = hasScreenPoint;
            ScreenPointNormalized = screenPointNormalized;
            HasPupilCenter = hasPupilCenter;
            PupilCenterNormalized = pupilCenterNormalized;
            HasGazeOrigin = hasGazeOrigin;
            GazeOrigin = gazeOrigin;
            HasGazeDirection = hasGazeDirection;
            GazeDirection = gazeDirection;
        }

        public bool HasScreenPoint { get; }

        public Vector2 ScreenPointNormalized { get; }

        public bool HasPupilCenter { get; }

        public Vector2 PupilCenterNormalized { get; }

        public bool HasGazeOrigin { get; }

        public Vector3 GazeOrigin { get; }

        public bool HasGazeDirection { get; }

        public Vector3 GazeDirection { get; }
    }

    public readonly struct BinocularGazeSample
    {
        public BinocularGazeSample(
            bool hasRecommendedScreenPoint,
            Vector2 recommendedScreenPointNormalized,
            BinocularEyeObservation leftEye,
            BinocularEyeObservation rightEye,
            long timestamp)
        {
            HasRecommendedScreenPoint = hasRecommendedScreenPoint;
            RecommendedScreenPointNormalized = recommendedScreenPointNormalized;
            LeftEye = leftEye;
            RightEye = rightEye;
            Timestamp = timestamp;
        }

        public bool HasRecommendedScreenPoint { get; }

        public Vector2 RecommendedScreenPointNormalized { get; }

        public BinocularEyeObservation LeftEye { get; }

        public BinocularEyeObservation RightEye { get; }

        public long Timestamp { get; }

        public bool HasStereoScreenPoints => LeftEye.HasScreenPoint && RightEye.HasScreenPoint;

        public bool HasAnySignal =>
            HasRecommendedScreenPoint ||
            LeftEye.HasScreenPoint ||
            RightEye.HasScreenPoint ||
            LeftEye.HasPupilCenter ||
            RightEye.HasPupilCenter ||
            LeftEye.HasGazeOrigin ||
            RightEye.HasGazeOrigin ||
            LeftEye.HasGazeDirection ||
            RightEye.HasGazeDirection;
    }

    public sealed class BinocularGazeSampleAccumulator
    {
        private long lastTimestamp = long.MinValue;
        private int sampleCount;
        private int recommendedCount;
        private int leftScreenCount;
        private int rightScreenCount;
        private int leftPupilCount;
        private int rightPupilCount;
        private int leftOriginCount;
        private int rightOriginCount;
        private int leftDirectionCount;
        private int rightDirectionCount;
        private Vector2 recommendedSum;
        private Vector2 leftScreenSum;
        private Vector2 rightScreenSum;
        private Vector2 leftPupilSum;
        private Vector2 rightPupilSum;
        private Vector3 leftOriginSum;
        private Vector3 rightOriginSum;
        private Vector3 leftDirectionSum;
        private Vector3 rightDirectionSum;

        public int SampleCount => sampleCount;

        public bool TryAddSample(in BinocularGazeSample sample)
        {
            if (sample.Timestamp == lastTimestamp || !sample.HasAnySignal)
            {
                return false;
            }

            lastTimestamp = sample.Timestamp;
            sampleCount += 1;

            if (sample.HasRecommendedScreenPoint)
            {
                recommendedSum += sample.RecommendedScreenPointNormalized;
                recommendedCount += 1;
            }

            AccumulateEye(sample.LeftEye, ref leftScreenSum, ref leftScreenCount, ref leftPupilSum, ref leftPupilCount, ref leftOriginSum, ref leftOriginCount, ref leftDirectionSum, ref leftDirectionCount);
            AccumulateEye(sample.RightEye, ref rightScreenSum, ref rightScreenCount, ref rightPupilSum, ref rightPupilCount, ref rightOriginSum, ref rightOriginCount, ref rightDirectionSum, ref rightDirectionCount);
            return true;
        }

        public bool TryBuildAverageSample(out BinocularGazeSample sample)
        {
            sample = default;

            if (sampleCount <= 0)
            {
                return false;
            }

            sample = new BinocularGazeSample(
                recommendedCount > 0,
                recommendedCount > 0 ? recommendedSum / recommendedCount : default,
                BuildAverageEye(
                    leftScreenCount,
                    leftScreenSum,
                    leftPupilCount,
                    leftPupilSum,
                    leftOriginCount,
                    leftOriginSum,
                    leftDirectionCount,
                    leftDirectionSum),
                BuildAverageEye(
                    rightScreenCount,
                    rightScreenSum,
                    rightPupilCount,
                    rightPupilSum,
                    rightOriginCount,
                    rightOriginSum,
                    rightDirectionCount,
                    rightDirectionSum),
                lastTimestamp);
            return sample.HasAnySignal;
        }

        private static void AccumulateEye(
            in BinocularEyeObservation eye,
            ref Vector2 screenSum,
            ref int screenCount,
            ref Vector2 pupilSum,
            ref int pupilCount,
            ref Vector3 originSum,
            ref int originCount,
            ref Vector3 directionSum,
            ref int directionCount)
        {
            if (eye.HasScreenPoint)
            {
                screenSum += eye.ScreenPointNormalized;
                screenCount += 1;
            }

            if (eye.HasPupilCenter)
            {
                pupilSum += eye.PupilCenterNormalized;
                pupilCount += 1;
            }

            if (eye.HasGazeOrigin)
            {
                originSum += eye.GazeOrigin;
                originCount += 1;
            }

            if (eye.HasGazeDirection)
            {
                directionSum += eye.GazeDirection;
                directionCount += 1;
            }
        }

        private static BinocularEyeObservation BuildAverageEye(
            int screenCount,
            Vector2 screenSum,
            int pupilCount,
            Vector2 pupilSum,
            int originCount,
            Vector3 originSum,
            int directionCount,
            Vector3 directionSum)
        {
            return new BinocularEyeObservation(
                screenCount > 0,
                screenCount > 0 ? screenSum / screenCount : default,
                pupilCount > 0,
                pupilCount > 0 ? pupilSum / pupilCount : default,
                originCount > 0,
                originCount > 0 ? originSum / originCount : default,
                directionCount > 0,
                directionCount > 0 ? directionSum / directionCount : default);
        }
    }
}
