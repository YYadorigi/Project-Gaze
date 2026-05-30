using UnityEngine;

namespace ProjectGaze.Gaze
{
    public sealed class SpatialPage : MonoBehaviour, ISpatialTarget
    {
        [SerializeField]
        private string pageId;

        private SpatialPageVisualController visualController;
        private Collider hitCollider;
        private float matchingRegionRadiusWorld;
        private string depthLayerId;
        private float depthLayerRayDistance;
        private float depthLayerTolerance;

        public string PageId => pageId;

        public string TargetId => pageId;

        public SpatialPageVisualController VisualController => visualController;

        public Collider HitCollider => hitCollider;

        public Vector3 MatchingRegionCenterWorld => hitCollider != null ? hitCollider.bounds.center : transform.position;

        public float MatchingRegionRadiusWorld => matchingRegionRadiusWorld > 0f
            ? matchingRegionRadiusWorld
            : ResolveFallbackMatchingRadius();

        public string DepthLayerId => depthLayerId;

        public float DepthLayerRayDistance => depthLayerRayDistance;

        public float DepthLayerTolerance => depthLayerTolerance;

        public bool HasDepthLayer => !string.IsNullOrWhiteSpace(depthLayerId) &&
                                     depthLayerRayDistance > 0f &&
                                     depthLayerTolerance > 0f;

        public void Initialize(string pageId, SpatialPageVisualController visualController, Collider hitCollider)
        {
            this.pageId = pageId;
            this.visualController = visualController;
            this.hitCollider = hitCollider;
        }

        public void ConfigureMatchingRegion(float matchingRegionRadiusWorld)
        {
            this.matchingRegionRadiusWorld = Mathf.Max(0.01f, matchingRegionRadiusWorld);
        }

        public void ConfigureDepthLayer(string depthLayerId, float depthLayerRayDistance, float depthLayerTolerance)
        {
            this.depthLayerId = depthLayerId;
            this.depthLayerRayDistance = Mathf.Max(0f, depthLayerRayDistance);
            this.depthLayerTolerance = Mathf.Max(0f, depthLayerTolerance);
        }

        public void ApplyState(SpatialPageVisualState state, GazeInteractionSettings settings)
        {
            visualController?.ApplyState(state, settings);
        }

        private float ResolveFallbackMatchingRadius()
        {
            if (hitCollider == null)
            {
                return 0.01f;
            }

            Vector3 extents = hitCollider.bounds.extents;
            return Mathf.Max(0.01f, Mathf.Sqrt((extents.x * extents.x) + (extents.y * extents.y)));
        }
    }
}
