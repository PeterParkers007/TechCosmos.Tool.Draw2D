using UnityEngine;

namespace TechCosmos.Tool.Draw2D
{
    public sealed class CircleDraw : DrawShapeBase
    {
        public float Radius = 1f;
        public float Width = 0.08f;
        public bool Filled;
        public int Segments = 64;

        protected override void Draw(Draw2D canvas, Vector2 position)
        {
            if (Filled)
                canvas.FillCircle(position, Radius, Color, Segments);
            else
                canvas.StrokeCircle(position, Radius, Color, Width, Segments);
        }
    }
}
