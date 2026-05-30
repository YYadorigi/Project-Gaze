using System.Collections.Generic;
using ProjectGaze.Calibration;
using ProjectGaze.Gaze.Depth;
using ProjectGaze.Hardware.ThinkVision;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    public sealed class LayeredPagesDemo : MonoBehaviour
    {
        public const string SceneName = "LayeredPagesScene";

        private readonly List<Material> createdMaterials = new();
        private readonly Dictionary<string, SpatialPage> pagesById = new();
        private readonly GazeInteractionSettings gazeSettings = new();
        private readonly ILayeredPageContentProvider pageContentProvider = new MockLayeredPageContentProvider();
        private readonly GazeInteractionRootFactory gazeInteractionRootFactory = new();
        private readonly LayeredPagesStatusOverlay statusOverlay = new();

        private IThinkVisionDisplayBridge displayBridge = ThinkVisionDisplayBridgeFactory.Create();
        private Camera mainCamera;
        private Transform worldRoot;
        private GazeInteractionController gazeController;
        private SpatialPageInteractionCoordinator interactionCoordinator;
        private LayeredSearchPageUiBuilder searchPageUiBuilder;
        private MainViewPageSwapper mainViewPageSwapper;
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
            if (gazeController == null)
            {
                return;
            }

            interactionCoordinator?.Tick(HandleConfirmedPageChanged);
            taskInteractionLogger?.TickSpaceFeedback(hasPendingInteraction: false);
        }

        private void OnGUI()
        {
            statusOverlay.Draw(
                displayBridge,
                gazeController,
                mainViewPageSwapper?.CurrentMainPageId ?? LayeredPagesSceneDefaults.InitialMainPageId,
                useStereoGazeInput,
                taskFeedbackText: taskInteractionLogger?.BuildOverlayText(hasPendingInteraction: false));
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
            searchPageUiBuilder = new LayeredSearchPageUiBuilder(mainCamera);

            worldRoot = new GameObject("LayeredPagesWorld").transform;
            worldRoot.SetParent(transform, false);

            BuildBackdrop();
            var pages = BuildSearchPages();
            worldRoot.localPosition = LayeredPagesSceneDefaults.WorldOffset;
            mainViewPageSwapper = new MainViewPageSwapper(this, pagesById);
            gazeController = gazeInteractionRootFactory.Create(
                transform,
                mainCamera,
                pages,
                gazeSettings,
                useStereoGazeInput,
                LayeredPagesSceneDefaults.InitialMainPageId,
                SceneName);
            interactionCoordinator = new SpatialPageInteractionCoordinator(
                gazeController,
                LayeredPagesSceneDefaults.InitialMainPageId);
            taskInteractionLogger = GazeTaskInteractionSceneLogger.Create(SceneName);
            ThinkVisionStereoSceneScale.ApplyCalibrationIfAvailable(
                mainCamera,
                GazeDepthLayerProfile.ZeroRayDistance,
                ThinkVisionStereoSceneScale.ReducedGhostingStereoStrength);
        }

        private void BuildBackdrop()
        {
            var backdropMaterial = LayeredPagesPrimitiveUtility.CreateMaterial(
                createdMaterials,
                new Color(0.07f, 0.09f, 0.14f, 1f),
                false);
            LayeredPagesPrimitiveUtility.CreateBlock(
                worldRoot,
                "Backdrop",
                new Vector3(0f, 0.25f, 34f),
                new Vector3(18f, 10f, 0.15f),
                backdropMaterial);
        }

        private List<SpatialPage> BuildSearchPages()
        {
            pagesById.Clear();
            var pages = new List<SpatialPage>();
            var specs = LayeredPagesLayoutProvider.GetPageSpecs();
            var depthTintProfile = LayeredPagesLayoutProvider.BuildDepthTintProfile(specs);

            foreach (var spec in specs)
            {
                var page = CreatePage(spec, depthTintProfile);
                pages.Add(page);
                pagesById[page.PageId] = page;
            }

            return pages;
        }

        private SpatialPage CreatePage(LayeredPageSpec spec, in SpatialPageDepthTintProfile depthTintProfile)
        {
            var pageObject = new GameObject(spec.Name);
            pageObject.transform.SetParent(worldRoot, false);
            pageObject.transform.localPosition = spec.Position;
            pageObject.transform.localScale = Vector3.one * spec.SlotScale;

            var pageBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pageBody.name = spec.Name + "_Body";
            pageBody.transform.SetParent(pageObject.transform, false);
            pageBody.transform.localPosition = Vector3.zero;
            pageBody.transform.localScale = LayeredPagesSceneDefaults.SharedPageBodyScale;

            var renderer = pageBody.GetComponent<Renderer>();
            var visualController = pageObject.AddComponent<SpatialPageVisualController>();
            visualController.Initialize(renderer, spec.Color, gazeSettings.DormantAlpha);
            pageObject.AddComponent<SpatialPageDepthTintController>().Initialize(visualController, depthTintProfile);

            var page = pageObject.AddComponent<SpatialPage>();
            page.Initialize(spec.Name, visualController, pageBody.GetComponent<Collider>());
            page.ConfigureMatchingRegion(spec.MatchingRegionRadiusWorld);
            page.ConfigureDepthLayer(spec.DepthLayerId, spec.DepthLayerRayDistance, spec.DepthLayerTolerance);

            var pageContent = pageContentProvider.GetContent(spec.Name);
            var contentGroup = searchPageUiBuilder.Build(pageObject.transform, pageContent, visualController);
            visualController.RegisterContentGroup(contentGroup, gazeSettings.ContentDormantAlpha);
            return page;
        }

        private void HandleConfirmedPageChanged(string confirmedPageId)
        {
            string previousMainPageId = mainViewPageSwapper?.CurrentMainPageId ?? LayeredPagesSceneDefaults.InitialMainPageId;
            mainViewPageSwapper?.TrySwapTo(confirmedPageId);
            string newMainPageId = mainViewPageSwapper?.CurrentMainPageId ?? confirmedPageId;
            taskInteractionLogger?.RecordLayeredPageSelection(
                gazeController,
                confirmedPageId,
                previousMainPageId,
                newMainPageId);
        }

    }
}
