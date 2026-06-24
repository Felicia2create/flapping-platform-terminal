# Flapping Platform Terminal (FPT)

> 基于 Unity 的机械臂与多设备控制终端，通过 ROS2 TCP / 串口通信实现机械臂、转台、传感器、变频器等设备的统一管理与可视化。

---

## 目录

- [项目简介](#项目简介)
- [技术栈](#技术栈)
- [项目结构](#项目结构)
- [核心架构](#核心架构)
- [快速开始](#快速开始)
- [模块说明](#模块说明)
- [通信方式](#通信方式)
- [开发指南](#开发指南)
- [常见问题](#常见问题)

---

## 项目简介

FPT（Flapping Platform Terminal）是一个 Unity 项目，用于：

1. **机械臂控制** — 通过 ROS2 订阅/发布关节状态，驱动 6 轴机械臂
2. **转台控制** — 旋转平台角度控制
3. **多设备管理** — 传感器、变频器等设备通过串口通信
4. **3D 可视化** — 实时显示机械臂运动姿态

---

## 技术栈

| 技术 | 说明 |
|------|------|
| **Unity 2022 LTS+** | 游戏引擎，提供 3D 渲染和物理模拟 |
| **URP** | Universal Render Pipeline，统一渲染管线 |
| **ROS-TCP-Connector** | Unity 官方 ROS2 TCP 通信包 |
| **UI Toolkit** | Unity 内置 UI 框架（UXML + USS） |
| **ArticulationBody** | Unity 物理引擎的关节系统 |
| **C#** | 主要编程语言 |

---

## 项目结构

```
Assets/
├── FPT/                          # 核心业务代码
│   ├── Core/                     # 核心层 — 接口、模型、命令定义
│   │   └── Runtime/
│   │       ├── Commands/         # 设备命令（关节、末端位姿、夹爪等）
│   │       ├── Enums/            # 枚举（连接状态、操作模式）
│   │       ├── Interfaces/       # 核心接口（IDeviceDriver, IDeviceCommand 等）
│   │       ├── Models/           # 数据模型（Pose, CommandResult, DeviceInfo）
│   │       └── States/           # 设备状态（RobotArmState, SensorState 等）
│   │
│   ├── Communication/            # 通信层 — ROS2 桥接、串口、协议编解码
│   │   └── Runtime/
│   │       ├── Bridge/           # ROS2 桥接（Ros2Bridge, Ros2MessageMapper）
│   │       ├── Channel/          # 通信通道抽象
│   │       ├── Manager/          # 通信管理器、连接配置
│   │       ├── Protocol/         # 协议编解码（SensorFrameCodec, VfdFrameCodec）
│   │       ├── Routing/          # 消息路由
│   │       └── Transport/        # 传输层（串口 SerialTransport）
│   │
│   ├── Business/                 # 业务层 — 应用上下文、设备管理、驱动
│   │   └── Runtime/
│   │       ├── AppContext.cs     # 应用入口，初始化所有服务（单例）
│   │       ├── Coordination/     # 设备协调器
│   │       ├── DeviceManager/    # 设备管理器
│   │       ├── Drivers/          # 设备驱动（机械臂、传感器、变频器、转台）
│   │       ├── Interceptors/     # 命令拦截器（关节限位、速度限制）
│   │       ├── Pipeline/         # 命令管道
│   │       └── StateMachines/    # 设备状态机
│   │
│   ├── Visualization/            # 可视化层 — 3D 场景控制
│   │   └── Runtime/
│   │       ├── PlatformJointController.cs  # 关节控制器（桥接 ArticulationBody）
│   │       ├── Camera/           # 相机控制
│   │       ├── Environment/      # 环境场景
│   │       ├── flapping_platform_prefabs/  # 机械臂预制体
│   │       └── Trajectory/       # 轨迹可视化
│   │
│   ├── UI/                       # UI 层 — UI Toolkit 界面
│   │   ├── UI_Toolkit/
│   │   │   ├── UXML/            # 布局文件
│   │   │   └── USS/             # 样式文件
│   │   └── Runtime/
│   │
│   ├── Editor/                   # 编辑器扩展工具
│   │   ├── AssignUssToUIDocument.cs
│   │   ├── FixPlatformMaterials.cs
│   │   └── SetupSceneLighting.cs
│   │
│   └── App/                      # 应用层入口
│
├── ROSJointStateSubscriber.cs    # 独立的 ROS 关节状态订阅脚本（可直接使用）
├── Resources/                    # 资源文件
├── Scenes/                       # 场景文件
└── Settings/                     # 渲染管线设置
```

---

## 核心架构

项目采用 **五层分层架构** + **Assembly 隔离**，各层职责清晰：

```
┌──────────────────────────────────────────────────┐
│               App 层 (FPT.App)                   │  应用入口 & 资源管理
├──────────────────────┬───────────────────────────┤
│   UI 层 (FPT.UI)     │ Visualization 层           │  用户界面 / 3D 可视化
├──────────────────────┴───────────────────────────┤
│            Business 层 (FPT.Business)             │  业务逻辑、设备管理
├──────────────────────────────────────────────────┤
│         Communication 层 (FPT.Communication)      │  ROS2 桥接 / 串口传输
├──────────────────────────────────────────────────┤
│             Core 层 (FPT.Core)                    │  接口定义 / 数据模型
└──────────────────────────────────────────────────┘

     Editor 层 (FPT.Editor)  ← 编辑器扩展，独立运行
```

### Assembly 依赖关系

```
FPT.Core              ← 无依赖（纯接口 & 模型）
    ↑
FPT.Communication     ← Core
    ↑
FPT.Business          ← Core + Communication
    ↑
FPT.UI                ← Business + Visualization
FPT.Visualization     ← Communication + Business
    ↑
FPT.App               ← 所有模块（顶层入口）
FPT.Editor            ← 独立
```

### 设计模式

| 模式 | 应用位置 | 说明 |
|------|----------|------|
| **单例** | `AppContext` | 全局唯一应用入口 |
| **策略** | `IDeviceDriver` / `IDeviceCommand` | 设备驱动多态抽象 |
| **责任链** | `CommandPipeline` | 命令拦截器链（限位、速度限制） |
| **观察者** | `OnStateChanged` 事件 | 设备状态 → UI 自动刷新 |
| **桥接** | `Ros2Bridge` | ROS2 协议封装 |
| **状态机** | `DeviceStateMachine` | 设备连接/运行/错误状态管理 |
| **工厂方法** | `CreateCodecForDevice` | 按设备类型创建编解码器 |
| **中介者** | `DeviceCoordinator` | 协调多设备联动 |

> 完整的架构图（含 Mermaid 类图、时序图、数据流图）请查看 **[ARCHITECTURE.md](./ARCHITECTURE.md)**

### 数据流 — 命令流

```
用户操作 → UI 控制器 → InputTerminal → DeviceDriver.ExecuteCommand()
    → CommandPipeline（拦截器链） → Communication → ROS2/串口 → 物理设备
```

### 数据流 — 状态流

```
物理设备 → ROS2/串口 → Ros2Bridge/SerialTransport
    → DeviceDriver.OnStateChanged → DeviceManager.OnAnyDeviceStateChanged → UI 刷新
    → Ros2Bridge.SubscribeJointStates → PlatformJointController → 3D 关节驱动
```

---

## 快速开始

### 前置条件

1. **Unity 2022 LTS 或更高版本**
2. **Unity Robotics Hub**（可选，用于 ROS-TCP-Connector）
3. **ROS2 环境**（可选，用于真实机械臂通信）

### 打开项目

```bash
# 1. 使用 Unity Hub 打开本项目目录
# File → Open Project → 选择 flapping_platform_terminal 文件夹

# 2. 等待 Unity 导入所有资源（首次打开可能需要几分钟）

# 3. 打开场景
# File → Open Scene → Assets/Scenes/SampleScene.unity

# 4. 点击 Play 运行
```

### 最小化运行（无 ROS2）

项目支持在没有 ROS2 的情况下运行，用于 UI 和可视化调试：

1. 打开 `SampleScene` 场景
2. 点击 Play
3. `AppContext` 会自动初始化所有设备管理器
4. `PlatformJointController` 会自动发现关节

### 连接 ROS2

1. 在 Inspector 中找到 `AppContext` 组件
2. 设置 ROS2 主机地址（默认 `127.0.0.1:10000`）
3. 勾选 `Auto Connect` 或在代码中调用 `AppContext.Instance.ConnectRos2()`
4. ROS2 端需运行对应的节点，发布 `/joint_states` 话题

---

## 模块说明

### Core 层 — 核心接口与模型

定义了所有设备的统一接口和数据模型：

```csharp
// 设备命令接口
public interface IDeviceCommand
{
    string TargetDeviceId { get; }
    string CommandType { get; }
    bool RequiresAcknowledgment { get; }
}

// 设备驱动接口
public interface IDeviceDriver
{
    string DeviceId { get; }
    Task<CommandResult> ExecuteCommandAsync(IDeviceCommand command);
}
```

**命令类型：**

| 命令 | 说明 |
|------|------|
| `JointCommand` | 关节空间运动（6 轴角度） |
| `EePoseCommand` | 末端执行器位姿（XYZ + 欧拉角） |
| `GripperCommand` | 夹爪开合 |
| `HomeCommand` | 回零 |
| `StopCommand` | 紧急停止 |
| `SetModeCommand` | 切换操作模式 |

### Business 层 — 业务逻辑

#### AppContext（应用上下文 — 单例）

所有服务的入口点，负责初始化和依赖注入：

```csharp
// 获取单例
var ctx = AppContext.Instance;

// 连接 ROS2
ctx.ConnectRos2();

// 访问各服务
ctx.Ros2Bridge      // ROS2 通信
ctx.DeviceManager   // 设备管理
ctx.ArmDriver       // 机械臂驱动
```

#### 设备驱动

| 驱动 | 通信方式 | 说明 |
|------|----------|------|
| `RobotArmDriver` | ROS2 TCP | 6 轴机械臂 |
| `TurntableDriver` | 预留 | 转台旋转 |
| `SensorDriver` | 串口 | 传感器数据采集 |
| `VfdMotorDriver` | 串口 | 变频器控制 |

### Communication 层 — 通信

#### Ros2Bridge（ROS2 桥接）

封装 `ROSConnection`，提供 FPT 风格的接口：

```csharp
var bridge = new Ros2Bridge();
bridge.Connect("127.0.0.1", 10000);

// 订阅关节状态
bridge.SubscribeJointStates((names, angles, vels, torques) =>
{
    // angles 已自动从弧度转为角度
    for (int i = 0; i < names.Length; i++)
        Debug.Log($"{names[i]} = {angles[i]:F1}°");
});

// 发布关节指令
bridge.PublishJointCommand(new double[] { 30, -45, 60, 20, -10, 0 });
```

#### ROS 话题映射

| 话题 | 方向 | 消息类型 | 说明 |
|------|------|----------|------|
| `/joint_states` | 订阅 | `sensor_msgs/JointState` | 关节状态反馈 |
| `/joint_commands` | 发布 | `sensor_msgs/JointState` | 关节运动指令 |
| `/ee_pose_command` | 发布 | `geometry_msgs/PoseStamped` | 末端位姿指令 |
| `/gripper_command` | 发布 | `std_msgs/Float32` | 夹爪指令 |

#### 串口通信

非 ROS2 设备通过 `SerialTransport` + 协议编解码器通信：

- `SensorFrameCodec` — 传感器数据帧
- `VfdFrameCodec` — 变频器数据帧

### Visualization 层 — 可视化

#### PlatformJointController

桥接 Unity 物理引擎与 ROS 关节数据：

```csharp
// 关节映射
joint1       → arm1_link1    // 机械臂第 1 轴
joint2       → arm1_link2    // 机械臂第 2 轴
joint3       → arm1_link3    // 机械臂第 3 轴
joint4       → arm1_link4    // 机械臂第 4 轴
joint5       → arm1_link5    // 机械臂第 5 轴
joint6       → arm1_link6    // 机械臂第 6 轴
plate_joint  → platform_plate_Link  // 转台
```

**驱动原理：** 通过 `ArticulationBody.xDrive.target` 设置目标角度（度），Unity 物理引擎自动插值运动。

### UI 层

使用 Unity UI Toolkit（UXML + USS）构建界面。详细的 UI 开发规范请参考 `Assets/UI_Toolkit_Development_Specification.md`。

---

## 通信方式

| 设备 | 通信协议 | 默认地址 |
|------|----------|----------|
| 机械臂 | ROS2 TCP | `127.0.0.1:10000` |
| 转台 | ROS2 TCP | 同上 |
| 传感器 | 串口 | `COM3 @ 115200` |
| 变频器 | 串口 | `COM3 @ 115200` |

---

## 开发指南

### 添加新设备

1. **在 Core 层** 定义命令和状态模型
2. **在 Communication 层** 添加对应的编解码器（如有自定义协议）
3. **在 Business 层** 创建 `DeviceDriverBase` 子类
4. **在 AppContext 中** 注册新驱动

```csharp
// 示例：注册新设备驱动
DeviceManager.RegisterDriver(new MyNewDriver("my_device"));
```

### 添加新命令

```csharp
public class MyCustomCommand : IDeviceCommand
{
    public string TargetDeviceId { get; set; }
    public string CommandType => "MyCustomCommand";
    public bool RequiresAcknowledgment { get; set; } = true;
    
    // 自定义参数
    public double MyParameter { get; set; }
}
```

### 命令拦截器

通过拦截器对命令进行预处理（如限位检查、速度限制）：

- `JointLimitInterceptor` — 关节角度限位
- `SpeedLimitInterceptor` — 速度限制

---

## 常见问题

### Q: 没有 ROS2 能运行吗？

可以。项目支持纯 Unity 模式运行，用于 UI 调试和可视化预览。`AppContext` 中的 `_autoConnect` 默认为 `false`。

### Q: 如何修改机械臂关节数量？

修改 `PlatformJointController` 中的 `_jointNames` 数组和 `GetJointGameObjectMapping()` 映射表。

### Q: ROS 弧度和 Unity 角度如何转换？

`Ros2Bridge` 和 `PlatformJointController` 内部自动处理：
- ROS 发布的是**弧度（rad）**
- Unity ArticulationBody 使用**角度（deg）**
- 转换公式：`角度 = 弧度 × (180 / π)`

### Q: 如何添加新的 UI 界面？

参考 `Assets/UI_Toolkit_Development_Specification.md`，使用 UXML 定义布局，USS 定义样式。注意 UI Toolkit 与 HTML/CSS 有重要差异。

---

> **FPT 开发团队** | 最后更新: 2026-06-03