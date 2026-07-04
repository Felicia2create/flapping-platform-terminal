## 分析完成，以下是落地执行计划

### 1. 与现有代码的兼容性评估

| 项目 | 状态 | 说明 |
|------|------|------|
| **Cinemachine** | ⚠️ 版本差异 | 你提到 2.10.7，但 `manifest.json` 安装的是 **3.1.7**（Cinemachine 3.x）。3.x 的 API 完全不同：用 `CinemachineCamera` 替代 `CinemachineVirtualCamera`，用 `CinemachineSplineDolly` 替代 `CinemachineTrackedDolly`。**计划将基于 3.x 编写。** |
| **TextMeshPro** | ✅ 已安装 | `com.unity.textmeshpro: 3.0.7` |
| **URP** | ✅ 已安装 | `14.0.12`，发光材质可用 `URP/Unlit` + Emission |
| **OrbitCameraController** | ⚠️ 需适配 | 现有拖拽自由视角脚本。Cinemachine Dolly 模式需与此脚本协作——需要添加一个"自动/手动切换"机制 |

**架构冲突点：** `OrbitCameraController` 直接控制 `transform.position/rotation`，而 Cinemachine 也会驱动相机。必须设计互斥逻辑：Dolly 模式激活时禁用 `OrbitCameraController`，反之亦然。

---

### 2. 新建组件和脚本列表

所有新文件放在 `Assets/FPT/Visualization/Runtime/` 下，符合现有架构：

| # | 文件 | 层级 | 职责 |
|---|------|------|------|
| 1 | `Cinemachine/CinemachineDollyManager.cs` | Visualization | 管理 Dolly Track、Target Group、自动/手动模式切换 |
| 2 | `Formation/FormationVisualizer.cs` | Visualization | 在 3 只鸟之间绘制 LineRenderer 三角形连线 |
| 3 | `Formation/WorldSpaceBillboardUI.cs` | Visualization | 计算 3 只鸟中心点、Billboard 朝向相机、显示数据 |

**现有文件需小改：**

| 文件 | 改动 |
|------|------|
| `OrbitCameraController.cs` | 添加 `public bool IsManualMode` 属性，外部可禁用其 `LateUpdate` 逻辑 |

---

### 3. 分步执行计划

#### 第一步：Cinemachine 运镜调度

**需要在 Unity Editor 中手动创建的 GameObject（我提供脚本，你创建物体）：**
- `CinemachineBrain`（挂到 Main Camera 上）
- `CinemachineCamera` (3.x API) 作为 Dolly Camera
- `CinemachineSplineDolly` + `SplineContainer`（圆形轨道）
- `CinemachineTargetGroup`（包含 3 只鸟的 Transform）

**脚本 `CinemachineDollyManager.cs` 功能：**
- `LateUpdate` 中检测鼠标是否拖拽 → 切换 Dolly / OrbitCamera 模式
- Dolly 模式：`CinemachineSplineDolly` 自动沿轨道运动（设置 `AutoDolly` 或手动推进 `CameraPosition`）
- 监听 `CinemachineTargetGroup` 的包围球大小 → 动态调整 Dolly 相机的轨道半径（类似推拉镜头）
- 鼠标闲置 N 秒后自动切回 Dolly 模式

#### 第二步：3D 阵型连线

**脚本 `FormationVisualizer.cs` 功能：**
- `[SerializeField] Transform[] _birds`（3 只鸟的 Transform 引用）
- `Start()` 中创建 `LineRenderer`，loop=true，点数=4（三角形闭合）
- `LateUpdate()` 中更新 3 个顶点位置
- 材质：运行时创建 `URP/Unlit` 材质，`_EmissionColor` 设为科技蓝，`_SurfaceType = Transparent`，Alpha ~0.6

#### 第三步：World Space Billboard UI

**场景搭建（我提供配置，你手动创建）：**
- 创建 `Canvas`（Render Mode = World Space），缩放 `0.002`，放在 3 只鸟中心上方
- 内含 `Panel`（半透明背景）+ `TextMeshProUGUI`（显示模拟数据）

**脚本 `WorldSpaceBillboardUI.cs` 功能：**
- `[SerializeField] Transform[] _trackedObjects`（3 只鸟）
- `LateUpdate()`：
  - `transform.position = 中心点 + Vector3.up * offset`
  - `transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward)`
- 提供 `public void SetData(string text)` 外部调用接口

#### 第四步：OrbitCameraController 适配

- 添加 `public bool IsPaused` 属性
- 当 `CinemachineDollyManager` 激活 Dolly 模式时，设 `IsPaused = true`
- `HandleInput()` 开头检查 `if (IsPaused) return;`

---

### 4. 需要确认的问题

1. **Cinemachine 版本**：你项目装的是 3.1.7，与你文档提到的 2.10.7 不同。我将基于 **3.x** 编写。如果你确实要用 2.x，需要先降级包。
2. **3 只鸟的引用**：在场景中，3 只鸟的 Transform 是否已经作为 Inspector 参数暴露在某个脚本上？还是需要我用 `GameObject.Find("Bird_Wrapper")` 来动态查找？
3. **圆形轨道**：Dolly Track 的圆心放在哪里？3 只鸟的几何中心？还是场景原点？

确认后请 **toggle to Act mode**，我将按上述计划逐步骤实施。