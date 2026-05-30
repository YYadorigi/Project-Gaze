using ProjectGaze.Gaze.Depth;
using UnityEngine;

namespace ProjectGaze.Gaze.Providers
{
    public readonly struct InvensunA8RawGazeSample
    {
        public InvensunA8RawGazeSample(
            Vector2 recommendedGazePointNormalized,
            bool recommendedGazePointValid,
            bool leftGazePointValid,
            bool rightGazePointValid,
            bool leftBlinkDetected,
            bool rightBlinkDetected,
            float leftOpenness,
            float rightOpenness,
            long systemTimestamp,
            BinocularGazeSample binocularGaze = default)
        {
            RecommendedGazePointNormalized = recommendedGazePointNormalized;
            RecommendedGazePointValid = recommendedGazePointValid;
            LeftGazePointValid = leftGazePointValid;
            RightGazePointValid = rightGazePointValid;
            LeftBlinkDetected = leftBlinkDetected;
            RightBlinkDetected = rightBlinkDetected;
            LeftOpenness = leftOpenness;
            RightOpenness = rightOpenness;
            SystemTimestamp = systemTimestamp;
            BinocularGaze = binocularGaze;
        }

        public Vector2 RecommendedGazePointNormalized { get; }

        public bool RecommendedGazePointValid { get; }

        public bool LeftGazePointValid { get; }

        public bool RightGazePointValid { get; }

        public bool LeftBlinkDetected { get; }

        public bool RightBlinkDetected { get; }

        public float LeftOpenness { get; }

        public float RightOpenness { get; }

        public long SystemTimestamp { get; }

        public BinocularGazeSample BinocularGaze { get; }
    }

    internal static class InvensunA8BinocularGazeSampleUtility
    {
        internal static BinocularGazeSample CreateSample(
            in InvensunA8EyeDataFrame frame,
            Vector2 recommendedPoint,
            bool recommendedPointValid,
            long timestamp)
        {
            return new BinocularGazeSample(
                recommendedPointValid,
                recommendedPoint,
                BuildEyeObservation(frame.LeftGaze, frame.LeftPupil),
                BuildEyeObservation(frame.RightGaze, frame.RightPupil),
                timestamp);
        }

        private static BinocularEyeObservation BuildEyeObservation(
            InvensunA8GazePoint gaze,
            InvensunA8PupilInfo pupil)
        {
            bool hasScreenPoint = TryResolveDisplayPoint(gaze, out Vector2 screenPoint);
            bool hasPupilCenter = TryResolvePupilCenter(pupil, out Vector2 pupilCenter);
            bool hasGazeOrigin = TryResolveOptionalPoint3D(gaze, InvensunA8GazeValidityBit.GazeOrigin, gaze.GazeOrigin, out Vector3 gazeOrigin);
            bool hasGazeDirection = TryResolveOptionalPoint3D(gaze, InvensunA8GazeValidityBit.GazeDirection, gaze.GazeDirection, out Vector3 gazeDirection);

            return new BinocularEyeObservation(
                hasScreenPoint,
                screenPoint,
                hasPupilCenter,
                pupilCenter,
                hasGazeOrigin,
                gazeOrigin,
                hasGazeDirection,
                gazeDirection);
        }

        private static bool TryResolveDisplayPoint(InvensunA8GazePoint gaze, out Vector2 displayPoint)
        {
            if (TryResolveOptionalPoint3D(gaze, InvensunA8GazeValidityBit.SmoothPoint, gaze.SmoothPoint, out Vector3 smoothPoint))
            {
                displayPoint = new Vector2(smoothPoint.x, smoothPoint.y);
                return true;
            }

            if (TryResolveOptionalPoint3D(gaze, InvensunA8GazeValidityBit.GazePoint, gaze.GazePoint, out Vector3 gazePoint))
            {
                displayPoint = new Vector2(gazePoint.x, gazePoint.y);
                return true;
            }

            if (TryResolveOptionalPoint3D(gaze, InvensunA8GazeValidityBit.RawPoint, gaze.RawPoint, out Vector3 rawPoint))
            {
                displayPoint = new Vector2(rawPoint.x, rawPoint.y);
                return true;
            }

            displayPoint = default;
            return false;
        }

        private static bool TryResolvePupilCenter(InvensunA8PupilInfo pupil, out Vector2 pupilCenter)
        {
            pupilCenter = default;
            if (!IsFinite(pupil.PupilCenter.X) || !IsFinite(pupil.PupilCenter.Y) || pupil.PupilCenter.X <= 0f || pupil.PupilCenter.Y <= 0f)
            {
                return false;
            }

            pupilCenter = new Vector2(pupil.PupilCenter.X, pupil.PupilCenter.Y);
            return true;
        }

        private static bool TryResolveOptionalPoint3D(
            InvensunA8GazePoint gaze,
            InvensunA8GazeValidityBit bit,
            InvensunA8Point3D point,
            out Vector3 resolvedPoint)
        {
            resolvedPoint = default;
            if (!InvensunA8BitMaskUtility.IsFlagSet(gaze.GazeBitMask, bit) ||
                !IsFinite(point.X) ||
                !IsFinite(point.Y) ||
                !IsFinite(point.Z))
            {
                return false;
            }

            resolvedPoint = new Vector3(point.X, point.Y, point.Z);
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public static class InvensunA8RecommendedGazeUtility
    {
        public static bool TryResolveViewportPoint(
            in InvensunA8RawGazeSample sample,
            out Vector2 viewportPoint)
        {
            return TryResolveViewportPoint(sample, null, out viewportPoint);
        }

        public static bool TryResolveViewportPoint(
            in InvensunA8RawGazeSample sample,
            Camera targetCamera,
            out Vector2 viewportPoint)
        {
            viewportPoint = default;

            if (!sample.RecommendedGazePointValid ||
                !IsFinite(sample.RecommendedGazePointNormalized))
            {
                return false;
            }

            viewportPoint = GazeViewportPointUtility.DisplayAreaToViewport(sample.RecommendedGazePointNormalized);
            return true;
        }

        private static bool IsFinite(Vector2 point)
        {
            return !float.IsNaN(point.x) &&
                   !float.IsNaN(point.y) &&
                   !float.IsInfinity(point.x) &&
                   !float.IsInfinity(point.y);
        }
    }
}
