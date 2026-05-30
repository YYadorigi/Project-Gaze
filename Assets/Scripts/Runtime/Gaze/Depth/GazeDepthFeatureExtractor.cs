using UnityEngine;

namespace ProjectGaze.Gaze.Depth
{
    public static class GazeDepthFeatureExtractor
    {
        public const int RecommendedXIndex = 0;
        public const int RecommendedYIndex = 1;
        public const int LeftXIndex = 2;
        public const int LeftYIndex = 3;
        public const int RightXIndex = 4;
        public const int RightYIndex = 5;
        public const int DisparityXIndex = 6;
        public const int DisparityYIndex = 7;
        public const int DisparityMagnitudeIndex = 8;
        public const int MidpointXIndex = 9;
        public const int MidpointYIndex = 10;
        public const int LeftPupilXIndex = 11;
        public const int LeftPupilYIndex = 12;
        public const int RightPupilXIndex = 13;
        public const int RightPupilYIndex = 14;
        public const int PupilDisparityXIndex = 15;
        public const int PupilDisparityYIndex = 16;
        public const int LeftOriginXIndex = 17;
        public const int LeftOriginYIndex = 18;
        public const int LeftOriginZIndex = 19;
        public const int RightOriginXIndex = 20;
        public const int RightOriginYIndex = 21;
        public const int RightOriginZIndex = 22;
        public const int OriginDisparityXIndex = 23;
        public const int OriginDisparityYIndex = 24;
        public const int OriginDisparityZIndex = 25;
        public const int LeftPupilAvailableIndex = 26;
        public const int RightPupilAvailableIndex = 27;
        public const int LeftOriginAvailableIndex = 28;
        public const int RightOriginAvailableIndex = 29;
        public const int FeatureCount = 30;

        public static bool TryBuildFeatureVector(in BinocularGazeSample sample, out float[] features)
        {
            features = null;

            if (!sample.HasRecommendedScreenPoint || !sample.HasStereoScreenPoints)
            {
                return false;
            }

            var left = sample.LeftEye.ScreenPointNormalized;
            var right = sample.RightEye.ScreenPointNormalized;
            if (!IsFinite(left) || !IsFinite(right) || !IsFinite(sample.RecommendedScreenPointNormalized))
            {
                return false;
            }

            features = new float[FeatureCount];
            Vector2 midpoint = (left + right) * 0.5f;
            Vector2 disparity = left - right;

            features[RecommendedXIndex] = sample.RecommendedScreenPointNormalized.x;
            features[RecommendedYIndex] = sample.RecommendedScreenPointNormalized.y;
            features[LeftXIndex] = left.x;
            features[LeftYIndex] = left.y;
            features[RightXIndex] = right.x;
            features[RightYIndex] = right.y;
            features[DisparityXIndex] = disparity.x;
            features[DisparityYIndex] = disparity.y;
            features[DisparityMagnitudeIndex] = disparity.magnitude;
            features[MidpointXIndex] = midpoint.x;
            features[MidpointYIndex] = midpoint.y;

            WriteOptionalVector2(features, LeftPupilXIndex, LeftPupilYIndex, sample.LeftEye.HasPupilCenter, sample.LeftEye.PupilCenterNormalized);
            WriteOptionalVector2(features, RightPupilXIndex, RightPupilYIndex, sample.RightEye.HasPupilCenter, sample.RightEye.PupilCenterNormalized);
            if (sample.LeftEye.HasPupilCenter && sample.RightEye.HasPupilCenter)
            {
                Vector2 pupilDisparity = sample.LeftEye.PupilCenterNormalized - sample.RightEye.PupilCenterNormalized;
                features[PupilDisparityXIndex] = pupilDisparity.x;
                features[PupilDisparityYIndex] = pupilDisparity.y;
            }

            WriteOptionalVector3(features, LeftOriginXIndex, sample.LeftEye.HasGazeOrigin, sample.LeftEye.GazeOrigin);
            WriteOptionalVector3(features, RightOriginXIndex, sample.RightEye.HasGazeOrigin, sample.RightEye.GazeOrigin);
            if (sample.LeftEye.HasGazeOrigin && sample.RightEye.HasGazeOrigin)
            {
                Vector3 originDisparity = sample.LeftEye.GazeOrigin - sample.RightEye.GazeOrigin;
                features[OriginDisparityXIndex] = originDisparity.x;
                features[OriginDisparityYIndex] = originDisparity.y;
                features[OriginDisparityZIndex] = originDisparity.z;
            }

            features[LeftPupilAvailableIndex] = sample.LeftEye.HasPupilCenter ? 1f : 0f;
            features[RightPupilAvailableIndex] = sample.RightEye.HasPupilCenter ? 1f : 0f;
            features[LeftOriginAvailableIndex] = sample.LeftEye.HasGazeOrigin ? 1f : 0f;
            features[RightOriginAvailableIndex] = sample.RightEye.HasGazeOrigin ? 1f : 0f;
            return true;
        }

        private static void WriteOptionalVector2(float[] features, int xIndex, int yIndex, bool available, Vector2 value)
        {
            if (!available || !IsFinite(value))
            {
                return;
            }

            features[xIndex] = value.x;
            features[yIndex] = value.y;
        }

        private static void WriteOptionalVector3(float[] features, int xIndex, bool available, Vector3 value)
        {
            if (!available || !IsFinite(value))
            {
                return;
            }

            features[xIndex] = value.x;
            features[xIndex + 1] = value.y;
            features[xIndex + 2] = value.z;
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
