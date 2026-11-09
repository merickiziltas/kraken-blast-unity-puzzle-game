using System;
using UnityEngine;
using DG.Tweening;
using KrakenCache.Core;
using KrakenCache.Data;
using KrakenCache.Items;

namespace KrakenCache.Controllers
{
    /// <summary>Owns disposable sprite effects; gameplay never depends on particle objects.</summary>
    public sealed class BoardEffects : MonoBehaviour
    {
        private GameAssets assets;
        private Transform effectRoot;
        public Transform Root => effectRoot;
        public void Setup(GameAssets gameAssets)
        {
            assets = gameAssets;
            if (effectRoot != null) return;
            effectRoot = new GameObject("Effects").transform;
            effectRoot.SetParent(transform, false);
        }

        public void ClearTransient()
        {
            // Cancelled chain coroutines may still own flying heads. Reset must
            // remove those along with their trails before a new board is shown.
            if (effectRoot == null) return;
            for (int i = effectRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = effectRoot.GetChild(i).gameObject;
                child.SetActive(false);
                Dispose(child);
            }
        }

        public void Burst(Item item, Vector3 position)
        {
            GameAudio.Play(item is CubeItem ? SoundCue.CubePop : item is StoneItem ? SoundCue.Stone
                : item is VaseItem ? SoundCue.Vase : SoundCue.Door);
            Sprite[] sprites = null;
            if (item is CubeItem cube)
                sprites = new[] { assets.GetCubeParticle(cube.Color) };
            else if (item is StoneItem) sprites = assets.stoneParticles;
            else if (item is VaseItem) sprites = assets.vaseParticles;
            else if (item is ChaliceBoxItem) sprites = assets.boxParticles;
            Emit(sprites, position, item is CubeItem ? 3 : 5, 0.8f);
        }

        public void DoorBreak(Vector3 position)
        {
            GameAudio.Play(SoundCue.Door);
            Emit(assets.doorParticles, position, 6, 1.05f);
        }
        public void Tnt(Vector3 position, int radius = 2, float cellSize = 1.44f)
        {
            GameAudio.Play(SoundCue.Tnt);
            Sprite ring = TntSprite("particle_tnt_01");
            Sprite burst = TntSprite("particle_tnt_02");
            Glow(ring, position, cellSize * 0.9f, cellSize * (radius * 2 + 1) * 0.85f,
                new Color(1, 0.68f, 0.32f, 0.38f), 0.24f, 98);
            Glow(burst, position, cellSize * 0.6f, cellSize * 1.4f,
                new Color(1, 0.83f, 0.55f, 0.48f), 0.13f, 103);
            Emit(new[] { assets.particleSmoke }, position, radius == 3 ? 6 : 4, 1.2f);
            Emit(new[] { assets.particleStar }, position, radius == 3 ? 5 : 3, 1.5f);
        }
        public void ComboGather(Vector3 position, float cellSize) => Glow(TntSprite("particle_tnt_01"),
            position, cellSize * 1.8f, cellSize * 0.6f, new Color(1, 0.72f, 0.2f, 0.26f), 0.24f, 98);
        public void ComboCharge(Vector3 position, float cellSize)
        {
            GameAudio.Play(SoundCue.ComboCharge);
            Glow(assets.particleSmoke, position, cellSize * 0.6f, cellSize * 1.9f,
                new Color(1, 0.75f, 0.18f, 0.28f), 0.18f, 99);
        }
        private Sprite TntSprite(string name) => assets.tntParticles == null ? null
            : Array.Find(assets.tntParticles, sprite => sprite != null && sprite.name.Contains(name));
        private void Glow(Sprite sprite, Vector3 position, float fromSize, float toSize, Color color, float duration, int order)
        {
            if (sprite == null || assets.starMaterial == null) return;
            var renderer = Create(sprite, position, fromSize);
            renderer.sharedMaterial = assets.starMaterial;
            renderer.color = color;
            renderer.sortingOrder = order;
            var target = renderer.transform;
            var sequence = DOTween.Sequence().SetTarget(target).SetLink(renderer.gameObject);
            sequence.Append(target.DOScale(target.localScale * (toSize / fromSize), duration).SetEase(Ease.OutCubic));
            sequence.Join(renderer.DOFade(0, duration).SetEase(Ease.InQuad));
            sequence.OnComplete(() => Dispose(renderer.gameObject));
        }
        public RocketBeam CreateRocketBeam(Vector3 origin, float cellSize)
        {
            if (assets.starMaterial == null) return null;
            var obj = new GameObject("RocketBeam");
            obj.transform.SetParent(effectRoot,false);
            var beam = obj.AddComponent<RocketBeam>();
            beam.Setup(assets.starMaterial,origin,cellSize);
            return beam;
        }
        public void RocketTrail(Vector3 position, Vector3 direction, float cellSize)
        {
            if (assets.particleSmoke == null) return;
            Vector3 exhaust = position - direction * cellSize * 0.18f;
            var smoke = Create(assets.particleSmoke, exhaust, cellSize * 0.22f);
            smoke.color = new Color(1, 0.89f, 0.74f, 0.44f);
            smoke.sortingOrder = 104;
            var target = smoke.transform;
            var sequence = DOTween.Sequence().SetTarget(target).SetLink(smoke.gameObject);
            sequence.Append(target.DOScale(target.localScale * 1.2f, 0.16f));
            sequence.Join(smoke.DOFade(0, 0.16f));
            sequence.OnComplete(() => Dispose(smoke.gameObject));
        }
        public void Celebrate(Vector3 position) => Emit(new[] { assets.particleStar }, position, 36, 5f);

        private SpriteRenderer Create(Sprite sprite, Vector3 position, float size)
        {
            var obj = new GameObject("FX_" + sprite.name);
            obj.transform.SetParent(effectRoot, false);
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            if (sprite == assets.particleStar && assets.starMaterial != null)
            {
                renderer.sharedMaterial = assets.starMaterial;
                renderer.color = new Color(1, 0.85f, 0.4f, 1);
            }
            renderer.sortingOrder = 100;
            obj.transform.localScale = Vector3.one * size / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            return renderer;
        }

        private void Emit(Sprite[] sprites, Vector3 position, int count, float spread)
        {
            if (sprites == null || sprites.Length == 0) return;
            for (int i = 0; i < count; i++)
            {
                Sprite sprite = sprites[UnityEngine.Random.Range(0, sprites.Length)];
                if (sprite == null) continue;
                var renderer = Create(sprite, position, UnityEngine.Random.Range(0.10f, 0.23f));
                var target = renderer.transform;
                Vector3 end = position + (Vector3)UnityEngine.Random.insideUnitCircle * spread;
                end.y -= 0.4f;
                float duration = UnityEngine.Random.Range(0.24f, 0.43f);
                var sequence = DOTween.Sequence().SetTarget(target).SetLink(renderer.gameObject);
                sequence.Append(target.DOJump(end, spread * 0.3f, 1, duration).SetEase(Ease.OutQuad));
                sequence.Join(target.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-180f, 180f)), duration));
                sequence.Join(target.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad));
                sequence.Join(renderer.DOFade(0, duration).SetEase(Ease.InQuad));
                sequence.OnComplete(() => { target.DOKill(); Destroy(renderer.gameObject); });
            }
        }

        public GameObject CreateRocketPart(Sprite sprite, Vector3 position)
        {
            if (sprite == null)
            {
                var part = new GameObject("RocketPart");
                part.transform.SetParent(effectRoot, false);
                return part;
            }
            var renderer = Create(sprite, position, 1.15f);
            renderer.sortingOrder = 110;
            return renderer.gameObject;
        }

        public static void Dispose(GameObject obj)
        {
            if (obj == null) return;
            obj.transform.DOKill();
            foreach (var renderer in obj.GetComponents<SpriteRenderer>()) renderer.DOKill();
            Destroy(obj);
        }

        private void OnDestroy()
        {
            if (effectRoot == null) return;
            foreach (Transform child in effectRoot)
            {
                child.DOKill();
                var renderer = child.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.DOKill();
            }
        }
    }
}

