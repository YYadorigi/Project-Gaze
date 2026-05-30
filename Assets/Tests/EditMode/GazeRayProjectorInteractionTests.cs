using NUnit.Framework;
using ProjectGaze.Gaze;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class GazeRayProjectorInteractionTests
    {
        [Test]
        public void ProjectFromGaze_UsesMatchingRegionToResolvePageHit()
        {
            using var scene = GazeRayProjectorTestScene.WithRegistry();
            var page = scene.CreatePage("Page_A", new Vector3(0f, 0f, 5f), new Vector3(2f, 2f, 0.1f), 1.5f);
            scene.InitializeRegistry(page);

            Physics.SyncTransforms();
            var hitResult = scene.Projector.ProjectFromGaze(scene.Camera, CenterViewportSample());

            Assert.That(hitResult.TrackingAvailable, Is.True);
            Assert.That(hitResult.HasHitPage, Is.True);
            Assert.That(hitResult.PageId, Is.EqualTo("Page_A"));
            Assert.That(hitResult.TargetId, Is.EqualTo("Page_A"));
            Assert.That(hitResult.Target, Is.SameAs(page));
        }

        [Test]
        public void ProjectFromGaze_PrefersNearestMatchingRegionCenterWhenMultiplePagesOverlap()
        {
            using var scene = GazeRayProjectorTestScene.WithRegistry();
            var primaryPage = scene.CreatePage(
                "Primary",
                new Vector3(0.2f, 0f, 5f),
                new Vector3(1.8f, 1.8f, 0.1f),
                5f,
                "PrimaryPage");
            var secondaryPage = scene.CreatePage(
                "Secondary",
                new Vector3(1.6f, 0f, 5f),
                new Vector3(1.8f, 1.8f, 0.1f),
                5f,
                "SecondaryPage");
            scene.InitializeRegistry(primaryPage, secondaryPage);

            Physics.SyncTransforms();
            var hitResult = scene.Projector.ProjectFromGaze(scene.Camera, CenterViewportSample());

            Assert.That(hitResult.HasHitPage, Is.True);
            Assert.That(hitResult.PageId, Is.EqualTo("Primary"));
        }

        [Test]
        public void ProjectFromMouse_SelectsNearestDepthPageUnderCursor()
        {
            using var scene = GazeRayProjectorTestScene.WithoutRegistry();
            scene.CreatePage("Near", new Vector3(0f, 0f, 5f), new Vector3(2f, 2f, 0.1f), objectName: "NearPage");
            scene.CreatePage("Far", new Vector3(0f, 0f, 8f), new Vector3(2f, 2f, 0.1f), objectName: "FarPage");

            Physics.SyncTransforms();
            var hitResult = scene.Projector.ProjectFromMouse(scene.Camera, CenterViewportSample(), scrollDelta: 0f);

            Assert.That(hitResult.HasHitPage, Is.True);
            Assert.That(hitResult.PageId, Is.EqualTo("Near"));
        }

        [Test]
        public void ProjectFromMouse_ScrollCyclesThroughOverlappingDepthPages()
        {
            using var scene = GazeRayProjectorTestScene.WithoutRegistry();
            scene.CreatePage("Near", new Vector3(0f, 0f, 5f), new Vector3(2f, 2f, 0.1f), objectName: "NearPage");
            scene.CreatePage("Far", new Vector3(0f, 0f, 8f), new Vector3(2f, 2f, 0.1f), objectName: "FarPage");

            Physics.SyncTransforms();
            var firstHit = scene.Projector.ProjectFromMouse(scene.Camera, CenterViewportSample(), scrollDelta: 0f);
            var secondHit = scene.Projector.ProjectFromMouse(scene.Camera, CenterViewportSample(), scrollDelta: 1f);

            Assert.That(firstHit.PageId, Is.EqualTo("Near"));
            Assert.That(secondHit.PageId, Is.EqualTo("Far"));
        }

        private static GazeTrackingSample CenterViewportSample()
        {
            return new GazeTrackingSample(true, new Vector2(0.5f, 0.5f), 1L, true, true);
        }
    }
}
