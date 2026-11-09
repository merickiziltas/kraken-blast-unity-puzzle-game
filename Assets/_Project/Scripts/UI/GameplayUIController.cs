using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using KrakenCache.Controllers;
using KrakenCache.Core;
using KrakenCache.Data;

namespace KrakenCache.UI
{
    public sealed class GameplayUIController : MonoBehaviour
    {
        [SerializeField] private Text movesText;
        [SerializeField] private Text goalCountText;
        [SerializeField] private Image goalIcon;
        [SerializeField] private Image goalCheckmark;
        [SerializeField] private GameObject winCelebrationPanel;
        [SerializeField] private Transform winStarTransform;
        [SerializeField] private Button tapToContinueButton;
        [SerializeField] private GameObject failPopupPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button tryAgainButton;
        [SerializeField] private GameAssets assets;
        private BoardManager board;
        private Tween returnToMenu;
        private RectTransform hud, safeRoot, goalArea, flightLayer;
        private Canvas canvas;
        private Rect lastSafe;
        private Vector2 lastScreen;
        private float lastCanvasScale;
        private readonly Dictionary<ObstacleType, GoalSlot> slots = new Dictionary<ObstacleType, GoalSlot>();
        private static readonly Color Ink = new Color32(255, 236, 202, 255);
        private sealed class GoalSlot
        {
            public RectTransform Root;
            public Image Icon, Check;
            public Text Count;
            public int Remaining;
        }
        private void Awake()
        {
            GameAudio.Prepare();
            if (assets == null) assets = Resources.Load<GameAssets>("GameAssets");
            canvas = GetComponent<Canvas>();
            BuildHud();
            BuildFailPopup();
        }
        private void Start()
        {
            winCelebrationPanel?.SetActive(false);
            failPopupPanel?.SetActive(false);
            board = BoardManager.Instance;
            if (board != null)
            {
                board.OnLevelInitialized += ResetGoals;
                board.OnMovesChanged += HandleMovesChanged;
                board.OnGoalProgress += HandleGoalProgress;
                board.OnChalicesReleased += FlyChalices;
                board.OnLevelWon += ShowWinCelebration;
                board.OnLevelLost += ShowFailPopup;
                if (board.Grid != null)
                {
                    HandleMovesChanged(board.RemainingMoves);
                    foreach (ObstacleType type in System.Enum.GetValues(typeof(ObstacleType)))
                        HandleGoalProgress(type, board.GetRemainingGoal(type));
                }
            }
            tapToContinueButton?.onClick.AddListener(OnWinContinueClicked);
            closeButton?.onClick.AddListener(OnContinueToMainScene);
            tryAgainButton?.onClick.AddListener(OnTryAgainClicked);
        }
        private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }
        private Text Label(string name, Transform parent, string text, int size, Color color)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.one);
            var label = rt.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text; label.fontSize = size; label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter; label.color = color;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 20; label.resizeTextMaxSize = size;
            return label;
        }
        private void BuildHud()
        {
            // Keep serialized popup references; replace the inaccurate legacy HUD contents.
            Transform old = transform.Find("TopUI");
            if (old != null) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
            safeRoot = Rect("SafeAreaHUD", transform, Vector2.zero, Vector2.one);
            hud = Rect("TopUI", safeRoot, new Vector2(0, 1), Vector2.one);
            hud.pivot = new Vector2(0.5f, 1);
            var image = hud.gameObject.AddComponent<Image>();
            image.sprite = assets.topUi;
            image.raycastTarget = false;
            var goalLabel = Rect("GoalLabel", hud, new Vector2(0.03f, 0.68f), new Vector2(0.29f, 0.86f));
            Label("Text", goalLabel, "TARGETS", 37, Ink);
            var moveLabel = Rect("MoveLabel", hud, new Vector2(0.71f, 0.68f), new Vector2(0.97f, 0.86f));
            Label("Text", moveLabel, "MOVES", 37, Ink);
            var moveArea = Rect("Moves", hud, new Vector2(0.74f, 0.17f), new Vector2(0.95f, 0.58f));
            movesText = Label("MovesText", moveArea, "", 80, Ink);
            goalArea = Rect("Goals", hud, new Vector2(0.054f, 0.17f), new Vector2(0.26f, 0.58f));
            SoundToggleView.Prepare(hud, new Vector2(.5f,.77f), Vector2.zero);
            flightLayer = Rect("GoalFlights", transform, Vector2.zero, Vector2.one);
            if (winCelebrationPanel != null) winCelebrationPanel.transform.SetAsLastSibling();
            if (failPopupPanel != null) failPopupPanel.transform.SetAsLastSibling();
            ApplySafeArea();
        }
        private void BuildFailPopup()
        {
            if (failPopupPanel == null) return;
            Transform popup = failPopupPanel.transform.Find("PopupBase");
            if (popup == null) return;
            Transform ribbon = popup.Find("Ribbon");
            if (ribbon != null) Label("Title", ribbon, "Out of Moves", 44, Color.white);
            var message = Rect("Message", popup, new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.7f));
            Label("Text", message, "The treasure is still down there.", 36, Ink);
            if (tryAgainButton != null) tryAgainButton.gameObject.AddComponent<MenuButtonVisual>().Configure(assets);
        }
        private void LateUpdate()
        {
            if (lastSafe != Screen.safeArea || lastScreen != new Vector2(Screen.width, Screen.height)
                || !Mathf.Approximately(lastCanvasScale, canvas.scaleFactor))
                ApplySafeArea();
        }
        private void ApplySafeArea()
        {
            lastSafe = Screen.safeArea;
            lastScreen = new Vector2(Screen.width, Screen.height);
            lastCanvasScale = canvas.scaleFactor;
            safeRoot.anchorMin = new Vector2(lastSafe.xMin / Mathf.Max(1, Screen.width), lastSafe.yMin / Mathf.Max(1, Screen.height));
            safeRoot.anchorMax = new Vector2(lastSafe.xMax / Mathf.Max(1, Screen.width), lastSafe.yMax / Mathf.Max(1, Screen.height));
            float width = lastSafe.width / Mathf.Max(.01f, lastCanvasScale);
            hud.sizeDelta = new Vector2(0, width * assets.topUi.rect.height / assets.topUi.rect.width);
        }
        private void ResetGoals()
        {
            returnToMenu?.Kill();
            if (winStarTransform != null) winStarTransform.DOKill();
            winCelebrationPanel?.SetActive(false);
            failPopupPanel?.SetActive(false);
            foreach (var slot in slots.Values)
            {
                foreach (Transform target in slot.Root.GetComponentsInChildren<Transform>()) target.DOKill();
                slot.Root.gameObject.SetActive(false);
                Destroy(slot.Root.gameObject);
            }
            slots.Clear();
            foreach (Transform target in flightLayer)
            {
                target.DOKill();
                Destroy(target.gameObject);
            }
        }
        private void HandleMovesChanged(int moves)
        {
            movesText.text = moves.ToString();
            Pulse(movesText.transform);
        }
        private void Pulse(Transform target)
        {
            target.DOKill(); target.localScale = Vector3.one;
            target.DOPunchScale(Vector3.one * 0.12f, 0.18f, 4, 0.4f).SetLink(target.gameObject);
        }
        private void HandleGoalProgress(ObstacleType type, int remaining)
        {
            if (!slots.TryGetValue(type, out GoalSlot slot))
            {
                if (remaining <= 0) return;
                slot = new GoalSlot { Root = Rect(type.ToString(), goalArea, Vector2.zero, Vector2.one), Remaining = remaining };
                var iconRt = Rect("Icon", slot.Root, new Vector2(0.06f, 0.44f), new Vector2(0.94f, 0.91f));
                slot.Icon = iconRt.gameObject.AddComponent<Image>();
                slot.Icon.sprite = type == ObstacleType.Stone ? assets.stone : type == ObstacleType.Vase ? assets.vaseFull : assets.chalice;
                slot.Icon.preserveAspect = true; slot.Icon.raycastTarget = false;
                var countRt = Rect("Count", slot.Root, new Vector2(0, 0.06f), new Vector2(1, 0.43f));
                slot.Count = Label("Text", countRt, remaining.ToString(), 54, Ink);
                var checkRt = Rect("Check", slot.Root, new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.43f));
                slot.Check = checkRt.gameObject.AddComponent<Image>();
                slot.Check.sprite = assets.goalCheck; slot.Check.preserveAspect = true; slot.Check.raycastTarget = false;
                slots.Add(type, slot);
                int index = 0;
                foreach (ObstacleType kind in System.Enum.GetValues(typeof(ObstacleType)))
                    if (slots.TryGetValue(kind, out GoalSlot entry))
                    {
                        entry.Root.anchorMin = new Vector2((float)index / slots.Count, 0);
                        entry.Root.anchorMax = new Vector2((float)(index + 1) / slots.Count, 1);
                        index++;
                    }
            }
            else if (type == ObstacleType.ChaliceBox && remaining < slot.Remaining) return;
            slot.Remaining = remaining;
            Refresh(slot);
        }
        private void Refresh(GoalSlot slot)
        {
            slot.Count.text = slot.Remaining.ToString();
            slot.Count.gameObject.SetActive(slot.Remaining > 0);
            slot.Check.gameObject.SetActive(slot.Remaining == 0);
        }
        private void FlyChalices(Vector3 origin, int count)
        {
            if (!slots.TryGetValue(ObstacleType.ChaliceBox, out GoalSlot slot)) return;
            Canvas.ForceUpdateCanvases();
            Camera camera = Camera.main;
            Vector2 screen = camera.WorldToScreenPoint(origin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(flightLayer, screen, null, out Vector2 start);
            Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(null, slot.Icon.rectTransform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(flightLayer, endScreen, null, out Vector2 end);
            for (int i = 0; i < count; i++)
            {
                var rt = Rect("FlyingChalice", flightLayer, Vector2.one * 0.5f, Vector2.one * 0.5f);
                rt.sizeDelta = new Vector2(72, 88);
                rt.anchoredPosition = start;
                var image = rt.gameObject.AddComponent<Image>();
                image.sprite = assets.chalice; image.preserveAspect = true; image.raycastTarget = false;
                Vector2 control = (start + end) * 0.5f + new Vector2((i % 2 == 0 ? 1 : -1) * 160, 180);
                var sequence = DOTween.Sequence().SetTarget(rt).SetLink(rt.gameObject);
                System.IDisposable arrival = board?.GoalAnimations.Begin();
                sequence.AppendInterval(i * 0.025f);
                sequence.Append(DOTween.To(() => 0f, t => rt.anchoredPosition = (1-t)*(1-t)*start + 2*(1-t)*t*control + t*t*end, 1, 0.6f).SetEase(Ease.InOutSine));
                sequence.Join(rt.DOScale(0.55f, 0.6f));
                sequence.OnComplete(() =>
                {
                    try
                    {
                        slot.Remaining = Mathf.Max(0, slot.Remaining - 1);
                        Refresh(slot); Pulse(slot.Icon.transform);
                        GameAudio.Play(SoundCue.Cup);
                    }
                    finally
                    {
                        arrival?.Dispose();
                        rt.DOKill(); Destroy(rt.gameObject);
                    }
                });
                sequence.OnKill(() => arrival?.Dispose());
            }
        }
        private void ShowWinCelebration()
        {
            if (winCelebrationPanel == null) return;
            failPopupPanel?.SetActive(false);
            winCelebrationPanel.SetActive(true);
            GameAudio.Play(SoundCue.Win);
            returnToMenu = DOVirtual.DelayedCall(2f, OnContinueToMainScene).SetTarget(transform).SetLink(gameObject);
            for (int i = 0; i < 28; i++)
            {
                var rt = Rect("CelebrationSpark", winCelebrationPanel.transform, Vector2.one * 0.5f, Vector2.one * 0.5f);
                rt.sizeDelta = Vector2.one * UnityEngine.Random.Range(24f, 56f);
                var spark = rt.gameObject.AddComponent<Image>();
                spark.sprite = assets.particleStar; spark.material = assets.starMaterial;
                spark.color = new Color(1, 0.85f, 0.4f, 1); spark.raycastTarget = false;
                rt.anchoredPosition = new Vector2(0, 80);
                var sequence = DOTween.Sequence().SetTarget(rt).SetLink(rt.gameObject);
                sequence.Append(rt.DOAnchorPos(new Vector2(UnityEngine.Random.Range(-470f, 470f), UnityEngine.Random.Range(-600f, 650f)), 1.3f).SetEase(Ease.OutCubic));
                sequence.Join(rt.DOScale(0, 1.3f).SetEase(Ease.InQuad));
                sequence.Join(rt.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-240f, 240f)), 1.3f));
                sequence.OnComplete(() => { rt.DOKill(); Destroy(rt.gameObject); });
            }
            if (winStarTransform == null) return;
            winStarTransform.DOKill(); winStarTransform.localScale = Vector3.zero;
            winStarTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetLink(winStarTransform.gameObject);
            winStarTransform.DORotate(new Vector3(0, 0, 360), 3, RotateMode.FastBeyond360).SetLoops(-1).SetLink(winStarTransform.gameObject);
        }
        private void ShowFailPopup()
        {
            if (failPopupPanel == null) return;
            returnToMenu?.Kill();
            if (winStarTransform != null) winStarTransform.DOKill();
            winCelebrationPanel?.SetActive(false);
            failPopupPanel.SetActive(true);
            GameAudio.Play(SoundCue.Lose);
            Transform popup = failPopupPanel.transform.Find("PopupBase");
            if (popup == null) return;
            popup.DOKill(); popup.localScale = Vector3.one * 0.8f;
            popup.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetLink(popup.gameObject);
        }
        private void OnWinContinueClicked()
        {
            GameAudio.Play(SoundCue.Continue);
            SceneManager.LoadScene("MainScene");
        }
        private void OnContinueToMainScene()
        {
            GameAudio.Play(SoundCue.UiTap);
            SceneManager.LoadScene("MainScene");
        }
        private void OnTryAgainClicked()
        {
            GameAudio.Play(SoundCue.UiTap);
            SceneManager.LoadScene("LevelScene");
        }
        private void OnDestroy()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true)) child.DOKill();
            if (board == null) return;
            board.OnLevelInitialized -= ResetGoals;
            board.OnMovesChanged -= HandleMovesChanged;
            board.OnGoalProgress -= HandleGoalProgress;
            board.OnChalicesReleased -= FlyChalices;
            board.OnLevelWon -= ShowWinCelebration;
            board.OnLevelLost -= ShowFailPopup;
        }
    }
}
