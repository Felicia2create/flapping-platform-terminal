# ROS2 端部署指南

> Unity 侧的 ROS2 接口定义见 `ROS2_INTERFACE.md`
> 本文档说明 ROS2 端需要做什么来配合 Unity 的 Ros2Node 通信

---

## 整体结构

Unity 通过 **ROS# (ros-sharp)** 连接 ROS2 的 **rosbridge_server**（WebSocket + JSON）。
ROS2 端需要：
1. `rosbridge_suite` — 提供 WebSocket 接口
2. `fpt_interface` — 自定义 msg/srv 定义
3. 你的业务节点 — 实现具体的 FK/IK/执行逻辑

```
your_ros2_ws/src/
├── fpt_interface/                    ← 纯接口包（msg/srv 定义）
│   ├── CMakeLists.txt
│   ├── package.xml
│   ├── msg/
│   │   └── PlanStatus.msg
│   └── srv/
│       ├── JointPlan.srv
│       ├── EEPlan.srv
│       └── Execute.srv
│
└── flapping_platform_unity_trans/    ← 主包（bridge 节点）
    ├── CMakeLists.txt
    ├── package.xml
    ├── config/
    │   └── unity_topics.yaml
    ├── src/
    │   └── unity_bridge.cpp
    └── launch/
        ├── unity_node.launch.py
        ├── unity_mock.launch.py
        └── unity_real.launch.py
```

**依赖关系**：`flapping_platform_unity_trans` 依赖 `fpt_interface`

---

## 第一步：创建 fpt_interface 包

### 目录结构

```
fpt_interface/
├── CMakeLists.txt
├── package.xml
├── msg/
│   └── PlanStatus.msg
└── srv/
    ├── JointPlan.srv
    ├── EEPlan.srv
    └── Execute.srv
```

### CMakeLists.txt

```cmake
cmake_minimum_required(VERSION 3.8)
project(fpt_interface)

if(CMAKE_COMPILER_IS_GNUCXX OR CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  add_compile_options(-Wall -Wextra -Wpedantic)
endif()

find_package(ament_cmake REQUIRED)
find_package(rosidl_default_generators REQUIRED)
find_package(std_msgs REQUIRED)
find_package(sensor_msgs REQUIRED)
find_package(geometry_msgs REQUIRED)

rosidl_generate_interfaces(${PROJECT_NAME}
  "msg/PlanStatus.msg"
  "srv/JointPlan.srv"
  "srv/EEPlan.srv"
  "srv/Execute.srv"
  DEPENDENCIES std_msgs sensor_msgs geometry_msgs
)

ament_package()
```

### package.xml

```xml
<?xml version="1.0"?>
<?xml-model href="http://download.ros.org/schema/package_format3.xsd"
            schematypens="http://www.w3.org/2001/XMLSchema"?>
<package format="3">
  <name>fpt_interface</name>
  <version>0.1.0</version>
  <description>FPT platform ROS2 interface definitions (msg/srv)</description>
  <maintainer email="dev@example.com">developer</maintainer>
  <license>MIT</license>

  <buildtool_depend>ament_cmake</buildtool_depend>
  <buildtool_depend>rosidl_default_generators</buildtool_depend>

  <depend>std_msgs</depend>
  <depend>sensor_msgs</depend>
  <depend>geometry_msgs</depend>

  <exec_depend>rosidl_default_runtime</exec_depend>

  <member_of_group>rosidl_interface_packages</member_of_group>

  <export>
    <build_type>ament_cmake</build_type>
  </export>
</package>
```

### msg/srv 文件

```
# msg/PlanStatus.msg
string status
string detail
```

```
# srv/JointPlan.srv
sensor_msgs/JointState request
---
geometry_msgs/PoseStamped response
```

```
# srv/EEPlan.srv
geometry_msgs/PoseStamped request
---
sensor_msgs/JointState response
```

```
# srv/Execute.srv
bool execute
---
bool success
string message
```

---

## 第二步：修改主包依赖

`flapping_platform_unity_trans/package.xml` 中添加：

```xml
<depend>fpt_interface</depend>
```

`flapping_platform_unity_trans/CMakeLists.txt` 中添加：

```cmake
find_package(fpt_interface REQUIRED)
```

如果你的 `unity_bridge.cpp` 中有 `#include "fpt_interface/srv/joint_plan.hpp"` 这样的头文件引用，
还需要在 `ament_target_dependencies` 中加上 `fpt_interface`：

```cmake
ament_target_dependencies(unity_bridge
  rclcpp
  fpt_interface    # ← 加上这行
  # ... 其他依赖
)
```

---

## 第三步：编译与验证

```bash
cd ~/your_ros2_ws

# 编译接口包（必须先编译，因为主包依赖它）
colcon build --packages-select fpt_interface

# source 编译结果
source install/setup.bash

# 验证 Python 能导入
python3 -c "from fpt_interface.srv import JointPlan, EEPlan, Execute; print('OK')"
python3 -c "from fpt_interface.msg import PlanStatus; print('OK')"

# 编译主包
colcon build --packages-select flapping_platform_unity_trans

# 全部 source
source install/setup.bash
```

---

## 第四步：启动 rosbridge

```bash
# 终端 1：启动 rosbridge_server（WebSocket 默认端口 9090）
ros2 launch rosbridge_server rosbridge_websocket_launch.xml

# 终端 2：验证 service 已注册（启动 Unity 连接后执行）
ros2 service list
# 应该看到：
# /joint_plan
# /ee_plan
# /execute

# 终端 3：手动测试 service（可选）
ros2 service call /joint_plan fpt_interface/JointPlan "{request: {position: [0,0,0,0,0,0]}}"
```

---

## 第五步：启动 Unity

Unity 的 `Ros2Node` 会自动连接 `ws://127.0.0.1:9090`。
连接成功后，rosbridge 日志应显示：

```
[INFO] Client connected. 1 clients total.
[INFO] [RosBridgeClient]: Subscribing to /joint_states
[INFO] [RosBridgeClient]: Advertising service /joint_plan
[INFO] [RosBridgeClient]: Advertising service /ee_plan
[INFO] [RosBridgeClient]: Advertising service /execute
```

---

## 常见问题

### `No module named 'fpt_interface'`
→ `fpt_interface` 没有编译或没有 source
```bash
colcon build --packages-select fpt_interface
source install/setup.bash
```

### `Unknown service class 'fpt_interface/JointPlan'`
→ endpoint 找不到 srv 类，检查：
1. `python3 -c "from fpt_interface.srv import JointPlan"` 是否成功
2. 确保启动 endpoint 的终端执行了 `source install/setup.bash`

### `list index out of range`
→ 某个消息名为空字符串。检查 Unity 侧是否有注册时传了空的 topic 或 message name

### 连接被拒绝
→ rosbridge 没启动或 IP/端口不匹配。确保 rosbridge 在 Unity 之前启动

### WebSocket 断开重连
→ `Ros2Node` 内置自动重连机制（默认 5 秒间隔，无限重试）
→ 检查 rosbridge 日志确认是否有连接断开

### `effort` 字段为 null 导致崩溃
→ Unity 侧已通过 `NullToDefaultConverter` 自动处理 null 值
→ 无需 ROS2 端特殊处理

---

## 端到端数据流

```
Unity (C#)                              ROS2 (rosbridge)                    ROS2 (你的节点)
──────────                              ──────────────────                  ────────────────
RosSocket.Subscribe("/joint_states")
  → WebSocket: {"op":"subscribe",       → 注册订阅
                "topic":"/joint_states"}

RosSocket.CallService("joint_plan", req)
  → WebSocket: {"op":"call_service",    → 查找 service
                "type":"fpt_interface/JointPlan",
                "service":"/joint_plan",
                "args":{...}}
                                          → rosbridge 调用 ROS2 service
                                                                                          → 处理请求
                                                                                          → 返回响应
  ← WebSocket: {"op":"service_response", ←
                "service":"/joint_plan",
                "values":{...}}
  → await 恢复，返回结果

/joint_states 发布时：
  ← WebSocket: {"op":"publish",          ← ROS2 topic 发布
                "topic":"/joint_states",
                "msg":{...}}
  → RosSocket 回调 → 主线程队列 → UI 更新
```
