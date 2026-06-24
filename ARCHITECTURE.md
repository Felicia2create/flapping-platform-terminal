# FPT 架构文档

> Flapping Platform Terminal — 模块划分与架构设计详解

---

## 目录

- [系统总览](#系统总览)
- [分层架构](#分层架构)
- [模块依赖关系](#模块依赖关系)
- [核心类关系图](#核心类关系图)
- [数据流](#数据流)
- [设计模式](#设计模式)
- [设备扩展机制](#设备扩展机制)

---

## 系统总览

```mermaid
graph TB
    subgraph 外部系统
        ROS2[ROS2 节点<br/>机械臂/转台]
        Sensor[传感器<br/>串口设备]
        VFD[变频器<br/>串口设备]
    end

    subgraph Unity_FPT["Unity FPT 应用"]
        UI["UI 层<br/>UI Toolkit"]
        VIS["Visualization 层<br/>3D 可视化"]
        BIZ["Business 层<br/>业务逻辑"]
        COMM["Communication 层<br/>通信桥接"]
        CORE["Core 层<br/>接口 & 模型"]
        EDITOR["Editor 层<br/>编辑器扩展"]
        APP["App 层<br/>应用入口"]
    end

    ROS2 -->|"TCP /joint_states"| COMM
    COMM -->|"TCP /joint_commands"| ROS2
    Sensor -->|"Serial (COM3)"| COMM
    VFD -->|"Serial (COM4)"| COMM

    APP --> BIZ
    APP --> UI
    APP --> VIS
    UI --> BIZ
    UI --> VIS
    VIS --> COMM
    BIZ --> COMM
    BIZ --> CORE
    COMM --> CORE

    style UI fill:#4A90D9,color:#fff
    style VIS fill:#7B68EE,color:#fff
    style BIZ fill:#E67E22,color:#fff
    style COMM fill:#27AE60,color:#fff
    style CORE fill:#8E44AD,color:#fff
    style EDITOR fill:#95A5A6,color:#fff
    style APP fill:#C0392B,color:#fff
```

---

## 分层架构

项目采用 **五层分层架构**，自上而下依赖，职责清晰分离：

```mermaid
graph TB
    subgraph 层次["分层架构"]
        L5["🔴 App 层<br/>应用入口 & 资源管理"]
        L4["🔵 UI 层<br/>用户界面 & 交互"]
        L3["🟣 Visualization 层<br/>3D 场景 & 物理模拟"]
        L2["🟠 Business 层<br/>业务逻辑 & 设备管理"]
        L1["🟢 Communication 层<br/>协议桥接 & 传输"]
        L0["🟪 Core 层<br/>接口定义 & 数据模型"]
        LE["⚙️ Editor 层<br/>编辑器工具（独立）"]
    end

    L5 --> L4
    L5 --> L3
    L5 --> L2
    L4 --> L2
    L4 --> L3
    L3 --> L1
    L2 --> L1
    L2 --> L0
    L1 --> L0

    style L5 fill:#C0392B,color:#fff
    style L4 fill:#4A90D9,color:#fff
    style L3 fill:#7B68EE,color:#fff
    style L2 fill:#E67E22,color:#fff
    style L1 fill:#27AE60,color:#fff
    style L0 fill:#8E44AD,color:#fff
    style LE fill:#95A5A6,color:#fff
```

### 各层职责

| 层 | Assembly | 命名空间 | 职责 | 依赖 |
|----|----------|----------|------|------|
| **App** | `FPT.App` | `FPT.App` | 应用入口、资源管理、场景配置 | 所有模块 |
| **UI** | `FPT.UI` | `FPT.UI` | UI Toolkit 界面（UXML/USS）、用户交互 | Business, Visualization |
| **Visualization** | `FPT.Visualization` | `FPT.Visualization` | 3D 场景渲染、关节物理驱动、相机控制 | Communication, Business |
| **Business** | `FPT.Business` | `FPT.Business` | 设备管理、驱动实现、命令管道、状态机 | Core, Communication |
| **Communication** | `FPT.Communication` | `FPT.Communication` | ROS2 桥接、串口传输、协议编解码、消息路由 | Core |
| **Core** | `FPT.Core` | `FPT.Core` | 接口定义、数据模型、命令类型、枚举 | 无 |
| **Editor** | `FPT.Editor` | `FPT.Editor` | 编辑器工具（USS绑定、材质修复、灯光设置） | 独立 |

---

## 模块依赖关系

```mermaid
graph TD
    APP["FPT.App"]
    UI["FPT.UI"]
    VIS["FPT.Visualization"]
    BIZ["FPT.Business"]
    COMM["FPT.Communication"]
    CORE["FPT.Core"]
    EDITOR["FPT.Editor"]

    APP -->|引用| CORE
    APP -->|引用| COMM
    APP -->|引用| BIZ
    APP -->|引用| UI
    APP -->|引用| VIS

    UI -->|引用| BIZ
    UI -->|引用| VIS

    VIS -->|引用| COMM
    VIS -->|引用| BUSINESS

    BIZ -->|引用| CORE
    BIZ -->|引用| COMM

    COMM -->|引用| CORE

    EDITOR -.->|独立| APP

    style APP fill:#C0392B,color:#fff
    style UI fill:#4A90D9,color:#fff
    style VIS fill:#7B68EE,color:#fff
    style BIZ fill:#E67E22,color:#fff
    style COMM fill:#27AE60,color:#fff
    style CORE fill:#8E44AD,color:#fff
    style EDITOR fill:#95A5A6,color:#fff
```

---

## 核心类关系图

### Core 层 — 接口与模型

```mermaid
classDiagram
    class IDeviceDriver {
        <<interface>>
        +string DeviceId
        +DeviceInfo Info
        +IDeviceState CurrentState
        +DeviceHealth Health
        +InitializeAsync(IDeviceChannel)
        +ShutdownAsync()
        +ExecuteCommand(IDeviceCommand) CommandResult
        +event OnStateChanged
        +event OnHealthChanged
    }

    class IDeviceCommand {
        <<interface>>
        +string TargetDeviceId
        +string CommandType
        +bool RequiresAcknowledgment
    }

    class IDeviceState {
        <<interface>>
        +string DeviceId
        +DeviceConnectionState Connection
    }

    class IDeviceChannel {
        <<interface>>
        +SendAsync(byte[])
        +Subscribe(Action~byte[]~)
    }

    class IMessage {
        <<interface>>
    }

    IDeviceDriver --> IDeviceChannel : 使用
    IDeviceDriver --> IDeviceCommand : 执行
    IDeviceDriver --> IDeviceState : 管理
    IDeviceChannel --> IMessage : 传输
```

### 命令类型

```mermaid
classDiagram
    class IDeviceCommand {
        <<interface>>
        +TargetDeviceId
        +CommandType
        +RequiresAcknowledgment
    }

    class JointCommand {
        +double[] Angles
    }
    class EePoseCommand {
        +DevicePose Pose
    }
    class GripperCommand {
        +double Opening
    }
    class HomeCommand
    class StopCommand
    class SetModeCommand {
        +ArmOperationMode Mode
    }

    IDeviceCommand <|.. JointCommand
    IDeviceCommand <|.. EePoseCommand
    IDeviceCommand <|.. GripperCommand
    IDeviceCommand <|.. HomeCommand
    IDeviceCommand <|.. StopCommand
    IDeviceCommand <|.. SetModeCommand
```

### Business 层 — 驱动与管理

```mermaid
classDiagram
    class AppContext {
        <<MonoBehaviour Singleton>>
        +Ros2Bridge Ros2Bridge
        +CommunicationManager CommManager
        +DeviceManager DeviceManager
        +DeviceCoordinator Coordinator
        +RobotArmDriver ArmDriver
        +AnimationDemoController AnimationDemo
        +ConnectRos2()
    }

    class DeviceManager {
        +RegisterDriver(IDeviceDriver)
        +InitializeAllAsync()
        +ShutdownAllAsync()
        +GetDriver(string) IDeviceDriver
        +AllStates Dictionary
    }

    class DeviceDriverBase {
        <<abstract>>
        +string DeviceId
        +ExecuteCommand(IDeviceCommand) CommandResult
    }

    class RobotArmDriver {
        +Bind(Ros2Bridge)
    }
    class SensorDriver
    class VfdMotorDriver
    class TurntableDriver

    class CommandPipeline {
        +AddInterceptor(ICommandInterceptor)
        +Execute(command, state, send) CommandResult
    }

    class ICommandInterceptor {
        <<interface>>
        +Name
        +Intercept(cmd, state) CommandResult
    }

    class JointLimitInterceptor
    class SpeedLimitInterceptor

    class DeviceCoordinator {
        +Subscribe()
    }

    class DeviceStateMachine {
        +CurrentState
        +Transition()
    }

    AppContext --> DeviceManager
    AppContext --> Ros2Bridge
    AppContext --> CommunicationManager
    DeviceManager --> DeviceDriverBase : 管理
    DeviceDriverBase <|-- RobotArmDriver
    DeviceDriverBase <|-- SensorDriver
    DeviceDriverBase <|-- VfdMotorDriver
    DeviceDriverBase <|-- TurntableDriver
    RobotArmDriver --> CommandPipeline
    CommandPipeline --> ICommandInterceptor
    ICommandInterceptor <|.. JointLimitInterceptor
    ICommandInterceptor <|.. SpeedLimitInterceptor
    DeviceCoordinator --> DeviceManager
```

### Communication 层 — 通信架构

```mermaid
classDiagram
    class Ros2Bridge {
        +Connect(host, port)
        +Subscribe~T~(topic, callback)
        +Publish~T~(topic, message)
        +SubscribeJointStates(callback)
        +PublishJointCommand(anglesDeg)
        +PublishEePoseCommand(pose)
        +PublishGripperCommand(opening)
    }

    class Ros2PlanningBridge {
        +PlanJointMove(angles)
        +PlanEeMove(pose)
        +Execute()
    }

    class CommunicationManager {
        +OpenChannel(config) ICommunicationChannel
        +CloseChannel(deviceId)
        +CloseAllChannels()
    }

    class CommunicationChannel {
        +SendAsync(data)
        +Subscribe(callback)
    }

    class ICommunicationChannel {
        <<interface>>
    }

    class ITransport {
        <<interface>>
        +ConnectAsync(config)
        +SendAsync(byte[])
        +OnDataReceived
    }

    class SerialTransport {
        +ConnectAsync(config)
    }

    class IProtocolCodec {
        <<interface>>
        +Encode(IMessage) byte[]
        +Decode(byte[]) IMessage
    }

    class SensorFrameCodec
    class VfdFrameCodec

    class MessageRouter {
        +Route(IMessage)
    }

    Ros2PlanningBridge --> Ros2Bridge
    CommunicationManager --> CommunicationChannel
    CommunicationChannel --> ITransport
    CommunicationChannel --> IProtocolCodec
    CommunicationChannel --> MessageRouter
    ITransport <|.. SerialTransport
    IProtocolCodec <|.. SensorFrameCodec
    IProtocolCodec <|.. VfdFrameCodec
```

### UI 层 — 控制器

```mermaid
classDiagram
    class MainViewController {
        <<MonoBehaviour>>
        -TopBarController _topBar
        -DashboardController _dashboard
        -ControlPanelController _controlPanel
        -StatusBarController _statusBar
        -AnimationPageController _animationController
        -OrbitCameraController _orbitCamera
        -GhostArmController _ghostArm
        +SwitchPage(page)
    }

    class TopBarController {
        +Dispose()
    }
    class DashboardController {
        +Dispose()
    }
    class ControlPanelController {
        +UpdateEeDebounce()
        +UpdateModeHint()
        +Dispose()
    }
    class StatusBarController {
        +Dispose()
    }
    class AnimationPageController {
        +Dispose()
    }

    MainViewController --> TopBarController
    MainViewController --> DashboardController
    MainViewController --> ControlPanelController
    MainViewController --> StatusBarController
    MainViewController --> AnimationPageController
    MainViewController --> AppContext : 依赖
    MainViewController --> GhostArmController : 驱动
    MainViewController --> OrbitCameraController : 控制
```

### Visualization 层

```mermaid
classDiagram
    class PlatformJointController {
        <<MonoBehaviour>>
        -Dictionary~string,ArticulationBody~ _joints
        +DiscoverJoints()
        +SetJointAngle(name, angle)
        +SetArmJointAngles(angles)
        +SetTurntableAngle(angle)
        +UpdateFromRosJointStates(names, positions)
        +Bind(Ros2Bridge)
    }

    class GhostArmController {
        +Bind(InputTerminal)
    }

    class AnimationPlatformController {
        +Activate()
        +Deactivate()
    }

    class OrbitCameraController {
        +ActiveArea Rect
        +ExcludeAreas List
    }

    class TrajectoryRenderer {
        +AddPoint(pos)
        +Clear()
    }

    class GridRenderer

    PlatformJointController --> Ros2Bridge : 绑定订阅
    GhostArmController --> InputTerminal : 绑定
```

---

## 数据流

### 命令流（用户操作 → 设备执行）

```mermaid
sequenceDiagram
    actor User as 用户
    participant UI as UI 层
    participant IT as InputTerminal
    participant Driver as DeviceDriver
    participant Pipeline as CommandPipeline
    participant Interceptor as Interceptor
    participant Comm as Communication
    participant Device as 物理设备/ROS2

    User->>UI: 点击按钮/拖动滑块
    UI->>IT: 发送命令
    IT->>Driver: ExecuteCommand(cmd)
    Driver->>Pipeline: Execute(cmd, state)
    Pipeline->>Interceptor: Intercept(cmd, state)
    alt 被拦截
        Interceptor-->>Pipeline: 返回错误结果
        Pipeline-->>Driver: 返回拦截结果
    else 通过
        Interceptor-->>Pipeline: null（放行）
        Pipeline->>Comm: sendToDevice(cmd)
        Comm->>Device: 发送数据
    end
```

### 状态流（设备 → UI 更新）

```mermaid
sequenceDiagram
    participant Device as 物理设备/ROS2
    participant Comm as Communication
    participant Driver as DeviceDriver
    participant DM as DeviceManager
    participant UI as UI 层
    participant VIS as Visualization

    Device->>Comm: 状态数据（/joint_states 等）
    Comm->>Driver: 解码后数据
    Driver->>Driver: 更新 CurrentState
    Driver-->>DM: OnStateChanged 事件
    DM-->>UI: OnAnyDeviceStateChanged 事件
    UI->>UI: 刷新仪表盘/状态栏

    par 并行：3D 可视化
        Comm->>VIS: Ros2Bridge.SubscribeJointStates
        VIS->>VIS: PlatformJointController 驱动关节
    end
```

---

## 设计模式

| 模式 | 位置 | 说明 |
|------|------|------|
| **单例 (Singleton)** | `AppContext` | 全局唯一应用入口，管理所有服务生命周期 |
| **策略 (Strategy)** | `IDeviceDriver` / `IDeviceCommand` | 设备驱动和命令的多态抽象，新增设备只需实现接口 |
| **责任链 (Chain of Responsibility)** | `CommandPipeline` + `ICommandInterceptor` | 命令经过拦截器链预处理（限位、速度限制等） |
| **观察者 (Observer)** | `OnStateChanged` / `OnHealthChanged` | 事件驱动的状态同步，UI 层订阅设备状态变更 |
| **桥接 (Bridge)** | `Ros2Bridge` | 将 ROS2 协议栈封装为 FPT 统一接口 |
| **MVC/MVP** | UI Toolkit Controllers | MainViewController 管理子控制器，分离视图与业务 |
| **状态机 (State Machine)** | `DeviceStateMachine` | 管理设备连接/运行/错误等状态转换 |
| **工厂方法 (Factory Method)** | `CommunicationManager.CreateCodecForDevice` | 根据设备类型创建对应的协议编解码器 |
| **中介者 (Mediator)** | `DeviceCoordinator` | 协调多设备之间的联动关系 |
| **命令 (Command)** | `IDeviceCommand` 及子类 | 将操作封装为对象，支持队列、拦截、撤销 |

---

## 设备扩展机制

新增设备的完整步骤：

```mermaid
flowchart LR
    A["1. Core 层<br/>定义命令 & 状态"] --> B["2. Communication 层<br/>添加编解码器"]
    B --> C["3. Business 层<br/>实现 IDeviceDriver"]
    C --> D["4. AppContext<br/>注册驱动"]
    D --> E["5. UI 层<br/>添加界面控件"]
    E --> F["6. Visualization<br/>添加 3D 模型"]

    style A fill:#8E44AD,color:#fff
    style B fill:#27AE60,color:#fff
    style C fill:#E67E22,color:#fff
    style D fill:#C0392B,color:#fff
    style E fill:#4A90D9,color:#fff
    style F fill:#7B68EE,color:#fff
```

### 现有设备

| 设备 | Driver | 通信方式 | 通信对象 | 状态类 |
|------|--------|----------|----------|--------|
| 6 轴机械臂 | `RobotArmDriver` | ROS2 TCP | `Ros2Bridge` | `RobotArmState` |
| 转台 | `TurntableDriver` | 预留 | — | `TurntableState` |
| 传感器 | `SensorDriver` | 串口 | `SerialTransport` + `SensorFrameCodec` | `SensorState` |
| 变频器 | `VfdMotorDriver` | 串口 | `SerialTransport` + `VfdFrameCodec` | `VfdMotorState` |

---

## 项目资源

| 资源类型 | 路径 | 说明 |
|----------|------|------|
| 主场景 | `Assets/Scenes/SampleScene.unity` | 主运行场景 |
| 机械臂 URDF | `Assets/FPT/Visualization/Runtime/flapping_platform_prefabs/` | flapping_platform.urdf 及预制体 |
| 鹰模型 | `Assets/bodymodel/BaldEagle/` | 白头鹰 3D 模型（FBX + 材质 + 动画） |
| UI 字体 | `Assets/FPT/UI/Runtime/Resources/` | 微软雅黑 + NotoSansSymbols |
| ROS2 预制体 | `Assets/Resources/ROSConnectionPrefab.prefab` | ROS TCP 连接预设 |
| UI 规范文档 | `Assets/UI_Toolkit_Development_Specification.md` | UI Toolkit 开发规范 |

---

> **FPT 开发团队** | 最后更新: 2026-06-24