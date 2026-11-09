using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using KrakenCache.Data;
using KrakenCache.Controllers;

namespace KrakenCache.UI
{
    [ExecuteAlways]
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button levelButton;
        [SerializeField] private Text levelButtonText;

        [Header("Assets")]
        [SerializeField] private GameAssets assets;

        private void Awake()
        {
            ResolveAssets();
        }

        private void ResolveAssets()
        {
            if (assets == null)
            {
                assets = Resources.Load<GameAssets>("GameAssets");
            }
        }

        private void OnEnable()
        {
            ResolveAssets();
            GameProgress.OnLevelChanged += HandleLevelChanged;
            UpdateMenuVisuals();
        }

        private void OnDisable() => GameProgress.OnLevelChanged -= HandleLevelChanged;
        private void HandleLevelChanged(int level) => UpdateMenuVisuals();

        private void Start()
        {
            if (!Application.IsPlaying(gameObject)) return;
            GameAudio.Prepare();
            PrepareButtonVisual();
            UpdateMenuVisuals();

            if (levelButton != null)
            {
                levelButton.onClick.RemoveAllListeners();
                levelButton.onClick.AddListener(OnLevelClicked);
            }
        }

        public void PrepareButtonVisual()
        {
            ResolveAssets();
            SoundToggleView.Prepare(transform, Vector2.one, new Vector2(-80,-80));
            PrepareTitle();
            Transform oldFrame = levelButton != null ? levelButton.transform.Find("Frame") : null;
            if (oldFrame != null) oldFrame.gameObject.SetActive(false);
            if (levelButton != null && assets != null)
            {
                var rect = (RectTransform)levelButton.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .48f);
                rect.sizeDelta = new Vector2(410, 112);
                rect.anchoredPosition = Vector2.zero;
                var visual = levelButton.GetComponent<MenuButtonVisual>();
                if (visual == null) visual = levelButton.gameObject.AddComponent<MenuButtonVisual>();
                visual.Configure(assets);
            }
        }

        private void PrepareTitle()
        {
            var gold = new Color32(255, 214, 147, 255);
            MakeLabel("Eyebrow", "T H E   S H I P W R E C K   C O V E", .81f, 33, gold);
            MakeLabel("TitleTop", "KRAKEN", .742f, 122, new Color32(255, 237, 194, 255));
            MakeLabel("TitleBottom", "BLAST", .676f, 122, gold);
            MakeLabel("Tagline", "A pirate treasure puzzle", .603f, 39, new Color32(224, 230, 238, 255));
        }

        private void MakeLabel(string name, string value, float height, int size, Color color)
        {
            var rect = transform.Find(name) as RectTransform;
            if (rect == null)
            {
                rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(transform, false);
            }
            rect.anchorMin = new Vector2(.12f, height);
            rect.anchorMax = new Vector2(.88f, height);
            rect.sizeDelta = new Vector2(0, size * 1.3f);
            rect.anchoredPosition = Vector2.zero;

            var text = rect.GetComponent<Text>() ?? rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.color = color;
            text.text = value;
            var shadow = rect.GetComponent<Shadow>() ?? rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(8, 14, 27, 200);
            shadow.effectDistance = new Vector2(0, -4);
        }

        public void UpdateMenuVisuals()
        {
            if (backgroundImage != null && assets != null && assets.menuBackground != null)
            {
                backgroundImage.sprite = assets.menuBackground;
            }

            if (GameProgress.IsAllLevelsFinished)
            {
                if (levelButtonText != null) levelButtonText.text = "All loot secured";
                if (levelButton != null) levelButton.interactable = false;
            }
            else
            {
                if (levelButtonText != null) levelButtonText.text = $"Set sail  /  {GameProgress.LastPlayedLevel:00}";
                if (levelButton != null) levelButton.interactable = true;
            }
        }

        private void OnLevelClicked()
        {
            if (!GameProgress.IsAllLevelsFinished)
            {
                GameAudio.Play(SoundCue.UiTap);
                SceneManager.LoadScene("LevelScene");
            }
        }
    }
}
