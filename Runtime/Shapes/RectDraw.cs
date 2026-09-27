using UnityEngine;

namespace TechCosmos.Tool.Draw2D
{
    public sealed class RectDraw : DrawShapeBase
    {
        public Vector2 Size = new(2f, 1f);
        public float RotationDegrees;
        public float Width = 0.08f;
        public bool Filled;

        protected override void Draw(Draw2D canvas, Vector2 position)
        {
            if (Filled)
                canvas.FillRect(position, Size, RotationDegrees, Color);
            else
                canvas.StrokeRect(position, Size, RotationDegrees, Color, Width);
        }
    }
}
