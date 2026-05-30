using NUnit.Framework;
using ProjectGaze.Gaze;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class GazeInteractionStateMachineTests
    {
        [Test]
        public void Idle_WithStablePage_TransitionsToPreviewing()
        {
            var stateMachine = new GazeInteractionStateMachine(new GazeInteractionSettings());

            var snapshot = stateMachine.Tick(new GazeInteractionInput(true, "Page_A", "Page_A", false, 0.016f));

            Assert.That(snapshot.Mode, Is.EqualTo(GazeInteractionMode.Previewing));
            Assert.That(snapshot.PreviewPageId, Is.EqualTo("Page_A"));
            Assert.That(snapshot.ConfirmedPageId, Is.Null);
        }

        [Test]
        public void Previewing_WithBlink_TransitionsToConfirmed()
        {
            var stateMachine = new GazeInteractionStateMachine(new GazeInteractionSettings());
            stateMachine.Tick(new GazeInteractionInput(true, "Page_A", "Page_A", false, 0.016f));

            var snapshot = stateMachine.Tick(new GazeInteractionInput(true, "Page_A", "Page_A", true, 0.016f));

            Assert.That(snapshot.Mode, Is.EqualTo(GazeInteractionMode.Confirmed));
            Assert.That(snapshot.ConfirmedPageId, Is.EqualTo("Page_A"));
            Assert.That(snapshot.PreviewPageId, Is.Null);
        }

        [Test]
        public void Confirmed_WithAnotherStablePage_TransitionsToSwitchPreview()
        {
            var stateMachine = new GazeInteractionStateMachine(new GazeInteractionSettings());
            stateMachine.Tick(new GazeInteractionInput(true, "Page_A", "Page_A", false, 0.016f));
            stateMachine.Tick(new GazeInteractionInput(true, "Page_A", "Page_A", true, 0.016f));
            stateMachine.Tick(new GazeInteractionInput(true, "Page_A", "Page_A", false, 0.6f));

            var snapshot = stateMachine.Tick(new GazeInteractionInput(true, "Page_B", "Page_B", false, 0.016f));

            Assert.That(snapshot.Mode, Is.EqualTo(GazeInteractionMode.SwitchPreview));
            Assert.That(snapshot.ConfirmedPageId, Is.EqualTo("Page_A"));
            Assert.That(snapshot.PreviewPageId, Is.EqualTo("Page_B"));
        }

        [Test]
        public void SwitchPreview_WithBlink_TransitionsToConfirmedNewPage()
        {
            var stateMachine = new GazeInteractionStateMachine(new GazeInteractionSettings());
            stateMachine.Tick(new GazeInteractionInput(true, "Page_A", "Page_A", false, 0.016f));
            stateMachine.Tick(new GazeInteractionInput(true, "Page_A", "Page_A", true, 0.016f));
            stateMachine.Tick(new GazeInteractionInput(true, "Page_A", "Page_A", false, 0.6f));
            stateMachine.Tick(new GazeInteractionInput(true, "Page_B", "Page_B", false, 0.016f));

            var snapshot = stateMachine.Tick(new GazeInteractionInput(true, "Page_B", "Page_B", true, 0.016f));

            Assert.That(snapshot.Mode, Is.EqualTo(GazeInteractionMode.Confirmed));
            Assert.That(snapshot.ConfirmedPageId, Is.EqualTo("Page_B"));
            Assert.That(snapshot.PreviewPageId, Is.Null);
        }

        [Test]
        public void SeedConfirmedPage_StartsWithPersistentMainViewPage()
        {
            var stateMachine = new GazeInteractionStateMachine(new GazeInteractionSettings());

            stateMachine.SeedConfirmedPage("Page_A");

            Assert.That(stateMachine.CurrentSnapshot.Mode, Is.EqualTo(GazeInteractionMode.Confirmed));
            Assert.That(stateMachine.CurrentSnapshot.ConfirmedPageId, Is.EqualTo("Page_A"));
            Assert.That(stateMachine.CurrentSnapshot.PreviewPageId, Is.Null);
        }

        [Test]
        public void SeededConfirmedPage_WithAnotherStablePage_TransitionsToSwitchPreview()
        {
            var stateMachine = new GazeInteractionStateMachine(new GazeInteractionSettings());
            stateMachine.SeedConfirmedPage("Page_A");

            var snapshot = stateMachine.Tick(new GazeInteractionInput(true, "Page_B", "Page_B", false, 0.016f));

            Assert.That(snapshot.Mode, Is.EqualTo(GazeInteractionMode.SwitchPreview));
            Assert.That(snapshot.ConfirmedPageId, Is.EqualTo("Page_A"));
            Assert.That(snapshot.PreviewPageId, Is.EqualTo("Page_B"));
        }

        [Test]
        public void GazeInteractionController_SetDepthMatchingMode_UpdatesProjectorMode()
        {
            var controllerObject = new GameObject("Controller");
            var registryObject = new GameObject("Registry");
            var projectorObject = new GameObject("Projector");

            try
            {
                var registry = registryObject.AddComponent<SpatialPageRegistry>();
                registry.Initialize(System.Array.Empty<SpatialPage>());
                var projector = projectorObject.AddComponent<GazeRayProjector>();
                projector.Initialize(registry);

                var controller = controllerObject.AddComponent<GazeInteractionController>();
                controller.Initialize(
                    null,
                    registry,
                    projector,
                    new GazeInteractionSettings(),
                    null,
                    null,
                    PageSelectionMode.MouseFallback,
                    "Initial");

                controller.SetDepthMatchingMode(GazeDepthMatchingMode.ViewportOnly);

                Assert.That(controller.DepthMatchingMode, Is.EqualTo(GazeDepthMatchingMode.ViewportOnly));
            }
            finally
            {
                Object.DestroyImmediate(projectorObject);
                Object.DestroyImmediate(registryObject);
                Object.DestroyImmediate(controllerObject);
            }
        }

        [Test]
        public void GazeInteractionRootFactory_UsesProvidedInitialConfirmedTarget()
        {
            var parentObject = new GameObject("Parent");
            var cameraObject = new GameObject("Camera");
            var pageObject = new GameObject("Page");
            GameObject pageBody = null;

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                var page = pageObject.AddComponent<SpatialPage>();
                pageBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pageBody.transform.SetParent(pageObject.transform, false);
                page.Initialize("Web_A", null, pageBody.GetComponent<Collider>());

                var controller = new GazeInteractionRootFactory().Create(
                    parentObject.transform,
                    camera,
                    new[] { page },
                    new GazeInteractionSettings(),
                    useStereoGazeInput: false,
                    initialConfirmedTargetId: "Web_A");

                var registry = controller.GetComponent<SpatialPageRegistry>();
                var projector = controller.GetComponent<GazeRayProjector>();

                Assert.That(controller.CurrentSnapshot.ConfirmedPageId, Is.EqualTo("Web_A"));
                Assert.That(registry.Pages.Count, Is.EqualTo(1));
                Assert.That(projector.DepthMatchingMode, Is.EqualTo(GazeDepthMatchingMode.DiscreteDepthLayer));
                Assert.That(controller.GetComponent<SpatialPageVisualStateApplier>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(pageBody);
                Object.DestroyImmediate(pageObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(parentObject);
            }
        }
    }
}
