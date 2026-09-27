using UnityEngine;

namespace TechCosmos.Tool.Draw2D
{
    public interface IDraw
    {
        Transform Transform { get; set; }
        bool Enabled { get; set; }
        void Update(Draw2D canvas);
    }
}
