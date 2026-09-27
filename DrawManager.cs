using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.Tool.Draw2D
{
    public class DrawManager
    {
        private readonly List<IDraw> _draws = new();

        public IReadOnlyList<IDraw> Draws => _draws;
        public bool IsDrawNull => _draws.Count <= 0;

        public void UpdateDraw(Draw2D canvas)
        {
            if (canvas == null)
                return;

            for (int i = _draws.Count - 1; i >= 0; i--)
            {
                _draws[i].Update(canvas);
            }
        }

        public void CreateDraw(IDraw draw)
        {
            if (draw == null) return;
            if (_draws.Contains(draw)) return;
            _draws.Add(draw);
        }

        public void RemoveDraw(IDraw draw)
        {
            if (draw == null) return;
            _draws.Remove(draw);
        }

        public void RemoveAll() => _draws.Clear();
    }
}
