using System;
using System.Collections.Generic;
using ProjectGaze.Calibration;
using ProjectGaze.Gaze.Depth;
using ProjectGaze.Hardware.ThinkVision;
using UnityEngine;
using static ProjectGaze.Gaze.DepthGatedAgentUiUtility;

namespace ProjectGaze.Gaze
{
    public sealed class DepthGatedAgentDemo : MonoBehaviour
    {
        public const string SceneName = "DepthGatedAgentScene";

        private const string InitialMainPageId = "Web_A";
        private static readonly Vector3 WorldOffset = LayeredPagesSceneDefaults.WorldOffset;
        private static readonly Vector3 SharedPageBodyScale = LayeredPagesSceneDefaults.SharedPageBodyScale;
        private static readonly Vector3 AgentPanelActivePosition = DepthGatedAgentLayoutProvider.GetAgentPanelActivePosition();
        private static readonly Vector3 AgentPanelActiveScale = Vector3.one * DepthGatedAgentLayoutProvider.GetAgentPanelActiveScale();
        private static readonly float AgentPanelActiveMatchingRadius = DepthGatedAgentLayoutProvider.GetAgentPanelActiveMatchingRadius();
        private const float AgentMorphTransitionDurationSeconds = 0.34f;

        private readonly List<Material> createdMaterials = new();
        private readonly Dictionary<string, SpatialPage> pagesById = new();
        private readonly GazeInteractionSettings gazeSettings = new();
        private readonly GazeInteractionRootFactory gazeInteractionRootFactory = new();
        private readonly LayeredPagesStatusOverlay statusOverlay = new();
        private readonly DepthGatedAgentPanelCoordinator agentPanelCoordinator = new();
        private readonly DepthGatedAgentRoundTripState agentRoundTripState = new();

        private IThinkVisionDisplayBridge displayBridge = ThinkVisionDisplayBridgeFactory.Create();
        private Camera mainCamera;
        private Transform worldRoot;
        private GazeInteractionController gazeController;
        private SpatialPageInteractionCoordinator interactionCoordinator;
        private LayeredSearchPageUiBuilder webPageUiBuilder;
        private MainViewPageSwapper webPageSwapper;
        private DepthGatedAgentPageMorphPresenter agentPagePresenter;
        private GazeTaskInteractionSceneLogger taskInteractionLogger;
        private bool useStereoGazeInput;

        private void Start()
        {
            BuildScene();
        }

        private void OnDestroy()
        {
            LayeredPagesPrimitiveUtility.DestroyCreatedMaterials(createdMaterials);
            interactionCoordinator?.Dispose();
            taskInteractionLogger?.FlushIfDirty();
            statusOverlay.Dispose();
        }

        private void Update()
        {
            interactionCoordinator?.Tick(HandleConfirmedPageChanged);
            taskInteractionLogger?.TickSpaceFeedback(agentRoundTripState.HasPending);
        }

        private void OnGUI()
        {
            statusOverlay.Draw(
                displayBridge,
                gazeController,
                webPageSwapper?.CurrentMainPageId ?? InitialMainPageId,
                useStereoGazeInput,
                "Depth Agent Status",
                useStereoGazeInput
                    ? "Stereo mode: gaze at normal web pages and blink to confirm. Confirm the far-depth Gaze Agent logo to expand it into the simulated AI panel; confirm another web page to collapse it."
                    : "Mouse fallback: hover pages to preview, scroll through overlapping depth hits, and left click to confirm. Confirm the far-depth Gaze Agent logo to expand it into the simulated AI panel.",
                taskInteractionLogger?.BuildOverlayText(agentRoundTripState.HasPending));
        }

        private void BuildScene()
        {
            displayBridge = ThinkVisionDisplayBridgeFactory.Create();
            useStereoGazeInput = GazeSceneInputModeResolver.ShouldUseStereoGazeInput(
                displayBridge,
                Application.persistentDataPath,
                CalibrationSceneFlow.ShouldForceMouseFallbackThisSession());
            gazeSettings.Clamp();
            mainCamera = LayeredPagesCameraUtility.ResolveAndConfigureCamera(displayBridge);
            webPageUiBuilder = new LayeredSearchPageUiBuilder(mainCamera);

            worldRoot = new GameObject("DepthGatedAgentWorld").transform;
            worldRoot.SetParent(transform, false);
            worldRoot.localPosition = WorldOffset;

            BuildLandscapeBackdrop();
            var pages = BuildPages();

            gazeController = gazeInteractionRootFactory.Create(
                transform,
                mainCamera,
                pages,
                gazeSettings,
                useStereoGazeInput,
                InitialMainPageId,
                SceneName);
            interactionCoordinator = new SpatialPageInteractionCoordinator(gazeController, InitialMainPageId);
            taskInteractionLogger = GazeTaskInteractionSceneLogger.Create(SceneName);
            var zeroLayer = GazeDepthLayerProfile.GetLayer(3);
            webPageSwapper = new MainViewPageSwapper(
                this,
                pagesById,
                InitialMainPageId,
                LayeredPagesSceneDefaults.MainViewSwapDurationSeconds,
                AgentPanelActivePosition,
                AgentPanelActiveScale,
                AgentPanelActiveMatchingRadius,
                zeroLayer.LayerId,
                zeroLayer.RayDistance,
                zeroLayer.Tolerance);
            webPageSwapper.TrySwapTo(InitialMainPageId);

            ThinkVisionStereoSceneScale.ApplyCalibrationIfAvailable(
                mainCamera,
                GazeDepthLayerProfile.ZeroRayDistance,
                ThinkVisionStereoSceneScale.ReducedGhostingStereoStrength);
        }

        private List<SpatialPage> BuildPages()
        {
            pagesById.Clear();
            var pages = new List<SpatialPage>();
            var depthTintProfile = LayeredPagesLayoutProvider.BuildDepthTintProfile();

            foreach (var spec in DepthGatedAgentLayoutProvider.GetWebPageSpecs())
            {
                AddPage(pages, CreateWebPage(spec, depthTintProfile));
            }

            AddPage(pages, CreateAgentLogoPage(DepthGatedAgentLayoutProvider.GetAgentLogoSpec(), depthTintProfile));
            return pages;
        }

        private SpatialPage CreateWebPage(DepthGatedAgentPageSpec spec, in SpatialPageDepthTintProfile depthTintProfile)
        {
            var page = CreateBasePage(
                spec.PageId,
                spec.LocalPosition,
                spec.SlotScale,
                new Color(0.93f, 0.95f, 0.98f),
                spec.DepthLayerId,
                spec.DepthLayerRayDistance,
                spec.DepthLayerTolerance,
                spec.MatchingRadius,
                depthTintProfile);
            var visualController = page.VisualController;
            var contentGroup = webPageUiBuilder.Build(
                page.transform,
                DepthGatedAgentContentFactory.BuildWebContent(spec.Title, spec.Query, spec.AccentColor),
                visualController);
            visualController.RegisterContentGroup(contentGroup, gazeSettings.ContentDormantAlpha);
            return page;
        }

        private SpatialPage CreateAgentLogoPage(DepthGatedAgentPageSpec spec, in SpatialPageDepthTintProfile depthTintProfile)
        {
            var page = CreateBasePage(
                spec.PageId,
                spec.LocalPosition,
                spec.SlotScale,
                new Color(0.86f, 0.92f, 1f),
                spec.DepthLayerId,
                spec.DepthLayerRayDistance,
                spec.DepthLayerTolerance,
                spec.MatchingRadius,
                depthTintProfile);
            BuildAgentMorphUi(page, spec);
            return page;
        }

        private SpatialPage CreateBasePage(
            string pageId,
            Vector3 localPosition,
            float slotScale,
            Color color,
            string depthLayerId,
            float depthLayerRayDistance,
            float depthLayerTolerance,
            float matchingRadius,
            in SpatialPageDepthTintProfile depthTintProfile)
        {
            var pageObject = new GameObject(pageId);
            pageObject.transform.SetParent(worldRoot, false);
            pageObject.transform.localPosition = localPosition;
            pageObject.transform.localScale = Vector3.one * slotScale;

            var pageBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pageBody.name = pageId + "_Body";
            pageBody.transform.SetParent(pageObject.transform, false);
            pageBody.transform.localPosition = Vector3.zero;
            pageBody.transform.localScale = SharedPageBodyScale;

            var renderer = pageBody.GetComponent<Renderer>();
            var visualController = pageObject.AddComponent<SpatialPageVisualController>();
            visualController.Initialize(renderer, color, gazeSettings.DormantAlpha);
            pageObject.AddComponent<SpatialPageDepthTintController>().Initialize(visualController, depthTintProfile);

            var page = pageObject.AddComponent<SpatialPage>();
            page.Initialize(pageId, visualController, pageBody.GetComponent<Collider>());
            page.ConfigureMatchingRegion(matchingRadius);
            page.ConfigureDepthLayer(depthLayerId, depthLayerRayDistance, depthLayerTolerance);
            return page;
        }

        private void BuildLandscapeBackdrop()
        {
            CreateBackdropBlock("SkyBackdrop", new Vector3(0f, 1.7f, 31.5f), new Vector3(19f, 9f, 0.12f), new Color(0.43f, 0.68f, 0.95f, 1f));
            CreateBackdropBlock("MeadowNear", new Vector3(0f, -3.15f, 20.0f), new Vector3(20f, 2.2f, 0.14f), new Color(0.34f, 0.58f, 0.23f, 1f));
            CreateBackdropBlock("MeadowFar", new Vector3(0.6f, -2.45f, 30.0f), new Vector3(17f, 1.8f, 0.14f), new Color(0.24f, 0.49f, 0.20f, 1f));
        }

        private void BuildAgentMorphUi(SpatialPage page, DepthGatedAgentPageSpec spec)
        {
            var contentRoot = new GameObject("AgentMorphContent");
            contentRoot.transform.SetParent(page.transform, false);
            contentRoot.transform.localPosition = Vector3.zero;
            contentRoot.transform.localRotation = Quaternion.identity;
            contentRoot.transform.localScale = Vector3.one;

            var contentGroup = contentRoot.AddComponent<CanvasGroup>();
            page.VisualController.RegisterContentGroup(contentGroup, gazeSettings.ContentDormantAlpha);

            var logoGroup = BuildAgentLogoUi(contentRoot.transform, page.VisualController);
            var panelGroup = BuildAgentPanelUi(contentRoot.transform, page.VisualController);
            var zeroLayer = GazeDepthLayerProfile.GetLayer(3);
            var logoPose = new DepthGatedAgentMorphPose(
                spec.LocalPosition,
                Vector3.one * spec.SlotScale,
                spec.MatchingRadius,
                spec.DepthLayerId,
                spec.DepthLayerRayDistance,
                spec.DepthLayerTolerance,
                1f,
                0f);
            var panelPose = new DepthGatedAgentMorphPose(
                AgentPanelActivePosition,
                AgentPanelActiveScale,
                AgentPanelActiveMatchingRadius,
                zeroLayer.LayerId,
                zeroLayer.RayDistance,
                zeroLayer.Tolerance,
                0f,
                1f);

            agentPagePresenter = new DepthGatedAgentPageMorphPresenter(
                this,
                page,
                logoGroup,
                panelGroup,
                logoPose,
                panelPose,
                AgentMorphTransitionDurationSeconds);
            agentPagePresenter.HideImmediate();
        }

        private CanvasGroup BuildAgentLogoUi(Transform pageRoot, SpatialPageVisualController visualController)
        {
            var canvas = CreateWorldCanvas(
                "AgentLogoCanvas",
                pageRoot,
                mainCamera,
                760f,
                520f,
                new Vector3(0f, 0f, -SharedPageBodyScale.z * 0.5f - LayeredPagesSceneDefaults.PageCanvasSurfaceOffset),
                new Vector3(-(SharedPageBodyScale.x * 0.90f / 760f), SharedPageBodyScale.y * 0.88f / 520f, 1f));
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            CreatePanel("LogoHalo", canvas.transform, 248f, 70f, 264f, 264f, new Color(0.15f, 0.28f, 0.72f, 0.18f));
            CreatePanel("LogoCore", canvas.transform, 300f, 124f, 160f, 160f, new Color(0.10f, 0.23f, 0.62f, 0.92f));
            CreateText("LogoMark", canvas.transform, "GA", 64, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, 300f, 142f, 160f, 96f);
            CreateText("LogoTitle", canvas.transform, "Gaze Agent", 36, new Color(0.09f, 0.15f, 0.32f), FontStyle.Bold, TextAnchor.MiddleCenter, 150f, 346f, 460f, 54f);
            CreateText("LogoHint", canvas.transform, "Far-depth focus + blink", 22, new Color(0.22f, 0.28f, 0.42f), FontStyle.Normal, TextAnchor.MiddleCenter, 150f, 406f, 460f, 38f);
            RegisterCanvasGraphics(canvas.transform, visualController);
            return group;
        }

        private CanvasGroup BuildAgentPanelUi(Transform panelRoot, SpatialPageVisualController visualController)
        {
            var canvas = CreateWorldCanvas(
                "AgentPanelCanvas",
                panelRoot,
                mainCamera,
                1000f,
                620f,
                new Vector3(0f, 0f, -SharedPageBodyScale.z * 0.5f - LayeredPagesSceneDefaults.PageCanvasSurfaceOffset),
                new Vector3(-(SharedPageBodyScale.x * 0.94f / 1000f), SharedPageBodyScale.y * 0.90f / 620f, 1f));
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            CreatePanel("AgentHeader", canvas.transform, 24f, 20f, 952f, 78f, new Color(0.11f, 0.17f, 0.32f, 0.94f));
            CreateText("AgentTitle", canvas.transform, "Gaze Agent", 30, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft, 54f, 38f, 260f, 40f);
            CreateText("AgentSubtitle", canvas.transform, "simulated assistant panel / local demo", 17, new Color(0.76f, 0.82f, 0.94f), FontStyle.Normal, TextAnchor.MiddleLeft, 314f, 44f, 420f, 28f);
            CreatePanel("ThreadA", canvas.transform, 52f, 132f, 650f, 100f, new Color(0.96f, 0.97f, 0.99f, 0.94f));
            CreateText("ThreadAText", canvas.transform, "User: Summarize the depth-layer\nexperiment plan.", 22, new Color(0.10f, 0.13f, 0.20f), FontStyle.Normal, TextAnchor.MiddleLeft, 82f, 154f, 570f, 56f);
            CreatePanel("ThreadB", canvas.transform, 246f, 262f, 704f, 160f, new Color(0.88f, 0.93f, 1.00f, 0.96f));
            CreateText("ThreadBText", canvas.transform, "Agent: Use seven symmetric depth layers.\nRecord SVR depth error, then compare\npage and agent-panel tasks with SUS.", 20, new Color(0.08f, 0.12f, 0.22f), FontStyle.Normal, TextAnchor.MiddleLeft, 278f, 294f, 640f, 88f);
            CreatePanel("InputBar", canvas.transform, 52f, 506f, 812f, 62f, new Color(0.98f, 0.99f, 1.00f, 0.94f));
            CreateText("InputText", canvas.transform, "Ask a follow-up...", 20, new Color(0.38f, 0.42f, 0.52f), FontStyle.Normal, TextAnchor.MiddleLeft, 84f, 524f, 520f, 30f);
            CreatePanel("SendButton", canvas.transform, 880f, 506f, 96f, 62f, new Color(0.12f, 0.24f, 0.64f, 0.92f));
            CreateText("SendText", canvas.transform, "Send", 20, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, 880f, 524f, 96f, 30f);
            RegisterCanvasGraphics(canvas.transform, visualController);
            return group;
        }

        private void HandleConfirmedPageChanged(string confirmedPageId)
        {
            switch (agentPanelCoordinator.TickConfirmedPage(confirmedPageId))
            {
                case DepthGatedAgentPanelTransition.Show:
                    webPageSwapper?.TryRestoreCurrentMainPage();
                    agentPagePresenter?.Show();
                    BeginAgentRoundTrip();
                    break;

                case DepthGatedAgentPanelTransition.Hide:
                    agentPagePresenter?.Hide();
                    CompleteAgentRoundTrip(confirmedPageId);
                    break;
            }

            if (!string.Equals(confirmedPageId, DepthGatedAgentPanelCoordinator.AgentLogoPageId, System.StringComparison.Ordinal))
            {
                webPageSwapper?.TrySwapTo(confirmedPageId);
            }
        }

        private void BeginAgentRoundTrip()
        {
            taskInteractionLogger?.BeginAgentRoundTrip(agentRoundTripState, gazeController);
        }

        private void CompleteAgentRoundTrip(string closedByPageId)
        {
            taskInteractionLogger?.CompleteAgentRoundTrip(agentRoundTripState, closedByPageId, gazeController);
        }

        private void AddPage(List<SpatialPage> pages, SpatialPage page)
        {
            pages.Add(page);
            pagesById[page.PageId] = page;
        }

        private void CreateBackdropBlock(string name, Vector3 position, Vector3 scale, Color color)
        {
            LayeredPagesPrimitiveUtility.CreateBlock(
                worldRoot,
                name,
                position,
                scale,
                LayeredPagesPrimitiveUtility.CreateMaterial(createdMaterials, color, false));
        }

    }
}
