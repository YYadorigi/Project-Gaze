using System;
using System.IO;
using System.Linq;
using AS3DPlugin;
using UnityEngine;
using WindowsDisplayAPI.DisplayConfig;

namespace ProjectGaze.Hardware.ThinkVision
{
    public interface IThinkVisionDisplayBridge
    {
        bool IsVendorRuntimeDetected { get; }

        bool IsStereoDisplayActive { get; }

        string StatusText { get; }

        void Configure(Camera camera);
    }

    public static class ThinkVisionDisplayBridgeFactory
    {
        public static IThinkVisionDisplayBridge Create()
        {
            var probe = ThinkVisionBridgeEnvironment.Probe();

            if (probe.CanUseStereoBridge)
            {
                return new VendorThinkVisionDisplayBridge(probe);
            }

            if (probe.HasSdkRuntime)
            {
                return new PreviewThinkVisionDisplayBridge(probe);
            }

            return new FallbackThinkVisionDisplayBridge(ThinkVisionBridgeEnvironment.DetectVendorRuntime());
        }
    }

    internal static class ThinkVisionBridgeEnvironment
    {
        private static readonly string[] ThinkVision27DisplayMarkers = { "27 3D" };

        public static bool DetectVendorRuntime()
        {
            if (File.Exists(GetConfigPath()) || File.Exists(GetVendorPluginPath()))
            {
                return true;
            }

            var assemblyNames = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetName().Name ?? string.Empty);

            return assemblyNames.Any(name =>
                name.Contains("Lenovo", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("ThinkVision", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("3DExplorer", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("WindowsDisplayAPI", StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsWindowsPlatform()
        {
            return Application.platform == RuntimePlatform.WindowsEditor ||
                   Application.platform == RuntimePlatform.WindowsPlayer;
        }

        public static ThinkVisionDisplayProbe Probe()
        {
            var configPresent = File.Exists(GetConfigPath());
            var vendorPluginPresent = File.Exists(GetVendorPluginPath());
            var isWindowsPlatform = IsWindowsPlatform();
            var connectedDisplays = Array.Empty<string>();
            string matchedDisplayName = null;

            if (isWindowsPlatform && vendorPluginPresent)
            {
                TryDetectThinkVision27Display(out connectedDisplays, out matchedDisplayName);
            }

            return new ThinkVisionDisplayProbe(
                isWindowsPlatform,
                configPresent,
                vendorPluginPresent,
                matchedDisplayName != null,
                matchedDisplayName,
                connectedDisplays);
        }

        public static string GetConfigPath()
        {
            return Path.Combine(Application.streamingAssetsPath, "Config.xml");
        }

        public static string GetVendorPluginPath()
        {
            return Path.Combine(Application.dataPath, "Plugins", "ThinkVision", "AutoStereo", "Plugins", "WindowsDisplayAPI.dll");
        }

        private static void TryDetectThinkVision27Display(out string[] connectedDisplays, out string matchedDisplayName)
        {
            matchedDisplayName = null;

            try
            {
                var displayTargets = PathDisplayTarget.GetDisplayTargets();
                connectedDisplays = displayTargets
                    .Select(target => string.IsNullOrWhiteSpace(target.FriendlyName) ? "(Unnamed Display)" : target.FriendlyName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                matchedDisplayName = connectedDisplays.FirstOrDefault(IsThinkVision27DisplayName);
            }
            catch
            {
                connectedDisplays = Array.Empty<string>();
            }
        }

        private static bool IsThinkVision27DisplayName(string displayName)
        {
            return !string.IsNullOrWhiteSpace(displayName) &&
                   ThinkVision27DisplayMarkers.Any(marker => displayName.Contains(marker, StringComparison.OrdinalIgnoreCase));
        }
    }

    internal sealed class VendorThinkVisionDisplayBridge : IThinkVisionDisplayBridge
    {
        private readonly ThinkVisionDisplayProbe probe;
        private ThinkVisionRuntimeDiagnostics diagnostics;

        private string statusText;

        public VendorThinkVisionDisplayBridge(ThinkVisionDisplayProbe probe)
        {
            this.probe = probe;
            statusText = $"ThinkVision 27 3D detected as '{probe.MatchedDisplayName}'.";
        }

        public bool IsVendorRuntimeDetected => true;

        public bool IsStereoDisplayActive => true;

        public string StatusText => diagnostics != null ? $"{statusText} {diagnostics.BuildBridgeStatusText()}" : statusText;

        public void Configure(Camera camera)
        {
            ThinkVisionFullscreenController.EnsureFullscreenWindow();
            ThinkVisionCameraDefaults.Apply(camera);

            var stereoCamera = camera.GetComponent<StereoCam>() ?? camera.gameObject.AddComponent<StereoCam>();
            stereoCamera.VirtualScreenWidth = 8.8f;
            stereoCamera.StereoStrength = ThinkVisionStereoSceneScale.ReducedGhostingStereoStrength;
            stereoCamera.FrustumSyncEnable = true;
            stereoCamera.NearClamp = 0.1f;
            stereoCamera.ClearScreenWhileSwitch = true;
            stereoCamera.ExitBypass2DSwitch = false;
            stereoCamera._antiAliasing = 4;
            stereoCamera.ScriptsToCopy = Array.Empty<string>();
            stereoCamera.usedShader = StereoCam.AutoStereoShader.SideBySide;
            stereoCamera.HoldBeforeSwitch3DOn = false;
            ThinkVisionUrpPostProcessingGuard.Apply(camera, stereoCamera);

            if (camera.GetComponent<StereoUI>() == null)
            {
                camera.gameObject.AddComponent<StereoUI>();
            }

            diagnostics = ThinkVisionRuntimeDiagnostics.Attach(camera, stereoCamera);
            ThinkVisionStereoActivator.Attach(camera, stereoCamera);
        }
    }

    internal sealed class PreviewThinkVisionDisplayBridge : IThinkVisionDisplayBridge
    {
        private readonly ThinkVisionDisplayProbe probe;

        public PreviewThinkVisionDisplayBridge(ThinkVisionDisplayProbe probe)
        {
            this.probe = probe;
        }

        public bool IsVendorRuntimeDetected => true;

        public bool IsStereoDisplayActive => false;

        public string StatusText
        {
            get
            {
                if (!probe.IsWindowsPlatform)
                {
                    return "ThinkVision SDK is staged, but stereo output is only supported from the Windows editor/player. Running the original mono preview camera. Project safety guard expects render scale 1.0; keep Game view at 1x during stereo validation.";
                }

                if (!probe.VendorPluginPresent)
                {
                    return "ThinkVision SDK is staged, but WindowsDisplayAPI.dll is missing. Running the original mono preview camera. Project safety guard expects render scale 1.0; keep Game view at 1x during stereo validation.";
                }

                if (!probe.ConfigPresent)
                {
                    return "ThinkVision SDK is staged, but StreamingAssets/Config.xml is missing. Running the original mono preview camera. Project safety guard expects render scale 1.0; keep Game view at 1x during stereo validation.";
                }

                return $"ThinkVision SDK is staged, but no ThinkVision 27 3D display was detected. Connected displays: {probe.BuildDisplaySummary()}. Running the original mono preview camera. Project safety guard expects render scale 1.0; keep Game view at 1x during stereo validation.";
            }
        }

        public void Configure(Camera camera)
        {
            ThinkVisionCameraDefaults.Apply(camera);
        }
    }

    internal sealed class FallbackThinkVisionDisplayBridge : IThinkVisionDisplayBridge
    {
        private readonly bool vendorRuntimeDetected;

        public FallbackThinkVisionDisplayBridge(bool vendorRuntimeDetected)
        {
            this.vendorRuntimeDetected = vendorRuntimeDetected;
        }

        public bool IsVendorRuntimeDetected => vendorRuntimeDetected;

        public bool IsStereoDisplayActive => false;

        public string StatusText =>
            vendorRuntimeDetected
                ? "Detected Lenovo-related runtime, but the ThinkVision bridge is not fully configured. Check Windows platform support and StreamingAssets/Config.xml. Project safety guard expects render scale 1.0; keep Game view at 1x during stereo validation."
                : "Stereo runtime not linked. This demo uses a mono preview camera; final ThinkVision 27 3D validation must happen on Windows 10/11 with Lenovo 3D software installed. Project safety guard expects render scale 1.0; keep Game view at 1x during stereo validation.";

        public void Configure(Camera camera)
        {
            ThinkVisionCameraDefaults.Apply(camera);
        }
    }

}
