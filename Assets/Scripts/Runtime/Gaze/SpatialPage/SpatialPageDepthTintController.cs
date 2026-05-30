using UnityEngine;

namespace ProjectGaze.Gaze
{
    public sealed class SpatialPageDepthTintController : MonoBehaviour
    {
        private SpatialPageVisualController visualController;
        private SpatialPageDepthTintProfile profile;
        private bool initialized;

        public void Initialize(SpatialPageVisualController visualController, in SpatialPageDepthTintProfile profile)
        {
            this.visualController = visualController;
            this.profile = profile;
            initialized = visualController != null;
            ApplyCurrentDepthTint();
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                return;
            }

            ApplyCurrentDepthTint();
        }

        private void ApplyCurrentDepthTint()
        {
            float depthBias = SpatialPageDepthTintUtility.EvaluateDepthBias(transform.localPosition.z, profile);
            visualController.SetDepthBias(depthBias, profile);
        }
    }
}
