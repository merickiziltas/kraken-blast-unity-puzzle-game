using System;
using UnityEngine;
using DG.Tweening;
using KrakenCache.Core;
using KrakenCache.Data;
using KrakenCache.Items;

namespace KrakenCache.Controllers
{
    public sealed class ItemView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        public Item Item { get; private set; }
        private GameAssets assets;
        private Vector3 restingScale;
        private bool retiring;
        private SpriteRenderer doors;
        private readonly System.Collections.Generic.List<SpriteRenderer> chalices = new System.Collections.Generic.List<SpriteRenderer>();
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        public void Setup(Item item, GameAssets gameAssets)
        {
            Item = item;
            assets = gameAssets;
            item.ViewObject = gameObject;
            if (item is CubeItem cube) cube.OnHintChanged += HandleHintChanged;
            if (item is ObstacleItem obstacle) obstacle.OnDamageReceived += HandleObstacleDamage;
            if (item is ChaliceBoxItem box)
            {
                box.OnDoorBroken += HandleDoorBroken;
                box.OnChaliceCollected += HandleChaliceCollected;
                var doorObject = new GameObject("Doors");
                doorObject.transform.SetParent(transform, false);
                doors = doorObject.AddComponent<SpriteRenderer>();
                doors.sprite = assets.chaliceBoxDoors;
                for (int i = 0; i < ChaliceBoxItem.TargetChalices; i++)
                {
                    var cup = new GameObject("ChestCoin");
                    cup.transform.SetParent(transform, false);
                    cup.transform.localPosition = new Vector3((i % 5 - 2) * 0.44f, i < 5 ? 0.20f : -0.55f, 0);
                    var cupRenderer = cup.AddComponent<SpriteRenderer>();
                    cupRenderer.sprite = assets.chalice;
                    if (assets.chalice != null) cup.transform.localScale = Vector3.one * 0.45f / assets.chalice.bounds.size.y;
                    chalices.Add(cupRenderer);
                }
            }
            UpdateVisual();
            float size = BoardManager.Instance != null ? BoardManager.Instance.CellSize : 1.44f;
            float footprint = item is ChaliceBoxItem ? size * 2 + 0.02f : size;
            if (spriteRenderer.sprite != null)
            {
                float scale = footprint / Mathf.Max(spriteRenderer.sprite.bounds.size.x, spriteRenderer.sprite.bounds.size.y);
                restingScale = item is CubeItem
                    ? new Vector3(footprint / spriteRenderer.sprite.bounds.size.x, footprint / spriteRenderer.sprite.bounds.size.y, 1)
                    : Vector3.one * scale;
            }
            else restingScale = Vector3.one;
            transform.localScale = restingScale;
            UpdateSorting();
        }
        private void UpdateSorting()
        {
            spriteRenderer.sortingOrder = Item?.CurrentCell != null ? (30 - Item.CurrentCell.Row) * 2 : 20;
            if (doors != null) doors.sortingOrder = spriteRenderer.sortingOrder + 2;
            foreach (var cup in chalices) cup.sortingOrder = spriteRenderer.sortingOrder + 1;
        }
        private void OnDestroy()
        {
            transform.DOKill();
            if (spriteRenderer != null) spriteRenderer.DOKill();
            if (Item is CubeItem cube) cube.OnHintChanged -= HandleHintChanged;
            if (Item is ObstacleItem obstacle) obstacle.OnDamageReceived -= HandleObstacleDamage;
            if (Item is ChaliceBoxItem box)
            {
                box.OnDoorBroken -= HandleDoorBroken;
                box.OnChaliceCollected -= HandleChaliceCollected;
            }
            if (Item != null && Item.ViewObject == gameObject) Item.ViewObject = null;
        }
        public void UpdateVisual()
        {
            if (Item == null || assets == null) return;
            if (Item is CubeItem cube) spriteRenderer.sprite = assets.GetCubeSprite(cube.Color, cube.CurrentHint);
            else if (Item is RocketItem rocket) spriteRenderer.sprite = rocket.IsVertical ? assets.verticalRocket : assets.horizontalRocket;
            else if (Item is TntItem) spriteRenderer.sprite = assets.tnt;
            else if (Item is StoneItem) spriteRenderer.sprite = assets.stone;
            else if (Item is VaseItem vase) spriteRenderer.sprite = vase.CurrentHealth == 2 ? assets.vaseFull : assets.vaseDamaged;
            else if (Item is ChaliceBoxItem box)
            {
                spriteRenderer.sprite = assets.chaliceBoxBg;
                if (doors != null) doors.enabled = box.IsDoorPhase;
                for (int i = 0; i < chalices.Count; i++) chalices[i].enabled = !box.IsDoorPhase && i >= box.ChalicesCollected;
            }
        }
        private void HandleHintChanged(HintType hint) => UpdateVisual();
        private void HandleDoorBroken()
        {
            BoardManager.Instance?.Effects.DoorBreak(transform.position);
            UpdateVisual();
        }
        private void HandleChaliceCollected(int count) => BoardManager.Instance?.ReleaseChalices(transform.position, count);
        private void HandleObstacleDamage(ObstacleItem obstacle, int damage)
        {
            UpdateVisual();
            if (obstacle.IsCleared || retiring) return;
            if (obstacle is VaseItem) GameAudio.Play(SoundCue.Vase);
            transform.DOKill();
            transform.localScale = restingScale;
            transform.DOPunchScale(restingScale * 0.09f, 0.18f, 5, 0.5f).SetLink(gameObject);
        }
        public float AnimateFall(Vector3 target, float delay, Action onComplete = null)
        {
            if (retiring) return 0;
            transform.DOKill();
            transform.localScale = restingScale;
            float duration = Mathf.Clamp(Vector3.Distance(transform.position, target) * 0.04f, 0.12f, 0.38f);
            var sequence = DOTween.Sequence().SetTarget(transform).SetLink(gameObject);
            sequence.AppendInterval(delay);
            sequence.Append(transform.DOMove(target, duration).SetEase(Ease.InQuad));
            sequence.Append(transform.DOPunchScale(Vector3.Scale(restingScale, new Vector3(0.06f, -0.06f, 0)), 0.1f, 4, 0.3f));
            sequence.OnComplete(() => { UpdateSorting(); onComplete?.Invoke(); });
            return delay + duration + 0.1f;
        }
        public void AnimateAppear()
        {
            transform.DOKill();
            transform.localScale = Vector3.zero;
            transform.DOScale(restingScale, 0.2f).SetEase(Ease.OutBack).SetLink(gameObject);
        }
        public void AnimateQueuedActivation()
        {
            if (retiring) return;
            transform.DOKill();
            transform.localScale = restingScale;
            // Keep the queued reaction readable above blast debris, below flight heads.
            spriteRenderer.sortingOrder = 105;
            transform.DOPunchScale(restingScale * 0.12f, 0.18f, 4, 0.4f).SetLink(gameObject);
        }
        public void AnimateMergeTo(Vector3 target, Action onComplete)
        {
            if (retiring) return;
            retiring = true;
            transform.DOKill();
            var sequence = DOTween.Sequence().SetTarget(transform).SetLink(gameObject);
            sequence.Append(transform.DOMove(target, 0.22f).SetEase(Ease.InQuad));
            sequence.Join(transform.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack));
            sequence.OnComplete(() => { onComplete?.Invoke(); BoardEffects.Dispose(gameObject); });
        }
        public void PrepareComboAnchor(Sprite preview)
        {
            transform.DOKill();
            if (preview != null)
            {
                spriteRenderer.sprite = preview;
                float size = BoardManager.Instance != null ? BoardManager.Instance.CellSize : 1.44f;
                restingScale = Vector3.one * size / Mathf.Max(preview.bounds.size.x, preview.bounds.size.y);
            }
            transform.localScale = restingScale;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 106;
        }
        public void AnimateComboMergeTo(Vector3 target, Action onArrived)
        {
            if (retiring) { onArrived?.Invoke(); return; }
            retiring = true;
            transform.DOKill();
            transform.localScale = restingScale;
            spriteRenderer.sortingOrder = 104;
            bool signaled = false;
            Action arrived = () =>
            {
                if (signaled) return;
                signaled = true;
                onArrived?.Invoke();
            };
            var sequence = DOTween.Sequence().SetTarget(transform).SetLink(gameObject);
            // Stay large throughout the travel; only dissolve after reaching the anchor.
            sequence.Append(transform.DOMove(target, 0.3f).SetEase(Ease.InOutSine));
            sequence.Join(transform.DOScale(restingScale * 0.78f, 0.3f).SetEase(Ease.InQuad));
            sequence.Append(transform.DOScale(restingScale * 0.15f, 0.06f).SetEase(Ease.InQuad));
            sequence.Join(spriteRenderer.DOFade(0, 0.06f));
            sequence.OnComplete(() => { arrived(); BoardEffects.Dispose(gameObject); });
            sequence.OnKill(() => arrived());
        }
        public Tween AnimateComboCharge(float peakScale)
        {
            transform.DOKill();
            var sequence = DOTween.Sequence().SetTarget(transform).SetLink(gameObject);
            sequence.Append(transform.DOScale(restingScale * peakScale, 0.12f).SetEase(Ease.OutBack));
            sequence.Append(transform.DOScale(restingScale * 0.94f, 0.06f).SetEase(Ease.InQuad));
            return sequence;
        }
        public Tween AnimateRocketComboSpin()
        {
            transform.DOKill(); transform.localRotation=Quaternion.identity;
            var sequence=DOTween.Sequence().SetTarget(transform).SetLink(gameObject);
            sequence.Append(transform.DORotate(new Vector3(0,0,360),.34f,RotateMode.FastBeyond360).SetEase(Ease.InOutSine));
            sequence.Join(transform.DOScale(restingScale*1.2f,.17f).SetEase(Ease.OutQuad));
            sequence.Append(transform.DOScale(restingScale*.94f,.08f).SetEase(Ease.InQuad));
            return sequence;
        }
        public Tween AnimateTntComboCharge(float footprint)
        {
            transform.DOKill();
            float size=Mathf.Max(spriteRenderer.sprite.bounds.size.x,spriteRenderer.sprite.bounds.size.y);
            var sequence=DOTween.Sequence().SetTarget(transform).SetLink(gameObject);
            // Exact two-cell footprint, without OutBack overshoot or moving the center.
            sequence.Append(transform.DOScale(Vector3.one*(footprint/size),.24f).SetEase(Ease.OutCubic));
            sequence.AppendInterval(.08f);
            return sequence;
        }
        public void AnimateBlast(Action onComplete)
        {
            if (retiring) return;
            retiring = true;
            transform.DOKill();
            transform.localScale = restingScale;
            BoardManager.Instance?.Effects.Burst(Item, transform.position);
            var sequence = DOTween.Sequence().SetTarget(transform).SetLink(gameObject);
            sequence.Append(transform.DOScale(restingScale * 1.12f, 0.06f));
            sequence.Append(transform.DOScale(Vector3.zero, 0.12f).SetEase(Ease.InQuad));
            sequence.OnComplete(() => { onComplete?.Invoke(); BoardEffects.Dispose(gameObject); });
        }
    }
}
