using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectGaze.Gaze
{
    public readonly struct DepthGatedAgentMorphPose
    {
        public DepthGatedAgentMorphPose(
            Vector3 localPosition,
            Vector3 localScale,
            float matchingRadius,
            string depthLayerId,
            float depthLayerRayDistance,
            float depthLayerTolerance,
            float logoAlpha,
            float panelAlpha)
        {
            LocalPosition = localPosition;
            LocalScale = localScale;
            MatchingRadius = matchingRadius;
            DepthLayerId = depthLayerId;
            DepthLayerRayDistance = depthLayerRayDistance;
            DepthLayerTolerance = depthLayerTolerance;
            LogoAlpha = Mathf.Clamp01(logoAlpha);
            PanelAlpha = Mathf.Clamp01(panelAlpha);
        }

        public Vector3 LocalPosition { get; }

        public Vector3 LocalScale { get; }

        public float MatchingRadius { get; }

        public string DepthLayerId { get; }

        public float DepthLayerRayDistance { get; }

        public float DepthLayerTolerance { get; }

        public float LogoAlpha { get; }

        public float PanelAlpha { get; }
    }

    public sealed class DepthGatedAgentPageMorphPresenter
    {
        private readonly MonoBehaviour coroutineHost;
        private readonly SpatialPage agentPage;
        private readonly CanvasGroup logoGroup;
        private readonly CanvasGroup panelGroup;
        private readonly DepthGatedAgentMorphPose logoPose;
        private readonly DepthGatedAgentMorphPose panelPose;
        private readonly float transitionDurationSeconds;
        private Coroutine activeCoroutine;

        public DepthGatedAgentPageMorphPresenter(
            MonoBehaviour coroutineHost,
            SpatialPage agentPage,
            CanvasGroup logoGroup,
            CanvasGroup panelGroup,
            DepthGatedAgentMorphPose logoPose,
            DepthGatedAgentMorphPose panelPose,
            float transitionDurationSeconds)
        {
            this.coroutineHost = coroutineHost;
            this.agentPage = agentPage;
            this.logoGroup = logoGroup;
            this.panelGroup = panelGroup;
            this.logoPose = logoPose;
            this.panelPose = panelPose;
            this.transitionDurationSeconds = Mathf.Max(0.01f, transitionDurationSeconds);
        }

        public bool IsPanelVisible { get; private set; }

        public void Show()
        {
            AnimateTo(panelPose, true);
        }

        public void Hide()
        {
            AnimateTo(logoPose, false);
        }

        public void ShowImmediate()
        {
            ApplyPose(panelPose, true);
        }

        public void HideImmediate()
        {
            ApplyPose(logoPose, false);
        }

        private void AnimateTo(DepthGatedAgentMorphPose targetPose, bool panelVisible)
        {
            if (agentPage == null)
            {
                return;
            }

            agentPage.ConfigureMatchingRegion(targetPose.MatchingRadius);
            agentPage.ConfigureDepthLayer(targetPose.DepthLayerId, targetPose.DepthLayerRayDistance, targetPose.DepthLayerTolerance);

            if (coroutineHost == null || !Application.isPlaying)
            {
                ApplyPose(targetPose, panelVisible);
                return;
            }

            if (activeCoroutine != null)
            {
                coroutineHost.StopCoroutine(activeCoroutine);
            }

            activeCoroutine = coroutineHost.StartCoroutine(AnimatePage(targetPose, panelVisible));
        }

        private IEnumerator AnimatePage(DepthGatedAgentMorphPose targetPose, bool panelVisible)
        {
            Transform pageTransform = agentPage.transform;
            Vector3 startPosition = pageTransform.localPosition;
            Vector3 startScale = pageTransform.localScale;
            float startLogoAlpha = logoGroup != null ? logoGroup.alpha : 0f;
            float startPanelAlpha = panelGroup != null ? panelGroup.alpha : 0f;
            float elapsedSeconds = 0f;

            while (elapsedSeconds < transitionDurationSeconds)
            {
                float t = elapsedSeconds / transitionDurationSeconds;
                float easedT = t * t * (3f - (2f * t));
                pageTransform.localPosition = Vector3.Lerp(startPosition, targetPose.LocalPosition, easedT);
                pageTransform.localScale = Vector3.Lerp(startScale, targetPose.LocalScale, easedT);
                SetGroupAlpha(logoGroup, Mathf.Lerp(startLogoAlpha, targetPose.LogoAlpha, easedT));
                SetGroupAlpha(panelGroup, Mathf.Lerp(startPanelAlpha, targetPose.PanelAlpha, easedT));
                elapsedSeconds += Time.deltaTime;
                yield return null;
            }

            ApplyPose(targetPose, panelVisible);
            activeCoroutine = null;
        }

        private void ApplyPose(DepthGatedAgentMorphPose pose, bool panelVisible)
        {
            if (agentPage == null)
            {
                return;
            }

            agentPage.transform.localPosition = pose.LocalPosition;
            agentPage.transform.localScale = pose.LocalScale;
            agentPage.ConfigureMatchingRegion(pose.MatchingRadius);
            agentPage.ConfigureDepthLayer(pose.DepthLayerId, pose.DepthLayerRayDistance, pose.DepthLayerTolerance);
            SetGroupAlpha(logoGroup, pose.LogoAlpha);
            SetGroupAlpha(panelGroup, pose.PanelAlpha);
            IsPanelVisible = panelVisible;
        }

        private static void SetGroupAlpha(CanvasGroup group, float alpha)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = Mathf.Clamp01(alpha);
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}
