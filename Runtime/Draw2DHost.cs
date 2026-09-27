using UnityEngine;

namespace TechCosmos.Tool.Draw2D
{
    /// <summary>
    /// 把 <see cref="DrawManager"/> 和 <see cref="Draw2D"/> 接上：每帧 Begin → UpdateDraw → Submit。
    /// 全局单例，用法：<c>Draw2DHost.Instance.Manager.CreateDraw(...)</c>。
    /// </summary>
    public sealed class Draw2DHost : MonoBehaviour
    {
        public static Draw2DHost Instance { get; private set; }

        [SerializeField] Shader shader;
        [SerializeField] int renderQueue = 3000;
        [SerializeField] int vertexCapacity = 8192;
        [SerializeField] int indexCapacity = 24576;

        Draw2D _canvas;

        public DrawManager Manager { get; } = new();

        void Reset() => AssignDefaultShader();

        void OnValidate() => AssignDefaultShader();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            AssignDefaultShader();
            _canvas = new Draw2D(vertexCapacity, indexCapacity);
            _canvas.Configure(shader, renderQueue);
            _canvas.Attach(transform);
        }

        void LateUpdate()
        {
            if (_canvas == null)
                return;

            _canvas.Begin();
            Manager.UpdateDraw(_canvas);
            _canvas.Submit();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            _canvas?.Dispose();
            _canvas = null;
        }

        void AssignDefaultShader()
        {
            if (IsUsableWorldShader(shader))
                return;

            shader = Shader.Find("ShowInformation/Draw2DUnlit") ?? Shader.Find("Sprites/Default");
        }

        static bool IsUsableWorldShader(Shader value)
        {
            if (value == null)
                return false;

            // UI/Default 依赖 Canvas 裁剪，Graphics.DrawMesh 画在世界里会被裁没。
            return !value.name.StartsWith("UI/");
        }
    }
}
