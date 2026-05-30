using System.Collections.Generic;
using ProjectGaze.Gaze.Depth;
using ProjectGaze.Gaze.Providers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectGaze.Gaze
{
    public sealed class GazeInteractionRootFactory
    {
        public GazeInteractionController Create(
            Transform parent,
            Camera mainCamera,
            IReadOnlyList<SpatialPage> pages,
            GazeInteractionSettings gazeSettings,
            bool useStereoGazeInput,
            string initialConfirmedTargetId,
            string sceneId = null)
        {
            var root = new GameObject("GazeInteractionRoot").transform;
            root.SetParent(parent, false);
            string resolvedSceneId = string.IsNullOrWhiteSpace(sceneId)
                ? SceneManager.GetActiveScene().name
                : sceneId;

            var registry = root.gameObject.AddComponent<SpatialPageRegistry>();
            registry.Initialize(pages);

            var projector = root.gameObject.AddComponent<GazeRayProjector>();
            projector.Initialize(registry);
            projector.SetDepthMatchingMode(GazeDepthMatchingMode.DiscreteDepthLayer);

            IGazeTrackingProvider gazeProvider;
            IBlinkDetectionProvider blinkProvider;
            PageSelectionMode selectionMode;

            if (useStereoGazeInput)
            {
                var stereoGazeProvider = root.gameObject.AddComponent<InvensunA8GazeTrackingProvider>();
                stereoGazeProvider.Initialize();
                stereoGazeProvider.ConfigureTargetCamera(mainCamera);

                var stereoBlinkProvider = root.gameObject.AddComponent<InvensunA8BlinkDetectionProvider>();
                stereoBlinkProvider.Initialize();

                gazeProvider = stereoGazeProvider;
                blinkProvider = stereoBlinkProvider;
                selectionMode = PageSelectionMode.StereoGaze;
            }
            else
            {
                var mouseGazeProvider = root.gameObject.AddComponent<MouseCursorGazeTrackingProvider>();
                mouseGazeProvider.ConfigureTargetCamera(mainCamera);

                gazeProvider = mouseGazeProvider;
                blinkProvider = root.gameObject.AddComponent<MouseClickConfirmProvider>();
                selectionMode = PageSelectionMode.MouseFallback;
            }

            var gazeController = root.gameObject.AddComponent<GazeInteractionController>();
            gazeController.Initialize(
                mainCamera,
                registry,
                projector,
                gazeSettings,
                gazeProvider,
                blinkProvider,
                selectionMode,
                initialConfirmedTargetId);

            var visualStateApplier = root.gameObject.AddComponent<SpatialPageVisualStateApplier>();
            visualStateApplier.Initialize(registry, gazeSettings, gazeController);

            GazeExperimentDataCapture.AttachRuntimeLayerRecorder(
                root,
                mainCamera,
                registry,
                gazeController,
                resolvedSceneId);

            return gazeController;
        }
    }
}
