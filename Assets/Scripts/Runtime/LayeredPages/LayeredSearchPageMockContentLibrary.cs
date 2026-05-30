using System.Collections.Generic;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    internal static class LayeredSearchPageMockContentLibrary
    {
        public static Dictionary<string, LayeredSearchPageContentData> CreatePageMap()
        {
            return new Dictionary<string, LayeredSearchPageContentData>
            {
                ["Page_A"] = new LayeredSearchPageContentData(
                    "Open Search",
                    new Color(0.19f, 0.44f, 0.88f),
                    "ThinkVision 27 3D Unity SDK",
                    "About 28,400 results (0.29 seconds)",
                    new[] { "All", "Docs", "Images", "News", "Videos" },
                    new[]
                    {
                        new LayeredSearchResultEntryData("ThinkVision 27 3D Unity plugin setup guide", "docs.projectgaze.local/thinkvision/unity-plugin", "Install the 27 3D driver, place the AutoStereo runtime in Assets/Plugins, copy Config.xml into StreamingAssets, and bridge StereoCam from a project-owned camera adapter."),
                        new LayeredSearchResultEntryData("LADM + 27 3D driver checklist for URP", "support.lenovo.local/27-3d/ladm-driver-urp", "Confirm Windows display naming, validate SideBySide output on device, and keep a mono preview branch for ordinary monitors during editor iteration."),
                        new LayeredSearchResultEntryData("StereoCam integration notes for Project Gaze", "wiki.projectgaze.local/thinkvision/stereocam-bridge", "Use a narrow bridge surface, avoid vendor edits inside gameplay code, and preserve fallback camera behavior when a real 27 3D panel is not detected.")
                    },
                    "Device snapshot",
                    new[]
                    {
                        "Display: 27-inch glasses-free 3D panel",
                        "Runtime: Lenovo AutoStereo SDK v2.5",
                        "Pipeline: Unity URP with mono fallback",
                        "Goal: stable bridge before gaze input"
                    },
                    new[]
                    {
                        "thinkvision 27 3d config xml",
                        "unity stereocam mono fallback",
                        "lenovo 3d driver ladm setup"
                    }),
                ["Page_B"] = new LayeredSearchPageContentData(
                    "Open Search",
                    new Color(0.92f, 0.47f, 0.18f),
                    "7Invensun A8 Unity gaze tracking",
                    "About 12,900 results (0.21 seconds)",
                    new[] { "All", "API", "Guides", "Examples", "Hardware" },
                    new[]
                    {
                        new LayeredSearchResultEntryData("7Invensun A8 screen-based Unity bootstrap", "docs.projectgaze.local/7invensun/a8-bootstrap", "Extract the minimal A8 runtime, start the SDK with StreamingAssets config, convert recommended gaze coordinates to viewport rays, and keep the interaction path fully eye-tracker driven."),
                        new LayeredSearchResultEntryData("Gaze sample validity and blink heuristics", "research.projectgaze.local/7invensun/validity-blink", "Prefer the recommended gaze point when valid, fall back to per-eye gaze, and combine blink plus openness signals into a conservative blink-confirm rule."),
                        new LayeredSearchResultEntryData("Layered pages provider boundary in Project Gaze", "wiki.projectgaze.local/layered-pages/provider-boundary", "Keep 7Invensun as a project-owned interaction layer first, then decide later whether the same data should feed ThinkVision frustum sync.")
                    },
                    "Tracking stack",
                    new[]
                    {
                        "Input: 7Invensun A8",
                        "Provider: InvensunA8GazeTrackingProvider",
                        "Pointer Fallback: Removed",
                        "State machine: Preview / Confirm"
                    },
                    new[]
                    {
                        "tobii spark unity eye tracker",
                        "gaze ray viewport conversion",
                        "blink confirmation provider"
                    }),
                ["Page_C"] = new LayeredSearchPageContentData(
                    "Open Search",
                    new Color(0.19f, 0.62f, 0.42f),
                    "gaze interaction design patterns",
                    "About 41,700 results (0.33 seconds)",
                    new[] { "All", "Research", "UI", "Papers", "Patterns" },
                    new[]
                    {
                        new LayeredSearchResultEntryData("Preview-confirm interaction for gaze-first systems", "journal.hci.local/preview-confirm-patterns", "Separate stable dwell from explicit confirmation so users can inspect interface targets without committing every time their eyes pass over them."),
                        new LayeredSearchResultEntryData("Suppressing non-target content with graded transparency", "design.projectgaze.local/visual-state/suppression", "Dormant pages remain visible enough for orientation, preview pages gain brightness and alpha, and confirmed pages become fully readable while unrelated pages recede."),
                        new LayeredSearchResultEntryData("Switch-preview without losing the confirmed context", "notes.projectgaze.local/switch-preview", "Keep the current confirmed page until the user previews a new page long enough and triggers an intentional confirmation action.")
                    },
                    "Pattern cues",
                    new[]
                    {
                        "Stable dwell before preview",
                        "Intentional confirm after preview",
                        "Low-jitter candidate tracking",
                        "Suppressed secondary content"
                    },
                    new[]
                    {
                        "gaze preview confirmed state",
                        "transparency hierarchy for gaze ui",
                        "eye-first interaction patterns"
                    }),
                ["Page_D"] = new LayeredSearchPageContentData(
                    "Open Search",
                    new Color(0.17f, 0.53f, 0.73f),
                    "world-space search results layout",
                    "About 8,520 results (0.18 seconds)",
                    new[] { "All", "3D UI", "Layouts", "Unity", "Spatial" },
                    new[]
                    {
                        new LayeredSearchResultEntryData("Designing readable web-like cards in 3D space", "ux.projectgaze.local/world-space/card-layout", "Use a shallow panel, high-contrast type, broad margins, and a single visual rhythm so the page still reads like a web result surface when distributed across depth."),
                        new LayeredSearchResultEntryData("World-space canvas sizing for depth-separated pages", "docs.projectgaze.local/world-space/canvas-sizing", "Anchor a world-space canvas slightly in front of the page body, scale it proportionally to the page dimensions, and keep the preview affordance closer to the camera than the page collider."),
                        new LayeredSearchResultEntryData("Search page composition for layered page scenes", "notes.projectgaze.local/demo/search-page-composition", "Top band, search box, result list, side card, and related queries are enough to imply a recognisable search engine layout without needing a full browser implementation.")
                    },
                    "Layout checklist",
                    new[]
                    {
                        "Inset world-space canvas",
                        "Large line spacing for depth viewing",
                        "Consistent margins and cards",
                        "Preview button floats in front"
                    },
                    new[]
                    {
                        "unity world space search card",
                        "3d page ui readability",
                        "depth layered web layouts"
                    }),
                ["Page_E"] = new LayeredSearchPageContentData(
                    "Open Search",
                    new Color(0.84f, 0.36f, 0.22f),
                    "blink confirm dwell time HCI",
                    "About 17,300 results (0.27 seconds)",
                    new[] { "All", "Papers", "Metrics", "HCI", "Notes" },
                    new[]
                    {
                        new LayeredSearchResultEntryData("Intentional blink windows for gaze confirmation", "research.projectgaze.local/blink/windows", "A conservative first-pass rule uses dual-eye invalidity lasting roughly 100 to 260 ms, preceded by a stable preview target and followed by recovery on the same page."),
                        new LayeredSearchResultEntryData("Cooldown strategies after gaze confirmation", "docs.projectgaze.local/blink/cooldown", "After a confirm event, enforce a short cooldown so repeated involuntary blinks or noisy tracking loss do not cause rapid page switching."),
                        new LayeredSearchResultEntryData("EditMode tests for blink policy and page states", "tests.projectgaze.local/editmode/blink-policy", "Keep the blink rule pure and deterministic so confirmation timing and visual state transitions remain testable outside Play Mode.")
                    },
                    "Blink policy",
                    new[]
                    {
                        "Min duration: 100 ms",
                        "Max duration: 260 ms",
                        "Cooldown: 400 ms",
                        "Requires stable preview page"
                    },
                    new[]
                    {
                        "blink confirm hci timing",
                        "gaze dwell blink cooldown",
                        "preview confirm editmode tests"
                    }),
                ["Page_F"] = new LayeredSearchPageContentData(
                    "Open Search",
                    new Color(0.59f, 0.42f, 0.19f),
                    "switch framebuffer interaction metaphor",
                    "About 9,870 results (0.19 seconds)",
                    new[] { "All", "Rendering", "Patterns", "UI", "Notes" },
                    new[]
                    {
                        new LayeredSearchResultEntryData("Switch-framebuffer inspired UI swaps", "design.projectgaze.local/framebuffer/swap-ui", "Treat one large, readable page as the persistent focus surface and swap another page into that slot only after the user confirms intent."),
                        new LayeredSearchResultEntryData("Maintaining a dominant focus region in layered UIs", "notes.projectgaze.local/focus-region", "Keep one surface bright and central so users always have a stable reading target while peripheral pages remain available for preview and replacement."),
                        new LayeredSearchResultEntryData("Page-slot exchange interactions for spatial content", "research.projectgaze.local/page-slot-exchange", "A confirmed page can inherit the dominant slot while the old dominant page recedes to the displaced slot, preserving spatial continuity.")
                    },
                    "Swap metaphor",
                    new[]
                    {
                        "One dominant slot remains readable",
                        "Peripheral pages stay swappable",
                        "Confirmation triggers slot exchange",
                        "Attention stays centered"
                    },
                    new[]
                    {
                        "framebuffer swap ui metaphor",
                        "dominant focus surface interaction",
                        "spatial page slot exchange"
                    }),
                ["Page_G"] = new LayeredSearchPageContentData(
                    "Open Search",
                    new Color(0.29f, 0.56f, 0.64f),
                    "multilayer spatial browsing with gaze",
                    "About 14,200 results (0.24 seconds)",
                    new[] { "All", "Spatial", "Layouts", "Research", "Examples" },
                    new[]
                    {
                        new LayeredSearchResultEntryData("Peripheral-to-central browsing in layered scenes", "journal.projectgaze.local/peripheral-central-browsing", "Users can scan smaller peripheral pages, preview them, and promote one page into a larger central slot only when it becomes relevant."),
                        new LayeredSearchResultEntryData("Gaze-friendly placement for depth-separated cards", "ux.projectgaze.local/depth-card-placement", "Place candidate surfaces around a dominant page, vary their depth, and avoid forcing precise fixation on tiny controls."),
                        new LayeredSearchResultEntryData("Large central canvas for low-strain reading", "docs.projectgaze.local/reading-surface", "Keep the currently active content wide and tall enough to hold reading attention without needing constant depth refocusing.")
                    },
                    "Layout cues",
                    new[]
                    {
                        "Central surface for reading",
                        "Peripheral surfaces for scouting",
                        "Depth-separated candidates",
                        "Low-precision selection regions"
                    },
                    new[]
                    {
                        "gaze layered browsing layout",
                        "central page peripheral page design",
                        "low precision gaze matching"
                    }),
                ["Page_H"] = new LayeredSearchPageContentData(
                    "Open Search",
                    new Color(0.53f, 0.32f, 0.68f),
                    "attention-preserving spatial ui transitions",
                    "About 6,430 results (0.16 seconds)",
                    new[] { "All", "Animation", "Transitions", "Attention", "XR" },
                    new[]
                    {
                        new LayeredSearchResultEntryData("Stable attention anchors during UI replacement", "research.projectgaze.local/attention-anchor", "Keep a persistent central anchor while secondary surfaces exchange roles around it to reduce disorientation."),
                        new LayeredSearchResultEntryData("Minimal-motion slot swapping for readable content", "design.projectgaze.local/minimal-slot-swap", "Swapping page slots can feel lighter than moving every page through space, especially when the large reading surface stays aligned with the screen."),
                        new LayeredSearchResultEntryData("Confirm-before-promote interaction in spatial systems", "notes.projectgaze.local/confirm-before-promote", "Promotion into the main slot should require confirmation so preview remains exploratory and low-risk.")
                    },
                    "Transition policy",
                    new[]
                    {
                        "Stable central anchor",
                        "Promotion only after confirm",
                        "Small preview, large reading slot",
                        "Low disorientation swaps"
                    },
                    new[]
                    {
                        "attention anchor spatial ui",
                        "minimal slot swap transition",
                        "confirm before promote gaze ui"
                    })
            };
        }
    }
}
