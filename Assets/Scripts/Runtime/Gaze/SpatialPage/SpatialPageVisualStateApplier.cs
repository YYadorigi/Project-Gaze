using UnityEngine;

namespace ProjectGaze.Gaze
{
    public sealed class SpatialPageVisualStateApplier : MonoBehaviour
    {
        private SpatialPageRegistry pageRegistry;
        private GazeInteractionSettings settings;
        private GazeInteractionController controller;

        public void Initialize(
            SpatialPageRegistry pageRegistry,
            GazeInteractionSettings settings,
            GazeInteractionController controller)
        {
            if (this.controller != null)
            {
                this.controller.SnapshotChanged -= ApplySnapshot;
            }

            this.pageRegistry = pageRegistry;
            this.settings = settings;
            this.controller = controller;

            if (this.controller != null)
            {
                this.controller.SnapshotChanged += ApplySnapshot;
                ApplySnapshot(this.controller.CurrentSnapshot);
            }
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.SnapshotChanged -= ApplySnapshot;
            }
        }

        public void ApplySnapshot(GazeInteractionSnapshot snapshot)
        {
            if (pageRegistry == null || settings == null)
            {
                return;
            }

            foreach (var page in pageRegistry.Pages)
            {
                if (page == null)
                {
                    continue;
                }

                var visualState = SpatialPageVisualStateResolver.Resolve(page.PageId, snapshot);
                page.ApplyState(visualState, settings);
            }
        }
    }
}
