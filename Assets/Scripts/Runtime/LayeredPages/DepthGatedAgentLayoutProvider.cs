using ProjectGaze.Gaze.Depth;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    public readonly struct DepthGatedAgentPageSpec
    {
        public DepthGatedAgentPageSpec(
            string pageId,
            Vector3 localPosition,
            float slotScale,
            string depthLayerId,
            float depthLayerRayDistance,
            float depthLayerTolerance,
            float matchingRadius,
            string title,
            string query,
            Color accentColor)
        {
            PageId = pageId;
            LocalPosition = localPosition;
            SlotScale = slotScale;
            DepthLayerId = depthLayerId;
            DepthLayerRayDistance = depthLayerRayDistance;
            DepthLayerTolerance = depthLayerTolerance;
            MatchingRadius = matchingRadius;
            Title = title;
            Query = query;
            AccentColor = accentColor;
        }

        public string PageId { get; }

        public Vector3 LocalPosition { get; }

        public float SlotScale { get; }

        public string DepthLayerId { get; }

        public float DepthLayerRayDistance { get; }

        public float DepthLayerTolerance { get; }

        public float MatchingRadius { get; }

        public string Title { get; }

        public string Query { get; }

        public Color AccentColor { get; }
    }

    public static class DepthGatedAgentLayoutProvider
    {
        public const float AgentPanelVisualDepthOffset = -0.16f;

        public static DepthGatedAgentPageSpec[] GetWebPageSpecs()
        {
            return new[]
            {
                CreateMappedSpec("Web_A", "Page_E", "Research workspace", "active search session", new Color(0.16f, 0.43f, 0.82f)),
                CreateMappedSpec("Web_B", "Page_D", "Mail console", "advisor feedback and tasks", new Color(0.80f, 0.38f, 0.20f)),
                CreateMappedSpec("Web_C", "Page_G", "Literature board", "gaze depth papers", new Color(0.25f, 0.56f, 0.42f)),
                CreateMappedSpec("Web_D", "Page_H", "Experiment tracker", "SUS and task logs", new Color(0.50f, 0.35f, 0.70f)),
                CreateMappedSpec("Web_E", "Page_K", "Visualization draft", "depth error chart plan", new Color(0.30f, 0.54f, 0.68f))
            };
        }

        public static DepthGatedAgentPageSpec GetAgentLogoSpec()
        {
            return CreateMappedSpec(
                DepthGatedAgentPanelCoordinator.AgentLogoPageId,
                "Page_O",
                "Gaze Agent",
                "far-depth assistant trigger",
                new Color(0.10f, 0.23f, 0.62f));
        }

        public static Vector3 GetAgentPanelActivePosition()
        {
            var mainSlot = LayeredPagesLayoutProvider.GetPageSpec(LayeredPagesSceneDefaults.InitialMainPageId);
            return mainSlot.Position + new Vector3(0f, 0f, AgentPanelVisualDepthOffset);
        }

        public static float GetAgentPanelActiveScale()
        {
            return LayeredPagesLayoutProvider.GetPageSpec(LayeredPagesSceneDefaults.InitialMainPageId).SlotScale;
        }

        public static float GetAgentPanelActiveMatchingRadius()
        {
            return LayeredPagesLayoutProvider.GetPageSpec(LayeredPagesSceneDefaults.InitialMainPageId).MatchingRegionRadiusWorld;
        }

        private static DepthGatedAgentPageSpec CreateMappedSpec(
            string pageId,
            string layeredPageSlotId,
            string title,
            string query,
            Color accentColor)
        {
            var slot = LayeredPagesLayoutProvider.GetPageSpec(layeredPageSlotId);
            return new DepthGatedAgentPageSpec(
                pageId,
                slot.Position,
                slot.SlotScale,
                slot.DepthLayerId,
                slot.DepthLayerRayDistance,
                slot.DepthLayerTolerance,
                slot.MatchingRegionRadiusWorld,
                title,
                query,
                accentColor);
        }
    }
}
