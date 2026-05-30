using System.Collections.Generic;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    internal static class LayeredPagesPrimitiveUtility
    {
        public static GameObject CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = scale;

            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return block;
        }

        public static Material CreateMaterial(ICollection<Material> createdMaterials, Color color, bool transparent)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));

            if (transparent)
            {
                TransparentMaterialUtility.Configure(material);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            createdMaterials?.Add(material);
            return material;
        }

        public static void DestroyCreatedMaterials(IReadOnlyList<Material> createdMaterials)
        {
            if (createdMaterials == null)
            {
                return;
            }

            foreach (var material in createdMaterials)
            {
                if (material == null)
                {
                    continue;
                }

                UnityObjectLifecycleUtility.DestroyObject(material);
            }
        }
    }
}
