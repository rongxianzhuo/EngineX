# EngineX.Physics

> 3D 刚体物理引擎模块（零 GC、FP 数值、ECS 组件化集成）
> 状态：**M0 — 基建 + 形状定义**（仅地基，未做碰撞 / 解算）

---

## 1. 坐标系约定

- **右手系**，**Y-up**
- `Vector3.Forward = (0, 0, 1)`、`Up = (0, 1, 0)`、`Right = (1, 0, 0)`
- 旋转：四元数（`Quaternion`），左乘 `rotation * point` 应用到向量
- 角度：内部用弧度；`FP.PI` / `FP.Deg2Rad` / `FP.Rad2Deg` 互转
- 长度单位：米（m），无强约束；M3+ 的 `PhysicsWorld` 会用 `FP` 单位制

## 2. 数值精度预算

- `FP` = 64 位定点，**32 位整数 + 32 位小数**（`FractionBit=32`）
- 相对精度约 **2.3e-10**（与 `double` 同量级，但**完全跨平台确定性**）
- 位置精度：约 **2.3e-10 m**（亚纳米级）
- 速度积分（dt=1/60s、v≤100m/s）：单步位置漂移 ≤ 2.3e-10 m
- 角速度积分（ω≤10 rad/s、dt=1/60s）：单步角度漂移 ≤ 1.3e-9 rad
- 矩阵 / 四元数归一化：每帧 1 次归一化即可维持精度

> ⚠️ **物理引擎对极小值敏感**。M0 起所有几何 / 物理量都使用 `FP`，**严禁在 `Physics/` 内部引入 `float` / `double`**（除 `FromFloat` / `Single()` 桥接外）。

## 3. 命名空间 / 目录约定

```
EngineX.Physics             # Physics/{Shapes,Math,Bodies,World,Queries,Systems}/
EngineX.Physics.Components  # Physics/Components/                  IComponentData 桥
EngineX.Physics.Internal    # Physics/Internal/                    实现细节
```

> 与 `EngineX.ECS` 的命名空间分层一致。

## 4. 形状 API（M0 已交付）

| 类型 | 文件 | 关键成员 |
|---|---|---|
| `Sphere` | `Physics/Shapes/Sphere.cs` | `Center`, `Radius`, `Volume`, `Centroid`, `BoundingBox`, `Support` |
| `Box` | `Physics/Shapes/Box.cs` | `Center`, `HalfExtents`, `Orientation`, `Volume`, `Centroid`, `BoundingBox`, `Support` |
| `Capsule` | `Physics/Shapes/Capsule.cs` | `PointA`, `PointB`, `Radius`, `Axis`, `Height`, `Volume`, `Centroid`, `BoundingBox`, `Support` |
| `Plane` | `Physics/Shapes/Plane.cs` | `Normal`, `D`, `SignedDistance`, `ClosestPointOnPlane`, `Project` |
| `ConvexHull` | `Physics/Shapes/ConvexHull.cs` | `Vertices`, `VertexCount`, `Centroid`, `BoundingBox`, `Support`（M0 stub；M2 完善为零 GC 版） |

所有形状实现 `ISupport`（GJK 入口），提供 `Support(Vector3 direction)` 方法。

## 5. 数学（M0 已交付）

| 类型 | 文件 | 用途 |
|---|---|---|
| `Aabb` | `Physics/Math/Aabb.cs` | 轴对齐包围盒；`Min/Max`, `Center/Extents`, `SurfaceArea/Volume`, `Contains/Overlaps/Merge/Expand` |
| `Ray` | `Physics/Math/Ray.cs` | 射线；`Origin`, `Direction`（构造时自动归一化），`GetPoint(t)` |
| `ISupport` | `Physics/Math/ISupport.cs` | 形状支持函数接口（M2 的 GJK 用） |
| `FpMathExtensions` | `Physics/Internal/Math/FpMathExtensions.cs` | `InvSqrt(FP)` —— M0 补的唯一 FP 数学（`FP.Sqrt` 已在 `Baseline`，`FpMath.Atan2` 已在 `Baseline`） |

## 6. M0 状态

✅ 交付完成：

- 5 个 `readonly struct` 形状（Sphere / Box / Capsule / Plane / ConvexHull stub）
- Aabb / Ray / ISupport
- FpInvSqrt
- 测试项目骨架（`Tests/Physics/`）
- 形状测试（构造、体积、质心、Aabb、`IEquatable`、ISupport）
- FP 数学精度测试（与 `double` 对比）

❌ 尚未交付（M1+）：

- 形状-形状距离 / 碰撞检测
- 刚体动力学 / 积分器
- 宽相 / 窄相 / 接触流形
- 顺序脉冲解算器
- ECS 集成
- 射线 / 扫掠 / 重叠查询
- 层级过滤

## 7. 跑测试

```bash
cd Tests/Physics && dotnet run -c Release
```

末尾应打印 `=== XX passed, 0 failed ===`。

## 8. 设计备忘 / 注意事项

- **所有形状字段 `readonly`**：构造后不可变（运行时变换通过外层 `RigidBody` 的 `Position` / `Orientation` 字段；形状本身是世界坐标下的快照）
- **Box 的 AABB 走精确 OBB→AABB**（abs-rotation 矩阵 × halfExtents），不是 8 顶点枚举
- **Sphere / Box / Capsule 的 Support 都做了零向量兜底**（返回中心 / 中点），避免 GJK 起步时方向为零
- **ConvexHull 是 M0 stub**：用 `Vector3[]` 引用 + O(n) 扫描；M2 会换成 `NativeArray<Vector3>` + 0 GC
- **Shape equality 不做语义等价**：Box 比对是逐字段比（不是几何等价）；ConvexHull 只比引用

## 9. 下一步（M1 预告）

M1 写形状-形状最近距离算法：

- Sphere-Sphere / Sphere-Box / Sphere-Capsule / Sphere-Plane
- Box-Box（**直接 SAT**，不走 GJK）
- Box-Plane / Box-Capsule / Capsule-Capsule / Capsule-Plane
- `DistanceResult` 公开结构体
- 派发表 `ShapePairDispatcher<T0,T1>`
- `DistanceTests.cs` 覆盖 ≥5 case / 对
- `ZeroGcDistanceTests.cs` 验证单次 Distance 分配 < 64 字节
