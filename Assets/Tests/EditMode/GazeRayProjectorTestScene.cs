using System;
using System.Collections.Generic;
using ProjectGaze.Gaze;
using UnityEngine;

namespace ProjectGaze.Tests
{
    internal sealed class GazeRayProjectorTestScene : IDisposable
    {
        private readonly List<GameObject> pageObjects = new();
        private readonly List<GameObject> pageBodies = new();
        private readonly GameObject cameraObject;
        private readonly GameObject projectorObject;
        private readonly GameObject registryObject;

        private GazeRayProjectorTestScene(bool createRegistry)
        {
            cameraObject = new GameObject("TestCamera");
            Camera = cameraObject.AddComponent<Camera>();
            Camera.transform.position = Vector3.zero;
            Camera.transform.rotation = Quaternion.identity;

            projectorObject = new GameObject("Projector");
            Projector = projectorObject.AddComponent<GazeRayProjector>();

            if (createRegistry)
            {
                registryObject = new GameObject("Registry");
                Registry = registryObject.AddComponent<SpatialPageRegistry>();
            }
        }

        public Camera Camera { get; }

        public GazeRayProjector Projector { get; }

        public SpatialPageRegistry Registry { get; }

        public static GazeRayProjectorTestScene WithRegistry()
        {
            return new GazeRayProjectorTestScene(createRegistry: true);
        }

        public static GazeRayProjectorTestScene WithoutRegistry()
        {
            return new GazeRayProjectorTestScene(createRegistry: false);
        }

        public SpatialPage CreatePage(
            string targetId,
            Vector3 position,
            Vector3 scale,
            float? matchingRegionRadius = null,
            string objectName = null)
        {
            var pageObject = new GameObject(objectName ?? targetId);
            pageObjects.Add(pageObject);

            var page = pageObject.AddComponent<SpatialPage>();
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pageBodies.Add(body);
            body.transform.SetParent(pageObject.transform, false);
            body.transform.position = position;
            body.transform.localScale = scale;
            page.Initialize(targetId, null, body.GetComponent<Collider>());

            if (matchingRegionRadius.HasValue)
            {
                page.ConfigureMatchingRegion(matchingRegionRadius.Value);
            }

            return page;
        }

        public void InitializeRegistry(params SpatialPage[] pages)
        {
            Registry.Initialize(pages);
            Projector.Initialize(Registry);
        }

        public void Dispose()
        {
            foreach (var body in pageBodies)
            {
                UnityEngine.Object.DestroyImmediate(body);
            }

            foreach (var pageObject in pageObjects)
            {
                UnityEngine.Object.DestroyImmediate(pageObject);
            }

            UnityEngine.Object.DestroyImmediate(registryObject);
            UnityEngine.Object.DestroyImmediate(projectorObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
