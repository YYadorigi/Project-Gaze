using UnityEngine;

namespace ProjectGaze.Gaze
{
    public readonly struct SpatialPageDepthTintProfile
    {
        public SpatialPageDepthTintProfile(
            float zeroPlaneLocalZ,
            float nearDepthRange,
            float farDepthRange,
            float nearLightenStrength,
            float farDarkenStrength,
            float responseExponent,
            Color nearTintTarget,
            Color farTintTarget)
        {
            ZeroPlaneLocalZ = zeroPlaneLocalZ;
            NearDepthRange = Mathf.Max(0.001f, nearDepthRange);
            FarDepthRange = Mathf.Max(0.001f, farDepthRange);
            NearLightenStrength = Mathf.Clamp01(nearLightenStrength);
            FarDarkenStrength = Mathf.Clamp01(farDarkenStrength);
            ResponseExponent = Mathf.Clamp(responseExponent, 0.15f, 1.0f);
            NearTintTarget = SanitizeTintTarget(nearTintTarget);
            FarTintTarget = SanitizeTintTarget(farTintTarget);
        }

        public float ZeroPlaneLocalZ { get; }

        public float NearDepthRange { get; }

        public float FarDepthRange { get; }

        public float NearLightenStrength { get; }

        public float FarDarkenStrength { get; }

        public float ResponseExponent { get; }

        public Color NearTintTarget { get; }

        public Color FarTintTarget { get; }

        private static Color SanitizeTintTarget(Color target)
        {
            target.a = 1f;
            return target;
        }
    }

    public static class SpatialPageDepthTintUtility
    {
        public static float EvaluateDepthBias(float pageLocalZ, in SpatialPageDepthTintProfile profile)
        {
            float offset = pageLocalZ - profile.ZeroPlaneLocalZ;
            if (Mathf.Abs(offset) <= Mathf.Epsilon)
            {
                return 0f;
            }

            if (offset < 0f)
            {
                float nearNormalized = Mathf.Clamp01(-offset / profile.NearDepthRange);
                return -Mathf.Pow(nearNormalized, profile.ResponseExponent);
            }

            float farNormalized = Mathf.Clamp01(offset / profile.FarDepthRange);
            return Mathf.Pow(farNormalized, profile.ResponseExponent);
        }

        public static Color ApplyDepthTint(
            Color baseColor,
            float depthBias,
            float nearLightenStrength,
            float farDarkenStrength,
            Color nearTintTarget,
            Color farTintTarget)
        {
            float clampedBias = Mathf.Clamp(depthBias, -1f, 1f);
            Color tinted = baseColor;

            if (clampedBias < 0f)
            {
                float nearT = -clampedBias * nearLightenStrength;
                tinted = Color.Lerp(baseColor, ResolveNearColor(baseColor, nearTintTarget), nearT);
            }
            else if (clampedBias > 0f)
            {
                float farT = clampedBias * farDarkenStrength;
                tinted = Color.Lerp(baseColor, ResolveFarColor(baseColor, farTintTarget), farT);
            }

            tinted.a = baseColor.a;
            return tinted;
        }

        public static Color ApplyDepthTint(Color baseColor, float depthBias, in SpatialPageDepthTintProfile profile)
        {
            return ApplyDepthTint(
                baseColor,
                depthBias,
                profile.NearLightenStrength,
                profile.FarDarkenStrength,
                profile.NearTintTarget,
                profile.FarTintTarget);
        }

        private static Color ResolveNearColor(Color baseColor, Color nearTintTarget)
        {
            var lifted = Color.Lerp(baseColor, nearTintTarget, 0.82f);
            lifted = Color.Lerp(lifted, Color.white, 0.12f);
            lifted.a = baseColor.a;
            return lifted;
        }

        private static Color ResolveFarColor(Color baseColor, Color farTintTarget)
        {
            var cooled = Color.Lerp(baseColor * 0.22f, farTintTarget, 0.72f);
            cooled.a = baseColor.a;
            return cooled;
        }
    }
}
