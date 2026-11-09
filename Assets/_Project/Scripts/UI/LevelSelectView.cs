using System;
using KrakenCache.Data;
using UnityEngine;
using UnityEngine.UI;

namespace KrakenCache.UI
{
    internal static class LevelSelectView
    {
        private static readonly Vector2 Center = new Vector2(.5f, .5f);
        private static readonly Color Gold = new Color32(255, 214, 147, 255);

        public static GameObject Create(Transform parent, GameAssets assets, Action<int> select, Action close)
        {
            var overlay = MakeRect(parent, "LevelPicker", Center, Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            overlay.gameObject.AddComponent<Image>().color = new Color32(7, 12, 23, 215);

            var frame = MakeRect(overlay, "Frame", Center, Vector2.zero, new Vector2(810, 620));
            AddBackground(frame, new Color32(177, 115, 55, 255));
            var panel = MakeRect(frame, "Panel", Center, Vector2.zero, new Vector2(798, 608));
            AddBackground(panel, new Color32(15, 28, 46, 255));

            var title = MakeRect(panel, "Title", Center, new Vector2(0, 220), new Vector2(728, 80));
            AddLabel(title, "Choose a level", 58);

            const int columns = 5;
            for (int i = 0; i < GameProgress.MaxLevel; i++)
            {
                int level = i + 1;
                var position = new Vector2((i % columns - 2) * 138, 76 - (i / columns) * 130);
                MakeButton(panel, $"Level{level:00}", level.ToString("00"), Center,
                    position, new Vector2(122, 104), 42, assets, () => select(level));
            }

            MakeButton(panel, "Back", "Back", Center, new Vector2(0, -205), new Vector2(250, 83), 36, assets, close);
            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        public static Button MakeButton(Transform parent, string name, string label, Vector2 anchor,
            Vector2 position, Vector2 size, int fontSize, GameAssets assets, Action click)
        {
            var rect = MakeRect(parent, name, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color32(35, 59, 82, 255);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => click());

            var text = MakeRect(rect, "Text", Center, Vector2.zero, Vector2.zero);
            text.anchorMin = Vector2.zero;
            text.anchorMax = Vector2.one;
            text.offsetMin = text.offsetMax = Vector2.zero;
            AddLabel(text, label, fontSize);
            rect.gameObject.AddComponent<MenuButtonVisual>().Configure(assets);
            return button;
        }

        private static RectTransform MakeRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void AddBackground(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void AddLabel(RectTransform rect, string label, int size)
        {
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Gold;
            text.text = label;
            text.raycastTarget = false;
        }
    }
}
