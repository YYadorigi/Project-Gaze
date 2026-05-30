using UnityEngine;
using UnityEngine.UI;

namespace ProjectGaze.Gaze
{
    internal static class DepthGatedAgentUiUtility
    {
        public static Canvas CreateWorldCanvas(
            string name,
            Transform parent,
            Camera worldCamera,
            float width,
            float height,
            Vector3 localPosition,
            Vector3 localScale)
        {
            var canvasObject = new GameObject(name);
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = localPosition;
            canvasObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasObject.transform.localScale = localScale;

            var rectTransform = canvasObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(width, height);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = worldCamera;
            canvas.sortingOrder = 20;
            return canvas;
        }

        public static Image CreatePanel(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            Color color)
        {
            var rectTransform = CreateTopLeftRect(name, parent, x, y, width, height);
            var image = rectTransform.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            Color color,
            FontStyle fontStyle,
            TextAnchor anchor,
            float x,
            float y,
            float width,
            float height)
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

        public static void RegisterCanvasGraphics(Transform canvasTransform, SpatialPageVisualController visualController)
        {
            foreach (var graphic in canvasTransform.GetComponentsInChildren<Graphic>(true))
            {
                visualController.RegisterGraphic(graphic, graphic.color);
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
    }
}
