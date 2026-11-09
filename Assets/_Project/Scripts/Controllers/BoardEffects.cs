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
            Emit(sprites, position, 8, 1.3f);
            Emit(new[] { assets.particleStar }, position, 3, 0.7f);
        }

        public void DoorBreak(Vector3 position)
        {
            GameAudio.Play(SoundCue.Door);
            Emit(assets.doorParticles, position, 12, 1.7f);
        }
        public void Tnt(Vector3 position, int radius = 2, float cellSize = 1.44f)
        {
            GameAudio.Play(SoundCue.Tnt);
            Sprite ring = TntSprite("particle_tnt_01");
            Sprite burst = TntSprite("particle_tnt_02");
            Glow(ring, position, cellSize * 0.9f, cellSize * (radius * 2 + 1) * 0.85f,
                new Color(1, 0.62f, 0.12f, 0.75f), 0.3f, 98);
            Glow(burst, position, cellSize * 0.8f, cellSize * (radius == 3 ? 3.3f : 2.6f),
                new Color(1, 0.86f, 0.42f, 0.9f), 0.14f, 103);
            Emit(assets.tntParticles, position, radius == 3 ? 16 : 12, 2.2f, true);
            Emit(new[] { assets.particleSmoke }, position, radius == 3 ? 10 : 8, 1.7f);
            Emit(new[] { assets.particleStar }, position, radius == 3 ? 16 : 12, 2.4f);
        }
        public void ComboGather(Vector3 position, float cellSize) => Glow(TntSprite("particle_tnt_01"),
            position, cellSize * 1.8f, cellSize * 0.6f, new Color(1, 0.72f, 0.2f, 0.45f), 0.3f, 98);
        public void ComboCharge(Vector3 position, float cellSize)
        {
            GameAudio.Play(SoundCue.ComboCharge);
            Glow(assets.particleSmoke, position, cellSize * 0.6f, cellSize * 1.9f,
                new Color(1, 0.75f, 0.18f, 0.55f), 0.18f, 99);
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
        private readonly System.Random trailRandom = new System.Random(9771);
        private float TrailRange(float min,float max) => min+(float)trailRandom.NextDouble()*(max-min);
        public void RocketTrail(Vector3 position, Vector3 direction, float cellSize)
        {
            Vector3 side = new Vector3(-direction.y,direction.x,0);
            Vector3 exhaust = position-direction*cellSize*.22f;
            // Reference style: golden sparkle dust around a white-hot straight path.
            if (assets.particleSmoke != null && assets.starMaterial != null)
            {
                var glow=Create(assets.particleSmoke,exhaust,cellSize*.2f);
                glow.sharedMaterial=assets.starMaterial;
                glow.color=new Color(1,.78f,.1f,.48f);glow.sortingOrder=104;
                var target=glow.transform;
                var sequence=DOTween.Sequence().SetTarget(target).SetLink(glow.gameObject);
                sequence.Append(target.DOScale(target.localScale*.2f,.2f));
                sequence.Join(glow.DOFade(0,.2f));
                sequence.OnComplete(()=>Dispose(glow.gameObject));
            }
            if (assets.particleStar == null) return;
            for (int i=0;i<2;i++)
            {
                float offset=TrailRange(-.16f,.16f);
                var spark=Create(assets.particleStar,exhaust+side*offset*cellSize,cellSize*TrailRange(.08f,.14f));
                spark.color=new Color(1,.8f,.12f,.9f);spark.sortingOrder=108;
                var target=spark.transform;
                var sequence=DOTween.Sequence().SetTarget(target).SetLink(spark.gameObject);
                sequence.Append(target.DOMove(target.position+side*offset*cellSize-direction*cellSize*.12f,.24f).SetEase(Ease.OutQuad));
                sequence.Join(target.DOScale(Vector3.zero,.24f).SetEase(Ease.InQuad));
                sequence.Join(spark.DOFade(0,.24f));
                sequence.OnComplete(()=>Dispose(spark.gameObject));
            }
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

        private void Emit(Sprite[] sprites, Vector3 position, int count, float spread, bool additive = false)
        {
            if (sprites == null || sprites.Length == 0) return;
            for (int i = 0; i < count; i++)
            {
                Sprite sprite = sprites[UnityEngine.Random.Range(0, sprites.Length)];
                if (sprite == null) continue;
                var renderer = Create(sprite, position, UnityEngine.Random.Range(0.12f, 0.3f));
                if (additive && assets.starMaterial != null)
                {
                    renderer.sharedMaterial = assets.starMaterial;
                    renderer.color = new Color(1, 0.8f, 0.3f, 1);
                }
                var target = renderer.transform;
                Vector3 end = position + (Vector3)UnityEngine.Random.insideUnitCircle * spread;
                end.y -= 0.4f;
                float duration = UnityEngine.Random.Range(0.35f, 0.6f);
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

