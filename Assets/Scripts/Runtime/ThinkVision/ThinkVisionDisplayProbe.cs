using System;

namespace ProjectGaze.Hardware.ThinkVision
{
    internal sealed class ThinkVisionDisplayProbe
    {
        public ThinkVisionDisplayProbe(
            bool isWindowsPlatform,
            bool configPresent,
            bool vendorPluginPresent,
            bool thinkVisionDisplayDetected,
            string matchedDisplayName,
            string[] connectedDisplays)
        {
            IsWindowsPlatform = isWindowsPlatform;
            ConfigPresent = configPresent;
            VendorPluginPresent = vendorPluginPresent;
            ThinkVisionDisplayDetected = thinkVisionDisplayDetected;
            MatchedDisplayName = matchedDisplayName;
            ConnectedDisplays = connectedDisplays ?? Array.Empty<string>();
        }

        public bool IsWindowsPlatform { get; }

        public bool ConfigPresent { get; }

        public bool VendorPluginPresent { get; }

        public bool ThinkVisionDisplayDetected { get; }

        public string MatchedDisplayName { get; }

        public string[] ConnectedDisplays { get; }

        public bool HasSdkRuntime => IsWindowsPlatform && ConfigPresent && VendorPluginPresent;

        public bool CanUseStereoBridge => HasSdkRuntime && ThinkVisionDisplayDetected;

        public string BuildDisplaySummary()
        {
            return ConnectedDisplays.Length == 0 ? "No named displays reported" : string.Join(", ", ConnectedDisplays);
        }
    }
}
