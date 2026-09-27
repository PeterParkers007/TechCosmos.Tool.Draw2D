using UnityEngine;

namespace TechCosmos.Tool.Draw2D
{
    public sealed class SectorDraw : DrawShapeBase
    {
        public float Radius = 1f;
        public Vector2 Direction = Vector2.right;
        public float SweepDegrees = 60f;
        public float Width = 0.08f;
        public bool Filled = true;
        public int Segments = 48;

        protected override void Draw(Draw2D canvas, Vector2 position)
        {
            float facing = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
            float start = facing - SweepDegrees * 0.5f;
            if (Filled)
                canvas.FillSector(position, Radius, start, SweepDegrees, Color, Segments);
            else
                canvas.StrokeArc(position, Radius, start, SweepDegrees, Color, Width, Segments);
        }
    }
}
