using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ProjectGaze.Gaze
{
    public sealed class SpatialPageVisualController : MonoBehaviour
    {
        private readonly List<ManagedRenderer> managedRenderers = new();
        private readonly List<ManagedGraphic> managedGraphics = new();

        private Color baseColor = Color.white;
        private float currentAlpha = 1f;
        private float targetAlpha = 1f;
        private float currentContentAlpha = 1f;
        private float targetContentAlpha = 1f;
        private float currentBrightness = 1f;
        private float targetBrightness = 1f;
        private float fadeSpeed = 3.5f;
        private float depthBias;
        private SpatialPageDepthTintProfile depthTintProfile;
        private CanvasGroup contentGroup;

        private sealed class ManagedRenderer
        {
            public ManagedRenderer(Renderer targetRenderer, Material runtimeMaterial, Color baseColor)
            {
                TargetRenderer = targetRenderer;
                RuntimeMaterial = runtimeMaterial;
                BaseColor = baseColor;
            }

            public Renderer TargetRenderer { get; }

            public Material RuntimeMaterial { get; }

            public Color BaseColor { get; }
        }

        private sealed class ManagedGraphic
        {
            public ManagedGraphic(Graphic targetGraphic, Color baseColor)
            {
                TargetGraphic = targetGraphic;
                BaseColor = baseColor;
            }

            public Graphic TargetGraphic { get; }

            public Color BaseColor { get; }
        }

        public void Initialize(Renderer targetRenderer, Color baseColor, float startingAlpha)
        {
            this.baseColor = baseColor;
            RegisterRenderer(targetRenderer, baseColor);

            currentAlpha = targetAlpha = startingAlpha;
            currentBrightness = targetBrightness = 1f;
            ApplyVisualState();
        }

        public void RegisterRenderer(Renderer targetRenderer, Color rendererBaseColor)
        {
            if (targetRenderer == null)
            {
                return;
            }

            var runtimeMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            TransparentMaterialUtility.Configure(runtimeMaterial);
            targetRenderer.sharedMaterial = runtimeMaterial;
            managedRenderers.Add(new ManagedRenderer(targetRenderer, runtimeMaterial, rendererBaseColor));
            ApplyVisualState();
        }

        public void RegisterGraphic(Graphic targetGraphic, Color graphicBaseColor)
        {
            if (targetGraphic == null)
            {
                return;
            }

            managedGraphics.Add(new ManagedGraphic(targetGraphic, graphicBaseColor));
            ApplyVisualState();
        }

        public void RegisterContentGroup(CanvasGroup contentGroup, float startingAlpha)
        {
            this.contentGroup = contentGroup;
            if (this.contentGroup == null)
            {
                return;
            }

            currentContentAlpha = targetContentAlpha = Mathf.Clamp01(startingAlpha);
            this.contentGroup.interactable = false;
            this.contentGroup.blocksRaycasts = false;
            ApplyContentState();
        }

        public void ApplyState(SpatialPageVisualState state, GazeInteractionSettings settings)
        {
            fadeSpeed = settings.PageFadeSpeed;

            switch (state)
            {
                case SpatialPageVisualState.Dormant:
                    targetAlpha = settings.DormantAlpha;
                    targetContentAlpha = settings.ContentDormantAlpha;
                    targetBrightness = settings.DormantBrightness;
                    break;

                case SpatialPageVisualState.Preview:
                    targetAlpha = settings.PreviewAlpha;
                    targetContentAlpha = settings.ContentPreviewAlpha;
                    targetBrightness = settings.PreviewBrightness;
                    break;

                case SpatialPageVisualState.Confirmed:
                    targetAlpha = settings.ConfirmedAlpha;
                    targetContentAlpha = settings.ContentConfirmedAlpha;
                    targetBrightness = settings.ConfirmedBrightness;
                    break;

                case SpatialPageVisualState.Suppressed:
                    targetAlpha = settings.SuppressedAlpha;
                    targetContentAlpha = settings.ContentSuppressedAlpha;
                    targetBrightness = settings.SuppressedBrightness;
                    break;
            }
        }

        public void SetDepthBias(float depthBias, in SpatialPageDepthTintProfile depthTintProfile)
        {
            this.depthBias = Mathf.Clamp(depthBias, -1f, 1f);
            this.depthTintProfile = depthTintProfile;
            ApplyVisualState();
        }

        private void Update()
        {
            if (managedRenderers.Count == 0 && managedGraphics.Count == 0 && contentGroup == null)
            {
                return;
            }

            var delta = Time.deltaTime * fadeSpeed;
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, delta);
            currentContentAlpha = Mathf.MoveTowards(currentContentAlpha, targetContentAlpha, delta);
            currentBrightness = Mathf.MoveTowards(currentBrightness, targetBrightness, delta);
            ApplyVisualState();
        }

        private void OnDestroy()
        {
            foreach (var renderer in managedRenderers)
            {
                if (renderer.RuntimeMaterial == null)
                {
                    continue;
                }

                UnityObjectLifecycleUtility.DestroyObject(renderer.RuntimeMaterial);
            }
        }

        private void ApplyVisualState()
        {
            foreach (var renderer in managedRenderers)
            {
                ApplyMaterialColor(renderer);
            }

            foreach (var graphic in managedGraphics)
            {
                ApplyGraphicColor(graphic);
            }

            ApplyContentState();
        }

        private void ApplyMaterialColor(ManagedRenderer renderer)
        {
            if (renderer.RuntimeMaterial == null)
            {
                return;
            }

            var color = SpatialPageDepthTintUtility.ApplyDepthTint(renderer.BaseColor * currentBrightness, depthBias, depthTintProfile);
            color.a = currentAlpha;

            if (renderer.RuntimeMaterial.HasProperty("_BaseColor"))
            {
                renderer.RuntimeMaterial.SetColor("_BaseColor", color);
            }

            if (renderer.RuntimeMaterial.HasProperty("_Color"))
            {
                renderer.RuntimeMaterial.SetColor("_Color", color);
            }
        }

        private void ApplyGraphicColor(ManagedGraphic graphic)
        {
            if (graphic.TargetGraphic == null)
            {
                return;
            }

            var color = SpatialPageDepthTintUtility.ApplyDepthTint(graphic.BaseColor, depthBias, depthTintProfile);
            color.a = graphic.BaseColor.a;
            graphic.TargetGraphic.color = color;
        }

        private void ApplyContentState()
        {
            if (contentGroup == null)
            {
                return;
            }

            contentGroup.alpha = currentContentAlpha;
        }
    }
}
