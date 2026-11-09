using UnityEngine;
using UnityEngine.UI;

namespace KrakenCache.UI
{
    /// <summary>A resolution-independent speaker icon; no additional bitmap asset required.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SpeakerGraphic : MaskableGraphic
    {
        private bool muted;
        public void SetMuted(bool value) { muted = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            float size = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height);
            Vector2 origin = rectTransform.rect.center;
            Quad(helper, origin, size, new Vector2(-.34f,-.13f), new Vector2(-.14f,-.13f), new Vector2(-.14f,.13f), new Vector2(-.34f,.13f), color);
            Quad(helper, origin, size, new Vector2(-.14f,-.13f), new Vector2(.08f,-.29f), new Vector2(.08f,.29f), new Vector2(-.14f,.13f), color);
            if (muted)
            {
                Line(helper, origin, size, new Vector2(.18f,-.15f), new Vector2(.39f,.15f), .06f, color);
                Line(helper, origin, size, new Vector2(.18f,.15f), new Vector2(.39f,-.15f), .06f, color);
            }
            else
                for (int ring = 0; ring < 2; ring++)
                {
                    float radius = .2f + ring * .14f;
                    for (int i = 0; i < 10; i++)
                    {
                        float a = Mathf.Lerp(-55,55,i/10f) * Mathf.Deg2Rad;
                        float b = Mathf.Lerp(-55,55,(i+1)/10f) * Mathf.Deg2Rad;
                        Line(helper, origin, size, new Vector2(.06f,0) + new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                            new Vector2(.06f,0) + new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius, .045f, color);
                    }
                }
        }
        private static void Line(VertexHelper h, Vector2 origin, float size, Vector2 a, Vector2 b, float thickness, Color tint)
        {
            Vector2 normal = new Vector2(-(b-a).y,(b-a).x).normalized * thickness * .5f;
            Quad(h,origin,size,a-normal,b-normal,b+normal,a+normal,tint);
        }
        private static void Quad(VertexHelper h, Vector2 origin, float size, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int index = h.currentVertCount;
            foreach (Vector2 point in new[] { a,b,c,d }) h.AddVert(origin + point*size,tint,Vector2.zero);
            h.AddTriangle(index,index+1,index+2); h.AddTriangle(index,index+2,index+3);
        }
    }
}
