using UnityEngine;

namespace ProjectGaze.Gaze
{
    public static class LayeredPagesSceneDefaults
    {
        public const string InitialMainPageId = "Page_A";
        public const float PageCanvasSurfaceOffset = 0.025f;
        public const float MainViewSwapDurationSeconds = 0.32f;
        public const float NearDepthLightenStrength = 0.90f;
        public const float FarDepthDarkenStrength = 0.92f;
        public const float DepthTintResponseExponent = 0.42f;

        public static readonly Vector3 WorldOffset = new(0f, -2.25f, -11.0f);
        public static readonly Vector3 CameraPosition = new(0f, 2.35f, -11.0f);
        public static readonly Vector3 CameraRotationEuler = new(8f, 0f, 0f);
        public static readonly Vector3 SharedPageBodyScale = new(4.2f, 2.5f, 0.06f);
        public static readonly Color NearDepthTintTarget = new(1.00f, 0.90f, 0.72f, 1f);
        public static readonly Color FarDepthTintTarget = new(0.14f, 0.22f, 0.42f, 1f);
    }
}
