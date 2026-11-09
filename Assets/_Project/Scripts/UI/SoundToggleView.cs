using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using KrakenCache.Controllers;
using KrakenCache.Data;

namespace KrakenCache.UI
{
    [ExecuteAlways, RequireComponent(typeof(Image), typeof(Button))]
    public sealed class SoundToggleView : MonoBehaviour
    {
        [SerializeField] private SpeakerGraphic icon;
        public static SoundToggleView Prepare(Transform parent, Vector2 anchor, Vector2 position)
        {
            Transform found = parent.Find("SoundToggle");
            var rt = found != null ? found.GetComponent<RectTransform>() : new GameObject("SoundToggle",typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent,false); rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(88,88); rt.anchoredPosition = position;
            var view = rt.GetComponent<SoundToggleView>();
            if (view == null) view = rt.gameObject.AddComponent<SoundToggleView>();
            var visual = rt.GetComponent<MenuButtonVisual>();
            if (visual == null) visual = rt.gameObject.AddComponent<MenuButtonVisual>();
            visual.Configure(Resources.Load<GameAssets>("GameAssets"));
            if (view.icon == null)
            {
                var iconRt = new GameObject("Speaker",typeof(RectTransform)).GetComponent<RectTransform>();
                iconRt.SetParent(rt,false); iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = Vector2.one * 17; iconRt.offsetMax = Vector2.one * -17;
                view.icon = iconRt.gameObject.AddComponent<SpeakerGraphic>();
                view.icon.color = Color.white; view.icon.raycastTarget = false;
            }
            if (view.icon.GetComponent<CanvasRenderer>() == null) view.icon.gameObject.AddComponent<CanvasRenderer>();
            view.icon.transform.SetAsLastSibling(); view.Refresh(GameAudio.Muted);
            return view;
        }
        private void OnEnable() { GameAudio.OnMuteChanged += Refresh; Refresh(GameAudio.Muted); }
        private void OnDisable() { GameAudio.OnMuteChanged -= Refresh; transform.DOKill(); }
        private void Start()
        {
            if (Application.IsPlaying(gameObject)) GetComponent<Button>().onClick.AddListener(Toggle);
        }
        private void Refresh(bool muted) { if (icon != null) icon.SetMuted(muted); }
        private void Toggle()
        {
            GameAudio.SetMuted(!GameAudio.Muted);
            if (!GameAudio.Muted) GameAudio.Play(SoundCue.SoundToggle);
            transform.DOKill(); transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one*.08f,.16f,3,.3f).SetLink(gameObject);
        }
    }
}
