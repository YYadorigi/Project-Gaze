using ProjectGaze.Gaze.Depth;
using ProjectGaze.Hardware.ThinkVision;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    internal sealed class LayeredPagesStatusOverlay
    {
        private GUIStyle titleStyle;
        private GUIStyle wrappedBodyStyle;
        private GUIStyle panelStyle;
        private Texture2D panelTexture;

        public void Draw(
            IThinkVisionDisplayBridge displayBridge,
            GazeInteractionController gazeController,
            string currentMainPageId,
            bool usesStereoGazeInput,
            string title = "Layered Pages Status",
            string customInstructions = null,
            string taskFeedbackText = null)
        {
            EnsureStyles();

            const float leftMargin = 20f;
            const float topMargin = 18f;
            const float lineSpacing = 6f;
            const float panelWidth = 420f;
            const float innerWidth = panelWidth - 32f;

            var sourceText = gazeController != null
                ? $"Input Source: {gazeController.ActiveGazeProviderName}    Confirm: {gazeController.ActiveBlinkProviderName}"
                : "Input Source: Initializing    Confirm: Initializing";

            var instructions = !string.IsNullOrWhiteSpace(customInstructions)
                ? customInstructions
                : usesStereoGazeInput
                ? "Stereo mode: the large main-view page stays lit. Gaze at another page's matching region to preview it, then blink to swap it into the main-view slot."
                : "Mouse fallback: point at a page to preview it, use the scroll wheel to cycle overlapping depths, then left click to swap it into the main-view slot.";
            var stateText = gazeController != null
                ? $"State: {gazeController.CurrentSnapshot.Mode}    Main View: {currentMainPageId}    Preview: {gazeController.CurrentSnapshot.PreviewPageId ?? "-"}    Confirmed: {gazeController.CurrentSnapshot.ConfirmedPageId ?? "-"}"
                : $"State: Initializing    Main View: {currentMainPageId}    Preview: -    Confirmed: -";
            var depthText = gazeController != null
                ? BuildDepthText(gazeController)
                : "Depth Mode: Initializing";
            var geometryText = BuildGeometryErrorBudgetText();
            var taskText = string.IsNullOrWhiteSpace(taskFeedbackText)
                ? null
                : "Task Feedback:\n" + taskFeedbackText;

            var bridgeText = displayBridge != null ? displayBridge.StatusText : "Display bridge: Initializing";
            var sourceHeight = wrappedBodyStyle.CalcHeight(new GUIContent(sourceText), innerWidth);
            var instructionsHeight = wrappedBodyStyle.CalcHeight(new GUIContent(instructions), innerWidth);
            var stateHeight = wrappedBodyStyle.CalcHeight(new GUIContent(stateText), innerWidth);
            var depthHeight = wrappedBodyStyle.CalcHeight(new GUIContent(depthText), innerWidth);
            var geometryHeight = wrappedBodyStyle.CalcHeight(new GUIContent(geometryText), innerWidth);
            var taskHeight = string.IsNullOrWhiteSpace(taskText) ? 0f : wrappedBodyStyle.CalcHeight(new GUIContent(taskText), innerWidth);
            var bridgeHeight = wrappedBodyStyle.CalcHeight(new GUIContent(bridgeText), innerWidth);

            var panelHeight = 36f + sourceHeight + instructionsHeight + stateHeight + depthHeight + geometryHeight + taskHeight + bridgeHeight + (lineSpacing * 9f);
            GUI.Box(new Rect(leftMargin, topMargin, panelWidth, panelHeight), GUIContent.none, panelStyle);

            var cursorY = topMargin + 14f;
            var contentX = leftMargin + 16f;

            GUI.Label(new Rect(contentX, cursorY, innerWidth, 30f), title, titleStyle);
            cursorY += 32f;

            GUI.Label(new Rect(contentX, cursorY, innerWidth, sourceHeight), sourceText, wrappedBodyStyle);
            cursorY += sourceHeight + lineSpacing;

            GUI.Label(new Rect(contentX, cursorY, innerWidth, instructionsHeight), instructions, wrappedBodyStyle);
            cursorY += instructionsHeight + lineSpacing;

            GUI.Label(new Rect(contentX, cursorY, innerWidth, stateHeight), stateText, wrappedBodyStyle);
            cursorY += stateHeight + lineSpacing;

            GUI.Label(new Rect(contentX, cursorY, innerWidth, depthHeight), depthText, wrappedBodyStyle);
            cursorY += depthHeight + lineSpacing;

            GUI.Label(new Rect(contentX, cursorY, innerWidth, geometryHeight), geometryText, wrappedBodyStyle);
            cursorY += geometryHeight + lineSpacing;

            if (!string.IsNullOrWhiteSpace(taskText))
            {
                GUI.Label(new Rect(contentX, cursorY, innerWidth, taskHeight), taskText, wrappedBodyStyle);
                cursorY += taskHeight + lineSpacing;
            }

            GUI.Label(new Rect(contentX, cursorY, innerWidth, bridgeHeight), bridgeText, wrappedBodyStyle);
        }

        public void Dispose()
        {
            if (panelTexture == null)
            {
                return;
            }

            UnityObjectLifecycleUtility.DestroyObject(panelTexture);
        }

        private void EnsureStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold
                };
                titleStyle.normal.textColor = Color.white;
            }

            if (wrappedBodyStyle == null)
            {
                wrappedBodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    wordWrap = true
                };
                wrappedBodyStyle.normal.textColor = new Color(0.92f, 0.95f, 1f);
            }

            if (panelTexture == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelTexture.SetPixel(0, 0, new Color(0.05f, 0.08f, 0.12f, 0.82f));
                panelTexture.Apply();
            }

            if (panelStyle == null)
            {
                panelStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(16, 16, 14, 14)
                };
                panelStyle.normal.background = panelTexture;
            }
        }

        private static string BuildDepthText(GazeInteractionController gazeController)
        {
            var depthMode = gazeController.DepthMatchingMode;
            string predictedDistance = "-";
            if (gazeController.HasLastTrackingSample && gazeController.LastTrackingSample.PredictedRayDistance > 0f)
            {
                predictedDistance = gazeController.LastTrackingSample.PredictedRayDistance.ToString("0.00");
            }

            var lastHit = gazeController.LastHitResult;
            string layerText = string.IsNullOrWhiteSpace(lastHit.DepthLayerId)
                ? "-"
                : $"{lastHit.DepthLayerId} ({lastHit.DepthLayerRayDistance:0.0})";
            string hitText = lastHit.HasHitPage ? lastHit.PageId : "-";

            return $"Depth Mode: {depthMode}    Predicted Distance: {predictedDistance}    Layer: {layerText}    Hit: {hitText}";
        }

        private static string BuildGeometryErrorBudgetText()
        {
            const float baselineMeters = VergenceDepthEstimator.PopulationMeanIpdMillimeters / 1000f;
            const float angularErrorDegrees = VergenceDepthEstimator.A8MeanAccuracyDegrees;

            return "Vergence Error Budget: " +
                   BuildIntervalText("400mm", 0.4f, baselineMeters, angularErrorDegrees) + "    " +
                   BuildIntervalText("800mm", 0.8f, baselineMeters, angularErrorDegrees) + "    " +
                   BuildIntervalText("1200mm", 1.2f, baselineMeters, angularErrorDegrees);
        }

        private static string BuildIntervalText(string label, float depthMeters, float baselineMeters, float angularErrorDegrees)
        {
            if (!VergenceDepthEstimator.TryBuildErrorInterval(
                    depthMeters,
                    baselineMeters,
                    angularErrorDegrees,
                    out var interval))
            {
                return $"{label} [-, -]";
            }

            return $"{label} [{interval.MinimumDepth * 1000f:0}, {interval.MaximumDepth * 1000f:0}]";
        }
    }
}
