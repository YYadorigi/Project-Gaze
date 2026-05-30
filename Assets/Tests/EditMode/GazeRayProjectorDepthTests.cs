using NUnit.Framework;
using ProjectGaze.Gaze;
using ProjectGaze.Gaze.Depth;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class GazeRayProjectorDepthTests
    {
        [Test]
        public void ProjectFromGaze_UsesPredictedWorldPointToResolveDepth()
        {
            using var scene = GazeRayProjectorTestScene.WithRegistry();
            var nearPage = scene.CreatePage("Near", new Vector3(0f, 0f, 5f), new Vector3(2f, 2f, 0.1f), 1.5f, "NearPage");
            var farPage = scene.CreatePage("Far", new Vector3(0f, 0f, 8f), new Vector3(2f, 2f, 0.1f), 1.5f, "FarPage");
            scene.InitializeRegistry(nearPage, farPage);

            Physics.SyncTransforms();
            var hitResult = scene.Projector.ProjectFromGaze(scene.Camera, CenterViewportDepthSample(new Vector3(0f, 0f, 8f), 8f));

            Assert.That(hitResult.HasHitPage, Is.True);
            Assert.That(hitResult.PageId, Is.EqualTo("Far"));
        }

        [Test]
        public void ProjectFromGaze_DiscreteDepthLayerSelectsPredictedLayerWhenViewportOverlaps()
        {
            using var scene = GazeRayProjectorTestScene.WithRegistry();
            scene.Projector.SetDepthMatchingMode(GazeDepthMatchingMode.DiscreteDepthLayer);

            var nearPage = scene.CreatePage("Near", new Vector3(0f, 0f, 5f), new Vector3(2f, 2f, 0.1f), 2.0f, "NearPage");
            nearPage.ConfigureDepthLayer(GazeDepthLayerProfile.Near3LayerId, GazeDepthLayerProfile.Near3RayDistance, 2.5f);

            var farPage = scene.CreatePage("Far", new Vector3(0f, 0f, 8f), new Vector3(2f, 2f, 0.1f), 2.0f, "FarPage");
            farPage.ConfigureDepthLayer(GazeDepthLayerProfile.Far3LayerId, GazeDepthLayerProfile.Far3RayDistance, 4.5f);

            scene.InitializeRegistry(nearPage, farPage);

            Physics.SyncTransforms();
            var hitResult = scene.Projector.ProjectFromGaze(
                scene.Camera,
                CenterViewportDepthSample(new Vector3(0f, 0f, 5f), GazeDepthLayerProfile.Far3RayDistance - 0.4f));

            Assert.That(hitResult.HasHitPage, Is.True);
            Assert.That(hitResult.PageId, Is.EqualTo("Far"));
            Assert.That(hitResult.MatchingMode, Is.EqualTo(GazeDepthMatchingMode.DiscreteDepthLayer));
            Assert.That(hitResult.DepthLayerId, Is.EqualTo(GazeDepthLayerProfile.Far3LayerId));
        }

        [Test]
        public void ProjectFromGaze_ViewportOnlyIgnoresPredictedWorldPoint()
        {
            using var scene = GazeRayProjectorTestScene.WithRegistry();
            scene.Projector.SetDepthMatchingMode(GazeDepthMatchingMode.ViewportOnly);

            var nearPage = scene.CreatePage("Near", new Vector3(0f, 0f, 5f), new Vector3(2f, 2f, 0.1f), 2.0f, "NearPage");
            var farPage = scene.CreatePage("Far", new Vector3(0f, 0f, 8f), new Vector3(2f, 2f, 0.1f), 2.0f, "FarPage");
            scene.InitializeRegistry(nearPage, farPage);
            scene.Projector.SetDepthMatchingMode(GazeDepthMatchingMode.ViewportOnly);

            Physics.SyncTransforms();
            var hitResult = scene.Projector.ProjectFromGaze(
                scene.Camera,
                CenterViewportDepthSample(new Vector3(0f, 0f, 8f), GazeDepthLayerProfile.Far3RayDistance));

            Assert.That(hitResult.HasHitPage, Is.True);
            Assert.That(hitResult.PageId, Is.EqualTo("Near"));
            Assert.That(hitResult.MatchingMode, Is.EqualTo(GazeDepthMatchingMode.ViewportOnly));
        }

        private static GazeTrackingSample CenterViewportDepthSample(Vector3 predictedWorldPoint, float predictedRayDistance)
        {
            return new GazeTrackingSample(
                true,
                new Vector2(0.5f, 0.5f),
                1L,
                true,
                true,
                true,
                predictedWorldPoint,
                predictedRayDistance);
        }
    }
}
