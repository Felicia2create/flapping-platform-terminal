#!/usr/bin/env python3
"""
generate_formations.py
====================
使用 ikpy 库，从 flapping_platform.urdf 加载机械臂运动学链，
为三个机械臂求解 V-Shape 和 Y-Shape 阵型的逆运动学 (IK)，
生成包含多关键帧的 formation_trajectory.json。

用法：
    uv run python py_scripts/generate_shape.py

把 "py_scripts\Scheduled trajectory\formation_trajectory.json" 复制到
"Assets\Resources\formation_trajectory.json" 路径下
"""

import json
import math
from pathlib import Path

import numpy as np

# 尝试导入 ikpy
try:
    from ikpy.chain import Chain
except ImportError:
    print("❌ 请先安装 ikpy：uv add ikpy")
    exit(1)

# ═══════════════════════════════════════════════════════════════
# 配置参数
# ═══════════════════════════════════════════════════════════════

PROJECT_ROOT = Path(__file__).resolve().parent.parent  # 当前绝对路径的父目录的父目录
URDF_PATH = (
    PROJECT_ROOT
    / "Assets/FPT/Visualization/Runtime/flapping_platform_prefabs/flapping_platform.urdf"
)
# OUTPUT_PATH = PROJECT_ROOT / "formation_trajectory.json"
OUTPUT_PATH = (
    PROJECT_ROOT / "py_scripts" / "Scheduled trajectory" / "formation_trajectory.json"
)

# ── 底座安装参数 ──
PLATE_RADIUS = 1.21704  # 转台半径
BASE_Z = 0.095  # 底座高度

ARM_BASE_ANGLES_RAD = {
    1: math.pi,  # Arm1: 180°
    2: 5 * math.pi / 3,  # Arm2: 300°
    3: math.pi / 3,  # Arm3: 60°
}

# ── 阵型目标坐标 (世界坐标系，Z轴朝上，X轴朝前) ──

# 1. 人字形 (V-Shape): 领头鸟在前，双翼在后侧展开
#    Z 轴抬升至 ≥0.80，远离底盘，防止穿模
V_SHAPE_TARGETS = {
    1: np.array([0.50, 0.00, 0.85]),  # 领头
    2: np.array([-0.25, 0.60, 0.80]),  # 左后翼
    3: np.array([-0.25, -0.60, 0.80]),  # 右后翼
}

# 2. 丫字形 (Y-Shape): 领头鸟在中心高点，双翼向斜后方高举展开
#    Z 轴抬升至 ≥0.85，远离底盘，防止穿模
Y_SHAPE_TARGETS = {
    1: np.array([0.10, 0.00, 0.90]),  # 中心高点
    2: np.array([-0.40, 0.50, 0.85]),  # 左后高位展开
    3: np.array([-0.40, -0.50, 0.85]),  # 右后高位展开
}

# ── 初始种子姿态 (Seed State) ──
# 避免传入全零角度，提供一个“半展翅”初始姿态，引导 IK 求解器得出对称、一致的肘部姿态。
# 包含 7 个元素 (对应 ikpy 内部的 origin link + 6 个实际关节)
# 注意：种子值必须在 JOINT_SAFETY_BOUNDS 范围内，否则 IK 初始化失败
#   links[2] Joint2 ∈ [-2.5, -0.3] → 选 -1.5
#   links[3] Joint3 ∈ [0.0, 2.5]   → 选 1.0
SEED_ANGLES = [0.0, 0.0, -1.5, 1.0, 0.0, math.pi / 2, 0.0]

# ── 关节安全限位 (Anti-Clipping) ──
# 在 URDF 原始限位基础上进一步收紧，防止机械臂大臂垂向底盘
# 索引对应 chain.links 中的位置：links[2]=Joint2, links[3]=Joint3
JOINT_SAFETY_BOUNDS = {
    2: (-2.5, -0.3),  # Joint2: 限制在负角度范围，保持大臂上扬
    3: (0.0, 2.5),  # Joint3: 禁止负角度，防止肘部向下穿透底盘
}

# ═══════════════════════════════════════════════════════════════
# ROS → Unity 坐标系转换
# ═══════════════════════════════════════════════════════════════
#
# 关节角度映射规则：
#   - ROS 右手系 (Z-up)  →  Unity 左手系 (Y-up)
#   - URDF 导入器已处理关节轴的坐标系转换
#   - ArticulationBody.xDrive 的旋转方向与 URDF 定义一致
#   - 因此关节角度值无需翻转符号，直接透传即可
#
# 本函数保留为显式转换入口，便于将来调试和调整。


def convert_angles_ros_to_unity(angles_rad: list) -> list:
    """
    将 ROS 右手系下的关节弧度转换为 Unity 左手系兼容格式。

    当前实现：恒等映射（透传）。
    原因：URDF 导入器已处理坐标系转换，
          ArticulationBody.xDrive 的旋转方向与 URDF 定义一致。

    参数:
        angles_rad: 6 元素列表，ROS 坐标系下的关节弧度
    返回:
        6 元素列表，Unity 兼容的关节弧度
    """
    # 恒等映射：无需翻转符号
    # 如需调试，可在此处对特定关节取反，例如：
    #   angles_rad[5] = -angles_rad[5]  # Joint6 轴为 -Z，可能需要翻转
    return list(angles_rad)


# ═══════════════════════════════════════════════════════════════
# 数学工具函数
# ═══════════════════════════════════════════════════════════════


def rotation_matrix_z(theta: float) -> np.ndarray:
    c, s = math.cos(theta), math.sin(theta)
    return np.array(
        [
            [c, -s, 0, 0],
            [s, c, 0, 0],
            [0, 0, 1, 0],
            [0, 0, 0, 1],
        ]
    )


def translation_matrix(x: float, y: float, z: float) -> np.ndarray:
    return np.array(
        [
            [1, 0, 0, x],
            [0, 1, 0, y],
            [0, 0, 1, z],
            [0, 0, 0, 1],
        ]
    )


def get_arm_base_transform(arm_index: int) -> np.ndarray:
    theta = ARM_BASE_ANGLES_RAD[arm_index]
    x = PLATE_RADIUS * math.cos(theta - math.pi)
    y = PLATE_RADIUS * math.sin(theta - math.pi)
    z = BASE_Z
    T_trans = translation_matrix(x, y, z)
    T_rot = rotation_matrix_z(theta)
    return T_trans @ T_rot


def world_to_arm_base(target_world: np.ndarray, arm_index: int) -> np.ndarray:
    """
    将世界坐标系中的目标位姿转换到臂底座坐标系。

    返回 4x4 齐次变换矩阵：
      - 位置列 (:,3) = 目标世界坐标
      - 旋转矩阵 (:3,:3) = 单位阵（X前 Y左 Z上，即末端水平、朝前）
    """
    T_base_world = get_arm_base_transform(arm_index)
    T_world_base = np.linalg.inv(T_base_world)

    # 目标在世界坐标系中的位姿：
    #   方向：X轴=前, Y轴=左, Z轴=上（即末端水平、指向 +X）
    #   位置：target_world
    target_pose_world = np.eye(4)
    target_pose_world[:3, 3] = target_world

    return T_world_base @ target_pose_world


# ═══════════════════════════════════════════════════════════════
# IK 求解器封装
# ═══════════════════════════════════════════════════════════════


def solve_formation_ik(chain: Chain, shape_name: str, targets: dict) -> dict:
    """为指定的阵型目标求解三个臂的 IK（含姿态约束）"""
    print(f"\n[{shape_name}] 开始求解...")
    formation_data = {}

    for arm_idx in [1, 2, 3]:
        target_world = targets[arm_idx]
        # 完整 4x4 目标位姿（位置 + 姿态），已转换到底座坐标系
        target_pose_base = world_to_arm_base(target_world, arm_idx)

        try:
            # ✅ 位置和姿态分开传参（ikpy API 要求）
            #    target_position:  shape (3,)  → 目标 XYZ
            #    target_orientation: shape (3,3) → 目标旋转矩阵（单位阵 = 水平朝前）
            #    orientation_mode="all": 约束全部三个旋转轴
            joint_angles_rad = chain.inverse_kinematics(
                target_position=target_pose_base[:3, 3],
                target_orientation=target_pose_base[:3, :3],
                orientation_mode="all",
                initial_position=SEED_ANGLES,
            )

            # 截取 6 个有效关节
            if len(joint_angles_rad) > 6:
                joint_angles_rad = joint_angles_rad[1:7]

            joint_angles_rad = list(joint_angles_rad[:6])

            # 补齐不足 6 个的情况（防错）
            while len(joint_angles_rad) < 6:
                joint_angles_rad.append(0.0)

            formation_data[str(arm_idx)] = {
                "positions_rad": convert_angles_ros_to_unity(joint_angles_rad),
                "target_world": target_world.tolist(),
            }
            print(f"  ✅ Arm {arm_idx} 成功 | 目标 Z={target_world[2]:.2f}")

        except Exception as e:
            print(f"  ❌ Arm {arm_idx} 失败：{e}")
            return None

    return formation_data


# ═══════════════════════════════════════════════════════════════
# 主逻辑
# ═══════════════════════════════════════════════════════════════


def main():
    print("=" * 60)
    print("  多阵型轨迹生成器 (V-Shape & Y-Shape)")
    print("=" * 60)

    if not URDF_PATH.exists():
        print(f"❌ URDF 文件不存在：{URDF_PATH}")
        return

    print(f"\n📂 加载 URDF：{URDF_PATH.name}")
    try:
        chain = Chain.from_urdf_file(
            str(URDF_PATH),
            base_elements=["arm1_base_link"],
        )
    except Exception as e:
        print(f"❌ 加载 URDF 失败：{e}")
        return

    # ── 收紧关节安全限位 (Anti-Clipping) ──
    for joint_idx, (lb, ub) in JOINT_SAFETY_BOUNDS.items():
        if joint_idx < len(chain.links):
            old_bounds = chain.links[joint_idx].bounds
            chain.links[joint_idx].bounds = (lb, ub)
            print(f"  🔒 Joint {joint_idx} 限位: {old_bounds} → ({lb}, {ub})")

    # 准备要生成的阵型序列 (定义时间戳和对应的数据)
    formations_to_generate = [
        {"time": 0.0, "segment": "VShape_pose", "targets": V_SHAPE_TARGETS},
        {"time": 2.0, "segment": "YShape_pose", "targets": Y_SHAPE_TARGETS},
    ]

    trajectory_points = []

    for fmt in formations_to_generate:
        arm_data = solve_formation_ik(chain, fmt["segment"], fmt["targets"])

        if arm_data is None:
            print(f"\n⚠️ {fmt['segment']} 求解失败，终止生成。")
            return

        trajectory_points.append(
            {"t": fmt["time"], "segment": fmt["segment"], "arms": arm_data}
        )

    # 生成 JSON
    output_data = {
        "schema_version": "1.1",
        "formation_type": "MultiShapeTrajectory",
        "description": "包含 V-Shape(t=0s) 和 Y-Shape(t=2s) 的阵型过渡轨迹",
        "reference_frame": "platform_plate_Link",
        "joint_names": ["joint1", "joint2", "joint3", "joint4", "joint5", "joint6"],
        "points": trajectory_points,
    }

    with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
        json.dump(output_data, f, indent=2, ensure_ascii=False)

    print(f"\n{'=' * 60}")
    print(f"✅ formation_trajectory.json 已成功生成：\n   {OUTPUT_PATH}")
    print(f"{'=' * 60}")


if __name__ == "__main__":
    main()
