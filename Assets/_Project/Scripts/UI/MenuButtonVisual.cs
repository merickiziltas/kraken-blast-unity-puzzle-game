using UnityEngine;
using UnityEngine.UI;
using KrakenCache.Data;

namespace KrakenCache.UI
{
    /// <summary>Build a brass-trimmed button from mirrored left caps and a stretched center strip.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image), typeof(Button))]
    public sealed class MenuButtonVisual : MonoBehaviour
    {
        [SerializeField] private GameAssets assets;
        [SerializeField] private RectTransform outer, inner;
        public void Configure(GameAssets gameAssets)
        {
            assets = gameAssets;
            if (assets == null || assets.menuButtonFrame == null || assets.menuButton == null) return;
            GetComponent<Image>().sprite = null;
            GetComponent<Image>().color = Color.clear;
            GetComponent<Button>().transition = Selectable.Transition.None;
            outer = Layer("ButtonFrame", assets.menuButtonFrame, 0);
            inner = Layer("ButtonFill", assets.menuButton, 6);
            Transform text = transform.Find("Text");
            if (text != null) text.SetAsLastSibling();
            Layout();
        }
        private RectTransform Layer(string name, Sprite sprite, float inset)
        {
            var root = ChildRect(transform, name);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.one * inset; root.offsetMax = Vector2.one * -inset;
            var centerRect = ChildRect(root, "Center");
            var center = centerRect.GetComponent<RawImage>();
            if (center == null) center = centerRect.gameObject.AddComponent<RawImage>();
            center.texture = sprite.texture;
            Rect rect = sprite.textureRect;
            center.uvRect = new Rect((rect.xMax - 1) / sprite.texture.width, rect.yMin / sprite.texture.height,
                1f / sprite.texture.width, rect.height / sprite.texture.height);
            center.raycastTarget = false;
            for (int i = 0; i < 2; i++)
            {
                var capRect = ChildRect(root, i == 0 ? "Left" : "Right");
                var cap = capRect.GetComponent<Image>();
                if (cap == null) cap = capRect.gameObject.AddComponent<Image>();
                cap.sprite = sprite; cap.raycastTarget = false;
                cap.rectTransform.anchorMin = new Vector2(i, 0);
                cap.rectTransform.anchorMax = new Vector2(i, 1);
                if (i == 1) cap.transform.localScale = new Vector3(-1, 1, 1);
            }
            return root;
        }
        private static RectTransform ChildRect(Transform parent, string name)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null) return existing;
            var child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            child.SetParent(parent, false);
            return child;
        }
        private void OnEnable() => Layout();
        private void OnRectTransformDimensionsChange() { if (outer != null) Layout(); }
        private void Layout()
        {
            if (assets == null) return;
            Fit(outer, assets.menuButtonFrame);
            Fit(inner, assets.menuButton);
        }
        private void Fit(RectTransform root, Sprite sprite)
        {
            if (root == null || sprite == null) return;
            float capWidth = Mathf.Min(root.rect.width * 0.5f, root.rect.height * sprite.rect.width / sprite.rect.height);
            var center = root.Find("Center").GetComponent<RectTransform>();
            center.anchorMin = Vector2.zero; center.anchorMax = Vector2.one;
            center.offsetMin = new Vector2(capWidth - 0.5f, 0); center.offsetMax = new Vector2(-capWidth + 0.5f, 0);
            var left = root.Find("Left").GetComponent<RectTransform>();
            var right = root.Find("Right").GetComponent<RectTransform>();
            left.sizeDelta = right.sizeDelta = new Vector2(capWidth, 0);
            left.anchoredPosition = new Vector2(capWidth * 0.5f, 0);
            right.anchoredPosition = new Vector2(-capWidth * 0.5f, 0);
        }
    }
}
