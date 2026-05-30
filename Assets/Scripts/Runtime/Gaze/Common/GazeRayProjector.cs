using System.Collections.Generic;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    public sealed class GazeRayProjector : MonoBehaviour
    {
        private readonly TargetMatcherPipeline matcherPipeline = new();
        private SpatialPageRegistry pageRegistry;
        private int mouseDepthCycleIndex;
        private string mouseDepthCycleKey;

        public GazeDepthMatchingMode DepthMatchingMode { get; private set; } = GazeDepthMatchingMode.DiscreteDepthLayer;

        public void Initialize(SpatialPageRegistry pageRegistry)
        {
            this.pageRegistry = pageRegistry;
            ResetMouseDepthCycle();
        }

        public void SetDepthMatchingMode(GazeDepthMatchingMode depthMatchingMode)
        {
            DepthMatchingMode = depthMatchingMode;
        }

        public GazeHitResult ProjectFromGaze(Camera targetCamera, in GazeTrackingSample sample)
        {
            if (!sample.IsValid)
            {
                return new GazeHitResult(false, false, null, null, DepthMatchingMode);
            }

            if (matcherPipeline.TryMatch(
                    targetCamera,
                    pageRegistry?.Targets,
                    sample,
                    DepthMatchingMode,
                    out var hitResult))
            {
                return hitResult;
            }

            return new GazeHitResult(true, false, null, null, DepthMatchingMode);
        }

        public GazeHitResult ProjectFromMouse(Camera targetCamera, in GazeTrackingSample sample, float scrollDelta)
        {
            if (!sample.IsValid || targetCamera == null)
            {
                return new GazeHitResult(false, false, null, null, GazeDepthMatchingMode.ViewportOnly);
            }

            var worldRay = GazeViewportPointUtility.BuildWorldRay(targetCamera, sample.NormalizedViewportPoint);
            var hits = Physics.RaycastAll(worldRay, 100f);
            System.Array.Sort(hits, static (left, right) => left.distance.CompareTo(right.distance));

            var hoveredPages = CollectHoveredPages(hits);
            if (hoveredPages.Count == 0)
            {
                ResetMouseDepthCycle();
                return new GazeHitResult(true, false, null, null, GazeDepthMatchingMode.ViewportOnly);
            }

            UpdateMouseDepthCycle(hoveredPages, scrollDelta);
            var selectedPage = hoveredPages[mouseDepthCycleIndex];

            return new GazeHitResult(
                true,
                true,
                selectedPage,
                selectedPage.TargetId,
                GazeDepthMatchingMode.ViewportOnly,
                selectedPage.DepthLayerId,
                selectedPage.DepthLayerRayDistance);
        }

        private static List<SpatialPage> CollectHoveredPages(RaycastHit[] hits)
        {
            var hoveredPages = new List<SpatialPage>();
            var seenPages = new HashSet<SpatialPage>();

            foreach (var hit in hits)
            {
                var page = hit.collider.GetComponent<SpatialPage>() ?? hit.collider.GetComponentInParent<SpatialPage>();
                if (page == null || !seenPages.Add(page))
                {
                    continue;
                }

                hoveredPages.Add(page);
            }

            return hoveredPages;
        }

        private void UpdateMouseDepthCycle(IReadOnlyList<SpatialPage> hoveredPages, float scrollDelta)
        {
            string nextKey = BuildMouseDepthCycleKey(hoveredPages);
            if (!string.Equals(mouseDepthCycleKey, nextKey, System.StringComparison.Ordinal))
            {
                mouseDepthCycleKey = nextKey;
                mouseDepthCycleIndex = 0;
            }

            if (hoveredPages.Count > 1)
            {
                if (scrollDelta > 0.001f)
                {
                    mouseDepthCycleIndex = (mouseDepthCycleIndex + 1) % hoveredPages.Count;
                }
                else if (scrollDelta < -0.001f)
                {
                    mouseDepthCycleIndex = (mouseDepthCycleIndex - 1 + hoveredPages.Count) % hoveredPages.Count;
                }
            }

            mouseDepthCycleIndex = Mathf.Clamp(mouseDepthCycleIndex, 0, hoveredPages.Count - 1);
        }

        private void ResetMouseDepthCycle()
        {
            mouseDepthCycleIndex = 0;
            mouseDepthCycleKey = null;
        }

        private static string BuildMouseDepthCycleKey(IReadOnlyList<SpatialPage> hoveredPages)
        {
            if (hoveredPages == null || hoveredPages.Count == 0)
            {
                return null;
            }

            System.Text.StringBuilder builder = new();
            for (int index = 0; index < hoveredPages.Count; index += 1)
            {
                if (index > 0)
                {
                    builder.Append('|');
                }

                builder.Append(hoveredPages[index].TargetId);
            }

            return builder.ToString();
        }
    }
}
