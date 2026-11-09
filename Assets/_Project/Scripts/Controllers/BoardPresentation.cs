using UnityEngine;
using KrakenCache.Data;

namespace KrakenCache.Controllers
{
    /// <summary>Fits the board below the HUD in the camera's safe viewport.</summary>
    public sealed class BoardPresentation : MonoBehaviour
    {
        private Camera boardCamera;
        private Transform root;
        private SpriteRenderer backdrop;
        private Vector2 boardSize;
        private Rect lastSafeArea;
        private int lastWidth, lastHeight;
        private float hudAspect;

        public void Setup(Camera camera, Transform boardRoot, GameAssets assets, Vector2 size)
        {
            boardCamera = camera;
            root = boardRoot;
            boardSize = size;
            hudAspect = assets.topUi.rect.height / assets.topUi.rect.width;
            var background = new GameObject("Atmosphere");
            background.transform.SetParent(transform, false);
            backdrop = background.AddComponent<SpriteRenderer>();
            backdrop.sprite = assets.menuBackground;
            backdrop.color = new Color(0.65f, 0.69f, 0.78f, 1);
            backdrop.sortingOrder = -100;
            Reframe();
        }

        private void LateUpdate()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafeArea)
                Reframe();
        }

        private void Reframe()
        {
            if (boardCamera == null || root == null) return;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
            float aspect = boardCamera.aspect;
            Rect safe = Screen.safeArea;
            float bottom = safe.yMin / Mathf.Max(1, Screen.height) + 0.12f;
            // HUD scales with the safe-area width, preserving the source image aspect.
            float top = safe.yMax / Mathf.Max(1, Screen.height)
                - hudAspect * safe.width / Mathf.Max(1, Screen.height) - 0.02f;
            float availableHeight = Mathf.Max(0.2f, top - bottom);
            float availableWidth = Mathf.Max(0.2f, safe.width / Mathf.Max(1, Screen.width) - 0.07f);
            boardCamera.orthographic = true;
            boardCamera.orthographicSize = Mathf.Max(boardSize.y / (2 * availableHeight),
                boardSize.x / (2 * aspect * availableWidth));
            boardCamera.transform.position = new Vector3(0, 0, -10);
            float worldHeight = 2 * boardCamera.orthographicSize;
            root.position = new Vector3((safe.center.x / Mathf.Max(1, Screen.width) - 0.5f) * worldHeight * aspect,
                ((top + bottom) * 0.5f - 0.5f) * worldHeight, 0);
            if (backdrop != null && backdrop.sprite != null)
            {
                backdrop.transform.position = Vector3.zero;
                Vector2 spriteSize = backdrop.sprite.bounds.size;
                float cover = Mathf.Max(worldHeight * aspect / spriteSize.x, worldHeight / spriteSize.y);
                backdrop.transform.localScale = Vector3.one * cover;
            }
        }
    }
}
