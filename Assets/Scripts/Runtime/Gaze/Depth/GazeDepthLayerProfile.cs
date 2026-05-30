namespace ProjectGaze.Gaze.Depth
{
    public sealed class DepthLayerProfile
    {
        public DepthLayerProfile(
            string profileVersion,
            float zeroRayDistance,
            GazeDepthLayerDefinition[] layers)
        {
            ProfileVersion = profileVersion;
            ZeroRayDistance = zeroRayDistance;
            Layers = layers ?? System.Array.Empty<GazeDepthLayerDefinition>();
        }

        public string ProfileVersion { get; }

        public float ZeroRayDistance { get; }

        public System.Collections.Generic.IReadOnlyList<GazeDepthLayerDefinition> Layers { get; }
    }

    public readonly struct GazeDepthLayerDefinition
    {
        public GazeDepthLayerDefinition(
            string layerId,
            float rayDistance,
            float offsetFromZeroPlane,
            float tolerance)
        {
            LayerId = layerId;
            RayDistance = rayDistance;
            OffsetFromZeroPlane = offsetFromZeroPlane;
            Tolerance = tolerance;
        }

        public string LayerId { get; }

        public float RayDistance { get; }

        public float OffsetFromZeroPlane { get; }

        public float Tolerance { get; }
    }

    public static class GazeDepthLayerProfile
    {
        public const string SymmetricSevenV1ProfileVersion = "symmetric-seven-v1";
        public const string WideV1ProfileVersion = SymmetricSevenV1ProfileVersion;

        public const string Near3LayerId = "Near3";
        public const string Near2LayerId = "Near2";
        public const string Near1LayerId = "Near1";
        public const string ZeroLayerId = "Zero";
        public const string Far1LayerId = "Far1";
        public const string Far2LayerId = "Far2";
        public const string Far3LayerId = "Far3";

        public const float Near3RayDistance = 9.0f;
        public const float Near2RayDistance = 12.0f;
        public const float Near1RayDistance = 15.0f;
        public const float ZeroRayDistance = 18.0f;
        public const float Far1RayDistance = 21.0f;
        public const float Far2RayDistance = 24.0f;
        public const float Far3RayDistance = 27.0f;

        public const float NearRayDistance = Near2RayDistance;
        public const float MidRayDistance = ZeroRayDistance;
        public const float FarRayDistance = Far2RayDistance;

        private static readonly GazeDepthLayerDefinition[] SymmetricSevenLayers =
        {
            new(Near3LayerId, Near3RayDistance, -9.0f, 2.5f),
            new(Near2LayerId, Near2RayDistance, -6.0f, 3.0f),
            new(Near1LayerId, Near1RayDistance, -3.0f, 3.0f),
            new(ZeroLayerId, ZeroRayDistance, 0.0f, 3.5f),
            new(Far1LayerId, Far1RayDistance, 3.0f, 4.0f),
            new(Far2LayerId, Far2RayDistance, 6.0f, 4.0f),
            new(Far3LayerId, Far3RayDistance, 9.0f, 4.5f)
        };

        public static int LayerCount => SymmetricSevenLayers.Length;

        public static GazeDepthLayerDefinition GetLayer(int index)
        {
            if (index < 0 || index >= SymmetricSevenLayers.Length)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index));
            }

            return SymmetricSevenLayers[index];
        }

        public static DepthLayerProfile CreateSymmetricSevenV1Profile()
        {
            return new DepthLayerProfile(
                SymmetricSevenV1ProfileVersion,
                ZeroRayDistance,
                (GazeDepthLayerDefinition[])SymmetricSevenLayers.Clone());
        }

        public static float[] BuildWideV1RayDistances()
        {
            return BuildSymmetricSevenV1RayDistances();
        }

        public static float[] BuildSymmetricSevenV1RayDistances()
        {
            return new[]
            {
                Near3RayDistance,
                Near2RayDistance,
                Near1RayDistance,
                ZeroRayDistance,
                Far1RayDistance,
                Far2RayDistance,
                Far3RayDistance
            };
        }
    }
}
