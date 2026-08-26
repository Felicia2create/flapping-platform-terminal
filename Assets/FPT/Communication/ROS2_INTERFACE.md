3# Unity ROS2 通信接口文档

> 本文档列出 Unity 侧（Ros2Node）使用的所有 ROS2 Topic 和 Service 接口。
> ROS2 端需按此文档实现对应的 Service Server 和 Topic Publisher。

---

## 连接信息

| 项目 | 值 |
|------|-----|
| 协议 | ROS# / rosbridge v2.0 (WebSocket + JSON) |
| 默认地址 | `ws://127.0.0.1:9090` |
| 节点名称 | `unity_node` |
| ROS2 端依赖 | `rosbridge_suite` |

---

## Service 接口（Unity Client → ROS2 Server）

### 1. `joint_plan` — 关节空间规划

| 项目 | 值 |
|------|-----|
| **服务类型** | `fpt_interface/JointPlan` |
| **服务名称** | `/joint_plan` |
| **功能** | 输入关节角度，返回末端执行器位姿（FK） |

**Request (`fpt_interface/JointPlanRequest`)**
```
sensor_msgs/JointState request
```
- `request`: 标准 JointState 消息，包含关节名称和位置（弧度）

**Response (`fpt_interface/JointPlanResponse`)**
```
geometry_msgs/PoseStamped response
```
- `response`: 标准 PoseStamped 消息，末端执行器位姿

**ROS2 端实现要点**：
- 接收 JointState，执行正运动学（FK）计算
- 返回末端执行器的 PoseStamped
- 关节角度单位为**弧度**（ROS2 标准）

---

### 2. `ee_plan` — 笛卡尔空间规划

| 项目 | 值 |
|------|-----|
| **服务类型** | `fpt_interface/EEPlan` |
| **服务名称** | `/ee_plan` |
| **功能** | 输入末端位姿，返回关节角度（IK） |

**Request (`fpt_interface/EEPlanRequest`)**
```
geometry_msgs/PoseStamped request
```
- `request`: 目标末端位姿

**Response (`fpt_interface/EEPlanResponse`)**
```
sensor_msgs/JointState response
```
- `response`: 逆运动学求解得到的关节角度

**ROS2 端实现要点**：
- 接收 PoseStamped，执行逆运动学（IK）计算
- 返回 JointState（关节名称 + 位置弧度）
- 若 IK 无解，返回空 JointState 或通过 topic 发送 failed 状态

---

### 3. `execute` — 执行规划

| 项目 | 值 |
|------|-----|
| **服务类型** | `fpt_interface/Execute` |
| **服务名称** | `/execute` |
| **功能** | 确认执行当前已规划的轨迹 |

**Request (`fpt_interface/ExecuteRequest`)**
```
bool execute
```
- `execute`: 通常为 `true`

**Response (`fpt_interface/ExecuteResponse`)**
```
bool success
string message
```
- `success`: 执行是否成功启动
- `message`: 状态描述信息

**ROS2 端实现要点**：
- 收到请求后开始执行之前规划的轨迹
- 通过 `/plan_status` topic 持续反馈执行进度

---

## Topic 接口（ROS2 → Unity，Unity 订阅）

### 1. `/joint_states` — 实时关节状态

| 项目 | 值 |
|------|-----|
| **消息类型** | `sensor_msgs/JointState` |
| **方向** | ROS2 → Unity |
| **频率** | 实时（建议 ≥30Hz） |

**用途**：
- 驱动 Unity 中的 3D 模型关节动画
- 更新 UI 中的关节角度显示
- 更新设备状态为 `Operational`

**字段要求**：
- `name[]`: 关节名称（与 URDF 一致，如 `joint1`~`joint6`, `plate_joint`）
- `position[]`: 关节位置（弧度）
- `velocity[]`（可选）: 关节速度
- `effort[]`（可选）: 关节力矩

---

### 2. `/ee_pose` — 实时末端位姿

| 项目 | 值 |
|------|-----|
| **消息类型** | `geometry_msgs/PoseStamped` |
| **方向** | ROS2 → Unity |
| **频率** | 实时（建议 ≥30Hz） |

**用途**：
- 更新 UI 中的末端位姿显示
- 用于实时可视化

---

### 3. `/plan_status` — 规划/执行状态

| 项目 | 值 |
|------|-----|
| **消息类型** | `fpt_interface/PlanStatus` |
| **方向** | ROS2 → Unity |
| **频率** | 事件触发 |

**自定义消息定义 (`fpt_interface/PlanStatus`)**：
```
string status
string detail
```

**status 枚举值**：

| status | 含义 | Unity 响应 |
|--------|------|-----------|
| `planning` | 正在规划中 | 显示 loading 状态 |
| `success` | 规划完成 | 允许执行（笛卡尔模式下设 PlanReady=true） |
| `failed` | 规划失败 | 显示错误信息，重置状态 |
| `executed` | 执行完成 | 重置 PlanReady |

---

## ROS2 端 Service 定义文件参考

### srv 文件

```
# fpt_interface/srv/JointPlan.srv
sensor_msgs/JointState request
---
geometry_msgs/PoseStamped response
```

```
# fpt_interface/srv/EEPlan.srv
geometry_msgs/PoseStamped request
---
sensor_msgs/JointState response
```

```
# fpt_interface/srv/Execute.srv
bool execute
---
bool success
string message
```

### msg 文件

```
# fpt_interface/msg/PlanStatus.msg
string status
string detail
```

---

## Unity 侧架构

| 组件 | 文件路径 | 职责 |
|------|---------|------|
| `Ros2Node` | `Communication/Runtime/Node/Ros2Node.cs` | ROS# 连接管理、Topic 订阅/发布、Service 调用 |
| `JointTrajectoryMapper` | `Communication/Runtime/Bridge/JointTrajectoryMapper.cs` | ROS2 消息 ↔ FPT 类型转换 |
| `InputTerminal` | `Business/Runtime/InputTerminal.cs` | 数据同步中枢，FK/IK 请求 |
| `RobotArmDriver` | `Business/Runtime/Drivers/RobotArmDriver.cs` | 设备驱动，订阅 /joint_states 和 /ee_pose |
| `NullToDefaultConverter` | `Packages/com.siemens.ros-sharp/.../NullToDefaultConverter.cs` | 处理 null 数组值 |

---

## ROS2 端需要实现的节点示例

```python
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import JointState
from geometry_msgs.msg import PoseStamped
from fpt_interface.srv import JointPlan, EEPlan, Execute
from fpt_interface.msg import PlanStatus

class FPTBridgeNode(Node):
    def __init__(self):
        super().__init__('fpt_bridge')

        # Service Servers
        self.joint_plan_srv = self.create_service(JointPlan, 'joint_plan', self.handle_joint_plan)
        self.ee_plan_srv = self.create_service(EEPlan, 'ee_plan', self.handle_ee_plan)
        self.execute_srv = self.create_service(Execute, 'execute', self.handle_execute)

        # Publishers
        self.joint_states_pub = self.create_publisher(JointState, '/joint_states', 10)
        self.ee_pose_pub = self.create_publisher(PoseStamped, '/ee_pose', 10)
        self.plan_status_pub = self.create_publisher(PlanStatus, '/plan_status', 10)

    def handle_joint_plan(self, request, response):
        """FK: 关节角 → 末端位姿"""
        # TODO: 实现 FK 计算
        response.response = PoseStamped()
        return response

    def handle_ee_plan(self, request, response):
        """IK: 末端位姿 → 关节角"""
        # TODO: 实现 IK 计算
        response.response = JointState()
        return response

    def handle_execute(self, request, response):
        """执行已规划的轨迹"""
        # TODO: 实现轨迹执行
        response.success = True
        response.message = "Execution started"
        return response
```

---

## 已废弃/删除的接口

| 原接口 | 状态 | 替代方案 |
|--------|------|---------|
| ROS-TCP-Connector (TCP) | ❌ 已删除 | ROS# / rosbridge (WebSocket) |
| `ros_tcp_endpoint` | ❌ 已删除 | `rosbridge_suite` |
| `/joint_commands` (Topic) | ❌ 已删除 | `joint_plan` Service |
| `/compute_fk` (Topic) | ❌ 已删除 | `joint_plan` Service |
| `/compute_ik` (Topic) | ❌ 已删除 | `ee_plan` Service |
| `/ee_pose_command` (Topic) | ❌ 已删除 | `ee_plan` Service |
| `/execute` (Topic) | ❌ 已删除 | `execute` Service |
| `/gripper_command` (Topic) | ❌ 已删除 | 无物理夹爪，不需要 |
| 串口通信 (SerialTransport) | ❌ 已删除 | 全部走 ROS2 |
