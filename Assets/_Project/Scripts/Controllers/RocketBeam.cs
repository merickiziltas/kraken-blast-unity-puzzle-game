using UnityEngine;
using DG.Tweening;

namespace KrakenCache.Controllers
{
    /// <summary>A stationary golden flight path that follows the head, then fades.</summary>
    public sealed class RocketBeam : MonoBehaviour
    {
        private LineRenderer core, halo;
        private readonly Color coreColor = new Color(1, .98f, .58f, .95f);
        private readonly Color haloColor = new Color(1, .66f, .035f, .42f);
        public void Setup(Material material, Vector3 origin, float cellSize)
        {
            halo = Line("Halo", material, origin, cellSize*.3f, haloColor, 102);
            core = Line("Core", material, origin, cellSize*.065f, coreColor, 107);
        }
        private LineRenderer Line(string name, Material material, Vector3 origin, float width, Color color, int order)
        {
            var child = new GameObject(name); child.transform.SetParent(transform,false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material; line.useWorldSpace = true; line.positionCount = 2;
            line.startWidth = line.endWidth = width; line.startColor = line.endColor = color;
            line.sortingOrder = order; line.numCapVertices = 4;
            line.SetPosition(0,origin); line.SetPosition(1,origin);
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex",Texture2D.whiteTexture); line.SetPropertyBlock(properties);
            return line;
        }
        public void Follow(Vector3 endpoint)
        {
            if(core==null)return;
            core.SetPosition(1,endpoint); halo.SetPosition(1,endpoint);
        }
        public void Finish()
        {
            DOVirtual.Float(1,0,.22f,alpha=>
            {
                var a=coreColor; a.a*=alpha; core.startColor=core.endColor=a;
                var b=haloColor; b.a*=alpha; halo.startColor=halo.endColor=b;
            }).SetTarget(transform).SetLink(gameObject).OnComplete(()=>BoardEffects.Dispose(gameObject));
        }
        private void OnDestroy()=>transform.DOKill();
    }
}
