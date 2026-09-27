# TechCosmos.Tool.Draw2D

在场景的世界坐标里画 2D 图形：圆、弧、扇形、矩形、线段、箭头。所有图形合成一张网格，每帧画一次。用来做范围圈、瞄准扇形、连线和箭头，不走 Canvas，也不参与物理。

脚本命名空间：

```csharp
using TechCosmos.Tool.Draw2D;
```

程序集名也是 `TechCosmos.Tool.Draw2D`。Unity 2022.3。

图形都画在世界 XY 平面上，Z 固定为 0。场景相机要能看到这个平面。普通 2D 相机放在 `(0, 0, -10)`、朝向原点即可。

角度用度。0 度朝右（+X），90 度朝上（+Y），正角度逆时针。

颜色用 `Color`，第四个分量是透明度。`new Color(1f, 0f, 0f, 0.5f)` 是半透明红。

## 接到场景

1. 新建空物体，挂上 `Draw2DHost`。整个场景只留一个。再挂第二个时，后挂的那个物体会在运行时被删掉。
2. 在自己的脚本里创建图形，交给 `Draw2DHost.Instance.Manager`。

下面这个脚本挂到玩家身上，会在玩家脚下画一个半径 3 的黄圈。空格切换显示。

```csharp
using UnityEngine;
using TechCosmos.Tool.Draw2D;

public class RangeCircle : MonoBehaviour
{
    CircleDraw _circle;

    void Start()
    {
        _circle = new CircleDraw
        {
            Transform = transform,
            Radius = 3f,
            Width = 0.08f,
            Color = new Color(1f, 0.85f, 0.2f, 0.9f)
        };
        Draw2DHost.Instance.Manager.CreateDraw(_circle);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            _circle.Enabled = !_circle.Enabled;
    }

    void OnDestroy()
    {
        if (Draw2DHost.Instance != null)
            Draw2DHost.Instance.Manager.RemoveDraw(_circle);
    }
}
```

`CreateDraw` 把图形放进每帧绘制列表。同一个实例加两次也只会有一份。`Enabled = false` 只是这一帧不画，图形还在列表里。物体销毁时调用 `RemoveDraw`，把图形从列表里拿掉。

`Draw2DHost` 在每帧 `LateUpdate` 里清空画布、让列表里的图形各画一次、再提交网格。业务脚本里不要再调用 `Begin` 或 `Submit`。

## 图形放在哪

圆、弧、扇形、矩形用同一套位置。线段和箭头见后面一节，两端是单独设的。

| 你设置了什么 | 图形中心 |
| --- | --- |
| 指定了 `Transform` | 该物体的世界坐标 + `LocalOffset` |
| 没有 `Transform` | `WorldPosition` + `LocalOffset` |
| `Transform` 曾经赋值，物体后来被销毁 | 这一帧不画，不会改用 `WorldPosition` |

`LocalOffset` 是世界坐标下的平移，不随物体旋转。

固定在世界坐标 `(4, 1)`：

```csharp
var circle = new CircleDraw
{
    WorldPosition = new Vector2(4f, 1f),
    Radius = 1.5f
};
```

跟着某个物体，并往上偏 0.5：

```csharp
var circle = new CircleDraw
{
    Transform = target,
    LocalOffset = new Vector2(0f, 0.5f),
    Radius = 1.5f
};
```

`Color` 默认白色。`Enabled` 默认打开。

## 圆 CircleDraw

`Filled = false`（默认）画圆环，线宽是 `Width`。`Filled = true` 画实心圆，`Width` 不起作用。

`Radius` 默认 1。`Width` 默认 0.08。`Segments` 默认 64，越大越圆，有效范围 8 到 256，超出会被夹回这个范围。半径或线宽小于等于 0.001 时不画。

```csharp
// 原点上的空心圆
Manager.CreateDraw(new CircleDraw
{
    WorldPosition = Vector2.zero,
    Radius = 2f,
    Width = 0.1f,
    Color = new Color(0.3f, 0.8f, 1f, 0.9f)
});

// 实心圆
Manager.CreateDraw(new CircleDraw
{
    WorldPosition = new Vector2(3f, 0f),
    Radius = 0.6f,
    Filled = true,
    Color = new Color(1f, 0.4f, 0.3f, 0.8f)
});
```

下面例子里的 `Manager` 都是 `Draw2DHost.Instance.Manager`。

## 弧 ArcDraw

只画一段弧线，不连到圆心，也没有填充。

`StartDegrees` 是起始角，默认 0（朝右）。`SweepDegrees` 是从起始角扫过的角度，默认 90。正数逆时针，负数顺时针。例如 `StartDegrees = 0`、`SweepDegrees = 90` 是从右扫到上的四分之一圆弧。扫过角度的绝对值小于等于 0.01 时不画。

`Radius` 默认 1，`Width` 默认 0.08，`Segments` 默认 48。

```csharp
Manager.CreateDraw(new ArcDraw
{
    WorldPosition = Vector2.zero,
    Radius = 2f,
    StartDegrees = 0f,
    SweepDegrees = 90f,
    Width = 0.1f,
    Color = new Color(0.4f, 1f, 0.6f, 0.9f)
});
```

## 扇形 SectorDraw

以 `Direction` 为中线，向左右各张开一半张角。`Direction` 默认朝右 `(1, 0)`。`SweepDegrees` 是整个张角，默认 60。所以默认扇形从 -30 度到 +30 度。

`Direction = Vector2.up`、`SweepDegrees = 90` 时，扇形朝上，从 45 度铺到 135 度。

`Filled` 默认 `true`，画实心扇形。设为 `false` 时只画外侧那段弧，不画两条半径，这时用 `Width` 控制弧的粗细。

`Radius` 默认 1，`Segments` 默认 48。`Direction` 不要给零向量。

```csharp
var sector = new SectorDraw
{
    Transform = transform,
    Radius = 4f,
    Direction = Vector2.up,
    SweepDegrees = 60f,
    Filled = true,
    Color = new Color(1f, 0.3f, 0.3f, 0.35f)
};
Manager.CreateDraw(sector);

// 朝向可以每帧改
sector.Direction = (mouseWorld - (Vector2)transform.position).normalized;
```

## 矩形 RectDraw

`Size` 是宽和高，默认 `(2, 1)`，也就是沿 X 宽 2、沿 Y 高 1。`RotationDegrees` 绕矩形中心旋转，默认 0，正角度逆时针。

`Filled = false`（默认）画四条边，边宽是 `Width`（默认 0.08）。`Filled = true` 画实心矩形。宽或高小于等于 0.001 时不画。

```csharp
var rect = new RectDraw
{
    WorldPosition = new Vector2(-2f, 1f),
    Size = new Vector2(1.6f, 0.9f),
    RotationDegrees = 15f,
    Filled = false,
    Width = 0.06f,
    Color = new Color(0.8f, 0.6f, 1f, 0.9f)
};
Manager.CreateDraw(rect);

// 持续旋转
rect.RotationDegrees += 20f * Time.deltaTime;
```

## 线段 LineDraw

线段不看 `WorldPosition`。两端这样取：

| 端 | 怎么取 |
| --- | --- |
| 起点 | 有 `Transform` 时用物体坐标 + `LocalOffset`，否则用 `From` |
| 终点 | 有 `ToTransform` 时用那个物体的坐标，否则用 `To` |

`From` 默认 `(0, 0)`，`To` 默认 `(1, 0)`，`Width` 默认 0.08。两端几乎重合时不画。

没有挂起点物体时，`WorldPosition` 和 `LocalOffset` 不会挪动起点，请直接写 `From` 和 `To`。

`ToTransform` 曾经赋值、物体后来被销毁时，整条线这一帧不画，不会退回 `To`。

世界坐标里从原点画到 `(2, 1)`：

```csharp
Manager.CreateDraw(new LineDraw
{
    From = Vector2.zero,
    To = new Vector2(2f, 1f),
    Width = 0.06f,
    Color = Color.white
});
```

从自己连到另一个物体，终点可以每帧改：

```csharp
var line = new LineDraw
{
    Transform = transform,
    To = Vector2.zero,
    Width = 0.06f
};
Manager.CreateDraw(line);
line.To = mouseWorld;
```

## 箭头 ArrowDraw

和线段同一套端点规则。箭头在终点那一头。`HeadLength` 默认 0.35，是箭头长度；`HeadWidth` 默认 0.28，是箭头底部的宽度。箭头长度不会超过整段距离。箭杆停在箭头底部。

```csharp
var arrow = new ArrowDraw
{
    From = Vector2.zero,
    To = new Vector2(2f, 0.5f),
    Width = 0.07f,
    HeadLength = 0.35f,
    HeadWidth = 0.28f,
    Color = new Color(1f, 0.55f, 0.2f, 1f)
};
Manager.CreateDraw(arrow);
```

从物体 A 指向物体 B：

```csharp
Manager.CreateDraw(new ArrowDraw
{
    Transform = fromObject,
    ToTransform = toObject,
    Width = 0.07f
});
```

## 叠在一起时谁在上面

先 `CreateDraw` 的图形画在上面，后添加的画在下面。图形会盖住场景里的普通物体（深度测试始终通过），并使用透明混合。

单帧能画的顶点数量有上限，默认 8192 个顶点、24576 个索引，在 `Draw2DHost` 上改。超出的部分整块不画，Console 里会出现一次警告：`[Draw2D] 当帧几何超出固定容量`。运行过程中改这两个数不会生效，要在进 Play 之前改好。

## 自己加一种图形

继承 `DrawShapeBase`，实现 `Draw`。参数 `position` 就是上一节算出来的中心。然后同样 `CreateDraw`。

```csharp
public sealed class CrossDraw : DrawShapeBase
{
    public float Size = 1f;
    public float Width = 0.06f;

    protected override void Draw(Draw2D canvas, Vector2 position)
    {
        var half = new Vector2(Size * 0.5f, 0f);
        canvas.StrokeLine(position - half, position + half, Color, Width);
        canvas.StrokeLine(
            position - new Vector2(0f, half.x),
            position + new Vector2(0f, half.x),
            Color,
            Width);
    }
}
```

也可以不继承，自己实现 `IDraw`（`Transform`、`Enabled`、`Update(Draw2D canvas)`），在 `Update` 里调用下面的方法。

| 方法 | 画出 |
| --- | --- |
| `StrokeCircle(center, radius, color, width, segments)` | 圆环 |
| `FillCircle(center, radius, color, segments)` | 实心圆 |
| `StrokeArc(center, radius, startDegrees, sweepDegrees, color, width, segments, closed)` | 弧。`closed` 为 true 时首尾连上，画整圆时会用到 |
| `FillSector(center, radius, startDegrees, sweepDegrees, color, segments)` | 从圆心填到弧的扇形。这里的角度是起始角加扫角，不是中线 |
| `StrokeLine(from, to, color, width)` | 线段 |
| `FillRect(center, size, rotationDegrees, color)` | 实心矩形 |
| `StrokeRect(center, size, rotationDegrees, color, width)` | 矩形边框 |
| `Arrow(from, to, color, width, headLength, headWidth)` | 箭头 |

`width` 默认 0.08，`segments` 默认 64。扇形的 `FillSector` 使用起始角，和 `SectorDraw` 的“中线 + 张角”不是同一个数。`SectorDraw` 会把中线换算成起始角再交给 `FillSector`。

## 不用 Host

一般用上面的 Host。只有自己要控制提交时机时才直接持有 `Draw2D`：

```csharp
var canvas = new Draw2D(8192, 24576);
canvas.Configure(shader, 3000);
canvas.Attach(transform);

canvas.Begin();
canvas.FillCircle(Vector2.zero, 1f, Color.cyan);
canvas.StrokeLine(Vector2.zero, Vector2.right, Color.white, 0.08f);
canvas.Submit();

canvas.Dispose();
```

`Attach` 会在给定物体下创建一个叫 `Draw2DMesh` 的子物体来渲染。`Dispose` 释放缓冲并销毁这个子物体。用 Host 时这些都由 Host 处理，不要再 `new Draw2D`。

## Host 上的参数

选中挂了 `Draw2DHost` 的物体：

| 参数 | 默认 | 作用 |
| --- | --- | --- |
| Shader | `ShowInformation/Draw2DUnlit` | 绘制用的着色器。找不到时用 `Sprites/Default` |
| Render Queue | 3000 | 渲染队列，默认在透明物体这一档 |
| Vertex Capacity | 8192 | 一帧最多多少顶点 |
| Index Capacity | 24576 | 一帧最多多少索引 |

不要指定名字以 `UI/` 开头的着色器。那些着色器依赖 Canvas 裁剪，画在世界里会什么都看不到。

`Runtime/Draw2DUnlit.shader` 就是默认着色器，名字是 `ShowInformation/Draw2DUnlit`。它按顶点颜色做透明混合。

## 示例场景

在带 `Draw2DHost` 的物体上再挂 `Draw2DDemo`（它会自动要求 Host）。进入 Play 后能看到：

- 原点一个半径 3 的圆，圆外还有一段弧
- 左下方一个旋转的矩形
- 跟着鼠标的圆
- 朝向鼠标的扇形
- 从原点连到鼠标的线段和箭头

场景里要有带 `MainCamera` 标签的相机。示例用鼠标位置换算到世界坐标 z = 0 的平面上。

## 画面上没有东西时

- 场景里没有 `Draw2DHost`，或 `Instance` 还是空的就去 `CreateDraw` 了。Host 至少要已经 `Awake`。
- 图形 `Enabled` 是 false，半径、长宽或线宽接近 0，线段两端重合，扇形或弧的扫角接近 0。
- `Transform` 或 `ToTransform` 指向的物体已经销毁。这种情况下该图形会停画。
- Host 上的 Shader 是 `UI/` 开头的。
- Console 里有容量警告。把 Vertex Capacity 和 Index Capacity 调大后再进 Play。
- 相机没有对着 Z = 0 的 XY 平面，图形在画面外面。
