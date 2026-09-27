using UnityEngine;

namespace TechCosmos.Tool.Draw2D
{
    public sealed class ArrowDraw : DrawShapeBase
    {
        public Transform ToTransform;
        public Vector2 From;
        public Vector2 To = Vector2.right;
        public float Width = 0.08f;
        public float HeadLength = 0.35f;
        public float HeadWidth = 0.28f;

        protected override void Draw(Draw2D canvas, Vector2 position)
        {
            if (IsDestroyed(ToTransform))
                return;

            var from = Transform != null ? position : From;
            var to = ToTransform != null ? (Vector2)ToTransform.position : To;
            canvas.Arrow(from, to, Color, Width, HeadLength, HeadWidth);
        }
    }
}
