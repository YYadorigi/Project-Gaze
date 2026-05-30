using UnityEngine;

namespace ProjectGaze.Gaze
{
    internal static class DepthGatedAgentContentFactory
    {
        public static LayeredSearchPageContentData BuildWebContent(string engineName, string query, Color accentColor)
        {
            return new LayeredSearchPageContentData(
                engineName,
                accentColor,
                query,
                "Mock workspace page for depth-gated interaction",
                new[] { "All", "Tasks", "Notes", "Sources" },
                new[]
                {
                    new LayeredSearchResultEntryData("Depth-aware gaze selection", "workspace.local/depth-gaze", "Use predicted ray distance to separate targets that overlap in screen space."),
                    new LayeredSearchResultEntryData("Blink-confirm interaction rule", "workspace.local/blink-confirm", "Preview is driven by stable gaze; confirmation is driven by a deliberate confirmation signal."),
                    new LayeredSearchResultEntryData("Agent panel trigger", "workspace.local/agent-trigger", "A far-depth logo can expand into a simulated assistant panel.")
                },
                "Scene note",
                new[]
                {
                    "Pages span Near2 through Far1",
                    "The agent logo is placed at Far3",
                    "Confirm another page to collapse the panel back into the logo"
                },
                new[]
                {
                    "SUS usability test",
                    "SVR depth error curve",
                    "seven depth layers"
                });
        }
    }
}
