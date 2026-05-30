using UnityEngine;
using UnityEngine.UI;

namespace ProjectGaze.Gaze
{
    internal sealed class LayeredSearchPageUiBuilder
    {
        private readonly Camera mainCamera;

        public LayeredSearchPageUiBuilder(Camera mainCamera)
        {
            this.mainCamera = mainCamera;
        }

        public CanvasGroup Build(
            Transform pageRoot,
            LayeredSearchPageContentData contentData,
            SpatialPageVisualController visualController)
        {
            const float canvasWidth = 1000f;
            const float canvasHeight = 620f;

            var canvasObject = new GameObject("SearchCanvas");
            canvasObject.transform.SetParent(pageRoot, false);
            canvasObject.transform.localPosition = new Vector3(
                0f,
                0f,
                -LayeredPagesSceneDefaults.SharedPageBodyScale.z * 0.5f - LayeredPagesSceneDefaults.PageCanvasSurfaceOffset);
            canvasObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasObject.transform.localScale = new Vector3(
                -(LayeredPagesSceneDefaults.SharedPageBodyScale.x * 0.92f / canvasWidth),
                LayeredPagesSceneDefaults.SharedPageBodyScale.y * 0.88f / canvasHeight,
                1f);

            var rectTransform = canvasObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(canvasWidth, canvasHeight);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;
            canvas.sortingOrder = 10;

            var canvasGroup = canvasObject.AddComponent<CanvasGroup>();

            CreateUiPanel("HeaderBand", canvasObject.transform, 22f, 18f, 956f, 88f, new Color(0.93f, 0.95f, 0.99f, 0.92f));
            CreateUiPanel("SearchBar", canvasObject.transform, 170f, 34f, 618f, 42f, new Color(0.98f, 0.98f, 0.99f, 0.96f));
            CreateUiPanel("SearchAction", canvasObject.transform, 798f, 34f, 48f, 42f, contentData.AccentColor);
            CreateUiPanel("KnowledgeCard", canvasObject.transform, 714f, 118f, 228f, 292f, new Color(0.96f, 0.97f, 0.99f, 0.90f));
            CreateUiPanel("RelatedCard", canvasObject.transform, 714f, 426f, 228f, 134f, new Color(0.98f, 0.98f, 0.99f, 0.88f));

            CreateUiText("EngineLabel", canvasObject.transform, contentData.EngineName, 30, contentData.AccentColor, FontStyle.Bold, TextAnchor.UpperLeft, 38f, 34f, 120f, 30f);
            CreateUiText("QueryText", canvasObject.transform, contentData.Query, 19, new Color(0.08f, 0.1f, 0.16f), FontStyle.Normal, TextAnchor.MiddleLeft, 190f, 41f, 560f, 24f);
            CreateUiText("SearchActionText", canvasObject.transform, "Go", 16, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, 798f, 42f, 48f, 20f);
            CreateUiText("SummaryText", canvasObject.transform, contentData.ResultSummary, 12, new Color(0.36f, 0.39f, 0.46f), FontStyle.Normal, TextAnchor.UpperLeft, 38f, 118f, 620f, 18f);

            BuildHeaderTabs(canvasObject.transform, contentData);
            BuildResultBlocks(canvasObject.transform, contentData);
            BuildKnowledgeCard(canvasObject.transform, contentData);
            BuildRelatedQueries(canvasObject.transform, contentData);

            foreach (var image in canvasObject.GetComponentsInChildren<Image>(true))
            {
                visualController.RegisterGraphic(image, image.color);
            }

            return canvasGroup;
        }

        private void BuildHeaderTabs(Transform parent, LayeredSearchPageContentData contentData)
        {
            var tabX = 38f;
            for (var index = 0; index < contentData.Tabs.Length; index += 1)
            {
                var tabColor = index == 0
                    ? new Color(contentData.AccentColor.r, contentData.AccentColor.g, contentData.AccentColor.b, 0.82f)
                    : new Color(0.97f, 0.97f, 0.98f, 0.78f);
                var labelColor = index == 0 ? contentData.AccentColor : new Color(0.28f, 0.31f, 0.37f);
                CreateUiPanel("Tab_" + index, parent, tabX, 144f, 94f, 28f, tabColor);
                CreateUiText("TabLabel_" + index, parent, contentData.Tabs[index], 13, labelColor, FontStyle.Bold, TextAnchor.MiddleCenter, tabX, 149f, 94f, 18f);
                tabX += 102f;
            }

            CreateUiRule("HeaderRule", parent, 38f, 178f, 620f, new Color(0f, 0f, 0f, 0.14f));
        }

        private void BuildResultBlocks(Transform parent, LayeredSearchPageContentData contentData)
        {
            const float resultX = 30f;
            const float resultWidth = 644f;
            const float resultHeight = 102f;

            for (var index = 0; index < contentData.Results.Length; index += 1)
            {
                var top = 194f + index * 110f;
                CreateUiPanel("ResultCard_" + index, parent, resultX, top, resultWidth, resultHeight, new Color(0.98f, 0.98f, 0.99f, 0.90f));
                CreateUiPanel("ResultAccent_" + index, parent, resultX, top, 6f, resultHeight, contentData.AccentColor);
                CreateUiText("ResultUrl_" + index, parent, contentData.Results[index].DisplayUrl, 12, new Color(0.2f, 0.43f, 0.28f), FontStyle.Normal, TextAnchor.UpperLeft, 48f, top + 10f, 560f, 16f);
                CreateUiText("ResultTitle_" + index, parent, contentData.Results[index].Title, 18, new Color(0.18f, 0.33f, 0.62f), FontStyle.Bold, TextAnchor.UpperLeft, 48f, top + 30f, 560f, 24f);
                CreateUiText("ResultSnippet_" + index, parent, contentData.Results[index].Snippet, 13, new Color(0.18f, 0.2f, 0.25f), FontStyle.Normal, TextAnchor.UpperLeft, 48f, top + 58f, 560f, 34f);
            }
        }

        private void BuildKnowledgeCard(Transform parent, LayeredSearchPageContentData contentData)
        {
            CreateUiText("KnowledgeTitle", parent, contentData.KnowledgeTitle, 16, new Color(0.08f, 0.1f, 0.16f), FontStyle.Bold, TextAnchor.UpperLeft, 732f, 136f, 190f, 22f);
            CreateUiRule("KnowledgeRule", parent, 732f, 166f, 190f, new Color(0f, 0f, 0f, 0.14f));
            CreateUiText("KnowledgeBody", parent, string.Join("\n", contentData.KnowledgeLines), 13, new Color(0.18f, 0.2f, 0.25f), FontStyle.Normal, TextAnchor.UpperLeft, 732f, 180f, 190f, 186f);

            CreateUiPanel("KnowledgeBadge", parent, 732f, 372f, 112f, 26f, new Color(contentData.AccentColor.r, contentData.AccentColor.g, contentData.AccentColor.b, 0.74f));
            CreateUiText("KnowledgeBadgeLabel", parent, "Search Snapshot", 12, contentData.AccentColor, FontStyle.Bold, TextAnchor.MiddleCenter, 732f, 376f, 112f, 18f);
        }

        private void BuildRelatedQueries(Transform parent, LayeredSearchPageContentData contentData)
        {
            CreateUiText("RelatedTitle", parent, "Related searches", 15, new Color(0.08f, 0.1f, 0.16f), FontStyle.Bold, TextAnchor.UpperLeft, 732f, 444f, 180f, 20f);

            for (var index = 0; index < contentData.RelatedQueries.Length; index += 1)
            {
                CreateUiPanel("RelatedChip_" + index, parent, 732f, 474f + index * 26f, 190f, 22f, new Color(0.98f, 0.98f, 0.99f, 0.88f));
                CreateUiText("RelatedChipLabel_" + index, parent, contentData.RelatedQueries[index], 12, new Color(0.15f, 0.29f, 0.58f), FontStyle.Normal, TextAnchor.MiddleLeft, 742f, 476f + index * 26f, 170f, 18f);
            }
        }

        private static RectTransform CreateTopLeftRect(string name, Transform parent, float x, float y, float width, float height)
        {
            var element = new GameObject(name, typeof(RectTransform));
            element.transform.SetParent(parent, false);

            var rectTransform = element.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(x, -y);
            rectTransform.sizeDelta = new Vector2(width, height);
            return rectTransform;
        }

        private static Image CreateUiPanel(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            var rectTransform = CreateTopLeftRect(name, parent, x, y, width, height);
            var image = rectTransform.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void CreateUiRule(string name, Transform parent, float x, float y, float width, Color color)
        {
            CreateUiPanel(name, parent, x, y, width, 3f, color);
        }

        private Text CreateUiText(string name, Transform parent, string value, int fontSize, Color color, FontStyle fontStyle, TextAnchor anchor, float x, float y, float width, float height)
        {
            var rectTransform = CreateTopLeftRect(name, parent, x, y, width, height);
            var text = rectTransform.gameObject.AddComponent<Text>();
            text.font = LayeredPagesUiFontUtility.GetDefaultFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.text = value;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, fontSize - 4);
            text.resizeTextMaxSize = fontSize;
            text.lineSpacing = 1.08f;
            text.supportRichText = false;
            return text;
        }

    }
}
