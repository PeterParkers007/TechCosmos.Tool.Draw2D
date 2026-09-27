using UnityEngine;

namespace TechCosmos.Tool.Draw2D
{
    /// <summary>
    /// 具体图形的公共位：跟 Transform 或世界坐标，每帧向传入的 <see cref="Draw2D"/> 提交。
    /// Transform 曾赋值后又被销毁时本帧不画，不回落到 WorldPosition。
    /// </summary>
    public abstract class DrawShapeBase : IDraw
    {
        public Transform Transform { get; set; }
        public bool Enabled { get; set; }

        public Vector2 LocalOffset;
        public Vector2 WorldPosition;
        public Color Color = Color.white;

        public DrawShapeBase(bool enabled = true) => Enabled = enabled;

        public void Update(Draw2D canvas)
        {
            if (!Enabled || canvas == null)
                return;

            if (!TryGetPosition(out var position))
                return;

            Draw(canvas, position);
        }

        protected bool TryGetPosition(out Vector2 position)
        {
            if (IsDestroyed(Transform))
            {
                position = default;
                return false;
            }

            if (Transform != null)
            {
                position = (Vector2)Transform.position + LocalOffset;
                return true;
            }

            position = WorldPosition + LocalOffset;
            return true;
        }

        protected static bool IsDestroyed(Object obj)
            => !ReferenceEquals(obj, null) && obj == null;

        protected abstract void Draw(Draw2D canvas, Vector2 position);
    }
}
