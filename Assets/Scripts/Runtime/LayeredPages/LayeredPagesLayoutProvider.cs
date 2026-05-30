using System.Collections.Generic;
using System;
using ProjectGaze.Gaze.Depth;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    public readonly struct LayeredPageSpec
    {
        public LayeredPageSpec(
            string name,
            Vector3 position,
            float slotScale,
            Color color,
            string depthLayerId,
            float depthLayerRayDistance,
            float depthLayerTolerance,
            float matchingRegionRadiusWorld)
        {
            Name = name;
            Position = position;
            SlotScale = slotScale;
            Color = color;
            DepthLayerId = depthLayerId;
            DepthLayerRayDistance = depthLayerRayDistance;
            DepthLayerTolerance = depthLayerTolerance;
            MatchingRegionRadiusWorld = matchingRegionRadiusWorld;
        }

        public string Name { get; }

        public Vector3 Position { get; }

        public float SlotScale { get; }

        public Color Color { get; }

        public string DepthLayerId { get; }

        public float DepthLayerRayDistance { get; }

        public float DepthLayerTolerance { get; }

        public float MatchingRegionRadiusWorld { get; }
    }

    public static class LayeredPagesLayoutProvider
    {
        public static IReadOnlyList<LayeredPageSpec> GetPageSpecs()
        {
            return new[]
            {
                new LayeredPageSpec("Page_A", new Vector3(0.0f, 0.18f, 18.0f), 2.35f, new Color(0.93f, 0.95f, 0.98f), GazeDepthLayerProfile.ZeroLayerId, GazeDepthLayerProfile.ZeroRayDistance, 3.5f, 3.2f),
                new LayeredPageSpec("Page_B", new Vector3(-6.7f, 2.65f, 9.0f), 0.86f, new Color(0.92f, 0.95f, 0.97f), GazeDepthLayerProfile.Near3LayerId, GazeDepthLayerProfile.Near3RayDistance, 2.5f, 1.7f),
                new LayeredPageSpec("Page_C", new Vector3(6.4f, 2.25f, 9.0f), 0.88f, new Color(0.94f, 0.95f, 0.95f), GazeDepthLayerProfile.Near3LayerId, GazeDepthLayerProfile.Near3RayDistance, 2.5f, 1.7f),
                new LayeredPageSpec("Page_D", new Vector3(-5.7f, -2.35f, 12.0f), 0.98f, new Color(0.92f, 0.95f, 0.98f), GazeDepthLayerProfile.Near2LayerId, GazeDepthLayerProfile.Near2RayDistance, 3.0f, 1.8f),
                new LayeredPageSpec("Page_E", new Vector3(5.9f, -2.10f, 12.0f), 1.00f, new Color(0.94f, 0.94f, 0.97f), GazeDepthLayerProfile.Near2LayerId, GazeDepthLayerProfile.Near2RayDistance, 3.0f, 1.8f),
                new LayeredPageSpec("Page_F", new Vector3(-3.4f, 3.35f, 15.0f), 1.08f, new Color(0.96f, 0.95f, 0.92f), GazeDepthLayerProfile.Near1LayerId, GazeDepthLayerProfile.Near1RayDistance, 3.0f, 1.9f),
                new LayeredPageSpec("Page_G", new Vector3(3.5f, -3.20f, 15.0f), 1.08f, new Color(0.93f, 0.96f, 0.94f), GazeDepthLayerProfile.Near1LayerId, GazeDepthLayerProfile.Near1RayDistance, 3.0f, 1.9f),
                new LayeredPageSpec("Page_H", new Vector3(-6.4f, 0.10f, 18.0f), 1.12f, new Color(0.95f, 0.94f, 0.98f), GazeDepthLayerProfile.ZeroLayerId, GazeDepthLayerProfile.ZeroRayDistance, 3.5f, 1.9f),
                new LayeredPageSpec("Page_I", new Vector3(6.5f, 0.55f, 18.0f), 1.12f, new Color(0.92f, 0.96f, 0.96f), GazeDepthLayerProfile.ZeroLayerId, GazeDepthLayerProfile.ZeroRayDistance, 3.5f, 1.9f),
                new LayeredPageSpec("Page_J", new Vector3(-4.4f, -3.35f, 21.0f), 1.20f, new Color(0.95f, 0.96f, 0.92f), GazeDepthLayerProfile.Far1LayerId, GazeDepthLayerProfile.Far1RayDistance, 4.0f, 2.0f),
                new LayeredPageSpec("Page_K", new Vector3(4.2f, 3.35f, 21.0f), 1.20f, new Color(0.95f, 0.93f, 0.96f), GazeDepthLayerProfile.Far1LayerId, GazeDepthLayerProfile.Far1RayDistance, 4.0f, 2.0f),
                new LayeredPageSpec("Page_L", new Vector3(-2.3f, 3.85f, 24.0f), 1.32f, new Color(0.91f, 0.94f, 0.98f), GazeDepthLayerProfile.Far2LayerId, GazeDepthLayerProfile.Far2RayDistance, 4.0f, 2.2f),
                new LayeredPageSpec("Page_M", new Vector3(2.8f, -3.85f, 24.0f), 1.32f, new Color(0.96f, 0.94f, 0.91f), GazeDepthLayerProfile.Far2LayerId, GazeDepthLayerProfile.Far2RayDistance, 4.0f, 2.2f),
                new LayeredPageSpec("Page_N", new Vector3(-0.9f, -4.25f, 27.0f), 1.46f, new Color(0.92f, 0.95f, 0.93f), GazeDepthLayerProfile.Far3LayerId, GazeDepthLayerProfile.Far3RayDistance, 4.5f, 2.4f),
                new LayeredPageSpec("Page_O", new Vector3(1.0f, 4.30f, 27.0f), 1.46f, new Color(0.95f, 0.93f, 0.98f), GazeDepthLayerProfile.Far3LayerId, GazeDepthLayerProfile.Far3RayDistance, 4.5f, 2.4f)
            };
        }

        public static LayeredPageSpec GetPageSpec(string pageId)
        {
            var specs = GetPageSpecs();
            for (int index = 0; index < specs.Count; index += 1)
            {
                if (string.Equals(specs[index].Name, pageId, StringComparison.Ordinal))
                {
                    return specs[index];
                }
            }

            throw new InvalidOperationException($"Layered page slot '{pageId}' was not found.");
        }

        public static SpatialPageDepthTintProfile BuildDepthTintProfile()
        {
            return BuildDepthTintProfile(GetPageSpecs());
        }

        public static SpatialPageDepthTintProfile BuildDepthTintProfile(IReadOnlyList<LayeredPageSpec> specs)
        {
            float zeroPlaneLocalZ = 0f;
            float nearDepthRange = 0.001f;
            float farDepthRange = 0.001f;

            for (int index = 0; index < specs.Count; index += 1)
            {
                if (specs[index].Name == LayeredPagesSceneDefaults.InitialMainPageId)
                {
                    zeroPlaneLocalZ = specs[index].Position.z;
                    break;
                }
            }

            for (int index = 0; index < specs.Count; index += 1)
            {
                float offset = specs[index].Position.z - zeroPlaneLocalZ;
                if (offset < 0f)
                {
                    nearDepthRange = Mathf.Max(nearDepthRange, -offset);
                }
                else
                {
                    farDepthRange = Mathf.Max(farDepthRange, offset);
                }
            }

            return new SpatialPageDepthTintProfile(
                zeroPlaneLocalZ,
                nearDepthRange,
                farDepthRange,
                LayeredPagesSceneDefaults.NearDepthLightenStrength,
                LayeredPagesSceneDefaults.FarDepthDarkenStrength,
                LayeredPagesSceneDefaults.DepthTintResponseExponent,
                LayeredPagesSceneDefaults.NearDepthTintTarget,
                LayeredPagesSceneDefaults.FarDepthTintTarget);
        }
    }
}
