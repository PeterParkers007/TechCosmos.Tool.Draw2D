using UnityEngine;

namespace TechCosmos.Tool.Draw2D
{
    /// <summary>
    /// 测试用：挂在带 <see cref="Draw2DHost"/> 的物体上，Play 后画几类图形，扇形/线/箭头跟鼠标。
    /// </summary>
    [RequireComponent(typeof(Draw2DHost))]
    public sealed class Draw2DDemo : MonoBehaviour
    {
        CircleDraw _mouseCircle;
        SectorDraw _sector;
        LineDraw _line;
        ArrowDraw _arrow;
        RectDraw _rect;

        void Awake()
        {
            var manager = GetComponent<Draw2DHost>().Manager;

            manager.CreateDraw(new CircleDraw
            {
                WorldPosition = Vector2.zero,
                Radius = 3f,
                Color = new Color(0.35f, 0.85f, 1f, 0.9f)
            });

            manager.CreateDraw(new ArcDraw
            {
                WorldPosition = Vector2.zero,
                Radius = 3.4f,
                StartDegrees = 20f,
                SweepDegrees = 80f,
                Color = new Color(0.4f, 1f, 0.6f, 0.9f),
                Width = 0.1f
            });

            _rect = new RectDraw
            {
                WorldPosition = new Vector2(-2.5f, -1.5f),
                Size = new Vector2(1.6f, 0.9f),
                Color = new Color(0.8f, 0.6f, 1f, 0.85f)
            };
            manager.CreateDraw(_rect);

            _mouseCircle = new CircleDraw
            {
                Radius = 1.1f,
                Color = new Color(1f, 0.85f, 0.25f, 0.9f)
            };
            manager.CreateDraw(_mouseCircle);

            _sector = new SectorDraw
            {
                WorldPosition = Vector2.zero,
                Radius = 2.2f,
                SweepDegrees = 55f,
                Filled = true,
                Color = new Color(1f, 0.35f, 0.3f, 0.35f)
            };
            manager.CreateDraw(_sector);

            _line = new LineDraw
            {
                From = Vector2.zero,
                Color = new Color(1f, 1f, 1f, 0.75f),
                Width = 0.06f
            };
            manager.CreateDraw(_line);

            _arrow = new ArrowDraw
            {
                From = Vector2.zero,
                Color = new Color(1f, 0.55f, 0.2f, 0.95f),
                Width = 0.07f
            };
            manager.CreateDraw(_arrow);
        }

        void Update()
        {
            var mouse = MouseWorld();
            _mouseCircle.WorldPosition = mouse;
            _sector.Direction = mouse.sqrMagnitude > 0.0001f ? mouse.normalized : Vector2.right;
            _line.To = mouse;
            _arrow.To = mouse;
            _rect.RotationDegrees += 20f * Time.deltaTime;
        }

        static Vector2 MouseWorld()
        {
            var cam = Camera.main;
            if (cam == null)
                return Vector2.zero;

            var screen = Input.mousePosition;
            screen.z = -cam.transform.position.z;
            return cam.ScreenToWorldPoint(screen);
        }
    }
}
