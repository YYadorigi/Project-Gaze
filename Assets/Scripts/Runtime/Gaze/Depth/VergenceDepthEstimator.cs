using UnityEngine;

namespace ProjectGaze.Gaze.Depth
{
    public readonly struct DepthErrorInterval
    {
        public DepthErrorInterval(float nominalDepth, float minimumDepth, float maximumDepth)
        {
            NominalDepth = nominalDepth;
            MinimumDepth = minimumDepth;
            MaximumDepth = maximumDepth;
        }

        public float NominalDepth { get; }

        public float MinimumDepth { get; }

        public float MaximumDepth { get; }

        public float LowerError => NominalDepth - MinimumDepth;

        public float UpperError => MaximumDepth - NominalDepth;
    }

    public static class VergenceDepthEstimator
    {
        public const float PopulationMeanIpdMillimeters = 63.0f;
        public const float A8MeanAccuracyDegrees = 0.35f;
        public const float A8PrecisionOneSigmaDegrees = 0.14f;
        public const float FoveateErrorDegrees95Ci = 0.30f;
        public const float DefaultErrorCorrelation = 0.80f;

        public static bool TryEstimateDepthFromDirections(
            Vector3 leftGazeDirection,
            Vector3 rightGazeDirection,
            float binocularBaseline,
            out float depth)
        {
            depth = 0f;
            if (binocularBaseline <= 0f ||
                !IsFinite(leftGazeDirection) ||
                !IsFinite(rightGazeDirection) ||
                leftGazeDirection.sqrMagnitude <= Mathf.Epsilon ||
                rightGazeDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            float vergenceAngleRadians = Vector3.Angle(leftGazeDirection, rightGazeDirection) * Mathf.Deg2Rad;
            return TryEstimateDepthFromAngle(vergenceAngleRadians, binocularBaseline, out depth);
        }

        public static bool TryEstimateDepthFromAngle(
            float vergenceAngleRadians,
            float binocularBaseline,
            out float depth)
        {
            depth = 0f;
            if (binocularBaseline <= 0f ||
                !IsFinite(vergenceAngleRadians) ||
                vergenceAngleRadians <= Mathf.Epsilon ||
                vergenceAngleRadians >= Mathf.PI)
            {
                return false;
            }

            float denominator = 2f * Mathf.Tan(vergenceAngleRadians * 0.5f);
            if (!IsFinite(denominator) || Mathf.Abs(denominator) <= Mathf.Epsilon)
            {
                return false;
            }

            depth = binocularBaseline / denominator;
            return IsFinite(depth) && depth > 0f;
        }

        public static bool TryBuildErrorInterval(
            float trueDepth,
            float binocularBaseline,
            float angularErrorDegrees,
            out DepthErrorInterval interval)
        {
            interval = default;
            if (trueDepth <= 0f || binocularBaseline <= 0f || angularErrorDegrees < 0f)
            {
                return false;
            }

            float nominalAngle = 2f * Mathf.Atan(binocularBaseline / (2f * trueDepth));
            float angularErrorRadians = angularErrorDegrees * Mathf.Deg2Rad;
            float smallerAngle = Mathf.Max(Mathf.Epsilon, nominalAngle - angularErrorRadians);
            float largerAngle = Mathf.Min(Mathf.PI - Mathf.Epsilon, nominalAngle + angularErrorRadians);

            if (!TryEstimateDepthFromAngle(nominalAngle, binocularBaseline, out float nominalDepth) ||
                !TryEstimateDepthFromAngle(largerAngle, binocularBaseline, out float minimumDepth) ||
                !TryEstimateDepthFromAngle(smallerAngle, binocularBaseline, out float maximumDepth))
            {
                return false;
            }

            interval = new DepthErrorInterval(nominalDepth, minimumDepth, maximumDepth);
            return true;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
