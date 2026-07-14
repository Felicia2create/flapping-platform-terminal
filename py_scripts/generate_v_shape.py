#!/usr/bin/env python3
"""
generate_formations.py
====================
使用 ikpy 库，从 flapping_platform.urdf 加载机械臂运动学链，
为三个机械臂求解 V-Shape 和 Y-Shape 阵型的逆运动学 (IK)，
生成包含多关键帧的 formation_trajectory.json。

用法：
    uv run python scripts/generate_formations.py
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
V_SHAPE_TARGETS = {
    1: np.array([0.50, 0.00, 0.60]),  # 领头
    2: np.array([-0.25, 0.60, 0.35]),  # 左后翼
    3: np.array([-0.25, -0.60, 0.35]),  # 右后翼
}

# 2. 丫字形 (Y-Shape): 领头鸟在中心高点，双翼向斜后方高举展开
Y_SHAPE_TARGETS = {
    1: np.array([0.10, 0.00, 0.70]),  # 中心高点
    2: np.array([-0.40, 0.50, 0.55]),  # 左后高位展开
    3: np.array([-0.40, -0.50, 0.55]),  # 右后高位展开
}

# ── 初始种子姿态 (Seed State) ──
# 避免传入全零角度，提供一个“半展翅”初始姿态，引导 IK 求解器得出对称、一致的肘部姿态。
# 包含 7 个元素 (对应 ikpy 内部的 origin link + 6 个实际关节)
SEED_ANGLES = [0.0, 0.0, math.pi / 4, -math.pi / 4, 0.0, math.pi / 2, 0.0]

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
    T_base_world = get_arm_base_transform(arm_index)
    T_world_base = np.linalg.inv(T_base_world)
    target_pose_world = np.eye(4)
    target_pose_world[:3, 3] = target_world
    return T_world_base @ target_pose_world


# ═══════════════════════════════════════════════════════════════
# IK 求解器封装
# ═══════════════════════════════════════════════════════════════


def solve_formation_ik(chain: Chain, shape_name: str, targets: dict) -> dict:
    """为指定的阵型目标求解三个臂的 IK"""
    print(f"\n[{shape_name}] 开始求解...")
    formation_data = {}

    for arm_idx in [1, 2, 3]:
        target_world = targets[arm_idx]
        target_pose_base = world_to_arm_base(target_world, arm_idx)
        target_pos = target_pose_base[:3, 3]

        try:
            # 传入 SEED_ANGLES 强制姿态偏好
            joint_angles_rad = chain.inverse_kinematics(
                target_pos, initial_position=SEED_ANGLES
            )

            # 截取 6 个有效关节
            if len(joint_angles_rad) > 6:
                joint_angles_rad = joint_angles_rad[1:7]

            joint_angles_rad = list(joint_angles_rad[:6])

            # 补齐不足 6 个的情况（防错）
            while len(joint_angles_rad) < 6:
                joint_angles_rad.append(0.0)

            formation_data[str(arm_idx)] = {
                "positions_rad": joint_angles_rad,
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
