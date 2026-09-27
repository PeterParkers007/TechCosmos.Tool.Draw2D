using UnityEngine;

namespace TechCosmos.Tool.Draw2D
{
    public sealed class ArcDraw : DrawShapeBase
    {
        public float Radius = 1f;
        public float StartDegrees;
        public float SweepDegrees = 90f;
        public float Width = 0.08f;
        public int Segments = 48;

        protected override void Draw(Draw2D canvas, Vector2 position)
        {
            canvas.StrokeArc(position, Radius, StartDegrees, SweepDegrees, Color, Width, Segments);
        }
    }
}
