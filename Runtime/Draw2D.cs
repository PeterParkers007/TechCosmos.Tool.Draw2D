using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace TechCosmos.Tool.Draw2D
{
    /// <summary>
    /// 2D 合批绘制。缓冲启动时一次分配，运行中不扩容；超容量丢弃当帧多余几何。
    /// 顶点在世界 XY、z=0。由 Host 每帧 Begin → UpdateDraw → Submit。
    /// </summary>
    public sealed class Draw2D
    {
        const int DefaultSegments = 64;
        const int MinSegments = 8;
        const int MaxSegments = 256;

        readonly int _vertexCapacity;
        readonly int _indexCapacity;
        NativeArray<Vector3> _positions;
        NativeArray<Color32> _colors;
        NativeArray<int> _indices;

        int _vertCount;
        int _indexCount;
        bool _overflowLogged;
        bool _shaderLogged;

        Mesh _mesh;
        Material _material;
        Shader _shader;
        int _renderQueue = 3000;
        GameObject _view;
        MeshFilter _filter;
        MeshRenderer _renderer;

        public Draw2D(int vertexCapacity = 8192, int indexCapacity = 24576)
        {
            _vertexCapacity = Mathf.Max(16, vertexCapacity);
            _indexCapacity = Mathf.Max(16, indexCapacity);
            _positions = new NativeArray<Vector3>(_vertexCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _colors = new NativeArray<Color32>(_vertexCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _indices = new NativeArray<int>(_indexCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        public void Configure(Shader shader, int renderQueue = 3000)
        {
            _shader = shader;
            _renderQueue = renderQueue;
            if (_material != null)
                _material.renderQueue = renderQueue;
        }

        public void Attach(Transform parent)
        {
            if (_view != null)
                return;

            _view = new GameObject("Draw2DMesh");
            if (parent != null)
            {
                _view.transform.SetParent(parent, false);
                _view.layer = parent.gameObject.layer;
            }

            _filter = _view.AddComponent<MeshFilter>();
            _renderer = _view.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _renderer.allowOcclusionWhenDynamic = false;
            _renderer.sortingOrder = 32767;
            _renderer.enabled = false;
        }

        public void Begin()
        {
            _vertCount = 0;
            _indexCount = 0;
        }

        public void Submit()
        {
            EnsureResources();
            if (_mesh == null || _filter == null || _renderer == null)
                return;

            if (_vertCount == 0 || _indexCount == 0)
            {
                _renderer.enabled = false;
                return;
            }

            _mesh.Clear();
            _mesh.SetVertices(_positions, 0, _vertCount);
            _mesh.SetColors(_colors, 0, _vertCount);
            _mesh.SetIndices(_indices, 0, _indexCount, MeshTopology.Triangles, 0, true);
            _filter.sharedMesh = _mesh;
            if (_material != null)
                _renderer.sharedMaterial = _material;
            _renderer.enabled = true;
        }

        public void Dispose()
        {
            if (_positions.IsCreated)
                _positions.Dispose();
            if (_colors.IsCreated)
                _colors.Dispose();
            if (_indices.IsCreated)
                _indices.Dispose();

            if (_mesh != null)
            {
                Object.Destroy(_mesh);
                _mesh = null;
            }

            if (_material != null)
            {
                Object.Destroy(_material);
                _material = null;
            }

            if (_view != null)
            {
                Object.Destroy(_view);
                _view = null;
            }

            _filter = null;
            _renderer = null;
        }

        public void StrokeCircle(Vector2 center, float radius, Color color, float width = 0.08f, int segments = DefaultSegments)
        {
            if (radius <= 0.001f || width <= 0.001f)
                return;

            StrokeArc(center, radius, 0f, 360f, color, width, segments, closed: true);
        }

        public void FillCircle(Vector2 center, float radius, Color color, int segments = DefaultSegments)
        {
            if (radius <= 0.001f)
                return;

            FillSector(center, radius, 0f, 360f, color, segments);
        }

        public void StrokeArc(
            Vector2 center,
            float radius,
            float startDegrees,
            float sweepDegrees,
            Color color,
            float width = 0.08f,
            int segments = DefaultSegments,
            bool closed = false)
        {
            if (radius <= 0.001f || width <= 0.001f || Mathf.Abs(sweepDegrees) <= 0.01f)
                return;

            int segs = ClampSegments(segments);
            int points = closed ? segs : segs + 1;
            if (!Ensure(points * 2, segs * 6))
                return;

            var col = (Color32)color;
            float half = width * 0.5f;
            float inner = Mathf.Max(0f, radius - half);
            float outer = radius + half;
            float start = startDegrees * Mathf.Deg2Rad;
            float sweep = sweepDegrees * Mathf.Deg2Rad;
            int v0 = _vertCount;

            for (int i = 0; i < points; i++)
            {
                float t = i / (float)segs;
                float a = start + sweep * t;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                AddVert(center + dir * inner, col);
                AddVert(center + dir * outer, col);
            }

            for (int i = 0; i < segs; i++)
            {
                int a = v0 + i * 2;
                int b = a + 1;
                int c = v0 + ((i + 1) % points) * 2 + 1;
                int d = v0 + ((i + 1) % points) * 2;
                AddQuad(a, b, c, d);
            }
        }

        public void FillSector(
            Vector2 center,
            float radius,
            float startDegrees,
            float sweepDegrees,
            Color color,
            int segments = DefaultSegments)
        {
            if (radius <= 0.001f || Mathf.Abs(sweepDegrees) <= 0.01f)
                return;

            int segs = ClampSegments(segments);
            bool full = Mathf.Abs(Mathf.Abs(sweepDegrees) - 360f) <= 0.01f;
            int rim = full ? segs : segs + 1;
            if (!Ensure(1 + rim, segs * 3))
                return;

            var col = (Color32)color;
            float start = startDegrees * Mathf.Deg2Rad;
            float sweep = sweepDegrees * Mathf.Deg2Rad;
            int v0 = _vertCount;

            AddVert(center, col);
            for (int i = 0; i < rim; i++)
            {
                float t = i / (float)segs;
                float a = start + sweep * t;
                AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, col);
            }

            for (int i = 0; i < segs; i++)
            {
                int a = v0 + 1 + i;
                int b = full ? v0 + 1 + ((i + 1) % rim) : v0 + 2 + i;
                AddTri(v0, a, b);
            }
        }

        public void StrokeLine(Vector2 from, Vector2 to, Color color, float width = 0.08f)
        {
            if (width <= 0.001f)
                return;

            var delta = to - from;
            if (delta.sqrMagnitude <= 0.000001f)
                return;

            if (!Ensure(4, 6))
                return;

            WriteLine(from, to, (Color32)color, width);
        }

        public void FillRect(Vector2 center, Vector2 size, float rotationDegrees, Color color)
        {
            if (size.x <= 0.001f || size.y <= 0.001f)
                return;

            if (!Ensure(4, 6))
                return;

            GetRectCorners(center, size, rotationDegrees, out var c0, out var c1, out var c2, out var c3);
            var col = (Color32)color;
            int v0 = _vertCount;
            AddVert(c0, col);
            AddVert(c1, col);
            AddVert(c2, col);
            AddVert(c3, col);
            AddQuad(v0, v0 + 1, v0 + 2, v0 + 3);
        }

        public void StrokeRect(Vector2 center, Vector2 size, float rotationDegrees, Color color, float width = 0.08f)
        {
            if (size.x <= 0.001f || size.y <= 0.001f || width <= 0.001f)
                return;

            if (!Ensure(16, 24))
                return;

            GetRectCorners(center, size, rotationDegrees, out var c0, out var c1, out var c2, out var c3);
            var col = (Color32)color;
            WriteLine(c0, c1, col, width);
            WriteLine(c1, c2, col, width);
            WriteLine(c2, c3, col, width);
            WriteLine(c3, c0, col, width);
        }

        public void Arrow(Vector2 from, Vector2 to, Color color, float width = 0.08f, float headLength = 0.35f, float headWidth = 0.28f)
        {
            if (width <= 0.001f)
                return;

            var delta = to - from;
            float lenSq = delta.sqrMagnitude;
            if (lenSq <= 0.000001f)
                return;

            if (!Ensure(7, 9))
                return;

            float len = Mathf.Sqrt(lenSq);
            var dir = delta / len;
            var perp = new Vector2(-dir.y, dir.x);
            float hl = Mathf.Min(headLength, len);
            var neck = to - dir * hl;
            var col = (Color32)color;

            WriteLine(from, neck, col, width);

            int v0 = _vertCount;
            AddVert(to, col);
            AddVert(neck + perp * (headWidth * 0.5f), col);
            AddVert(neck - perp * (headWidth * 0.5f), col);
            AddTri(v0, v0 + 1, v0 + 2);
        }

        static void GetRectCorners(
            Vector2 center,
            Vector2 size,
            float rotationDegrees,
            out Vector2 c0,
            out Vector2 c1,
            out Vector2 c2,
            out Vector2 c3)
        {
            float rad = rotationDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            var axisX = new Vector2(cos, sin) * (size.x * 0.5f);
            var axisY = new Vector2(-sin, cos) * (size.y * 0.5f);
            c0 = center + axisX + axisY;
            c1 = center - axisX + axisY;
            c2 = center - axisX - axisY;
            c3 = center + axisX - axisY;
        }

        bool Ensure(int extraVerts, int extraIndices)
        {
            if (_vertCount + extraVerts <= _vertexCapacity &&
                _indexCount + extraIndices <= _indexCapacity)
                return true;

            if (!_overflowLogged)
            {
                _overflowLogged = true;
                Debug.LogWarning(
                    $"[Draw2D] 当帧几何超出固定容量（顶点 {_vertexCapacity} / 索引 {_indexCapacity}），多余部分丢弃。");
            }

            return false;
        }

        void WriteLine(Vector2 from, Vector2 to, Color32 color, float width)
        {
            var delta = to - from;
            if (delta.sqrMagnitude <= 0.000001f)
                return;

            var n = new Vector2(-delta.y, delta.x).normalized * (width * 0.5f);
            int v0 = _vertCount;
            AddVert(from - n, color);
            AddVert(from + n, color);
            AddVert(to + n, color);
            AddVert(to - n, color);
            AddQuad(v0, v0 + 1, v0 + 2, v0 + 3);
        }

        void AddVert(Vector2 p, Color32 color)
        {
            _positions[_vertCount] = new Vector3(p.x, p.y, 0f);
            _colors[_vertCount] = color;
            _vertCount++;
        }

        void AddQuad(int a, int b, int c, int d)
        {
            AddTri(a, b, c);
            AddTri(a, c, d);
        }

        void AddTri(int a, int b, int c)
        {
            _indices[_indexCount] = a;
            _indices[_indexCount + 1] = b;
            _indices[_indexCount + 2] = c;
            _indexCount += 3;
        }

        static int ClampSegments(int segments)
            => Mathf.Clamp(segments, MinSegments, MaxSegments);

        void EnsureResources()
        {
            if (_mesh == null)
            {
                _mesh = new Mesh
                {
                    name = "Draw2D",
                    hideFlags = HideFlags.HideAndDontSave
                };
                _mesh.MarkDynamic();
            }

            if (_material != null)
                return;

            var shader = Shader.Find("ShowInformation/Draw2DUnlit");
            if (!IsUsableWorldShader(shader))
                shader = IsUsableWorldShader(_shader) ? _shader : Shader.Find("Sprites/Default");
            if (shader == null)
            {
                if (!_shaderLogged)
                {
                    _shaderLogged = true;
                    Debug.LogError("[Draw2D] 找不到 Shader。");
                }

                return;
            }

            _material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = _renderQueue
            };
        }

        static bool IsUsableWorldShader(Shader value)
            => value != null && !value.name.StartsWith("UI/");
    }
}
